# SESSION LOG — Stickler's Gate

## Session 1 — March 10, 2026
**Focus:** Project planning and setup
**What happened:**
- Chose tech stack: Unity 2D, C#, Android + iOS, Landscape
- Chose tone: funny dialogue, real stakes
- Prioritized features: combat → character creation → story → world
- Created Game Design Document (GDD)
- Created agent pipeline task breakdown (20 tasks across 5 phases)
- Decided against agent pipeline for cost reasons — working in Claude.ai sessions instead
- Created session memory system (this file and siblings)
- Created Unity setup guide

**Decisions made:**
- Unity LTS with Built-In 2D Render Pipeline (no URP needed)
- ScriptableObject-driven architecture
- Simplified D&D 5e rules (cooldowns instead of spell slots, front/back row instead of grid)
- 4 classes: Sticknight, Stickwizard, Stickrogue, Stickleric
- Phase 1 art: simple programmatic stick figures via LineRenderer
- Event-driven UI (state machine emits, UI subscribes)

**Files created:**
- Sticklers_Gate_GDD.md (full game design document)
- Sticklers_Gate_Pipeline_Tasks.md (task breakdown, still useful as reference)
- Unity_Setup_Guide.md
- _memory/ folder (this system)

**Next session:** Install Unity, create project, start Phase 0 Task 0.1

## Session 2 — March 10, 2026
**Focus:** Project setup + Phase 1 + Phase 2 implementation
**What happened:**
- Created private GitHub repo (github.com/amihali1/sticklers-gate)
- Added .gitignore (Unity), CLAUDE.md (project instructions), README.md
- Set up git workflow rules (feature branches + PRs, no direct master commits)
- Installed Unity 6 LTS (6000.3.10f1), created project in SticklersGate/
- Configured: landscape-only, SticklerStudios company, SticklersGate product, Android API 24+
- Created full folder structure with .gitkeep and .meta files
- Phase 1 complete: GameEnums, GameConstants, StatBlock, AbilitySO, CharacterClassSO, EnemySO, EncounterSO
- Phase 2 complete: DiceRoller, CombatUnit, DamageCalculator, EnemyAI, CombatStateMachine

**Decisions made:**
- Unity 6 LTS (not Unity 2022 as originally planned — using what's installed)
- Product name is SticklersGate (with L), namespace is SticklersGate.*
- Company name: SticklerStudios
- StatusEffectType includes None as first value (default)
- GetModifier uses Mathf.FloorToInt for correct negative rounding
- Enemies die immediately when downed (no death saves)
- DealsDamage/Heals checks include flat bonuses

**PRs merged:** #1 (Unity setup), #2 (enums/constants), #3 (ScriptableObjects)
**PRs in review:** #4 (combat engine)

**Next session:** Review PR #4, then Phase 3 (Combat UI)

## Session 3 — March 10, 2026
**Focus:** Phase 3 — Combat UI implementation
**What happened:**
- Implemented all Phase 3 tasks (3.1–3.4) on `feature/combat-ui` branch
- Created 10 new UI scripts covering the full combat interface
- Added SubmitDefend() to CombatStateMachine for defend action support
- Programmatic stick figures via LineRenderer (no sprite assets needed)
- Narrator system with typewriter animation and message queue
- Unit tests for TargetPanel.GetValidTargets and defend mechanics

**Scripts created (Scripts/UI/):**
- CombatUIController — main controller wiring events to sub-components
- ActionPanel — ability buttons + defend for player turns
- TargetPanel — target selection with static GetValidTargets()
- UnitHPBar — HP bar bound to CombatUnit events
- TurnOrderDisplay — initiative order display
- NarratorDisplay — typewriter text overlay with queue
- DamagePopup — floating damage/heal numbers
- CombatResultPanel — victory/defeat screen
- StickFigureRenderer — programmatic stick figures with 6 poses
- CombatSceneSetup — scene bootstrapper

**Tests created:**
- TargetPanelTests (8 tests) — target filtering by type, dead/unconscious exclusion, enemy perspective
- CombatDefendTests (2 tests) — bonus AC application and reset on tick

**Decisions made:**
- Programmatic stick figures using LineRenderer (no sprite generation)
- Color-coded figures: players by class type, enemies by tier
- SubmitDefend() is a dedicated path in CombatStateMachine (not a hack via SubmitPlayerAction)
- Using Unity UI Text (not TextMeshPro yet — TMP install deferred)

**Next session:** Open PR, address review, then Phase 4 (Content: define 4 classes + 3 enemies + test encounter)

## Session 4 — March 11, 2026
**Focus:** Class renames + Phase 4 content implementation
**What happened:**
- Renamed Stickromancer → Stickwizard across entire codebase (PR #7)
- Renamed Stickeric → Stickleric across entire codebase (PR #8)
- Implemented Phase 4: all content as ScriptableObject .asset files (PR #9)
  - 16 player ability assets (4 per class: 2 basic + 1 level 3 + 1 level 5)
  - 5 enemy ability assets (RustyShiv, BoneClub, BoneThrow, Greatclub, GroundSlam)
  - 4 CharacterClassSO assets (Sticknight, Stickwizard, Stickrogue, Stickleric)
  - 3 EnemySO assets (Stick Goblin, Stick Skeleton, Stick Ogre)
  - 1 EncounterSO asset (Ambush at the Inkwell — 3x Stick Goblin)
- Unit tests for content data validation (ContentDataTests.cs)
- Generated assets via Python script with proper GUID cross-references

**Decisions made:**
- CunningDodge uses Shielded status effect for +AC buff
- ShadowStep applies Invisible status effect after dealing damage
- Champion's Strike auto-crit mechanic noted in description (needs code support in Phase 5)
- Narrator quips written for all abilities (hit/miss/crit for attacks, hit for heals/buffs)

**Next session:** Phase 5 (Integration: wire combat scene, integration testing, polish)

## Session 5 — March 11, 2026
**Focus:** Phase 5 — Combat scene integration
**What happened:**
- Created CombatBootstrapper — programmatic runtime scene builder that creates entire UI hierarchy
  - Canvas, CanvasScaler, GraphicRaycaster, EventSystem
  - NarratorDisplay with typewriter text and CanvasGroup fade
  - TurnOrderDisplay with vertical layout for initiative entries
  - ActionPanel with horizontal button container + Defend button
  - TargetPanel with horizontal button container + Cancel button
  - CombatResultPanel with title/subtitle/background
  - HP bar containers for players and enemies
  - Damage popup container
  - All prefab templates created as inactive GameObjects (HP bar, damage popup, ability/target buttons, turn entry)
  - Spawn points for 4 players + 4 enemies
  - Stick figure spawning and animation handling
  - Wires all [SerializeField] references via reflection at runtime
- Created Combat.unity scene with single CombatBootstrapper GameObject
  - Pre-configured with full 4-class party (Ser Sticksworth, Twiggy, Slick, Sister Splinter)
  - References AmbushAtTheInkwell encounter (3x Stick Goblin)
- Integration tests (CombatIntegrationTests.cs): party creation, enemy spawning, damage/heal, cooldowns, target selection, enemy AI

**Decisions made:**
- Runtime bootstrapper approach avoids needing hand-authored prefabs and complex scene YAML
- All UI created programmatically — no prefab .asset files needed
- Scene file is minimal: just one GameObject with CombatBootstrapper + SO references
- Party uses GDD character names (Ser Sticksworth, Twiggy, Slick, Sister Splinter)

**Next session:** Phase 5.3 polish pass, test in Unity Editor

## Session 6 — March 12, 2026
**Focus:** Runtime bug fixes + Milestone 2 (Phases 6-7)
**What happened:**
- Fixed combat not progressing: CombatUIController.OnEnable timing bug (PR #11)
- Fixed inactive prefab clones (SetActive(true) after Instantiate) (PR #11)
- Removed CombatSceneSetup from bootstrapper (caused double StartCombat) (PR #11)
- Milestone 1 declared complete, started Milestone 2
- Phase 6: Multi-target ability resolution
  - CombatStateMachine.ResolveAbility now gathers all targets for AllEnemies/AllAllies
  - UI auto-submits multi-target abilities without target selection
  - EnemyAI scores AoE abilities higher with more targets
- Phase 6: 3 new enemies + 7 new abilities + 2 new encounters
  - Stick Bandit (Standard): Crossbow Shot + Dagger Slash
  - Stick Spider (Standard): Venomous Bite + Web Spit + Poison Spit (AoE)
  - Stick Necromancer (Elite): Dark Bolt + Life Drain
  - The Bandit Crossing encounter, The Necromancer's Study encounter
- Phase 7: Full item/consumable system
  - ItemSO ScriptableObject (consumable, weapon, armor, accessory)
  - Inventory class with add/remove/stack/events
  - CombatStateMachine.SubmitUseItem resolves heal/damage/status effects
  - ActionPanel shows consumable buttons, TargetPanel supports item targeting
  - 3 item assets: Health Potion, Antidote, Alchemist Fire
  - Combat.unity scene updated with starter items
- Tests: multi-target, AoE AI preference, new enemy data, 10 inventory tests

**Decisions made:**
- Multi-target abilities auto-submit from UI (no target picker needed)
- GatherTargets method centralizes target collection for both abilities and items
- Inventory is shared across party (not per-character)
- Items appear as green-tinted buttons in ActionPanel alongside abilities
- ItemSO GUID collision caught and fixed (was same as CombatBootstrapper)

**PRs:** #11 (bug fixes), Milestone 2 branch in progress

**Next session:** Phase 8 (encounter difficulty, integration testing, polish), then PR + merge

## Session 7 — March 12, 2026
**Focus:** PR #12 review fixes + Grid combat planning
**What happened:**
- Addressed 4 Copilot review comments on PR #12:
  1. Moved inventory setup from OnEnable to HandleCombatStarted (timing bug)
  2. Item effects now apply across all gathered targets (not just single target)
  3. Status-only abilities now check saving throws (Web Spit was 100% stun)
  4. Python generator validates skeleton_guid before writing encounter
- PR #12 merged to master
- Planned grid-based combat system (Phase 9) as 5 incremental PRs:
  - PR A: Grid Data Foundation (GridPosition, CombatGrid, Pathfinder, GridLayoutSO)
  - PR B: Core Combat Integration (CombatUnit position, AbilitySO range, remove CombatRow)
  - PR C: Movement Phase (new combat phase, SubmitPlayerMovement, AI movement)
  - PR D: Grid UI (GridRenderer, tile input, movement highlights)
  - PR E: Content & Cleanup (asset updates, integration tests)
- Updated GDD: combat arena section rewritten for grid, Milestone 2 updated
- Updated all memory files with grid system design
- Deferred Phase 8 (polish/difficulty) until after grid system

**Decisions made:**
- Grid replaces front-row/back-row entirely (CombatRow enum removed)
- Default grid: 8×6 tiles, up to 12×8 for larger encounters
- Movement speed: melee classes 3 tiles, ranged classes 2 tiles
- Ability range: melee 1, ranged 3-6, heals 4-5, AoE unlimited
- 7 terrain types defined (Normal, Difficult, Elevated, Water, Lava, Abyss, Cover) but only Normal implemented initially
- Phase 8 deferred since grid changes encounter design fundamentally

**Next session:** Implement PR A (Grid Data Foundation)

## Session 8 — July 29, 2026
**Focus:** Phase 9 PR A (Grid Data Foundation) + class-roster direction

**What happened:**
- Confirmed direction: FFT-style tactical grid combat (already the Phase 9 plan) + expanded concrete D&D role classes
- Implemented PR A on `feature/grid-data`:
  - `GridPosition` struct (Data) — Manhattan distance, orthogonal neighbors, operators, dictionary-key ready
  - `TerrainType` enum added to GameEnums.cs (7 types; only Normal used initially)
  - `CombatGrid` (Combat) — terrain map, occupancy w/ reverse lookup, walkability, movement cost, `FromLayout` factory
  - `Pathfinder` (Combat) — Dijkstra (terrain-cost aware): reachable-range + shortest path, blocks on occupancy/impassable
  - `GridLayoutSO` (Data) — dimensions, terrain overrides, player/enemy spawns
  - Grid constants added to GameConstants.cs
  - Tests: GridPositionTests, CombatGridTests, PathfinderTests (NUnit)
  - Minimal .meta files generated for all 7 new files
- Tests NOT yet executed in Unity (no CLI runner wired this session) — written, need EditMode run before merge

**Decisions made:**
- Sequencing: grid foundation FIRST, class expansion after PR-A/B (new abilities need range/movement fields grid adds)
- Class roster: full 12-class D&D set — current 4 + Sorcerer, Druid, Barbarian, Ranger, Bard, Monk, Warlock, Paladin (keep "Stick" prefix)
- Sorcerer vs Wizard: metamagic/burst (CHA, single-target nukes + twin/empower) vs INT versatility/AoE control
- Layering: GridLayoutSO stays pure data; CombatGrid.FromLayout does construction (Combat→Data, no cycle)
- Movement is 4-directional (orthogonal), Manhattan distance — matches FFT tactical feel

**Next session:** Run EditMode tests, open PR for `feature/grid-data`, then PR B (Core Combat Integration).

## Session 9 — July 30, 2026
**Focus:** Phase 9 PR B (Core Combat Integration) — Row → grid migration

**What happened:**
- Verified the 2 suspect test failures are PRE-EXISTING (bisected on parent f9bc0e8: both fail without any grid code)
- Implemented PR B on `feature/grid-combat-core`:
  - CombatUnit: removed Row, added `Position` (GridPosition) + `MovementSpeed`
  - AbilitySO: added `_range` (grid tiles, default 1); AoE/self ignore range
  - CharacterClassSO / EnemySO: `_preferredRow` → `_movementSpeed` (default 3)
  - DamageCalculator: added `IsInRange()` + `AttackResult.WasInRange`; removed back-row AC bonus
  - CombatStateMachine: builds CombatGrid (optional GridLayoutSO param, else default 8×6), places units at spawns/default columns, exposes `Grid`
  - EnemyAI: HighestThreat now = peak ability damage; Healers = has-heal-ability (was row-based)
  - Removed CombatRow enum, BACK_ROW_AC_BONUS, EnemySpawn.row
  - Migrated 4 test files (Python script for the 2 big ones); replaced back-row tests with 4 range tests
- EditMode: 145/147 pass (2 failures = the pre-existing unrelated ones)

**Decisions made:**
- Range NOT hard-enforced in PR B — only validated via IsInRange. Enforcement deferred to PR C (movement) + PR D (UI) so melee stays viable before a movement phase exists. Keeps every intermediate build playable.
- Default spawn formation: players left column (x=1), enemies right column (x=width-2), stacked by row; FindFreeTile fallback scan.
- Layering kept clean: GridLayoutSO pure data, CombatGrid.FromLayout does construction.
- 2 pre-existing test bugs (LethalDamage expects Die from TakeDamage; StandardArray expects equal stat totals) left for a separate cleanup PR — out of grid scope.

**Next session:** Commit + push PR B, open PRs A & B, then PR C (Movement Phase) — and begin hard-enforcing ability range.

## Session 10 — July 30, 2026
**Focus:** Phase 9 PR C (Movement Phase) + hard range enforcement

**What happened:**
- Opened PRs #14 (grid-data→master) and #15 (grid-combat-core→grid-data)
- Implemented PR C on `feature/grid-movement`:
  - CombatPhase.PlayerChooseMovement (before PlayerChooseAction)
  - CombatStateMachine: movement step in turn loop; SubmitPlayerMovement(dest) + SkipMovement(); IsReachable / MoveUnitTo helpers; OnMovementPhaseStarted + OnUnitMoved events
  - EnemyAI.DecideMovement: melee closes to adjacency, ranged holds at weapon range via min |dist - desiredRange|; NearestLivingTarget + MaxAbilityRange helpers
  - Enemy turn: move first, then act
  - Turned ON hard range enforcement: SubmitPlayerAction rejects out-of-range single-target (keeps turn), enemy forfeits if unreachable after moving
  - EnemyAIMovementTests (5 tests)
- EditMode: 150/152 (2 pre-existing failures only)

**Decisions / gotchas:**
- Bug caught pre-run: IsInRange null-guarded before the AoE/self check → AoE (null single-target) was wrongly rejected. Reordered so AoE/self short-circuit true first.
- Movement tests assert resulting DISTANCE not exact tile (multiple adjacent tiles tie; Pathfinder list order is non-deterministic).
- CombatStateMachine movement flow (coroutine/MonoBehaviour) not unit-tested in EditMode — covered by DecideMovement unit tests + deferred to PR E integration.

**Next session:** Commit + push PR C, open its PR (base feature/grid-combat-core). Then PR D (Grid UI) — makes the grid visible/playable.

## Session 11 — September 29, 2026
**Focus:** Fix CI, merge Phase 9 PRs A–C, build PR D (Grid UI)

**What happened:**
- CI had never passed. PR #18 fixed three stacked causes: Unity creds not passed (UNITY_EMAIL/PASSWORD), runner out of disk (added free-disk-space), missing `checks: write`. Also fixed/removed the 2 old test bugs → 111/111 green.
- Rebased grid stack onto master, merged #14 → #15 → #16 (retargeted bases to master before each merge).
- PR D on `feature/grid-ui`: GridRenderer + GridCoordinateMapper, MovementPanel (skip), tile-tap movement + tap-to-target, range-filtered targets, out-of-range ability greying, figures placed/animated on grid, CombatGrid.GetPositionsWithinDistance.
- EditMode: 169/169

**Decisions / gotchas:**
- Legacy Input Manager is active (activeInputHandler=0), so grid input uses Input.GetMouseButtonDown (touch maps to mouse 0).
- Grid visuals are SpriteRenderers on a runtime 1×1 white sprite — no art assets needed.
- Items have no range, so item targeting is not range-filtered.

**Next session:** Play-test PR D in the editor, merge, then PR E (grid content).

**Later in session 11:** Merged PR D (#19). Built PR E (`feature/grid-content`): GridLayoutSO assets for all 3 encounters, ranges on 23 single-target abilities (all had silently defaulted to 1 — spells were melee-only), movement speeds on all classes/enemies, EncounterSO.GridLayout, GridContentTests over real assets incl. an enemies-engage simulation. EditMode: 179/179. Phase 9 complete.

## Session 12 — October 8, 2026
**Focus:** Class roster expansion — design + mechanics (PR F)

**What happened:**
- PR E (#20) values corrected to match CONTENT.md (first pass had invented ranges/speeds/grid sizes).
- Drafted the 8 new classes. User feedback: follow 5e on the grid — no all-enemies/all-allies targeting, AoE needs range + shape, area spells hit allies. Party design decided (own character + recruited NPCs, 4 per battle, swap at camp → Milestone 3).
- PR F (`feature/class-mechanics`): sphere/emanation areas with friendly fire + save-for-half, area aiming UI, AI area scoring, status mechanics (Shielded, Blessed, Slowed, Restrained, Raging, Marked), effect ticking moved to turn end, self-effect / lifesteal / multi-hit / twin / empower, converted the 4 old AoE abilities.
- EditMode: 208/208

**Bugs found and fixed:** Cunning Dodge did nothing; Shadow Step made the enemy invisible; 1-turn Stun/Frightened expired before taking effect (ticked at turn start before the check); Life Drain didn't heal; Ray of Frost didn't slow; enemies downed by Burning never died.

- PR G (`feature/classes-casters`): Sticksorcerer, Stickwarlock, Stickdruid, Stickbard + ClassRosterTests. EditMode: 215/215.

**Next session:** PR H (martial).
