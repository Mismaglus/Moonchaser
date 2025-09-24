using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.InputSystem;
using Game.Common; // Ϊ�õ� CameraFrameOnGrid

namespace Game.Battle.Units
{
    [RequireComponent(typeof(BattleUnit))]
    public class UnitVisual2D : MonoBehaviour
    {
        [Header("Refs")]
        public SpriteRenderer spriteRenderer;   // ָ��Visual���������ϵ� SR
        public SortingGroup sortingGroup;       // ָ��Visual���������ϵ� SG����ѡ��

        [Header("Facing / Flip")]
        public bool flipByX = true;             // ����ʱ�����ҷ�ת
        public bool flipUseCameraRight = true;  // �á�����ҷ����ж����ң����������ת��ת��ֱ����

        [Header("Sorting")]
        public bool enableDynamicSort = true;   // ���أ���̬���� sortingOrder
        public int orderScale = 1000;

        [Header("Sync With Camera Mode")]
        public bool followCameraMode = true;    // ���ϣ������ģʽ�л���̬������
        public float isoXRotation = 0f;         // ���ǣ�ֱ����һ�� 0��
        public bool isoBillboardY = true;       // ���ǣ�ˮƽ���򿴰�������
        public float topDownXRotation = -90f;   // ���ӣ���ƽ��-90��
        public bool topDownUseZSort = true;     // ���ӣ��� Z ���������͸����(0,0,1)һ�£�

        Vector3 _lastPos;
        bool _isTopDown = false;

        void Reset()
        {
            if (!spriteRenderer) spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
            if (!sortingGroup) sortingGroup = GetComponentInChildren<SortingGroup>(true);
        }

        void OnEnable()
        {
            Reset();

            // �������ģʽ�仯
            CameraFrameOnGrid.OnModeChanged += HandleCameraModeChanged;

            // ��ʼ��һ�Σ����ű�����������ã�
            var framer = FindFramer();
            if (framer) HandleCameraModeChanged(framer.mode);

            _lastPos = transform.position;
            ApplyFacing(Vector3.right);
            ApplySortOrder();
        }

        void OnDisable()
        {
            CameraFrameOnGrid.OnModeChanged -= HandleCameraModeChanged;
        }

        void Update()
        {
            var pos = transform.position;
            var delta = pos - _lastPos;

            if (delta.sqrMagnitude > 1e-6f)
            {
                ApplyFacing(delta);
                _lastPos = pos;
            }

            if (enableDynamicSort) ApplySortOrder();

            // ����ʱ�ġ�������롱��ֻ����ˮƽ���򣬲��ı�ֱ����̬��
            if (!_isTopDown && isoBillboardY && spriteRenderer)
            {
                var cam = Camera.main;
                if (cam)
                {
                    var fwd = cam.transform.forward;
                    fwd.y = 0f;
                    if (fwd.sqrMagnitude > 1e-6f)
                    {
                        // ��������ת��֤��Զ���������ˮƽ����
                        var yOnly = Quaternion.LookRotation(fwd.normalized, Vector3.up);
                        var e = spriteRenderer.transform.localEulerAngles;
                        // �������� X ��ǣ�isoXRotation����ֻ�滻����Y
                        spriteRenderer.transform.rotation = yOnly * Quaternion.Euler(isoXRotation, 0f, 0f);
                    }
                }
            }
        }

        // ���� ���ģʽ�仯ʱ�л���̬/������� ����
        void HandleCameraModeChanged(CameraFrameOnGrid.Mode m)
        {
            _isTopDown = (m == CameraFrameOnGrid.Mode.OrthographicTopDown);
            if (!followCameraMode || spriteRenderer == null) return;

            var tr = spriteRenderer.transform;

            if (_isTopDown)
            {
                // ���ӣ���ƽ��ͨ������Ҫ����
                var e = tr.localEulerAngles;
                e.x = topDownXRotation; e.y = 0f; e.z = 0f;
                tr.localEulerAngles = e;
            }
            else
            {
                // ���ǣ�ֱ����X=isoXRotation����Y ��������� Update �￴��
                var e = tr.localEulerAngles;
                e.x = isoXRotation; e.z = 0f;
                tr.localEulerAngles = e;
            }

            // ��������л�
            ApplySortOrder();
        }

        // ���� ���ҷ�ת�������ƶ�������������Ļ�ҡ��ж� ����
        void ApplyFacing(Vector3 moveDelta)
        {
            if (!flipByX || spriteRenderer == null) return;
            if (moveDelta.sqrMagnitude < 1e-8f) return;

            float signed = moveDelta.x;

            if (flipUseCameraRight)
            {
                var cam = Camera.main;
                if (cam)
                {
                    var right = cam.transform.right; right.y = 0f;
                    if (right.sqrMagnitude > 1e-6f)
                        signed = Vector3.Dot(moveDelta, right.normalized);
                }
            }

            // ��Ļ���ߣ���ת�����ߣ�����
            spriteRenderer.flipX = (signed < 0f);
        }

        // ���� ���򣺸��ǰ� Y�����ӣ���ƽ���ɰ� Z ����
        void ApplySortOrder()
        {
            if (sortingGroup == null) return;

            float key = transform.position.y;
            if (_isTopDown && topDownUseZSort) key = transform.position.z;

            sortingGroup.sortingOrder = -Mathf.RoundToInt(key * orderScale);
        }

        CameraFrameOnGrid FindFramer()
        {
#if UNITY_2023_1_OR_NEWER
            return Object.FindFirstObjectByType<CameraFrameOnGrid>(FindObjectsInactive.Exclude);
#else
            return Object.FindObjectOfType<CameraFrameOnGrid>();
#endif
        }

        // ���� API������������ɵ�λ��������ͬ��һ��
        public void SyncPoseNow()
        {
            var framer = FindFramer();
            if (framer) HandleCameraModeChanged(framer.mode);
        }
    }
}
