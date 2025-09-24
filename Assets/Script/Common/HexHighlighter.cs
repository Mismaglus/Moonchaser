using System.Collections.Generic;
using UnityEngine;
using Core.Hex;

namespace Game.Common
{
    /// <summary>
    /// ���������Hover / Selected / Range������ MPB Ⱦɫ��֧�ֶ���ʣ��Զ�̽����ɫ���ԣ���
    /// ���ȼ���Hover > Selected > Range > None
    /// </summary>
    [DisallowMultipleComponent]
    public class HexHighlighter : MonoBehaviour
    {
        [Header("Refs")]
        public Game.Battle.BattleHexGrid grid;

        [Header("Colors")]
        public Color hoverColor = new Color(0.95f, 0.95f, 0.25f, 1f);
        public Color selectedColor = new Color(0.25f, 0.8f, 1.0f, 1f);
        public Color rangeColor = new Color(0.35f, 0.9f, 0.45f, 1f);
        [Range(0.1f, 4f)] public float hoverIntensity = 1.0f;
        [Range(0.1f, 4f)] public float selectedIntensity = 1.0f;
        [Range(0.1f, 4f)] public float rangeIntensity = 1.0f;

        [Header("Material Compatibility")]
        [Tooltip("��˳������Щ������������һ������������Ⱦɫ")]
        public string[] colorPropertyNames = new[] { "_BaseColor", "_Color", "_Tint" };
        [Tooltip("��ĳ����û�п�����ɫ���ԣ��Ƿ��ӡһ�ξ��棨��һ�Σ�")]
        public bool warnOnceIfNoColorProperty = true;

        // ÿ�񻺴棺Renderer + ���е���ɫ����ID��-1=δ���У�
        struct Slot { public MeshRenderer mr; public int colorPropId; }
        readonly Dictionary<(int q, int r), Slot> _slots = new();

        // ����״̬
        HexCoords? _hover;
        HexCoords? _selected;
        readonly HashSet<HexCoords> _range = new();

        MaterialPropertyBlock _mpb; // �ӳٴ���
        uint _lastGridVersion;
        bool _warned;

        void Awake()
        {
            if (_mpb == null) _mpb = new MaterialPropertyBlock();
        }

        void LateUpdate()
        {
            if (!grid) return;

            // �����ؽ����ؽ����沢��ˢ��ǰ״̬
            if (_lastGridVersion != grid.Version)
            {
                _lastGridVersion = grid.Version;
                RebuildCache();
                ReapplyAll();
            }
        }

        // ���� �ⲿ���� API ����

        public void RebuildCache()
        {
            _slots.Clear();
            if (!grid) return;

            var tags = grid.GetComponentsInChildren<TileTag>(true);
            foreach (var t in tags)
            {
                var mr = t.GetComponent<MeshRenderer>();
                if (!mr) continue;

                int pid = -1;
                var mat = mr.sharedMaterial;
                if (mat)
                {
                    for (int i = 0; i < colorPropertyNames.Length; i++)
                    {
                        string name = colorPropertyNames[i];
                        if (string.IsNullOrEmpty(name)) continue;
                        int id = Shader.PropertyToID(name);
                        if (mat.HasProperty(id)) { pid = id; break; }
                    }
                }

                if (pid == -1 && warnOnceIfNoColorProperty && !_warned)
                {
                    _warned = true;
                    Debug.LogWarning(
                        $"[HexHighlighter] ���� '{(mat ? mat.name : "null")}' δ�ҵ����õ���ɫ���ԡ�" +
                        $" �����˳��� _BaseColor/_Color/_Tint������ colorPropertyNames ������Զ�����������"
                    );
                }

                _slots[(t.Coords.q, t.Coords.r)] = new Slot { mr = mr, colorPropId = pid };
            }
        }

        void revertPaintHelper(HexCoords? coords)
        {
            if (coords.HasValue)
            {
                if (HasAnyState(coords)) Repaint(coords);   // �˻ص� Selected/Range ��
                else ClearPaint(coords);       // û���κ�״̬�� �� ֱ����
            }
        }

        public void SetHover(HexCoords? coords)
        {
            var old = _hover;
            _hover = coords; // ���ύ��״̬

            revertPaintHelper(old);

            Repaint(_hover); // �� hover ˢ��ȥ
        }


        // SetSelected
        public void SetSelected(HexCoords? coords)
        {
            var old = _selected;
            _selected = coords;

            revertPaintHelper(old);

            Repaint(_selected);
        }


        /// <summary>Ӧ��һ�鷶Χ�񣻴� null ��ռ��ϻ���շ�Χ��</summary>
        public void ApplyRange(IEnumerable<HexCoords> coords)
        {
            var old = new HashSet<HexCoords>(_range);

            // ���ύ�¼���
            _range.Clear();
            if (coords != null) foreach (var c in coords) _range.Add(c);

            // �����С�����û�� �� ������Ҫ������˻�����״̬
            foreach (var c in old)
            {
                if (!_range.Contains(c))
                {
                    revertPaintHelper(c);
                }
            }

            // �������뷶Χ�ĸ� �� ������
            foreach (var c in _range)
            {
                if (!old.Contains(c)) Repaint(c.q, c.r);
            }
        }


        public void ClearAll()
        {
            var toClear = new HashSet<HexCoords>();
            if (_hover.HasValue) toClear.Add(_hover.Value);
            if (_selected.HasValue) toClear.Add(_selected.Value);
            foreach (var v in _range) toClear.Add(v);

            _hover = null; _selected = null; _range.Clear(); // ����״̬

            foreach (var c in toClear) ClearPaint(c);        // ����ʽ����Ⱦ
        }

        void Repaint(HexCoords? c) { if (c.HasValue) Repaint(c.Value.q, c.Value.r); }

        void ReapplyAll()
        {
            if (_hover.HasValue) Repaint(_hover.Value.q, _hover.Value.r);
            if (_selected.HasValue) Repaint(_selected.Value.q, _selected.Value.r);
            foreach (var v in _range) Repaint(v.q, v.r);
        }

        void Repaint(int q, int r)
        {
            if (_mpb == null) _mpb = new MaterialPropertyBlock();
            if (!_slots.TryGetValue((q, r), out var slot) || !slot.mr) return;

            // ����ø�ǰӦ��ʾ����ɫ��Hover > Selected > Range��
            bool isHover = _hover.HasValue && _hover.Value.q == q && _hover.Value.r == r;
            bool isSelected = _selected.HasValue && _selected.Value.q == q && _selected.Value.r == r;
            bool inRange = _range.Contains(new HexCoords(q, r));

            Color? outColor = null;
            if (isHover) outColor = hoverColor * hoverIntensity;
            else if (isSelected) outColor = selectedColor * selectedIntensity;
            else if (inRange) outColor = rangeColor * rangeIntensity;

            if (outColor.HasValue)
            {
                _mpb.Clear();
                if (slot.colorPropId != -1)
                {
                    _mpb.SetColor(slot.colorPropId, outColor.Value);
                }
                else
                {
                    // ���ˣ��������ֶ���һ��
                    _mpb.SetColor(Shader.PropertyToID("_BaseColor"), outColor.Value);
                    _mpb.SetColor(Shader.PropertyToID("_Color"), outColor.Value);
                    _mpb.SetColor(Shader.PropertyToID("_Tint"), outColor.Value);
                }
                slot.mr.SetPropertyBlock(_mpb);
            }
            else
            {
                // ���κ�״̬���ָ�Ĭ��
                slot.mr.SetPropertyBlock(null);
            }
        }
        public void ClearPaint(HexCoords? c)
        {
            if (_slots.TryGetValue((c.Value.q, c.Value.r), out var slot) && slot.mr)
                slot.mr.SetPropertyBlock(null);
        }
        bool HasAnyState(HexCoords? c)
        {
            int q = c.Value.q;
            int r = c.Value.r;
            if (_hover is HexCoords h && h.q == q && h.r == r) return true;
            if (_selected is HexCoords s && s.q == q && s.r == r) return true;
            return _range.Contains(new HexCoords(q, r));
        }
    }
}
