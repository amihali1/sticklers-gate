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

### Phase 9: Grid Combat System — Complete (PRs #14–#16, #19, PR E)

Replacing front-row/back-row with a full grid movement system. Broken into 5 PRs:

#### PR A: Grid Data Foundation (`feature/grid-data`) — Merged (#14)
- [x] 9.1 GridPosition struct (x, y value type, Manhattan distance + orthogonal neighbors + operators)
- [x] 9.2 TerrainType enum (Normal, Difficult, Elevated, Water, Lava, Abyss, Cover) — added to GameEnums.cs
- [x] 9.3 CombatGrid class (width/height, terrain map, occupancy w/ reverse lookup, walkability, movement cost, FromLayout factory)
- [x] 9.4 Pathfinder class (Dijkstra — reachable range + shortest path, terrain-cost aware, blocks on occupancy)
- [x] 9.5 GridLayoutSO (dimensions, terrain overrides, player/enemy spawn points)
- [x] 9.6 Grid constants (DEFAULT/MAX grid size, melee/ranged range + movement, terrain costs, elevated/cover bonuses)
- [x] 9.7 Unit tests: GridPositionTests, CombatGridTests, PathfinderTests

#### PR B: Core Combat Integration (`feature/grid-combat-core`) — Merged (#15)
- [x] 9.8 CombatUnit: Row → GridPosition Position, added MovementSpeed (from SO)
- [x] 9.9 AbilitySO: added _range field (default DEFAULT_MELEE_RANGE=1); AoE ignores range
- [x] 9.10 CharacterClassSO / EnemySO: _preferredRow → _movementSpeed (default 3)
- [x] 9.11 DamageCalculator: IsInRange(attacker,target,ability) helper + AttackResult.WasInRange flag (informational; NOT hard-enforced yet — see note)
- [x] 9.12 CombatStateMachine: builds CombatGrid (optional GridLayoutSO param, else default 8×6), places all units at spawns/default columns; exposes Grid
- [x] 9.13 Removed CombatRow enum, BACK_ROW_AC_BONUS, EnemySpawn.row, back-row AC logic; EnemyAI threat/healer heuristics now ability-based not row-based
- [x] 9.14 Migrated all tests (Row→Position); replaced back-row AC tests with range tests; 145/147 pass (2 failures pre-existing & unrelated)

**Range enforcement deferred:** IsInRange is a validation helper only in PR B. Hard enforcement (rejecting out-of-range actions) lands in PR C (AI stays at range / closes distance) + PR D (UI filters targets). Enforcing now would break melee since no movement phase exists yet to close distance. Keeps every intermediate build playable.

#### PR C: Movement Phase (`feature/grid-movement`) — Merged (#16)
- [x] 9.15 CombatPhase.PlayerChooseMovement added (before PlayerChooseAction)
- [x] 9.16 CombatStateMachine: movement in turn loop, SubmitPlayerMovement(dest) + SkipMovement(); IsReachable/MoveUnitTo helpers
- [x] 9.17 EnemyAI.DecideMovement: melee closes to adjacency, ranged holds at max weapon range (abs(dist-desiredRange)); helpers NearestLivingTarget, MaxAbilityRange
- [x] 9.18 Events OnMovementPhaseStarted(unit), OnUnitMoved(unit, from, to)
- [x] 9.19 EnemyAIMovementTests (5 tests, all pass)
- [x] BONUS: hard range enforcement now ON — SubmitPlayerAction rejects out-of-range single-target (keeps turn); enemy forfeits attack if unreachable after moving. Fixed IsInRange AoE/self short-circuit (was rejecting AoE w/ null target).

#### PR D: Grid UI (`feature/grid-ui`) — Merged (#19)
- [x] 9.20 GridRenderer: world-space tile grid (terrain-colored, runtime-generated square sprite), Movement / AbilityRange / ActiveUnit highlight overlays. Coordinate math in plain-C# GridCoordinateMapper (row 0 = top, grid centered, FitTileSize).
- [x] 9.21 Movement input: tile click/tap → SubmitPlayerMovement (own tile = stay). Legacy Input (activeInputHandler=0); clicks over UI ignored via EventSystem.IsPointerOverGameObject.
- [x] 9.22 CombatBootstrapper: builds grid on OnCombatStarted sized to free screen band; stick figures placed on tiles (height = 0.65 × tile); OnUnitMoved lerps figure; optional `_gridLayout` field passed to StartCombat. Old spawn-point transforms removed.
- [x] 9.23 CombatUIController: movement phase shows reachable tiles + MovementPanel ("Don't Move" skip button); ability selection highlights range diamond; tap a unit on the grid to target it; damage popups positioned from grid.
- [x] 9.24 ActionPanel: abilities with no target in range disabled + "(out of range)" label (TargetPanel.HasTargetInRange)
- [x] 9.25 TargetPanel: GetTargetsInRange filters by DamageCalculator.IsInRange (items unaffected — no range)
- [x] CombatGrid.GetPositionsWithinDistance (ability reach; ignores terrain/occupancy) + tests; GridCoordinateMapperTests; TargetPanel range tests

#### PR E: Content & Cleanup (`feature/grid-content`) — Complete
- [x] 9.26 GridLayoutSO assets for all 3 encounters in `Data/Encounters/Layouts/` (sizes per CONTENT.md: 8×6, 10×6, 8×8; spawns; terrain — only Difficult/Water/Abyss, the terrain types with working mechanics)
- [x] 9.27 AbilitySO ranges — before this every ability defaulted to range 1 (spells/crossbows were melee-only). Values taken from CONTENT.md class/enemy sections.
- [x] 9.28 Movement speeds on all class/enemy SOs, per CONTENT.md
- [x] 9.29 EncounterSO._gridLayout; StartCombat falls back to encounter.GridLayout (bootstrapper `_gridLayout` field from PR D removed)
- [x] 9.30 GridContentTests: real-asset checks (layouts, spawns, path connectivity, ranges, speeds) + enemies-engage-party simulation using real EnemyAI.DecideMovement
- [x] 9.31 Combat.unity — no change needed: scene references the encounter, which now carries its layout

### Phase 10: Class Roster Expansion — Complete (PRs F, G, H)
Design approved 2026-10-08 (see CONTENT.md "Planned Classes"). User direction: follow 5e closely on the grid — ranges and area templates, no whole-side targeting; area spells hit allies too.

#### PR F: Class Mechanics (`feature/class-mechanics`) — Complete
- [x] Grid areas: sphere (point within range) + emanation (around user), AreaAffects, friendly fire, save-for-half damage
- [x] Area aiming UI: tap a tile in range to preview the blast, tap again to cast
- [x] Enemy AI scores areas by foes minus friends caught
- [x] Status mechanics: Shielded, Blessed, Slowed (was Frozen), Restrained, Raging, Marked (+source)
- [x] Effects tick at the end of the affected unit's turn (fixes 1-turn Stun/Frightened expiring before doing anything); self-applied effects keep full duration
- [x] Ability extras: self effect, lifesteal, multi-hit, Twinned extra targets, Empowered max damage; Thunder damage type
- [x] Converted Fireball, Mass Heal, Ground Slam, Poison Spit to grid areas
- [x] Fixed: Cunning Dodge did nothing (Shielded had no mechanic); Shadow Step made the *target* invisible; Life Drain didn't heal; Ray of Frost didn't slow; enemies downed by Burning never died
- [x] Tests: AreaTargetingTests, StatusEffectMechanicsTests, content guards (no AllEnemies/AllAllies abilities, area consistency)

#### PR G: Arcane & Nature Classes (`feature/classes-casters`) — Complete
- [x] Sticksorcerer, Stickwarlock, Stickdruid, Stickbard: class + 15 ability assets (`Data/Abilities/<Class>/`), stick-figure colors
- [x] CharacterClassType entries for all 8 new classes (PR H reuses them)
- [x] ClassRosterTests over real class assets (one asset per type, naming, stat budget 70–80, D&D hit dice, level progression, builds a CombatUnit)
- [x] Touch spells (Vampiric Touch) allowlisted in the ranged-reach guard

#### PR H: Martial Classes (`feature/classes-martial`) — Complete
- [x] Stickbarbarian, Stickmonk, Stickranger, Stickpaladin: class + 16 ability assets
- [x] Range guard now skips Self abilities (range is ignored for self/emanation)

#### Later (Milestone 3): Party & Recruitment — decided 2026-10-08
Player creates their own character (any class). A few NPCs of varied classes are found early and can join. Roster size unlimited; only 4 fight per battle; party swaps only at camp. Until then new classes are data-only (swap via CombatBootstrapper `_partyClasses`).

### Play-test Round 1 Fixes (`fix/playtest-round-1`) — 2026-10-08
- [x] Enemy names blank (empty `nameOverride` string) → fall back to EnemyName; duplicates lettered (Stick Goblin A/B/C) via `UnitNaming`
- [x] Blurry text → all UI on TextMeshPro (TMP Essential Resources imported; `Assets/Editor/ProjectSetup.ImportTmpEssentials` can re-run headless)
- [x] Split movement: per-turn movement budget usable in several moves, before and after the action; tapping your own tile does nothing; End Turn button after acting
- [x] Player turn no longer advances while the action is still resolving

## Next Action
Stacked PRs open: E (#20) → F (#21) → G (#22) → H. Merge in order. Then play-test (grid UI, area aiming, a few new classes swapped into the party) and pick the next milestone: Phase 8 polish/difficulty, terrain effects (Elevated/Cover/Lava), or Milestone 3 (character creation + NPC recruitment + camp).

## CI
GitHub Actions `unity-tests.yml` (game-ci/unity-test-runner@v4) green since PR #18 (2026-09-29). Needs UNITY_EMAIL + UNITY_PASSWORD + UNITY_LICENSE secrets, a free-disk-space step (Unity 6 image is huge), and `checks: write`. Runs on PRs to any branch. The 2 old test bugs (LethalDamage, StandardArray) were fixed/removed in #18.

## Class Roster Expansion (queued after grid PR-A/B)
Full 12-class D&D set decided 2026-07-29. Current 4: Sticknight(Fighter), Stickwizard(Wizard), Stickrogue(Rogue), Stickleric(Cleric). Add 8: Sorcerer(CHA burst/metamagic — distinct from Wizard's INT versatility), Druid, Barbarian, Ranger, Bard, Monk, Warlock, Paladin. Keep "Stick" pun prefix. Build after grid foundation so all class abilities get range/movement fields natively.

## Known Issues / Decisions Pending
- Input System package not installed (Unity shows deprecation warning for legacy Input Manager)
- .meta files for scripts are minimal (missing MonoImporter section) — Unity accepts them fine
- Phase 8 (polish/difficulty) deferred until grid system is complete
- Elevated/Cover/Lava terrain effects (AC/range bonus, fire damage) not implemented — only movement cost (Difficult/Water) and impassable (Abyss) work
- Grid UI (PR D) visuals/input verified only by EditMode logic tests — needs an in-editor play-test

## Session Count: 7
