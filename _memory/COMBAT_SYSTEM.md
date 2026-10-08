# COMBAT SYSTEM — Stickler's Gate

## D&D 5e Rules (Simplified)

**What we keep:**
- d20 attack rolls vs Armor Class
- Ability score modifiers: floor((score - 10) / 2) — uses Mathf.FloorToInt for correct negative rounding
- Initiative: d20 + DEX modifier
- Damage dice per weapon/spell
- Advantage/Disadvantage: roll 2d20, take best/worst
- Crit on nat 20 (double damage dice, not modifier)
- Crit fail on nat 1 (Stick Break: lose next turn + comedy)
- Saving throws: d20 + modifier vs DC (nat 20 always succeeds, nat 1 always fails)
- Death saves: 3 successes = stabilize, 3 fails = death, nat 20 = revive with 1 HP, nat 1 = two failures

**What we simplified:**
- Grid movement with per-unit speed (replaces D&D's feet-based movement)
- No spell slots — cooldown system (recharge after X turns)
- 1 Move + 1 Action per turn (no bonus actions)
- No concentration
- Proficiency baked into class stats
- No multiclassing

**Custom mechanics:**
- Stick Break on nat 1 (lose next turn, comic animation)
- Narrator interruptions on notable rolls
- Plot Armor: main characters don't permadeath in story mode (return with 1 HP after combat)
- Enemies die immediately when downed (no death saves)
- Advantage from Invisible, disadvantage from Frightened/Poisoned
- Burning: 1d6 fire damage at turn start

## Combat Flow
```
Setup → Roll Initiative → [Turn Loop: TurnStart → (move ↔ move …) → Action → (move …) → TurnEnd → Check End] → Victory/Defeat
```
**Split movement (5e):** each turn `ResetMovement()` fills `RemainingMovement` from `EffectiveMovement`. In `PlayerChooseAction` the player can tap blue tiles any number of times (`SubmitPlayerMovement` spends the path cost from `Pathfinder.GetReachableCosts`), then act. After the action, if movement remains the phase becomes `PlayerChooseMovement` (blue tiles + End Turn button); running out of movement or `EndTurn()` ends the turn. The player turn loop waits for `TurnEnd`, so an action fully resolves before the next unit goes. Enemy AI still makes one move, then acts.

## Grid System (Phase 9 — complete)

**Implemented so far:** GridPosition, TerrainType, CombatGrid, Pathfinder, GridLayoutSO (PR A); CombatUnit.Position/MovementSpeed, AbilitySO.Range, CharacterClassSO/EnemySO.MovementSpeed, DamageCalculator.IsInRange, CombatStateMachine grid setup + unit placement, CombatRow fully removed (PR B); PlayerChooseMovement phase + SubmitPlayerMovement/SkipMovement, EnemyAI.DecideMovement (melee closes / ranged kites), OnMovementPhaseStarted + OnUnitMoved events, **hard range enforcement now ON** (PR C). GridRenderer + GridCoordinateMapper, tile-tap movement, MovementPanel skip, range-filtered targeting and out-of-range ability greying (PR D). Per-encounter GridLayoutSO assets, ability ranges, movement speeds, EncounterSO.GridLayout (PR E).

**Turn flow now:** TurnStart → PlayerChooseMovement (or AI DecideMovement) → PlayerChooseAction → PlayerChooseTarget → ResolveAction → TurnEnd. Player movement: pick a reachable tile (Pathfinder within MovementSpeed) or skip. Out-of-range single-target actions are rejected (player keeps turn); enemies forfeit the attack if still unreachable after moving. AoE/self ignore range.


**Grid:** CombatGrid holds width×height terrain map + occupancy. Default 8×6 for small encounters, up to 12×8.

**Movement:** Each unit has a movement speed (tiles per turn). BFS pathfinding calculates reachable tiles. Movement happens before action each turn. Movement is optional (can skip).

**Range:** Each ability has a range in tiles. Melee = 1, ranged = 3-6, heals = 4-5, self = 0. AoE abilities ignore range (hit all valid targets).

**Terrain types:** Normal, Difficult (2× movement cost), Elevated (+1 AC, +1 ranged range), Water (2× cost, halve fire dmg), Lava (2d6 fire on enter/turn start), Abyss (impassable, fall = death), Cover (+2 AC vs ranged). Only Normal used in initial encounters.

**Key classes:**
- GridPosition — struct (int x, int y), with Manhattan distance + equality
- CombatGrid — manages terrain, occupancy, bounds checking
- Pathfinder — BFS for movement range, A*/BFS for shortest path
- GridLayoutSO — per-encounter grid dimensions, terrain map, spawn points
- GridRenderer — UI: draws tiles, highlights movement/ability range

## Areas of Effect (Phase 10 PR F)
5e templates on the grid, Manhattan distance like movement/range. No ability uses AllEnemies/AllAllies any more (items only; GridContentTests enforce it).
- **Sphere** (`TargetType.Point`): player taps a tile within range to preview, taps again to cast. Covers center + radius. Can catch the caster.
- **Emanation** (`TargetType.Self`): around the user. Harmful ones exclude the user; ally-only auras include them.
- **AreaAffects:** AllCreatures (default — friendly fire, 5e), AlliesOnly, EnemiesOnly.
- **Damage:** `_saveForHalfDamage` → target saves vs DC; full on fail, half on success, no attack roll, no ability mod to damage; status effect only on a failed save. Otherwise one attack roll per creature.
- **Enemy AI:** scores areas by foes caught minus friends caught; skips areas worth ≤ 0; aims spheres at the best cluster.
- Logic in `AreaTargeting` (pure, tested); `CombatStateMachine.SubmitPlayerAreaAction(ability, tile)`.

## Status Effects (Phase 10 PR F)
Durations count the affected unit's turns and tick at the **end** of its turn (5e "until the end of its next turn"); an effect a unit puts on itself doesn't lose a turn on the turn it's cast. Cooldowns still tick at turn start.
| Effect | Mechanic |
|---|---|
| Stunned | Skips its turn |
| Burning | 1d6 fire at turn start |
| Poisoned / Frightened | Disadvantage on attacks |
| Invisible | Advantage on attacks; attackers have disadvantage; attacking ends it |
| Shielded | +4 AC |
| Blessed | +1d4 to attack rolls and saving throws |
| Slowed | −2 movement |
| Restrained | 0 movement; attacks against it have advantage, its attacks disadvantage |
| Raging | +2 melee damage; half slashing/piercing/bludgeoning damage taken |
| Marked | The unit that marked it deals +1d6 on attacks against it |

**Ability extras:** `_selfEffect` (applied to the user after resolving), `_lifestealPercent`, `_hitCount` (separate attack rolls), `_extraTargets` (Twinned: more enemies within range, nearest the primary first), `_maximizeDamage` (Empowered: max on every die).

## Data Models — All Implemented

### ScriptableObjects (Scripts/Data/)
- **StatBlock** — Serializable struct, six ability scores, GetScore(), GetModifier(). Throws on invalid AbilityScore.
- **AbilitySO** — Combat action definition. Damage dice, heal dice, flat bonuses, status effect, cooldown, narrator quips. Helper properties: DealsDamage, Heals, AppliesStatusEffect, HasCooldown.
- **CharacterClassSO** — Class definition. Base stats, AC, hit dice, abilities at levels 1/3/5, sprites. GetMaxHPAtLevel() (clamped 1–MAX_LEVEL), GetAbilitiesAtLevel() (null-safe).
- **EnemySO** — Enemy definition. Stats, abilities, AI params (aggression, target pref, retreat threshold), rewards, sprites, narrator quips.
- **EncounterSO** — Encounter config. EnemySpawn[] (SO ref + row + name override), victory condition, narrator lines.

### Runtime Classes (Scripts/Combat/)
- **CombatUnit** — Mutable runtime state from SOs. Factory methods: FromPlayerCharacter(), FromEnemy(). Manages HP, death saves, status effects, ability cooldowns, BonusAC. Events: OnDamageTaken, OnHealed, OnDeath, OnStatusEffectApplied/Removed.
- **RuntimeAbility** — Tracks cooldown state of an AbilitySO at runtime.
- **ActiveStatusEffect** — Tracks remaining duration of a status effect.
- **DamageCalculator** — Static. ResolveAttack(), ResolveHeal(), ResolveSavingThrow(). Returns AttackResult/HealResult/SaveResult structs.
- **EnemyAI** — Static. Decide() returns AIDecision (ability + target). Priority: self-heal if low → pick target by preference → pick strongest ability. 20% random suboptimal.
- **CombatStateMachine** — MonoBehaviour. Coroutine-based turn loop. SubmitPlayerAction() + SubmitDefend(). Events for UI: OnCombatStarted, OnTurnStarted, OnPlayerActionNeeded, OnAttackResolved, OnHealResolved, OnUnitDowned, OnUnitDied, OnDeathSaveResult, OnNarratorQuip, OnInitiativeRolled, OnCombatResult, OnPhaseChanged, OnCombatEnded.

### UI Classes (Scripts/UI/) — Implemented
- **CombatUIController** — Main controller. Subscribes to CombatStateMachine events, coordinates all sub-components. Handles ability→target→submit flow.
- **ActionPanel** — Shows ability buttons + defend button for player turns. Events: OnAbilitySelected, OnDefendSelected.
- **TargetPanel** — Target selection after ability pick. Static GetValidTargets() for filtering by AbilityTargetType. Events: OnTargetSelected, OnCancelled.
- **UnitHPBar** — HP bar with fill color (healthy/wounded/critical), name label, HP text. Binds to CombatUnit events.
- **TurnOrderDisplay** — Horizontal list of unit names showing initiative order, highlights active unit.
- **NarratorDisplay** — Top-of-screen text overlay with typewriter animation and message queue. Configurable speed/duration/fade.
- **DamagePopup** — Floating text that rises and fades. Self-destructs. Used for damage numbers, heal amounts, miss/crit text.
- **CombatResultPanel** — Victory/defeat screen with title, subtitle, colored background.
- **StickFigureRenderer** — Programmatic stick figures via LineRenderer. Poses: Idle, Attack, Hit, Death, Defend, Cast. Color-coded by class (player) or tier (enemy). Circle head drawn with line segments.
- **CombatSceneSetup** — Scene bootstrapper. Creates player party from SO data, spawns stick figures at position transforms, starts encounter.

### Utilities (Scripts/Utils/)
- **DiceRoller** — Static. Roll(), RollMultiple(), RollD20(), RollWithAdvantage/Disadvantage(), IsCriticalHit/Fail(), GetModifier(). History of last 20 rolls.

## Enums (Scripts/Data/GameEnums.cs) — Implemented
AbilityScore, DamageType, AbilityTargetType, AbilityCategory, StatusEffectType (includes None), CombatPhase, CharacterClassType, TerrainType, EnemyTier, AITargetPreference, VictoryCondition, ItemType, ItemRarity

*CombatRow REMOVED (PR B). TerrainType ADDED (PR A). CombatPhase will still gain PlayerChooseMovement in PR C.

## Constants (Scripts/Data/GameConstants.cs) — Implemented
BASE_AC=10, CRIT_THRESHOLD=20, CRIT_FAIL_THRESHOLD=1, MAX_PARTY_SIZE=4, MAX_LEVEL=5, DEATH_SAVE_SUCCESS/FAIL_NEEDED=3, DEFEND_AC_BONUS=2, FLEE_DC=12, XP_THRESHOLDS[]

Grid constants (PR A): DEFAULT_GRID_WIDTH=8, DEFAULT_GRID_HEIGHT=6, MAX_GRID_WIDTH=12, MAX_GRID_HEIGHT=8, DEFAULT_MELEE_RANGE=1, DEFAULT_RANGED_RANGE=4, DEFAULT_MELEE_MOVEMENT=3, DEFAULT_RANGED_MOVEMENT=2, NORMAL/DIFFICULT_MOVEMENT_COST=1/2, ELEVATED/COVER_AC_BONUS, ELEVATED_RANGE_BONUS. BACK_ROW_AC_BONUS REMOVED (PR B).
