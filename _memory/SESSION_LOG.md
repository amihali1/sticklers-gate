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
- 4 classes: Sticknight, Stickwizard, Stickrogue, Stickeric
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
