using System.Collections.Generic;
using UnityEngine;
using Core.Hex;          // HexCoords
using Game.Units;        // Unit

namespace Game.Grid
{
    /// ά�����ĸ����ӱ��ĸ���λռ�ݡ���Ψһ����
    public class GridOccupancy : MonoBehaviour
    {
        private readonly Dictionary<HexCoords, Unit> _map = new();

        // ��ѯ
        public bool HasUnitAt(HexCoords c) => _map.ContainsKey(c);
        public bool IsEmpty(HexCoords c) => !_map.ContainsKey(c);
        public bool TryGetUnitAt(HexCoords c, out Unit u) => _map.TryGetValue(c, out u);

        // ע��/��ע��
        public void Register(Unit u)
        {
            if (u == null) return;

            // ����ɼ�¼
            HexCoords oldKey = default;
            bool found = false;
            foreach (var kv in _map)
            {
                if (kv.Value == u) { oldKey = kv.Key; found = true; break; }
            }
            if (found) _map.Remove(oldKey);

            _map[u.Coords] = u;

            u.OnMoveFinished -= OnUnitMoved; // ���ظ�����
            u.OnMoveFinished += OnUnitMoved;
        }

        public void Unregister(Unit u)
        {
            if (u == null) return;
            u.OnMoveFinished -= OnUnitMoved;

            HexCoords key = default;
            bool found = false;
            foreach (var kv in _map)
            {
                if (kv.Value == u) { key = kv.Key; found = true; break; }
            }
            if (found) _map.Remove(key);
        }

        // ����/����/��ʼ�����ֶ�ͬ��
        public void SyncUnit(Unit u)
        {
            if (u == null) return;
            Unregister(u);
            _map[u.Coords] = u;
            u.OnMoveFinished -= OnUnitMoved;
            u.OnMoveFinished += OnUnitMoved;
        }

        public void ClearAll() => _map.Clear();

        private void OnUnitMoved(Unit u, HexCoords from, HexCoords to)
        {
            if (_map.TryGetValue(from, out var who) && who == u) _map.Remove(from);
            _map[to] = u;
        }
    }
}
