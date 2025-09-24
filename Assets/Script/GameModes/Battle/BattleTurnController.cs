using System.Linq;
using UnityEngine;

public class BattleTurnController : MonoBehaviour, ITurnOrderProvider
{
    [SerializeField] private TurnSystem turnSystem;
    [SerializeField] private BattleRules rules;

    private readonly System.Collections.Generic.List<ITurnActor> _actors = new();

    void Awake()
    {
        // 收集玩家/敌人等实现了 ITurnActor 的组件
        _actors.AddRange(FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                         .OfType<ITurnActor>());
        turnSystem.Initialize(this);
    }

    public void StartBattleRound() => turnSystem.StartRound();

    public System.Collections.Generic.IEnumerable<ITurnActor> BuildOrder()
    {
        // 例：先我方所有，再敌方所有（用 rules 判断阵营）
        var players = _actors.Where(a => rules.IsPlayer(a));
        var enemies = _actors.Where(a => rules.IsEnemy(a));
        foreach (var p in players) yield return p;
        foreach (var e in enemies) yield return e;
    }
}
