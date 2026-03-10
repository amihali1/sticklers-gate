# COMBAT SYSTEM — Stickler's Gate

## D&D 5e Rules (Simplified)

**What we keep:**
- d20 attack rolls vs Armor Class
- Ability score modifiers: (score - 10) / 2 rounded down
- Initiative: d20 + DEX modifier
- Damage dice per weapon/spell
- Advantage/Disadvantage: roll 2d20, take best/worst
- Crit on nat 20 (double damage dice, not modifier)
- Crit fail on nat 1 (Stick Break: lose next turn + comedy)
- Saving throws: d20 + modifier vs DC
- Death saves: 3 successes = stabilize, 3 fails = death

**What we simplified:**
- No movement grid — front row / back row only
- No spell slots — cooldown system (recharge after X turns)
- 1 Action + 1 Bonus Action per turn
- No concentration
- Proficiency baked into class stats
- No multiclassing

**Custom mechanics:**
- Stick Break on nat 1 (lose next turn, comic animation)
- Narrator interruptions on notable rolls
- Plot Armor: main characters don't permadeath in story mode (return with 1 HP after combat)

## Combat Flow
```
Setup → Roll Initiative → [Turn Loop: TurnStart → Choose Action → Choose Target → Resolve → TurnEnd → Check End] → Victory/Defeat
```
Enemy turns use AI decision instead of player input.

## Data Models

### AbilitySO (ScriptableObject)
Defines any combat action. Key fields: name, category (MeleeAttack/RangedAttack/Spell/Heal/Buff/Debuff), target type, damage dice (count + sides), damage type, ability score modifier, cooldown turns, heal dice, status effect application, narrator quips for hit/miss/crit.

**Status:** Not implemented yet

### CharacterClassSO (ScriptableObject)
Defines a playable class. Key fields: class name/type, base stats (HP, AC, 6 ability scores), hit dice sides, abilities at levels 1/3/5, sprites, flavor text, preferred combat row. Helper methods: GetMaxHPAtLevel(), GetAbilitiesAtLevel(), GetModifier().

**Status:** Not implemented yet

### EnemySO (ScriptableObject)
Defines an enemy type. Key fields: name, tier (Minion/Standard/Elite/Boss), stats, abilities, AI behavior (aggression, target preference, retreat threshold), rewards (XP, gold range, loot), sprites, narrator quips.

**Status:** Not implemented yet

### EncounterSO (ScriptableObject)
Defines a combat encounter. Key fields: enemy lineup (EnemySpawn structs with SO ref + row + name override), difficulty, victory condition (DefeatAll/DefeatBoss/SurviveTurns), background, narrator intro/victory/defeat lines.

**Status:** Not implemented yet

### CombatUnit (Runtime Class, not SO)
Mutable runtime state of a combatant. Created from CharacterClassSO or EnemySO at combat start. Fields: identity, copied stats, current HP, initiative, row, alive/conscious flags, death save counters, runtime abilities with cooldown tracking, active status effects. Events: OnDamageTaken, OnHealed, OnDeath, OnStatusEffectApplied/Removed. Factory methods: FromPlayerCharacter(), FromEnemy().

**Status:** Not implemented yet

### DamageCalculator (Static Class)
ResolveAttack(): d20 + modifier vs AC → hit/miss/crit → roll damage → apply. ResolveHeal(): roll heal dice → apply. ResolveSavingThrow(): d20 + modifier vs DC. Returns result structs (AttackResult, HealResult, SaveResult) with full roll details for UI display.

**Status:** Not implemented yet

### CombatStateMachine (MonoBehaviour)
Central controller. Manages turn order queue, current phase, action selection. Emits events for UI: OnCombatStarted, OnTurnStarted, OnPlayerActionNeeded, OnActionResolved, OnUnitDied, OnNarratorQuip, OnCombatEnded, etc. Never references UI directly.

**Status:** Not implemented yet

### EnemyAI (Static Class)
Priority logic: self-preservation (flee if low HP) → heal self → select target by preference (LowestHP/HighestThreat/Random/Healers) → pick strongest off-cooldown ability. 20% random suboptimal choice for flavor.

**Status:** Not implemented yet

### DiceRoller (Static Utility)
Roll(sides), RollMultiple(count, sides), RollD20(), RollWithAdvantage/Disadvantage(), IsCriticalHit/Fail(), GetModifier(). Stores last 20 rolls in history for combat log.

**Status:** Not implemented yet

## Enums Needed
- AbilityScore: STR, DEX, CON, INT, WIS, CHA
- DamageType: Slashing, Piercing, Bludgeoning, Fire, Cold, Lightning, Radiant, Necrotic, Psychic
- AbilityTargetType: SingleEnemy, SingleAlly, AllEnemies, AllAllies, Self
- AbilityCategory: MeleeAttack, RangedAttack, Spell, Heal, Buff, Debuff
- StatusEffect: Poisoned, Stunned, Blessed, Shielded, Burning, Frozen, Frightened, Invisible
- CombatPhase: Setup, RollInitiative, TurnStart, PlayerChooseAction, PlayerChooseTarget, ResolveAction, TurnEnd, CheckEndCondition, Victory, Defeat
- CharacterClassType: Sticknight, Stickromancer, Stickrogue, Stickeric
- CombatRow: FrontRow, BackRow

**Status:** Not implemented yet

## Constants
BASE_AC=10, CRIT_THRESHOLD=20, CRIT_FAIL_THRESHOLD=1, MAX_PARTY_SIZE=4, MAX_LEVEL=5, DEATH_SAVE_SUCCESS_NEEDED=3, DEATH_SAVE_FAIL_NEEDED=3
