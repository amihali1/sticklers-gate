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

#### PR B: Core Combat Integration (`feature/grid-combat-core`) — Complete
- [x] 9.8 CombatUnit: Row → GridPosition Position, added MovementSpeed (from SO)
- [x] 9.9 AbilitySO: added _range field (default DEFAULT_MELEE_RANGE=1); AoE ignores range
- [x] 9.10 CharacterClassSO / EnemySO: _preferredRow → _movementSpeed (default 3)
- [x] 9.11 DamageCalculator: IsInRange(attacker,target,ability) helper + AttackResult.WasInRange flag (informational; NOT hard-enforced yet — see note)
- [x] 9.12 CombatStateMachine: builds CombatGrid (optional GridLayoutSO param, else default 8×6), places all units at spawns/default columns; exposes Grid
- [x] 9.13 Removed CombatRow enum, BACK_ROW_AC_BONUS, EnemySpawn.row, back-row AC logic; EnemyAI threat/healer heuristics now ability-based not row-based
- [x] 9.14 Migrated all tests (Row→Position); replaced back-row AC tests with range tests; 145/147 pass (2 failures pre-existing & unrelated)

**Range enforcement deferred:** IsInRange is a validation helper only in PR B. Hard enforcement (rejecting out-of-range actions) lands in PR C (AI stays at range / closes distance) + PR D (UI filters targets). Enforcing now would break melee since no movement phase exists yet to close distance. Keeps every intermediate build playable.

#### PR C: Movement Phase (`feature/grid-movement`) — Complete
- [x] 9.15 CombatPhase.PlayerChooseMovement added (before PlayerChooseAction)
- [x] 9.16 CombatStateMachine: movement in turn loop, SubmitPlayerMovement(dest) + SkipMovement(); IsReachable/MoveUnitTo helpers
- [x] 9.17 EnemyAI.DecideMovement: melee closes to adjacency, ranged holds at max weapon range (abs(dist-desiredRange)); helpers NearestLivingTarget, MaxAbilityRange
- [x] 9.18 Events OnMovementPhaseStarted(unit), OnUnitMoved(unit, from, to)
- [x] 9.19 EnemyAIMovementTests (5 tests, all pass)
- [x] BONUS: hard range enforcement now ON — SubmitPlayerAction rejects out-of-range single-target (keeps turn); enemy forfeits attack if unreachable after moving. Fixed IsInRange AoE/self short-circuit (was rejecting AoE w/ null target).

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
PRs open on GitHub: #14 (grid-data→master), #15 (grid-combat-core→grid-data). PR C code-complete on `feature/grid-movement` (150/152, 2 pre-existing failures) — commit + push + open PR (base feature/grid-combat-core). Then PR D: Grid UI (`feature/grid-ui`) — GridRenderer (draw tiles, highlight movement + ability range), tile click→move, movement-phase UI + skip button, out-of-range ability greying. PR D is the piece that makes the grid visible/playable. Merge order: #14 → #15 → PR-C → PR-D → PR-E.

## Pre-existing test failures (NOT grid-related, verified on parent f9bc0e8)
- `CombatIntegrationTests.CombatUnit_LethalDamage_KillsEnemy` — asserts OnDeath fires from TakeDamage alone, but enemies only Die() via state machine (downed≠dead by design). Test expectation bug.
- `ContentDataTests.AllClasses_UseStandardArray` — asserts equal stat totals but classes use 73/75/77/79 (unequal by design). Test bug.
Both predate Phase 9. Fix or delete in a separate cleanup PR — out of grid scope.

## Class Roster Expansion (queued after grid PR-A/B)
Full 12-class D&D set decided 2026-07-29. Current 4: Sticknight(Fighter), Stickwizard(Wizard), Stickrogue(Rogue), Stickleric(Cleric). Add 8: Sorcerer(CHA burst/metamagic — distinct from Wizard's INT versatility), Druid, Barbarian, Ranger, Bard, Monk, Warlock, Paladin. Keep "Stick" pun prefix. Build after grid foundation so all class abilities get range/movement fields natively.

## Known Issues / Decisions Pending
- Input System package not installed (Unity shows deprecation warning for legacy Input Manager)
- .meta files for scripts are minimal (missing MonoImporter section) — Unity accepts them fine
- Life Drain ability doesn't actually heal the necromancer yet (needs code support for "damage + heal self" pattern)
- Phase 8 (polish/difficulty) deferred until grid system is complete
- Terrain effects beyond Normal are planned but not implemented until content needs them

## Session Count: 7
