using System.Collections.Generic;
using UnityEngine;
using Core.Hex;
using Game.Common;
using Game.Units;

namespace Game.Battle
{
    public enum RangeMode { None, Disk, Ring }
    public enum RangePivot { Hover, Selected }

    [DisallowMultipleComponent]
    public class SelectionManager : MonoBehaviour
    {
        [Header("Refs")]
        public BattleHexInput input;       // 事件来源（Hover/Click）
        public HexHighlighter highlighter; // 视觉（Hover/Selected/Range）
        public BattleHexGrid grid;         // 网格（用于 Version 检测）

        [Header("Range")]
        public RangeMode rangeMode = RangeMode.None;
        public RangePivot rangePivot = RangePivot.Hover;
        [Min(0)] public int radius = 2;

        // —— 选择 & Hover —— //
        HexCoords? _selected;
        HexCoords? _hoverCache;

        // —— 单位与占位表 —— //
        readonly Dictionary<HexCoords, Unit> _units = new();
        public Unit selectedUnit { get; private set; }

        void Reset()
        {
            if (!input) input = FindFirstObjectByType<BattleHexInput>(FindObjectsInactive.Exclude);
            if (!highlighter) highlighter = FindFirstObjectByType<HexHighlighter>(FindObjectsInactive.Exclude);
            if (!grid) grid = FindFirstObjectByType<BattleHexGrid>(FindObjectsInactive.Exclude);
        }

        void OnEnable()
        {
            if (!input)
                input = FindFirstObjectByType<BattleHexInput>(FindObjectsInactive.Exclude);

            if (input != null)
            {
                input.OnTileClicked += OnTileClicked;
                input.OnHoverChanged += OnHoverChanged;
            }
        }
        void OnDisable()
        {
            if (input != null)
            {
                input.OnTileClicked -= OnTileClicked;
                input.OnHoverChanged -= OnHoverChanged;
            }
        }

        [SerializeField] private Game.Grid.GridOccupancy occupancy;

        // —— 占位相关对外 API —— //
        public bool HasUnitAt(HexCoords c)
        {
            if (_units.ContainsKey(c)) return true;
            return occupancy != null && occupancy.HasUnitAt(c);
        }
        public bool IsEmpty(HexCoords c) => !HasUnitAt(c);
        public bool TryGetUnitAt(HexCoords c, out Unit u)
        {
            if (_units.TryGetValue(c, out u)) return true;

            if (occupancy != null && occupancy.TryGetUnitAt(c, out var occ))
            {
                RemoveUnitMapping(occ);
                _units[c] = occ;
                u = occ;
                return true;
            }

            u = null;
            return false;
        }
        // 若 SelectionManager 里有 Register/Unregister/Sync，改为：
        public void RegisterUnit(Unit u)
        {
            if (u == null) return;

            occupancy?.Register(u);

            RemoveUnitMapping(u);
            _units[u.Coords] = u;
            u.OnMoveFinished -= OnUnitMoveFinished;
            u.OnMoveFinished += OnUnitMoveFinished;
        }

        public void UnregisterUnit(Unit u)
        {
            if (u == null) return;

            occupancy?.Unregister(u);

            u.OnMoveFinished -= OnUnitMoveFinished;
            RemoveUnitMapping(u);

            if (selectedUnit == u)
            {
                selectedUnit = null;
                _selected = null;
                highlighter.SetSelected(null);
            }
        }

        public void SyncUnit(Unit u)
        {
            if (u == null) return;

            occupancy?.SyncUnit(u);

            RemoveUnitMapping(u);
            _units[u.Coords] = u;
        }

        void RemoveUnitMapping(Unit u)
        {
            HexCoords keyToRemove = default;
            bool found = false;
            foreach (var kv in _units)
            {
                if (kv.Value == u)
                {
                    keyToRemove = kv.Key;
                    found = true;
                    break;
                }
            }

            if (found)
                _units.Remove(keyToRemove);
        }

        public bool IsOccupied(HexCoords c) => HasUnitAt(c);

        // —— 输入回调 —— //

        void OnHoverChanged(HexCoords? h)
        {
            _hoverCache = h;
            if (rangePivot == RangePivot.Hover)
                RecalcRange();
        }

        void OnTileClicked(HexCoords c)
        {
            // 若点击到单位：切换选中单位
            if (TryGetUnitAt(c, out var unit))
            {
                selectedUnit = unit;
                _selected = c;
                highlighter.SetSelected(c);
                if (rangePivot == RangePivot.Selected) RecalcRange();
                return;
            }

            // 否则：尝试命令选中单位移动到“相邻且不被占用且存在”的格
            if (selectedUnit != null && !selectedUnit.IsMoving)
            {
                var from = selectedUnit.Coords;
                if (from.DistanceTo(c) != 1) return;
                if (IsOccupied(c)) return;

                // 交给单位检查该格是否存在（没有则返回 false）
                if (selectedUnit.TryMoveTo(c))
                {
                    // 预占位（简单防止快速连点造成穿插）
                    RemoveUnitMapping(selectedUnit);
                    _units[c] = selectedUnit;

                    // 选中视觉跟随到目标（也可以等移动结束再跟）
                    _selected = c;
                    highlighter.SetSelected(c);
                }
            }
        }

        void OnUnitMoveFinished(Unit u, HexCoords from, HexCoords to)
        {
            // 确保占位表一致（防止被其它逻辑改动）
            RemoveUnitMapping(u);
            _units[to] = u;

            // 若它是当前选中单位，让选择标记站在新格（安全起见再设一次）
            if (selectedUnit == u)
            {
                _selected = to;
                highlighter.SetSelected(to);
            }

            // 枢轴为 Selected 时，可在移动后重算范围
            if (rangePivot == RangePivot.Selected) RecalcRange();
        }

        // —— Range —— //

        public void SetRangeMode(RangeMode mode) { rangeMode = mode; RecalcRange(); }
        public void SetRadius(int r) { radius = Mathf.Max(0, r); RecalcRange(); }

        void RecalcRange()
        {
            if (rangeMode == RangeMode.None || radius <= 0)
            {
                highlighter.ApplyRange(null);
                return;
            }

            HexCoords? center = (rangePivot == RangePivot.Hover) ? _hoverCache : _selected;
            if (!center.HasValue) { highlighter.ApplyRange(null); return; }

            var set = new HashSet<HexCoords>();
            if (rangeMode == RangeMode.Disk)
                foreach (var c in center.Value.Disk(radius)) set.Add(c);
            else
                foreach (var c in center.Value.Ring(radius)) set.Add(c);

            highlighter.ApplyRange(set);
        }
    }
}
