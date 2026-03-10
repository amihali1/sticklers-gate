# Stickler's Gate — Session Memory System

## How It Works

You have a folder called `_memory/` in your project repo. It contains markdown files that represent the current state of the project's design and progress. When you start a new Claude session, upload the files relevant to your task. At the end of the session, Claude gives you updated files to save back.

## The Files

```
StickersGate/
└── _memory/
    ├── README.md              ← You're reading this
    ├── PROJECT_STATUS.md      ← Current progress, what's done, what's next
    ├── ARCHITECTURE.md        ← Tech decisions, patterns, conventions
    ├── COMBAT_SYSTEM.md       ← Combat design: rules, data models, state machine
    ├── CONTENT.md             ← Classes, enemies, encounters, narrator quips
    └── SESSION_LOG.md         ← Brief log of what was done each session
```

## Which Files to Upload Per Task

| What you're working on         | Upload these files                          |
|--------------------------------|---------------------------------------------|
| Any session (always)           | PROJECT_STATUS.md                           |
| ScriptableObjects / data model | ARCHITECTURE.md + COMBAT_SYSTEM.md          |
| Combat logic / state machine   | ARCHITECTURE.md + COMBAT_SYSTEM.md          |
| UI work                        | ARCHITECTURE.md + COMBAT_SYSTEM.md          |
| Defining classes / enemies     | COMBAT_SYSTEM.md + CONTENT.md               |
| Narrator / dialogue writing    | CONTENT.md                                  |
| Starting a brand new milestone | All files                                   |

**Rule of thumb:** Always upload PROJECT_STATUS.md. Then add 1-2 others based on the task. You rarely need all of them at once.

## Session Workflow

1. **Start:** Upload the relevant memory files + any Unity scripts you need help with
2. **Tell Claude:** "Here's my project context. Today I want to work on [X]."
3. **Work:** Build the thing together
4. **End:** Claude gives you updated memory files. Save them back to `_memory/`.

## Keeping Files Small

Each memory file is designed to stay under ~2000 words. If a file gets too long, split it. The goal is to give Claude the minimum context needed — not the entire project history.

When a design decision is finalized and implemented, it moves from detailed spec to a brief summary. For example, once DiceRoller.cs is written and tested, COMBAT_SYSTEM.md doesn't need the full method signatures anymore — just "DiceRoller: done, see Scripts/Utils/DiceRoller.cs".
