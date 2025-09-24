using Game.Battle;
using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Common
{
    [ExecuteAlways]
    public class CameraFrameOnGrid : MonoBehaviour
    {
        public enum Mode { OrthographicTopDown, OrthoIso, IsoPerspective } // �� ���� OrthoIso

        [Header("OrthoIso (no perspective)")]
        [Range(10, 60)] public float orthoIsoPitch = 30f;  // ����
        [Range(-180, 180)] public float orthoIsoYaw = 35f; // ����
        public static event Action<Mode> OnModeChanged;

        [Header("Targets")]
        public Camera cam;                 // Inspector ���ֶ��ϣ��������Զ���
        public BattleHexGrid grid;

        [Header("Framing")]
        public Mode mode = Mode.OrthographicTopDown;
        [Range(1.0f, 2.0f)] public float margin = 1.12f;
        [Tooltip("��������ߵ㱣�ֵ���С��ֱ����")]
        public float groundClearance = 1.0f;

        [Header("Iso Settings")]
        [Range(20, 75)] public float isoPitch = 55f;
        [Range(-180, 180)] public float isoYaw = 35f;
        [Range(20, 70)] public float fov = 40f;

        [Header("Integration")]
        [Tooltip("�л�ģʽʱͬʱ���������͸��������")]
        public bool syncTransparencySortAxis = true;
        [Tooltip("����ʱ�������ᣨ��Z�Ÿ�ֱ�ۣ�0,0,1��")]
        public Vector3 sortAxisTopDown = new Vector3(0, 0, 1);
        [Tooltip("����͸��ʱ�������ᣨ��Y�ţ�0,1,0��")]
        public Vector3 sortAxisIso = new Vector3(0, 1, 0);

        uint _lastGridVersion;

        void Reset() { EnsureRefs(); ApplyModeSettings(); FrameNow(); }
        void OnEnable() { EnsureRefs(); ApplyModeSettings(); FrameNow(); OnModeChanged?.Invoke(mode); }
#if UNITY_EDITOR
        void OnValidate() { if (!isActiveAndEnabled) return; EnsureRefs(); ApplyModeSettings(); FrameNow(); OnModeChanged?.Invoke(mode); }
#endif

        void LateUpdate()
        {
            EnsureRefs();
            if (grid && grid.Version != _lastGridVersion)
            {
                _lastGridVersion = grid.Version;
                FrameNow();
            }
        }

        // ���� ����API��������ʱ/Editor��ť��������л�ģʽ ����
        public void SetMode(Mode m, bool reframe = true)
        {
            mode = m;
            ApplyModeSettings();
            if (reframe) FrameNow();
            OnModeChanged?.Invoke(mode); // �� ֪ͨ
        }
        public void ApplyModeSettings()
        {
            if (!cam) cam = GetComponent<Camera>() ?? Camera.main;
            if (!cam) return;

            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 2000f;

            if (mode == Mode.OrthographicTopDown)
            {
                cam.orthographic = true;
                cam.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                if (syncTransparencySortAxis)
                {
                    cam.transparencySortMode = TransparencySortMode.CustomAxis;
                    cam.transparencySortAxis = sortAxisTopDown; // ͨ�� (0,0,1)
                }
            }
            else if (mode == Mode.OrthoIso) // �� ����
            {
                cam.orthographic = true;
                cam.transform.rotation = Quaternion.Euler(orthoIsoPitch, orthoIsoYaw, 0f);
                if (syncTransparencySortAxis)
                {
                    cam.transparencySortMode = TransparencySortMode.CustomAxis;
                    cam.transparencySortAxis = sortAxisIso; // ����б���°� Y �Ÿ��ȣ�ͨ�� (0,1,0)
                }
            }
            else // IsoPerspective
            {
                cam.orthographic = false;
                cam.fieldOfView = fov;
                cam.transform.rotation = Quaternion.Euler(isoPitch, isoYaw, 0f);
                if (syncTransparencySortAxis)
                {
                    cam.transparencySortMode = TransparencySortMode.CustomAxis;
                    cam.transparencySortAxis = sortAxisIso; // ͨ�� (0,1,0)
                }
            }
        }


        // ���� �Զ������� ����
        public void EnsureRefs()
        {
            if (!cam) cam = GetComponent<Camera>() ?? Camera.main;

            if (!grid)
            {
#if UNITY_2023_1_OR_NEWER
                grid = Object.FindFirstObjectByType<BattleHexGrid>(FindObjectsInactive.Exclude);
#else
                grid = Object.FindObjectOfType<BattleHexGrid>();
#endif
            }
        }

        // ���� ���ģ����������Χ���ع����λ����ߴ� ����
        public void FrameNow()
        {
            EnsureRefs();
            if (!cam || !grid) return;

            var renderers = grid.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;

            var b = new Bounds(renderers[0].bounds.center, Vector3.zero);
            foreach (var r in renderers) if (r.enabled) b.Encapsulate(r.bounds);

            var c = b.center;
            var ex = b.extents;

            if (mode == Mode.OrthographicTopDown)
            {
                float halfW = b.extents.x;
                float halfH = b.extents.z;
                float size = Mathf.Max(halfH, halfW / Mathf.Max(0.0001f, cam.aspect)) * margin;
                cam.orthographicSize = size;

                float y = b.max.y + groundClearance;
                cam.transform.position = new Vector3(c.x, y, c.z);
            }
            else if (mode == Mode.OrthoIso) // �� ����
            {
                // �� AABB �������� (world X/Y/Z) �İ뾶ͶӰ����� right/up/forward ��
                Vector3 R = cam.transform.right;
                Vector3 U = cam.transform.up;
                Vector3 F = cam.transform.forward;

                // ͶӰ�����Ļ X ����
                float halfW = Mathf.Abs(ex.x * R.x) + Mathf.Abs(ex.y * R.y) + Mathf.Abs(ex.z * R.z);
                // ͶӰ��ߣ���Ļ Y ����
                float halfH = Mathf.Abs(ex.x * U.x) + Mathf.Abs(ex.y * U.y) + Mathf.Abs(ex.z * U.z);
                // ͶӰ�������� forward��������������ڰ�Χ��ǰ����
                float halfD = Mathf.Abs(ex.x * F.x) + Mathf.Abs(ex.y * F.y) + Mathf.Abs(ex.z * F.z);

                float size = Mathf.Max(halfH, halfW / Mathf.Max(0.0001f, cam.aspect)) * margin;
                cam.orthographicSize = size;

                // ������ŵ�����ǰ�������� clearence��
                float dist = halfD * margin + groundClearance;
                cam.transform.position = c - F * dist;
            }
            else // IsoPerspective
            {
                float halfW = b.extents.x;
                float halfH = b.extents.z;
                float radius = Mathf.Sqrt(halfW * halfW + halfH * halfH);

                float halfVert = cam.fieldOfView * 0.5f * Mathf.Deg2Rad;
                float halfHor = Mathf.Atan(Mathf.Tan(halfVert) * cam.aspect);
                float halfMin = Mathf.Min(halfVert, halfHor);
                float dist = (radius * margin) / Mathf.Sin(Mathf.Max(0.001f, halfMin));

                Vector3 fwd = cam.transform.forward;
                Vector3 pos = c - fwd * dist;
                if (pos.y < b.max.y + groundClearance) pos.y = b.max.y + groundClearance;
                cam.transform.position = pos;
            }
        }

    }
}
