using Core.Hex;                 // ��ԭ�е� HexCoords/DistanceTo
using Game.Common;             // ��ԭ���� SelectionManager ���������ռ䣨�粻ͬ��ģ�
using Game.Grid;               // IHexGridProvider, GridRecipe
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Units
{
    [DisallowMultipleComponent]
    public class Unit : MonoBehaviour
    {
        [Header("Refs")]
        public MonoBehaviour gridComponent;   // Inspector ���κ�ʵ�� IHexGridProvider �����
        IHexGridProvider grid;

        [Header("Motion")]
        [Tooltip("�ƶ�һ����������")]
        public float secondsPerTile = 0.2f;
        [Tooltip("��λ�ڶ���֮�϶���̧�ߵĸ߶�")]
        public float unitYOffset = 0.02f;
        [Tooltip("�ƶ�ʱ����Ŀ��")]
        public bool faceMovement = true;
        [Tooltip("��δ��ʽ��ʼ�������� Start �Զ�Ѱ�� Grid ��ע�ᵽ SelectionManager��")]
        public bool autoInitializeIfMissing = true;

        public HexCoords Coords { get; private set; }
        public bool IsMoving => _moveRoutine != null;

        // �ƶ�����¼���from -> to
        public System.Action<Unit, HexCoords, HexCoords> OnMoveFinished;

        // ���� ���� ���� //
        readonly Dictionary<HexCoords, Transform> _tileMap = new();
        uint _lastGridVersion;
        Coroutine _moveRoutine;
        bool _hasValidCoords;

        void Awake()
        {
            grid = gridComponent as IHexGridProvider;
        }

        void Start()
        {
            if (!autoInitializeIfMissing) return;

            // ��δ��ʼ����grid Ϊ�գ��������Զ�Ѱ���κ�ʵ�� IHexGridProvider �Ķ���
            if (grid == null)
                grid = FindFirstGridProviderInScene();

            if (grid == null) return; // ������û������Ͳ�����

            // ���� tile ӳ��
            if (_tileMap.Count == 0) RebuildTileMap();

            // �ý�����������ʵ�񣻳ɹ��� WarpTo
            if (TryPickTileUnderSelf(out var c))
            {
                WarpTo(c); // ���� Coords + _hasValidCoords = true
            }
            else
            {
                // û����Ƭ������������ Coords �ж��Ƿ���Ч�����ⱻ���� (0,0)
                _hasValidCoords = _tileMap.ContainsKey(Coords);
            }

            // ע�ᵽ SelectionManager����δע�ᣩ
            var sel = FindFirstObjectByType<SelectionManager>(FindObjectsInactive.Exclude);
            if (sel != null)
                sel.RegisterUnit(this);
        }

        IHexGridProvider FindFirstGridProviderInScene()
        {
            var monos = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < monos.Length; i++)
            {
                if (monos[i] is IHexGridProvider p) return p;
            }
            return null;
        }

        static readonly RaycastHit[] _hitsTmp = new RaycastHit[4];

        bool TryPickTileUnderSelf(out HexCoords coords)
        {
            coords = default;

            // ��ͷ����΢��һ�����´�һ�����ߣ���������� TileTag ����Ƭ����
            var ray = new Ray(transform.position + Vector3.up * 2f, Vector3.down);
            int n = Physics.RaycastNonAlloc(ray, _hitsTmp, 6f, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var h = _hitsTmp[i];
                if (!h.collider) continue;
                var tag = h.collider.GetComponent<TileTag>() ?? h.collider.GetComponentInParent<TileTag>();
                if (tag != null)
                {
                    coords = tag.Coords;
                    return true;
                }
            }
            return false;
        }

        void LateUpdate()
        {
            if (grid != null && _lastGridVersion != grid.Version)
            {
                _lastGridVersion = grid.Version;
                RebuildTileMap();

                // ������ˣ�����ǰλ������Ч������뵽����λ��
                if (_hasValidCoords && TryGetTileTopWorld(Coords, out var top))
                    transform.position = top;
            }
        }

        // �ⲿ��ʼ������ Spawner/Controller ���ã�
        public void Initialize(IHexGridProvider g, HexCoords start)
        {
            grid = g;
            gridComponent = g as MonoBehaviour;
            _lastGridVersion = 0;
            RebuildTileMap();
            WarpTo(start);
        }

        public void WarpTo(HexCoords c)
        {
            Coords = c;
            if (TryGetTileTopWorld(c, out var topPos))
            {
                transform.position = topPos;
                _hasValidCoords = true;
            }
        }

        // �������ڸ��ƶ���·���滮�Ժ��ټӣ�A*��
        public bool TryMoveTo(HexCoords target)
        {
            if (IsMoving || grid == null) return false;
            if (Coords.DistanceTo(target) != 1) return false;              // �ڸ�����
            if (!TryGetTileTopWorld(target, out var dst)) return false;    // Ŀ�겻����

            var src = transform.position;
            _moveRoutine = StartCoroutine(MoveRoutine(src, dst, target));
            return true;
        }

        IEnumerator MoveRoutine(Vector3 src, Vector3 dst, HexCoords target)
        {
            float t = 0f;
            float dur = Mathf.Max(0.01f, secondsPerTile);
            if (faceMovement)
            {
                Vector3 dir = (dst - src);
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.0001f)
                    transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
            }

            while (t < 1f)
            {
                t += Time.deltaTime / dur;
                transform.position = Vector3.Lerp(src, dst, Mathf.Clamp01(t));
                yield return null;
            }

            var from = Coords;
            Coords = target;
            _moveRoutine = null;
            OnMoveFinished?.Invoke(this, from, target);
        }

        // ���� ���ߣ�������������/����߶� ���� //

        void RebuildTileMap()
        {
            _tileMap.Clear();
            if (grid == null) return;

            foreach (var t in grid.EnumerateTiles())
            {
                if (t != null && !_tileMap.ContainsKey(t.Coords))
                    _tileMap.Add(t.Coords, t.transform);
            }
        }

        bool TryGetTileTopWorld(HexCoords c, out Vector3 pos)
        {
            pos = default;
            if (!_tileMap.TryGetValue(c, out var tr) || tr == null) return false;

            float top = (grid != null && grid.recipe != null) ? grid.recipe.thickness * 0.5f : 0f;
            pos = tr.position + new Vector3(0f, top + unitYOffset, 0f);
            return true;
        }
    }
}
