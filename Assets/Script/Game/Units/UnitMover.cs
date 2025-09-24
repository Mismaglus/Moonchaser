using UnityEngine;
using Core.Hex;

namespace Game.Units
{
    [RequireComponent(typeof(Unit))]
    public class UnitMover : MonoBehaviour
    {
        public int MaxStride = 3;
        public int Stride { get; private set; }

        Unit _unit;

        void Awake() { _unit = GetComponent<Unit>(); }

        public void ResetStride() => Stride = MaxStride;

        public bool TrySpendStride(int cost = 1)
        {
            if (Stride < cost) return false;
            Stride -= cost;
            return true;
        }

        // 单步邻格移动：花费 1 Stride
        public bool TryStepTo(HexCoords dst, System.Action onDone = null)
        {
            if (!TrySpendStride(1)) return false;

            // 调用通用 Unit 的实际移动（带 Lerp/朝向/事件）
            if (!_unit.TryMoveTo(dst))
            {
                // 失败回滚
                Stride += 1;
                return false;
            }

            void Handler(Unit u, HexCoords from, HexCoords to)
            {
                _unit.OnMoveFinished -= Handler;
                onDone?.Invoke();
            }
            _unit.OnMoveFinished += Handler;
            return true;
        }
    }
}
