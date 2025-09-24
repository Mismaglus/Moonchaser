using UnityEngine;
using Core.Hex;
using Game.Units;   // ��������ͨ�� Unit / UnitMover
using Game.Grid;    // ����Ҫ�õ� IHexGridProvider���ɱ���

namespace Game.Battle
{
    [DisallowMultipleComponent]
    public class UnitSpawner : MonoBehaviour
    {
        public BattleHexGrid grid;
        public SelectionManager selection;
        public BattleUnit unitPrefab;

        [Header("Spawn Coords")]
        public int startQ = 2;
        public int startR = 2;

        void Reset()
        {
            if (!grid) grid = FindFirstObjectByType<BattleHexGrid>(FindObjectsInactive.Exclude);
            if (!selection) selection = FindFirstObjectByType<SelectionManager>(FindObjectsInactive.Exclude);
        }

        [ContextMenu("Spawn Now")]
        public void SpawnNow()
        {
            if (!grid || !unitPrefab)
            {
                Debug.LogWarning("Missing grid or prefab.");
                return;
            }

            // 1) ʵ����
            var go = Instantiate(unitPrefab.gameObject, Vector3.zero, Quaternion.identity, transform);

            // 2) ��ͨ�� Unit ��ʼ����ָ����ע�⣺���� BattleUnit��
            var unit = go.GetComponent<Unit>();
            if (!unit)
            {
                Debug.LogError("Spawned prefab lacks Unit component.");
                return;
            }
            var c = new HexCoords(startQ, startR);
            unit.Initialize(grid, c);   // BattleHexGrid ʵ���� IHexGridProvider����ֱ�Ӵ�

            // 3) ��������롰ע��ռλ���Ĵ��루�ؼ����У�
            var sel = selection ? selection : Object.FindFirstObjectByType<SelectionManager>();
            sel?.RegisterUnit(unit);

            // 4) ��ѡ��ˢ�²��㣬�������̿ɶ�
            var mover = go.GetComponent<UnitMover>();
            mover?.ResetStride();
        }
    }
}
