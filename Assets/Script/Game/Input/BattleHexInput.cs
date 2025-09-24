using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;  // ������ϵͳ
using Core.Hex;
using Game.Common;

namespace Game.Battle
{
    /// <summary>��ȡ��꣨Input System������ Hover/Click ӳ�䵽��Ƭ��</summary>
    public class BattleHexInput : MonoBehaviour
    {
        [Header("Refs")]
        public Camera cam;                 // �������Զ�ȡ MainCamera
        public BattleHexGrid grid;         // ������������
        public HexHighlighter highlighter; // ������ĸ�����

        [Header("Options")]
        public bool ignoreWhenPointerOverUI = true;
        public LayerMask raycastMask = ~0; // ��ѡ��ֻ����Ƭ��

        // ����ص����Ժ�������ڱ�Ľű����ģ�
        public System.Action<HexCoords> OnTileClicked;
        public System.Action<HexCoords?> OnHoverChanged;
        HexCoords? _lastHover;
        void Reset()
        {
            cam = Camera.main;
            if (!grid) grid = FindFirstObjectByType<BattleHexGrid>(FindObjectsInactive.Exclude);
            if (!highlighter)
            {
                highlighter = FindFirstObjectByType<HexHighlighter>(FindObjectsInactive.Exclude);
                if (highlighter && !highlighter.grid) highlighter.grid = grid;
            }
        }

        void Update()
        {
            if (!cam) cam = Camera.main;
            if (!grid || !highlighter) return;

            // û������豸�ͱ��
            if (Mouse.current == null) return;

            // UI ����ʱ���������ɹأ�
            if (ignoreWhenPointerOverUI && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                highlighter.SetHover(null);
                return;
            }

            Vector2 pos = Mouse.current.position.ReadValue();
            if (HexRaycaster.TryPick(cam, pos, out var go, out var tag, (int)raycastMask))
            {
                var h = tag.Coords;
                highlighter.SetHover(h);
                if (!_lastHover.HasValue || !_lastHover.Value.Equals(h))
                {
                    _lastHover = h;
                    OnHoverChanged?.Invoke(h);
                }
                // ������
                if (Mouse.current.leftButton.wasPressedThisFrame)
                {
                    OnTileClicked?.Invoke(h);
                    // �ȴ����־��������������ⲿ�����ϲ��߼�
                    Debug.Log($"[Click] Hex {h}");
                }
            }
            else
            {
                highlighter.SetHover(null);
                if (_lastHover.HasValue)
                {
                    _lastHover = null;
                    OnHoverChanged?.Invoke(null);
                }
            }
        }
    }
}
