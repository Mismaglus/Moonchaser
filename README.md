
原则：**通用层**不包含战斗/世界差异逻辑；**模式层**不重复通用实现，只提供规则/控制器/外壳组件。

---

## 关键系统与最小 API（供 AI/Codex 对齐）

### 网格（通用）
- `IHexGridProvider`：统一向运行期对象提供网格信息  
  属性：`uint Version`（网格重建自增，触发缓存重建）、`GridRecipe recipe`（厚度/材质等配置，ScriptableObject）  
  方法：`IEnumerable<TileTag> EnumerateTiles()`（场景中所有 TileTag，含隐藏）  
- `BattleHexGrid`：实现 `IHexGridProvider`；每次重建时 `Version++`。

### 占位（通用，单一事实源）
- `GridOccupancy`：`HasUnitAt / IsEmpty / TryGetUnitAt / Register / Unregister / Sync / ClearAll`。  
- `SelectionManager` 与 `BattleRules` 均通过它判断占位；`SelectionManager` 专注“选中/高亮/输入桥接”。

### 单位与移动（通用）
- `Unit`：坐标、邻格移动（带 Lerp/可选朝向）、`OnMoveFinished(Unit, from, to)` 事件；支持 `autoInitializeIfMissing`（脚下拾取格并注册）。  
- `UnitMover`（移动资源 **Stride**）：  
  字段/方法：`MaxStride / Stride / ResetStride()`、`TryStepTo(HexCoords dst, Action onDone)`（花 1 Stride，内部调 `Unit.TryMoveTo`）。  
  注意：**Stride ≠ Action Points**（后者留给战斗技能系统）。

### 选择与高亮（通用）
- `SelectionManager`：  
  选中单位、Hover/Selected/Range 三态；Range 用 `Disk(Stride)`；  
  占位相关 API 代理到 `GridOccupancy`；  
  可选/可走逻辑不写死，委托给当前模式的 `Rules`。

### 回合（通用）
- `ITurnActor`：`OnTurnStart()`（刷新资源，例如 `ResetStride`）、`IEnumerator TakeTurn()`（玩家等待输入；敌人跑 AI）、`bool HasPendingAction`。  
- `ITurnOrderProvider`：`IEnumerable<ITurnActor> BuildOrder()`（例：先玩家全体，再敌人全体）。  
- `TurnSystem`：`Initialize(ITurnOrderProvider)`、`StartRound()`。

### 规则与模式（Battle）
- `BattleRules`：阵营、可选、可走、邻格候选等规则中心：  
  `IsPlayer(ITurnActor)` / `IsEnemy(...)`；  
  `IsTileWalkable(HexCoords)`（在网格内且未占用）；  
  `CanSelect(Unit)`（只选我方）；  
  `CanStep(Unit, dst)`（邻格 + 可走 + 有 Stride）。  
- `BattleTurnController`：组装 `TurnSystem + BattleRules + Actors`；控制启停输入与敌方 AI。  
- `BattleUnit`：战斗外壳（阵营/战斗属性/实现 `ITurnActor`），与 `Unit/UnitMover` 组合使用。

### 输入与相机
- `BattleHexInput`（通用输入事件）：`OnHoverChanged(HexCoords?)`、`OnTileClicked(HexCoords)`；非玩家回合或 Busy 阶段禁用输入。  
- `CameraFrameOnGrid`：OrthoIso 固定角度；`CameraFollower`：Lerp 跟随选中单位。

---

## 美术与素材约定（摘要）

- 角色 6 向序列；移动时调用 `UnitVisual2D.SetFacingFromStep(from, to)` 切向。  
- 长期主题：夜空/星空（“Moonlit Underdye”：银色外层 + 靛蓝内层挑染；冰蓝眼 + 银色内圈；星形高光）。  
- 精灵：PPU=100、pivot=脚底、SpriteAtlas 打包、Bilinear、关 Mipmap。  
- 2D 光照 + ShadowCaster2D；边线材质 Unlit 透明（ZWrite Off / ZTest Always）。

---

## 快速开始（本机运行）

1. Unity 工程设置  
   - Project Settings → Editor：Version Control 设为 **Visible Meta Files**；Asset Serialization 设为 **Force Text**。  
2. 场景连线（Demo）  
   - 场景放置：`BattleHexGrid`（实现 `IHexGridProvider`） + `GridOccupancy` + `SelectionManager` + `TurnSystem` + `BattleTurnController`；  
   - 单位 Prefab：`Unit + UnitMover + UnitVisual2D`（战斗再加 `BattleUnit`）；  
   - 生成流程：`unit.Initialize(grid, startCoords)` → `SelectionManager.RegisterUnit(unit)` → `UnitMover.ResetStride()`。  
3. 运行验证  
   - 玩家单位可邻格一步移动；每步 Stride–1；为 0 自动切敌方段落；  
   - 敌人尝试随机走一步；回到玩家段落；  
   - Range 显示随 Hover/Stride 变化，遮挡/排序正确。

---

## 版本控制与提交流程（建议）

- .gitignore 策略（“代码最小集”）：忽略 `Assets/*`，仅白名单 `Assets/Script/**`（与少量必要资源），并提交 `Packages/**` 与 `ProjectSettings/**`。  
- 合并工具：配置 UnityYAMLMerge 合并 `.unity/.prefab/.asset`。  
- 提交流程：功能分支 → PR → 代码评审（优先：架构正确性 → 运行正确性 → 可维护性）。

---

## 路线图 / 非目标（当前阶段）

**近期**：A* 与路径预览（多步）；技能/受击/数值（Action Points 与 Stride 解耦下的战斗回合）；屋顶淡出、3D 道具遮挡更多案例。  
**暂不包含**：存档/联机；复杂状态系统/装备/掉落；大型美术与 UI 多语言。
