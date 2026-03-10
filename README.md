# Stickler's Gate

*"Roll for initiative. Pray for mercy."*

A turn-based RPG for Android and iOS, built in Unity (C#). A loving parody of Baldur's Gate featuring stick-figure characters, D&D 5e-inspired mechanics, and a tone that blends genuine stakes with sharp humor.

## Core Pillars

- **Authentic D&D Feel** — d20 rolls, ability scores, AC, saving throws, advantage/disadvantage
- **Genuine Stakes, Funny Delivery** — The world is in real danger. The dialogue is hilarious. Permadeath is real.
- **Stick-Figure Charm** — Minimalist art that's expressive through animation and effects
- **Mobile-First Design** — Landscape, touch-friendly, works in 5-minute bursts or hour-long sessions

## Tech Stack

| Component | Choice |
|-----------|--------|
| Engine | Unity 2D (LTS) |
| Language | C# |
| Platforms | Android (API 24+), iOS (14+) |
| Orientation | Landscape |
| Architecture | ScriptableObject-driven, event-based UI |

## Project Status

**Current Milestone:** 1 — Combat Prototype
**Current Phase:** 0 — Project Bootstrap

See `_memory/PROJECT_STATUS.md` for detailed progress tracking.

## Project Structure

```
sticklers-gate/
├── CLAUDE.md              # Claude Code instructions
├── _docs/                 # Game design document and pipeline tasks
├── _memory/               # Project memory (status, architecture, combat, content)
└── SticklersGate/         # Unity project
    └── Assets/_Project/
        ├── Scripts/        # C# source code
        ├── Data/           # ScriptableObject assets
        ├── Scenes/         # Unity scenes
        ├── Art/            # Sprites, UI, backgrounds, effects
        ├── Audio/          # Music and SFX
        └── Prefabs/        # Reusable prefabs
```

## Classes

| Class | Role | Flavor |
|-------|------|--------|
| Sticknight | Tank / Melee DPS | "Hits things. Gets hit. Hits things harder." |
| Stickromancer | Ranged AoE DPS | "Glass cannon. Emphasis on glass." |
| Stickrogue | Burst DPS / Utility | "Steals your gold. Then your heart. Then your gold again." |
| Stickeric | Healer / Support | "Prays for you. Judges you. Heals you anyway." |

## Documentation

- **Game Design Document:** `_docs/Sticklers_Gate_GDD.md`
- **Architecture & Conventions:** `_memory/ARCHITECTURE.md`
- **Combat System Design:** `_memory/COMBAT_SYSTEM.md`
- **Content Definitions:** `_memory/CONTENT.md`
