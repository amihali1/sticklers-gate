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
- 4 classes: Sticknight, Stickromancer, Stickrogue, Stickeric
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
