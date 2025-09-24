using UnityEngine;
using Game.Units;

namespace Game.Battle
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Unit))]
    [RequireComponent(typeof(UnitMover))]
    public class BattleUnit : MonoBehaviour
    {
        [Header("Battle")]
        public bool isPlayer = true; // 或者用 Faction 枚举

        // 这里以后加：战斗属性（HP/护盾/元素）、实现 ITurnActor 等
        // ITurnActor.OnTurnStart() => GetComponent<UnitMover>().ResetStride();
        // ITurnActor.TakeTurn()    => 玩家等待输入 / 敌人AI走一步……
    }
}
