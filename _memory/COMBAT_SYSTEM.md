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
- Enemies die immediately when downed (no death saves)
- Advantage from Invisible, disadvantage from Frightened/Poisoned
- Burning: 1d6 fire damage at turn start

## Combat Flow
```
Setup → Roll Initiative → [Turn Loop: TurnStart → Choose Action → Choose Target → Resolve → TurnEnd → Check End] → Victory/Defeat
```
Enemy turns use AI decision instead of player input.
Player turns pause coroutine loop, resume via SubmitPlayerAction().

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
- **CombatStateMachine** — MonoBehaviour. Coroutine-based turn loop. Events for UI: OnCombatStarted, OnTurnStarted, OnPlayerActionNeeded, OnAttackResolved, OnHealResolved, OnUnitDowned, OnUnitDied, OnDeathSaveResult, OnNarratorQuip, OnInitiativeRolled, OnCombatResult, OnPhaseChanged, OnCombatEnded.

### Utilities (Scripts/Utils/)
- **DiceRoller** — Static. Roll(), RollMultiple(), RollD20(), RollWithAdvantage/Disadvantage(), IsCriticalHit/Fail(), GetModifier(). History of last 20 rolls.

## Enums (Scripts/Data/GameEnums.cs) — Implemented
AbilityScore, DamageType, AbilityTargetType, AbilityCategory, StatusEffectType (includes None), CombatPhase, CharacterClassType, CombatRow, EnemyTier, AITargetPreference, VictoryCondition

## Constants (Scripts/Data/GameConstants.cs) — Implemented
BASE_AC=10, CRIT_THRESHOLD=20, CRIT_FAIL_THRESHOLD=1, MAX_PARTY_SIZE=4, MAX_LEVEL=5, DEATH_SAVE_SUCCESS/FAIL_NEEDED=3, BACK_ROW_AC_BONUS=2, DEFEND_AC_BONUS=2, FLEE_DC=12, XP_THRESHOLDS[]
