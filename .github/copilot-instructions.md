# Copilot Instructions for HexTravelerPrototype

## Project Architecture
- **Layered Design:**
  - **Common Layer:** Contains reusable logic, agnostic to battle/world differences.
  - **Mode Layer:** Implements rules/controllers/shells for specific game modes, never duplicating common logic.
- **Key Systems:**
  - **Grid:**
    - `IHexGridProvider` (API for grid info, versioning, config via `GridRecipe`)
    - `BattleHexGrid` (runtime grid, increments version on rebuild)
  - **Occupancy:**
    - `GridOccupancy` (single source of truth for unit placement)
    - Used by both `SelectionManager` and `BattleRules` for queries
  - **Units & Movement:**
    - `Unit` (coordinates, movement, events)
    - `UnitMover` (stride-based movement, not action points)
  - **Selection & Highlight:**
    - `SelectionManager` (unit selection, hover, range, delegates logic to rules)
  - **Turn System:**
    - `ITurnActor`, `ITurnOrderProvider`, `TurnSystem` (turn order, round management)
  - **Battle Rules:**
    - `BattleRules` (faction, walkable tiles, selection, step logic)
    - `BattleTurnController` (assembles turn system, rules, actors)
    - `BattleUnit` (battle shell, combines with `Unit`/`UnitMover`)
  - **Input & Camera:**
    - `BattleHexInput` (input events, disables input when not player's turn)
    - `CameraFrameOnGrid`, `CameraFollower` (camera logic)

## Developer Workflows
- **Unity Setup:**
  - Project Settings: Version Control = Visible Meta Files, Asset Serialization = Force Text
- **Scene Wiring (Demo):**
  - Place: `BattleHexGrid`, `GridOccupancy`, `SelectionManager`, `TurnSystem`, `BattleTurnController`
  - Unit Prefab: `Unit`, `UnitMover`, `UnitVisual2D` (+ `BattleUnit` for battle)
  - Flow: `unit.Initialize(grid, startCoords)` → `SelectionManager.RegisterUnit(unit)` → `UnitMover.ResetStride()`
- **Run/Verify:**
  - Player units move one tile per stride, auto-switch to enemy turn when stride = 0
  - Enemy AI moves randomly, then returns to player turn
  - Range display updates with hover/stride, correct sorting/occlusion

## Conventions & Patterns
- **Occupancy:** All placement queries go through `GridOccupancy`.
- **Selection Logic:** Delegated to current mode's rules, not hardcoded.
- **Stride:** Used for movement, not action points (combat system will use AP separately).
- **Art:** SpriteAtlas, PPU=100, pivot=foot, Bilinear, no Mipmap, 2D lighting, Unlit transparent edge materials.
- **Version Control:**
  - Only whitelist `Assets/Script/**` and essential resources
  - Commit `Packages/**` and `ProjectSettings/**`
  - Use UnityYAMLMerge for `.unity/.prefab/.asset` files
  - PR workflow: feature branch → PR → review (architecture > runtime > maintainability)

## Integration Points
- **ScriptableObjects:** Used for grid config (`GridRecipe`)
- **Events:** Movement and input use event-driven patterns (`OnMoveFinished`, `OnHoverChanged`, etc.)
- **AI:** Enemy actions run via coroutine (`IEnumerator TakeTurn()`)

## Key Files & Directories
- `Assets/Script/` — main codebase, follows layered architecture
- `Assets/Script/Game/Units/` — unit logic, movement, highlighting
- `Assets/Script/Game/Grid/` — grid and occupancy systems
- `Assets/Script/Game/Battle/` — battle rules, turn controllers

---

For unclear patterns or missing documentation, ask for clarification or examples from maintainers.
