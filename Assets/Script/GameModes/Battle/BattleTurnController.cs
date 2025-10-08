using System.Linq;
using UnityEngine;
using Game.Battle;

public class BattleTurnController : MonoBehaviour, ITurnOrderProvider
{
    [SerializeField] private TurnSystem turnSystem;
    [SerializeField] private BattleRules rules;

    private readonly System.Collections.Generic.List<ITurnActor> _actors = new();

    void Awake()
    {
        RefreshActorList();
        if (turnSystem != null)
            turnSystem.Initialize(this);
    }

    public void RefreshActorList()
    {
        _actors.Clear();
        _actors.AddRange(FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                         .OfType<ITurnActor>());
    }

    public void StartBattleRound() => turnSystem?.StartRound();

    public System.Collections.Generic.IEnumerable<ITurnActor> BuildOrder()
    {
        foreach (var actor in EnumerateSide(TurnSide.Player))
            yield return actor;
        foreach (var actor in EnumerateSide(TurnSide.Enemy))
            yield return actor;
    }

    public System.Collections.Generic.IEnumerable<ITurnActor> EnumerateSide(TurnSide side)
    {
        if (rules == null)
            yield break;

        var predicate = side == TurnSide.Player
            ? new System.Func<ITurnActor, bool>(rules.IsPlayer)
            : new System.Func<ITurnActor, bool>(rules.IsEnemy);

        foreach (var actor in _actors)
        {
            if (actor == null) continue;
            if (predicate(actor))
                yield return actor;
        }
    }
}
