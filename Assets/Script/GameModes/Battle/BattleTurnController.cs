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
        // �ռ����/���˵�ʵ���� ITurnActor �����
        _actors.AddRange(FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                         .OfType<ITurnActor>());
        turnSystem.Initialize(this);
    }

    public void StartBattleRound() => turnSystem.StartRound();

    public System.Collections.Generic.IEnumerable<ITurnActor> BuildOrder()
    {
        // �������ҷ����У��ٵз����У��� rules �ж���Ӫ��
        var players = _actors.Where(a => rules.IsPlayer(a));
        var enemies = _actors.Where(a => rules.IsEnemy(a));
        foreach (var p in players) yield return p;
        foreach (var e in enemies) yield return e;
    }
}
