# PROJECT STATUS — Stickler's Gate

## Quick Reference
- **Engine:** Unity 6 LTS (6000.3.10f1), C#
- **Platforms:** Android (API 24+) + iOS, Landscape
- **Current Milestone:** 2 — "Full Party Combat"
- **Current Phase:** 6 — Combat Expansion (in progress)
- **Unity Installed:** Yes
- **Repo:** github.com/amihali1/sticklers-gate (private)
- **Unity Project Folder:** SticklersGate/

## Milestone 1 — Complete (PRs #1–#11)

All phases done: Project bootstrap, core data model, combat engine, combat UI, content (4 classes, 3 enemies, 1 encounter), integration + scene wiring, runtime bug fixes.

## Milestone 2 Progress

### Phase 6: Combat Expansion
- [x] 6.1 Multi-target ability resolution (AllEnemies/AllAllies loop in CombatStateMachine)
- [x] 6.2 New enemies: Stick Bandit, Stick Spider, Stick Necromancer (7 new abilities, 3 enemies, 2 encounters)
- [x] 6.3 Enemy AI improvements (multi-target scoring)
- [ ] 6.4 Tests for multi-target + new enemies

### Phase 7: Item & Consumable System
- [x] 7.1 ItemSO data class (weapon, armor, consumable types)
- [x] 7.2 Inventory system (party carries items)
- [x] 7.3 Consumable resolution in combat (use item action)
- [x] 7.4 Item UI in combat action panel + target selection

### Phase 8: Polish & Difficulty
- [ ] 8.1 Encounter difficulty scaling
- [ ] 8.2 Integration testing for Milestone 2 features
- [ ] 8.3 Polish pass

## Next Action
Phase 8: Encounter difficulty scaling, integration testing, polish.

## Known Issues / Decisions Pending
- Input System package not installed (Unity shows deprecation warning for legacy Input Manager)
- .meta files for scripts are minimal (missing MonoImporter section) — Unity accepts them fine
- Life Drain ability doesn't actually heal the necromancer yet (needs code support for "damage + heal self" pattern)

## Session Count: 5
