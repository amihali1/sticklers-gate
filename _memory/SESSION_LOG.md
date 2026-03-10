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
