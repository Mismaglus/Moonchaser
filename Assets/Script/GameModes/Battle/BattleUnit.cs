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
        public bool isPlayer = true; // ������ Faction ö��

        // �����Ժ�ӣ�ս�����ԣ�HP/����/Ԫ�أ���ʵ�� ITurnActor ��
        // ITurnActor.OnTurnStart() => GetComponent<UnitMover>().ResetStride();
        // ITurnActor.TakeTurn()    => ��ҵȴ����� / ����AI��һ������
    }
}
