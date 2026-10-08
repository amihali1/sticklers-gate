# Stickler's Gate — Claude Code Instructions

## Project Overview
Turn-based RPG for Android/iOS built in Unity 2D (C#). D&D 5e-inspired mechanics, stick-figure art, sarcastic narrator. Parody of Baldur's Gate.

## Memory System
Project memory lives in `_memory/`. Always read `_memory/PROJECT_STATUS.md` at the start of a session to understand current progress. Consult other memory files as needed:
- `ARCHITECTURE.md` — tech decisions, folder structure, coding conventions
- `COMBAT_SYSTEM.md` — combat rules, data models, state machine design
- `CONTENT.md` — classes, enemies, encounters, narrator quips, story
- `SESSION_LOG.md` — brief log of each session's work

**Memory must be kept in sync with every PR:**
- **Start of session:** Read `PROJECT_STATUS.md` to understand current state.
- **Each PR:** Include memory file updates in the PR commits. Update `PROJECT_STATUS.md` (check off tasks, update next action), and update any other relevant memory files (ARCHITECTURE, COMBAT_SYSTEM, CONTENT) when designs are implemented or changed.
- **End of session:** Update `SESSION_LOG.md` with a summary of what was done.

## Game Design Document
Full GDD is at `_docs/Sticklers_Gate_GDD.md`. Refer to it for detailed design specs.

## Coding Conventions (C# / Unity)
- **Namespaces:** `SticklersGate.Combat`, `SticklersGate.Data`, `SticklersGate.UI`, `SticklersGate.Utils`
- **Naming:** PascalCase classes/methods/properties, _camelCase private fields, UPPER_SNAKE constants, PascalCase enums
- **ScriptableObjects:** PascalCase + SO suffix (e.g., `CharacterClassSO`)
- **Interfaces:** I prefix (e.g., `IDamageable`)
- **One class per file**, filename matches class name
- **`[SerializeField] private`** over public for inspector fields
- **XML doc comments** on all public classes and methods
- **MonoBehaviours stay thin** — logic in plain C# classes
- **Composition over inheritance**
- **C# events** for decoupling (not UnityEvents unless inspector wiring needed)
- **No singletons** except GameManager
- **Using order:** System → UnityEngine → project namespaces

## Testing
- Write unit tests for all testable code (static utilities, pure logic classes, data models).
- Tests live in `SticklersGate/Assets/_Project/Scripts/Tests/Editor/` using NUnit and the Unity Test Framework.
- Test assembly: `SticklersGate.Tests.Editor` (references `SticklersGate` main assembly).
- Include tests in the same PR as the code they cover — don't defer testing to a separate PR.
- Use `ScriptableObject.CreateInstance<T>()` and reflection to set `[SerializeField]` fields in tests.

## Architecture
- ScriptableObject-driven data layer — no magic numbers in code
- Runtime models are plain C# classes (e.g., `CombatUnit`), SOs are never mutated at runtime
- Event-driven UI — state machine emits events, UI subscribes
- Command pattern for combat actions

## Unity Project Structure
```
SticklersGate/Assets/_Project/
├── Scripts/{Core,Combat,Characters,UI,Dialogue,World,Inventory,Data,Utils,Tests}
├── Data/{Classes,Abilities,Enemies,Encounters,Items,Dialogue}
├── Prefabs/{Combat,UI,Characters}
├── Scenes/
├── Art/{StickFigures,UI,Backgrounds,Effects}
├── Audio/{Music,SFX,UI}
└── Fonts/
```

## Git Workflow
- **Never commit directly to master.** Always create a feature branch and open a PR.
- Branch naming: `feature/<short-description>`, `fix/<short-description>`, or `docs/<short-description>`
- Keep branches focused — one feature or fix per branch.
- Write clear PR titles and descriptions summarizing what changed and why.
- After the PR is merged, delete the feature branch.

## README
Keep `README.md` up to date as the project evolves. When making changes that affect any of the following, update the corresponding section in the README:
- Project status or current milestone/phase
- Project structure (new folders, reorganization)
- Tech stack changes or new dependencies
- New features, systems, or classes added

## Key Reminders
- Target platforms: Android (API 24+), iOS (14+), landscape only
- TextMeshPro for all text, DOTween for animations, new Input System for touch
- Simplified D&D 5e: cooldowns instead of spell slots; tactical grid movement (Phase 9) replaced front/back row
- Classes (full 12-class D&D roster, Phase 10): Sticknight, Stickwizard, Stickrogue, Stickleric, Sticksorcerer, Stickwarlock, Stickdruid, Stickbard; Stickbarbarian, Stickmonk, Stickranger, Stickpaladin in progress
