# ARCHITECTURE — Stickler's Gate

## Tech Stack
- Unity 2D, Built-In Render Pipeline, Latest LTS
- C# with .NET managed by Unity
- TextMeshPro for text, DOTween for animation, Input System for touch
- ScriptableObject-driven data architecture

## Architectural Pattern

Everything is data-driven via ScriptableObjects. Game logic reads from SO assets — adding new classes, enemies, or abilities means creating new assets, not modifying code.

**Core SO types:** CharacterClassSO, AbilitySO, EnemySO, EncounterSO, ItemSO, DialogueTreeSO, QuestSO

**Runtime models:** CombatUnit is a plain C# class created at combat start from SO data. It holds mutable state (current HP, cooldowns, status effects). SOs are never mutated at runtime.

**Event-driven UI:** The CombatStateMachine emits C# events. UI scripts subscribe and react. The state machine never references UI code directly.

## Folder Structure
```
SticklersGate/Assets/_Project/
├── Scripts/
│   ├── Core/         — GameManager, save/load, scene management
│   ├── Combat/       — Turn system, damage calc, status effects
│   ├── Characters/   — Stats, classes, party management
│   ├── UI/           — All UI controllers
│   ├── Dialogue/     — Dialogue runner, choice handling
│   ├── World/        — Map, exploration, encounters
│   ├── Inventory/    — Items, equipment, loot
│   ├── Data/         — ScriptableObject class definitions
│   ├── Utils/        — Dice roller, math helpers, extensions
│   └── Tests/Editor/ — Unit tests
├── Data/{Classes,Abilities,Enemies,Encounters,Items,Dialogue}/
├── Prefabs/{Combat,UI,Characters}/
├── Scenes/
├── Art/{StickFigures,UI,Backgrounds,Effects}/
├── Audio/{Music,SFX,UI}/
└── Fonts/
```

## Coding Conventions

**Naming:**
- Classes: PascalCase — `CombatManager`
- ScriptableObjects: PascalCase + SO — `CharacterClassSO`
- Interfaces: I prefix — `IDamageable`
- Private fields: _camelCase — `_currentHP`
- Public properties: PascalCase — `CurrentHP`
- Constants: UPPER_SNAKE — `MAX_PARTY_SIZE`
- Enums: PascalCase type and values
- Methods: PascalCase
- Parameters/locals: camelCase

**Namespaces:** `SticklersGate.Combat`, `SticklersGate.Data`, `SticklersGate.UI`, `SticklersGate.Utils`

**Rules:**
- One class per file, filename matches class name
- [SerializeField] private over public for inspector fields
- [Header("Section")] to organize inspector
- XML doc comments on all public classes and methods
- MonoBehaviours stay thin — logic goes in plain C# classes
- Composition over inheritance
- C# events for decoupling (not UnityEvents unless inspector wiring needed)
- No singletons except GameManager
- Coroutines for sequenced game events
- Group usings: System → UnityEngine → project namespaces

## Dependencies
| Package | Purpose | Status |
|---|---|---|
| TextMeshPro | Text rendering | Not installed yet |
| Input System | Touch + editor input | Not installed yet |
| DOTween | Animations | Not installed yet |
| Newtonsoft JSON | Save/load (M2) | Not needed yet |
