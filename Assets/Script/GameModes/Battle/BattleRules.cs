using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Core.Hex;          // HexCoords / �����
using Game.Grid;        // IHexGridProvider / GridRecipe
using Game.Units;       // Unit / UnitMover

namespace Game.Battle
{
    /// <summary>
    /// ս��ģʽ�µĹ������ģ���Ӫ�жϡ������ж���ѡ���ж��ȡ�
    /// �����������򡱣�������״̬��ռλ������ SelectionManager ά������
    /// </summary>
    public class BattleRules : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("�κ�ʵ���� IHexGridProvider ������������� BattleHexGrid��")]
        [SerializeField] private MonoBehaviour gridComponent;

        [Tooltip("ռλ/��ѯ���������������ջ��Զ����ҳ����е� SelectionManager��")]
        [SerializeField] private SelectionManager selection;
        [SerializeField] private Game.Grid.GridOccupancy occupancy; // ����

        private IHexGridProvider grid;

        void Awake()
        {
            grid = gridComponent as IHexGridProvider;
            if (selection == null)
                selection = FindFirstObjectByType<SelectionManager>(FindObjectsInactive.Exclude);
            if (grid == null)
                grid = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                        .OfType<IHexGridProvider>().FirstOrDefault();
        }

        // ���� ��Ӫ��� ���� //

        public bool IsPlayer(ITurnActor actor)
        {
            if (actor is MonoBehaviour mb &&
                mb.TryGetComponent(out BattleUnit bu))
                return bu.isPlayer;
            return false;
        }

        public bool IsEnemy(ITurnActor actor) => !IsPlayer(actor);

        public bool CanSelect(Unit unit)
        {
            // ������ѡ���ҷ���λ
            if (unit == null) return false;
            return unit.TryGetComponent(out BattleUnit bu) && bu.isPlayer;
        }

        // ���� ����/�����ж� ���� //

        public bool Contains(HexCoords c)
        {
            if (grid == null) return false;

            // ������������� Contains(c)�����Ի�������
            // ������ EnumerateTiles ��һ��ͨ�ö��ס�
            return grid.EnumerateTiles().Any(t => t != null && t.Coords.Equals(c));
        }

        public bool IsOccupied(HexCoords c)
        {
            return occupancy != null && occupancy.HasUnitAt(c);
        }

        public bool IsTileWalkable(HexCoords c)
        {
            // ս���У������������ڣ�������ռ��
            return Contains(c) && !IsOccupied(c);
        }

        public bool CanStep(Unit unit, HexCoords dst)
        {
            if (unit == null) return false;
            if (unit.Coords.DistanceTo(dst) != 1) return false;     // ���ڸ�
            if (!IsTileWalkable(dst)) return false;

            // �в��㣨Stride��
            if (unit.TryGetComponent(out UnitMover mover))
                return mover.Stride > 0;

            return true; // û�� UnitMover �����ⵥλ���������
        }

        /// <summary>
        /// ���ص�λ�ڵ�ǰ Stride �µĿɴ��ڸ�һ������
        /// �Ժ���չ A* ʱ�������������ಽѰ·��
        /// </summary>
        public IEnumerable<HexCoords> GetStepCandidates(Unit unit)
        {
            if (unit == null) yield break;
            foreach (var n in unit.Coords.Neighbors())
                if (CanStep(unit, n))
                    yield return n;
        }
    }
}
