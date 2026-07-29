# PROJECT STATUS — Stickler's Gate

## Quick Reference
- **Engine:** Unity 6 LTS (6000.3.10f1), C#
- **Platforms:** Android (API 24+) + iOS, Landscape
- **Current Milestone:** 2 — "Full Party Combat"
- **Current Phase:** 9 — Grid Combat System
- **Unity Installed:** Yes
- **Repo:** github.com/amihali1/sticklers-gate (private)
- **Unity Project Folder:** SticklersGate/

## Milestone 1 — Complete (PRs #1–#11)

All phases done: Project bootstrap, core data model, combat engine, combat UI, content (4 classes, 3 enemies, 1 encounter), integration + scene wiring, runtime bug fixes.

## Milestone 2 Progress

### Phase 6: Combat Expansion — Complete (PR #12)
- [x] 6.1 Multi-target ability resolution
- [x] 6.2 New enemies: Stick Bandit, Stick Spider, Stick Necromancer
- [x] 6.3 Enemy AI improvements (multi-target scoring)
- [x] 6.4 Copilot review fixes (inventory timing, multi-target items, saving throws, script validation)

### Phase 7: Item & Consumable System — Complete (PR #12)
- [x] 7.1 ItemSO data class
- [x] 7.2 Inventory system
- [x] 7.3 Consumable resolution in combat
- [x] 7.4 Item UI in combat action panel

### Phase 8: Polish & Difficulty — Deferred
- [ ] 8.1 Encounter difficulty scaling
- [ ] 8.2 Integration testing for Milestone 2 features
- [ ] 8.3 Polish pass

*Deferred until after grid system is in place, since grid changes encounter design significantly.*

### Phase 9: Grid Combat System — In Progress

Replacing front-row/back-row with a full grid movement system. Broken into 5 PRs:

#### PR A: Grid Data Foundation (`feature/grid-data`) — Complete
- [x] 9.1 GridPosition struct (x, y value type, Manhattan distance + orthogonal neighbors + operators)
- [x] 9.2 TerrainType enum (Normal, Difficult, Elevated, Water, Lava, Abyss, Cover) — added to GameEnums.cs
- [x] 9.3 CombatGrid class (width/height, terrain map, occupancy w/ reverse lookup, walkability, movement cost, FromLayout factory)
- [x] 9.4 Pathfinder class (Dijkstra — reachable range + shortest path, terrain-cost aware, blocks on occupancy)
- [x] 9.5 GridLayoutSO (dimensions, terrain overrides, player/enemy spawn points)
- [x] 9.6 Grid constants (DEFAULT/MAX grid size, melee/ranged range + movement, terrain costs, elevated/cover bonuses)
- [x] 9.7 Unit tests: GridPositionTests, CombatGridTests, PathfinderTests

#### PR B: Core Combat Integration (`feature/grid-combat-core`)
- [ ] 9.8 CombatUnit: replace Row with GridPosition, add MovementSpeed
- [ ] 9.9 AbilitySO: add _range field (grid distance)
- [ ] 9.10 CharacterClassSO / EnemySO: add _movementSpeed field
- [ ] 9.11 DamageCalculator: range validation (is target in range?)
- [ ] 9.12 CombatStateMachine: integrate CombatGrid, place units at spawn points
- [ ] 9.13 Remove CombatRow enum and BACK_ROW_AC_BONUS constant
- [ ] 9.14 Update existing tests for Row→GridPosition migration

#### PR C: Movement Phase (`feature/grid-movement`)
- [ ] 9.15 New CombatPhase: PlayerChooseMovement (between TurnStart and PlayerChooseAction)
- [ ] 9.16 CombatStateMachine: movement phase in turn loop, SubmitPlayerMovement()
- [ ] 9.17 EnemyAI: grid-aware movement (approach targets, stay at range for ranged)
- [ ] 9.18 Movement events: OnMovementPhaseStarted, OnUnitMoved
- [ ] 9.19 Unit tests for movement phase flow

#### PR D: Grid UI (`feature/grid-ui`)
- [ ] 9.20 GridRenderer: draw tile grid, highlight movement range, highlight ability range
- [ ] 9.21 Movement input: tile click/tap → move unit
- [ ] 9.22 Update CombatBootstrapper: create grid, position units on tiles
- [ ] 9.23 Update CombatUIController: handle movement phase, skip-movement button
- [ ] 9.24 Update ActionPanel: disable out-of-range abilities
- [ ] 9.25 Update TargetPanel: filter targets by ability range

#### PR E: Content & Cleanup (`feature/grid-content`)
- [ ] 9.26 GridLayoutSO assets for all 3 encounters (with spawn points)
- [ ] 9.27 Update all AbilitySO assets with range values
- [ ] 9.28 Update CharacterClassSO / EnemySO assets with movement speeds
- [ ] 9.29 Update EncounterSO to reference GridLayoutSO
- [ ] 9.30 Integration tests for full grid combat flow
- [ ] 9.31 Update Combat.unity scene

## Next Action
PR A (Grid Data Foundation) code complete on `feature/grid-data` — needs Unity EditMode test run + PR. Then start PR B: Core Combat Integration (`feature/grid-combat-core`) — migrate CombatUnit Row→GridPosition, add MovementSpeed, AbilitySO _range, remove CombatRow.

## Class Roster Expansion (queued after grid PR-A/B)
Full 12-class D&D set decided 2026-07-29. Current 4: Sticknight(Fighter), Stickwizard(Wizard), Stickrogue(Rogue), Stickleric(Cleric). Add 8: Sorcerer(CHA burst/metamagic — distinct from Wizard's INT versatility), Druid, Barbarian, Ranger, Bard, Monk, Warlock, Paladin. Keep "Stick" pun prefix. Build after grid foundation so all class abilities get range/movement fields natively.

## Known Issues / Decisions Pending
- Input System package not installed (Unity shows deprecation warning for legacy Input Manager)
- .meta files for scripts are minimal (missing MonoImporter section) — Unity accepts them fine
- Life Drain ability doesn't actually heal the necromancer yet (needs code support for "damage + heal self" pattern)
- Phase 8 (polish/difficulty) deferred until grid system is complete
- Terrain effects beyond Normal are planned but not implemented until content needs them

## Session Count: 7
