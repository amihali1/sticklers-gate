# STICKLER'S GATE — Agent Pipeline Task Breakdown
## Milestone 1: "First Blood" (Combat Prototype)

---

## How This Document Works

Each task is a plain-text prompt you feed to the appropriate agent role. Tasks are grouped into **phases** that must be completed in order. Within a phase, tasks can run in parallel where noted.

**Memory Keys** are suggested labels for your agent memory storage so downstream tasks can pull the right context.

**Role Legend:**
- **DESIGNER** → Makes architectural decisions, defines data structures, writes specs
- **ENGINEER** → Writes C# code, creates Unity assets, implements systems
- **TESTER** → Writes test cases, validates behavior, reports bugs
- **REVIEWER** → Reviews code quality, architecture consistency, suggests improvements

---

## PHASE 0: Project Bootstrap
*Get Unity project created and configured. This phase is mostly manual or engineer-only.*

---

### TASK 0.1 — Unity Project Setup
**Role:** ENGINEER
**Depends on:** Nothing
**Store to memory:** `project-structure`, `unity-settings`

```
Create a new Unity project called "StickersGate" using the 2D Built-In Render Pipeline template.

Configure the following project settings:
- Player Settings > Default Orientation: Landscape Left
- Player Settings > Company Name: [your studio name]
- Player Settings > Product Name: Stickler's Gate
- Player Settings > Target Platforms: Android (API 24+), iOS (14.0+)
- Quality Settings: Remove "Ultra" and "Very High" — mobile doesn't need them
- Set target frame rate to 60 FPS in a startup script

Install these packages via Unity Package Manager:
- TextMeshPro (should be included by default)
- Input System (com.unity.inputsystem)
- Install DOTween from the Asset Store (free version)

Create the following folder structure under Assets/_Project/:
  Scripts/Core/
  Scripts/Combat/
  Scripts/Characters/
  Scripts/UI/
  Scripts/Dialogue/
  Scripts/World/
  Scripts/Inventory/
  Scripts/Data/
  Scripts/Utils/
  Data/Classes/
  Data/Abilities/
  Data/Enemies/
  Data/Encounters/
  Data/Items/
  Data/Dialogue/
  Prefabs/Combat/
  Prefabs/UI/
  Prefabs/Characters/
  Scenes/
  Art/StickFigures/
  Art/UI/
  Art/Backgrounds/
  Art/Effects/
  Audio/Music/
  Audio/SFX/
  Fonts/

Create two scenes:
- MainMenu (empty for now)
- Combat (this is where Milestone 1 lives)

Store the final folder structure and any project settings notes to memory as "project-structure" and "unity-settings".
```

---

### TASK 0.2 — Coding Conventions Document
**Role:** DESIGNER
**Depends on:** Nothing
**Store to memory:** `coding-conventions`

```
Write a coding conventions document for the Stickler's Gate Unity project. This will be stored in agent memory and referenced by all future engineering and review tasks.

The project uses C# in Unity. The team architecture is: designer, engineer, tester, reviewer.

Define conventions for:

1. Naming:
   - Classes: PascalCase (e.g., CombatManager, DiceRoller)
   - ScriptableObjects: PascalCase with SO suffix (e.g., CharacterClassSO)
   - Interfaces: PascalCase with I prefix (e.g., IDamageable)
   - Private fields: _camelCase with underscore prefix
   - Public properties: PascalCase
   - Constants: UPPER_SNAKE_CASE
   - Enums: PascalCase for type, PascalCase for values
   - Methods: PascalCase
   - Parameters: camelCase
   - Local variables: camelCase

2. Architecture rules:
   - ScriptableObjects for all game data (stats, abilities, enemies, encounters)
   - MonoBehaviours only for things that live in scenes
   - Prefer composition over inheritance
   - Use events (C# events or UnityEvents) for decoupling systems
   - No singletons except GameManager — use dependency injection or service locator pattern
   - Keep MonoBehaviours thin — logic goes in plain C# classes

3. File rules:
   - One class per file
   - File name matches class name exactly
   - Every public class and method gets an XML doc comment
   - Group usings: System, then UnityEngine, then project namespaces
   - Use the namespace StickersGate.[SystemName] (e.g., StickersGate.Combat)

4. Unity-specific:
   - Use [SerializeField] private over public fields for inspector exposure
   - Use [Header("Section")] to organize inspector fields
   - Use #region sparingly — only for large files
   - Coroutines for sequenced game events, async/await for I/O only

Store the complete conventions document to memory as "coding-conventions".
```

---

## PHASE 1: Core Data Model
*Define all the ScriptableObject types and enums that everything else builds on. No gameplay logic yet — just data structures.*

---

### TASK 1.1 — Game Enums and Constants
**Role:** DESIGNER → ENGINEER
**Depends on:** `coding-conventions`
**Store to memory:** `game-enums`

**DESIGNER prompt:**
```
Design the core enums and constants for Stickler's Gate. This is a turn-based RPG using simplified D&D 5e rules.

Define the following enums:

1. AbilityScore — STR, DEX, CON, INT, WIS, CHA

2. DamageType — Slashing, Piercing, Bludgeoning, Fire, Cold, Lightning, Radiant, Necrotic, Psychic

3. AbilityTargetType — SingleEnemy, SingleAlly, AllEnemies, AllAllies, Self

4. AbilityCategory — MeleeAttack, RangedAttack, Spell, Heal, Buff, Debuff

5. StatusEffect — Poisoned, Stunned, Blessed, Shielded, Burning, Frozen, Frightened, Invisible

6. CombatPhase — Setup, RollInitiative, TurnStart, PlayerChooseAction, PlayerChooseTarget, ResolveAction, TurnEnd, CheckEndCondition, Victory, Defeat

7. CharacterClassType — Sticknight, Stickwizard, Stickrogue, Stickleric

8. CombatRow — FrontRow, BackRow (for simplified positioning)

Define a static constants class with:
- BASE_AC = 10
- CRIT_THRESHOLD = 20 (natural 20)
- CRIT_FAIL_THRESHOLD = 1 (natural 1)
- MAX_PARTY_SIZE = 4
- MAX_LEVEL = 5 (for MVP)
- DEATH_SAVE_SUCCESS_NEEDED = 3
- DEATH_SAVE_FAIL_NEEDED = 3

Output the full enum definitions and constants class as C# code. Follow the coding conventions in memory at "coding-conventions".

Store the finalized design to memory as "game-enums".
```

**ENGINEER prompt:**
```
Retrieve "game-enums" and "coding-conventions" from memory.

Create the following files in the Unity project:

1. Assets/_Project/Scripts/Data/GameEnums.cs
   - Contains all the enums defined in "game-enums"
   - Namespace: StickersGate.Data

2. Assets/_Project/Scripts/Data/GameConstants.cs
   - Static class with all constants from "game-enums"
   - Namespace: StickersGate.Data

Follow coding conventions exactly. Add XML doc comments to every enum and constant.

Store file paths and any notes to memory as "game-enums-files".
```

---

### TASK 1.2 — Ability ScriptableObject
**Role:** DESIGNER → ENGINEER → REVIEWER
**Depends on:** `game-enums`, `coding-conventions`
**Store to memory:** `ability-so-design`, `ability-so-file`

**DESIGNER prompt:**
```
Design the AbilitySO ScriptableObject for Stickler's Gate. This represents any action a character or enemy can use in combat: attacks, spells, heals, buffs.

Retrieve "game-enums" from memory for the available enums.

The AbilitySO needs these fields:

Identity:
- abilityName (string) — display name like "Greatsword Slash"
- description (string) — tooltip text
- icon (Sprite) — UI icon (nullable for now)
- category (AbilityCategory enum)

Targeting:
- targetType (AbilityTargetType enum)
- range (CombatRow) — which row this can target from

Mechanics:
- damageDiceCount (int) — number of dice to roll (e.g., 2 for 2d6)
- damageDiceSides (int) — sides per die (e.g., 6 for 2d6)
- damageType (DamageType enum)
- abilityScoreModifier (AbilityScore enum) — which stat adds to damage
- flatDamageBonus (int) — added on top of dice + modifier
- attackBonusStat (AbilityScore enum) — which stat adds to attack roll

Cooldown:
- cooldownTurns (int) — 0 means no cooldown (usable every turn)
- currentCooldown should NOT be on the SO (that's runtime state)

Healing (for heal abilities):
- healDiceCount (int)
- healDiceSides (int)
- flatHealBonus (int)
- isHeal (bool) — true if this restores HP instead of dealing damage

Status Effects:
- appliesStatus (StatusEffect enum, nullable/none)
- statusDuration (int) — turns the status lasts
- statusSaveDC (int) — DC for the saving throw to resist
- statusSaveStat (AbilityScore enum) — which stat the target rolls to resist

Flavor:
- narratorHitQuips (string[]) — random narrator lines on hit
- narratorMissQuips (string[]) — random narrator lines on miss
- narratorCritQuips (string[]) — random narrator lines on critical hit

Include a method: string GetDamageString() that returns a formatted string like "2d6 + 3 Fire"

Output the full ScriptableObject C# class. Follow "coding-conventions" from memory.

Store design to memory as "ability-so-design".
```

**ENGINEER prompt:**
```
Retrieve "ability-so-design" and "coding-conventions" from memory.

Create the file:
Assets/_Project/Scripts/Data/AbilitySO.cs

Implement the ScriptableObject exactly as specified in "ability-so-design".
- Use [CreateAssetMenu(fileName = "NewAbility", menuName = "StickersGate/Ability")] attribute
- Namespace: StickersGate.Data
- All inspector fields should use [SerializeField] private with public get properties
- Group fields with [Header("...")] attributes matching the design sections
- Implement GetDamageString() method
- Add [Tooltip("...")] to fields that benefit from editor hints

Store file path to memory as "ability-so-file".
```

**REVIEWER prompt:**
```
Retrieve "ability-so-design", "ability-so-file", "coding-conventions", and "game-enums" from memory.

Review the AbilitySO implementation for:
1. Does it match the design spec exactly?
2. Does it follow coding conventions?
3. Are there any Unity-specific issues (serialization problems, missing attributes)?
4. Is the CreateAssetMenu attribute correct?
5. Are the XML doc comments present and accurate?
6. Does GetDamageString() handle edge cases (0 dice, heal abilities)?

Provide feedback. If changes are needed, specify exact code corrections.
```

---

### TASK 1.3 — CharacterClass ScriptableObject
**Role:** DESIGNER → ENGINEER → REVIEWER
**Depends on:** `game-enums`, `ability-so-design`, `coding-conventions`
**Store to memory:** `class-so-design`, `class-so-file`

**DESIGNER prompt:**
```
Design the CharacterClassSO ScriptableObject for Stickler's Gate. This defines a playable class like Sticknight (Fighter) or Stickwizard (Wizard).

Retrieve "game-enums" and "ability-so-design" from memory.

Fields needed:

Identity:
- className (string) — e.g., "Sticknight"
- classType (CharacterClassType enum)
- classDescription (string) — flavor text
- classEmoji (string) — for UI flair
- classColor (Color) — UI accent color for this class

Base Stats (at level 1):
- baseHP (int) — starting max HP
- hitDiceSides (int) — HP gained per level (e.g., 10 for d10)
- baseAC (int) — starting armor class
- strength (int), dexterity (int), constitution (int), intelligence (int), wisdom (int), charisma (int)

Abilities:
- basicAbilities (AbilitySO[]) — available from level 1 (2 abilities)
- signatureAbility (AbilitySO) — unlocked at level 3
- ultimateAbility (AbilitySO) — unlocked at level 5

Progression:
- hpPerLevel (int[]) — HP gained at each level (index 0 = level 2 gain)
- statBoosts — which stats increase at which levels (define a simple serializable struct for this)

Visual:
- idleSprite (Sprite)
- attackSprite (Sprite)
- hurtSprite (Sprite)
- deathSprite (Sprite)

Flavor:
- flavorQuote (string) — tagline like "Hits things. Gets hit. Hits things harder."
- preferredRow (CombatRow) — where this class naturally stands

Include a helper method: int GetMaxHPAtLevel(int level) that calculates total HP for a given level.
Include a helper method: List<AbilitySO> GetAbilitiesAtLevel(int level) that returns available abilities based on level.
Include a helper method: int GetModifier(AbilityScore score) that returns the D&D modifier for a given ability score.

Output the full class design. Store to memory as "class-so-design".
```

**ENGINEER prompt:**
```
Retrieve "class-so-design" and "coding-conventions" from memory.

Create the file:
Assets/_Project/Scripts/Data/CharacterClassSO.cs

Implement exactly as specified. Use [CreateAssetMenu(fileName = "NewClass", menuName = "StickersGate/CharacterClass")].
Namespace: StickersGate.Data

For stat boosts, create a small serializable struct:
[System.Serializable]
public struct LevelStatBoost
{
    public int level;
    public AbilityScore stat;
    public int amount;
}

Store file path to memory as "class-so-file".
```

---

### TASK 1.4 — Enemy ScriptableObject
**Role:** DESIGNER → ENGINEER
**Depends on:** `game-enums`, `ability-so-design`, `coding-conventions`
**Store to memory:** `enemy-so-design`, `enemy-so-file`

**DESIGNER prompt:**
```
Design the EnemySO ScriptableObject for Stickler's Gate. This defines an enemy type like Goblin, Skeleton, or Ogre.

Retrieve "game-enums" and "ability-so-design" from memory.

Fields needed:

Identity:
- enemyName (string)
- description (string) — flavor description
- enemyTier (enum: Minion, Standard, Elite, Boss)

Stats:
- maxHP (int)
- armorClass (int)
- strength, dexterity, constitution, intelligence, wisdom, charisma (all int)

Combat:
- abilities (AbilitySO[]) — what this enemy can do
- preferredRow (CombatRow)
- challengeRating (float) — for encounter balancing

AI Behavior:
- aiAggression (float 0-1) — how likely to attack vs defend
- aiTargetPreference (enum: LowestHP, HighestThreat, Random, Healers)
- retreatThreshold (float 0-1) — HP percentage where enemy tries to flee
- canFlee (bool)

Rewards:
- xpReward (int)
- goldReward (int) — range min/max would be better
- goldRewardMin (int)
- goldRewardMax (int)
- lootTable (placeholder — just a string[] of item names for now)

Visual:
- idleSprite, attackSprite, hurtSprite, deathSprite (all Sprite)

Flavor:
- introQuip (string) — narrator says this when enemy appears
- deathQuip (string) — narrator says this when enemy dies

Output the full design. Store to memory as "enemy-so-design".
```

**ENGINEER prompt:**
```
Retrieve "enemy-so-design" and "coding-conventions" from memory.

Create the file:
Assets/_Project/Scripts/Data/EnemySO.cs

Use [CreateAssetMenu(fileName = "NewEnemy", menuName = "StickersGate/Enemy")].
Namespace: StickersGate.Data

Also create:
Assets/_Project/Scripts/Data/EnemyEnums.cs
- EnemyTier enum: Minion, Standard, Elite, Boss
- AITargetPreference enum: LowestHP, HighestThreat, Random, Healers

Store file paths to memory as "enemy-so-file".
```

---

### TASK 1.5 — Encounter ScriptableObject
**Role:** DESIGNER → ENGINEER
**Depends on:** `enemy-so-design`, `coding-conventions`
**Store to memory:** `encounter-so-design`, `encounter-so-file`

**DESIGNER prompt:**
```
Design the EncounterSO ScriptableObject. This defines a specific combat encounter — which enemies appear, battlefield setup, and win/loss conditions.

Retrieve "enemy-so-design" from memory.

Fields:

Setup:
- encounterName (string)
- encounterDescription (string) — narrator intro text
- difficulty (enum: Easy, Medium, Hard, Deadly — maps to D&D encounter difficulty)

Enemies:
- enemyLineup (EnemySpawn[]) — array of structs, each containing:
  - enemySO (EnemySO reference)
  - row (CombatRow)
  - nameOverride (string, optional — e.g., "Goblin Chief" instead of just "Goblin")

Environment:
- backgroundSprite (Sprite) — battlefield background
- ambientTrack (AudioClip, nullable)

Conditions:
- victoryCondition (enum: DefeatAllEnemies, DefeatBoss, SurviveTurns)
- survivalTurns (int) — only used if victoryCondition is SurviveTurns
- isBossFight (bool)

Rewards:
- bonusXP (int) — on top of individual enemy XP
- bonusGold (int)

Narrator:
- introNarration (string[]) — lines the narrator says before combat starts
- victoryNarration (string[])
- defeatNarration (string[])

Output the full design. Store as "encounter-so-design".
```

---

## PHASE 2: Combat Engine (Core Logic)
*The math and state management behind combat. No UI yet — pure logic.*

---

### TASK 2.1 — Dice Roller Utility
**Role:** ENGINEER → TESTER
**Depends on:** `coding-conventions`
**Store to memory:** `dice-roller-file`

**ENGINEER prompt:**
```
Retrieve "coding-conventions" from memory.

Create the file:
Assets/_Project/Scripts/Utils/DiceRoller.cs

This is a static utility class for all dice rolling in the game. Namespace: StickersGate.Utils

Implement:
- static int Roll(int sides) — roll a single die (d4, d6, d8, d10, d12, d20)
- static int RollMultiple(int count, int sides) — roll multiple dice and sum them (e.g., 2d6)
- static (int total, int[] rolls) RollMultipleDetailed(int count, int sides) — same but returns individual rolls for display
- static int RollD20() — shortcut for the most common roll
- static (int result, bool hadAdvantage) RollWithAdvantage() — roll 2d20, take highest
- static (int result, bool hadDisadvantage) RollWithDisadvantage() — roll 2d20, take lowest
- static bool IsCriticalHit(int d20Roll) — checks if natural 20
- static bool IsCriticalFail(int d20Roll) — checks if natural 1
- static int GetModifier(int abilityScore) — returns D&D modifier: (score - 10) / 2 rounded down

Also include:
- A static RollHistory list that stores the last 20 rolls (struct with: dice sides, result, timestamp, label string)
- A method to get the last N rolls for a combat log display

Use UnityEngine.Random for the RNG (not System.Random) so it works in Unity.

Store file path to memory as "dice-roller-file".
```

**TESTER prompt:**
```
Retrieve "dice-roller-file" from memory.

Write a Unity Test (EditMode test) for DiceRoller:
Assets/_Project/Scripts/Tests/Editor/DiceRollerTests.cs

Test cases:
1. Roll(6) always returns between 1-6 (run 1000 times, assert range)
2. Roll(20) always returns between 1-20
3. RollMultiple(2, 6) always returns between 2-12
4. RollMultipleDetailed(3, 8) returns exactly 3 individual rolls, each 1-8
5. GetModifier(10) returns 0
6. GetModifier(16) returns 3
7. GetModifier(8) returns -1
8. GetModifier(1) returns -5
9. IsCriticalHit(20) returns true, IsCriticalHit(19) returns false
10. IsCriticalFail(1) returns true, IsCriticalFail(2) returns false
11. RollHistory stores rolls and doesn't exceed 20 entries

Store test file path to memory as "dice-roller-tests".
```

---

### TASK 2.2 — Combat Unit (Runtime Character Model)
**Role:** DESIGNER → ENGINEER
**Depends on:** `class-so-design`, `enemy-so-design`, `game-enums`, `coding-conventions`
**Store to memory:** `combat-unit-design`, `combat-unit-file`

**DESIGNER prompt:**
```
Design the CombatUnit class. This is the RUNTIME representation of a combatant during combat — NOT a ScriptableObject. It holds the live, mutable state of a character or enemy during a fight.

Retrieve "class-so-design", "enemy-so-design", and "game-enums" from memory.

This is a plain C# class (not a MonoBehaviour). It gets created when combat starts by reading from either a CharacterClassSO (for player characters) or an EnemySO (for enemies).

Fields:

Identity:
- unitName (string)
- isPlayerCharacter (bool)
- level (int) — only relevant for player characters
- classType (CharacterClassType, nullable — null for enemies)

Stats (copied from SO at combat start):
- maxHP, currentHP
- armorClass
- strength, dexterity, constitution, intelligence, wisdom, charisma

Combat State:
- initiative (int) — rolled at combat start
- currentRow (CombatRow)
- isAlive (bool)
- isConscious (bool) — false when at 0 HP but not dead yet
- deathSaveSuccesses (int)
- deathSaveFailures (int)

Abilities:
- abilities (list of runtime ability references with cooldown tracking)
- Define a small class: RuntimeAbility { AbilitySO data; int currentCooldown; bool IsReady }

Status Effects:
- activeStatusEffects (list of ActiveStatusEffect structs: StatusEffect type, int remainingTurns)

Methods:
- TakeDamage(int amount) — reduces HP, handles going to 0
- Heal(int amount) — restores HP, caps at maxHP
- RollInitiative() — d20 + DEX modifier
- GetAbilityModifier(AbilityScore score) — returns modifier for a given stat
- TickCooldowns() — reduce all ability cooldowns by 1
- TickStatusEffects() — reduce durations, remove expired effects
- ApplyStatusEffect(StatusEffect effect, int duration)
- HasStatusEffect(StatusEffect effect) — check if affected
- RollDeathSave() — d20, 10+ is success, track successes/failures

Factory methods (static):
- static CombatUnit FromPlayerCharacter(CharacterClassSO classSO, string name, int level)
- static CombatUnit FromEnemy(EnemySO enemySO, string nameOverride = null)

Events (C# events):
- OnDamageTaken(int amount, DamageType type)
- OnHealed(int amount)
- OnDeath()
- OnStatusEffectApplied(StatusEffect effect)
- OnStatusEffectRemoved(StatusEffect effect)

Store design to memory as "combat-unit-design".
```

---

### TASK 2.3 — Damage Calculator
**Role:** DESIGNER → ENGINEER → TESTER
**Depends on:** `combat-unit-design`, `ability-so-design`, `dice-roller-file`, `game-enums`
**Store to memory:** `damage-calc-design`, `damage-calc-file`

**DESIGNER prompt:**
```
Design the DamageCalculator static class. This handles all attack resolution math.

Retrieve "combat-unit-design", "ability-so-design", and "game-enums" from memory.

This is a static utility class. Namespace: StickersGate.Combat

Methods:

1. AttackResult ResolveAttack(CombatUnit attacker, CombatUnit target, AbilitySO ability)
   - Roll d20 + attacker's attack stat modifier
   - Compare to target AC
   - On hit: roll damage dice + modifier
   - On crit: double the damage dice (not the modifier)
   - On critical fail: return special result for Stick Break
   - Apply damage to target
   - Return an AttackResult struct

2. HealResult ResolveHeal(CombatUnit healer, CombatUnit target, AbilitySO ability)
   - Roll healing dice + bonus
   - Apply healing to target
   - Return a HealResult struct

3. SaveResult ResolveSavingThrow(CombatUnit target, AbilityScore saveStat, int dc)
   - Roll d20 + target's save stat modifier
   - Compare to DC
   - Return success/failure

Define these result structs:

AttackResult:
- attackRoll (int) — the d20 result
- totalAttackValue (int) — roll + modifiers
- targetAC (int)
- isHit (bool)
- isCriticalHit (bool)
- isCriticalFail (bool) — triggers Stick Break
- damageRoll (int) — total damage dealt
- damageType (DamageType)
- individualDice (int[]) — each die result for display
- attackerName (string)
- targetName (string)

HealResult:
- healAmount (int)
- individualDice (int[])
- healerName (string)
- targetName (string)
- targetNewHP (int)
- targetMaxHP (int)

SaveResult:
- saveRoll (int)
- totalSaveValue (int)
- dc (int)
- succeeded (bool)
- saveStat (AbilityScore)

Store design to memory as "damage-calc-design".
```

**TESTER prompt:**
```
Retrieve "damage-calc-design" and "damage-calc-file" from memory.

Write tests for DamageCalculator:
Assets/_Project/Scripts/Tests/Editor/DamageCalculatorTests.cs

Test cases:
1. Attack with roll >= AC results in isHit = true
2. Attack with roll < AC results in isHit = false
3. Natural 20 always hits regardless of AC and isCriticalHit = true
4. Natural 1 always misses regardless of modifiers and isCriticalFail = true
5. Critical hit doubles damage dice count but not modifier
6. Heal cannot exceed target's maxHP
7. Saving throw with roll + modifier >= DC succeeds
8. Saving throw with roll + modifier < DC fails
9. Damage reduces target's currentHP correctly
10. Damage that reduces HP to 0 sets isConscious = false

Since dice are random, use a test approach where you either:
- Run many iterations and verify statistical bounds
- Or create a mock/injectable RNG for deterministic testing (preferred)

Note: If the engineer implemented DiceRoller with UnityEngine.Random, suggest refactoring to accept an optional System.Func<int,int> roller parameter for testability.

Store test file to memory as "damage-calc-tests".
```

---

### TASK 2.4 — Combat State Machine
**Role:** DESIGNER → ENGINEER → REVIEWER
**Depends on:** `combat-unit-design`, `damage-calc-design`, `encounter-so-design`, `game-enums`
**Store to memory:** `state-machine-design`, `state-machine-file`

**DESIGNER prompt:**
```
Design the CombatStateMachine — the central controller for all combat flow.

Retrieve "combat-unit-design", "damage-calc-design", "encounter-so-design", and "game-enums" from memory.

This is a MonoBehaviour that lives in the Combat scene. Namespace: StickersGate.Combat

State Flow:
  Setup → RollInitiative → TurnStart → PlayerChooseAction → PlayerChooseTarget → ResolveAction → TurnEnd → CheckEndCondition → (loop back to TurnStart OR Victory/Defeat)

For enemy turns, PlayerChooseAction/Target is replaced with AI decision.

Core Fields:
- currentPhase (CombatPhase enum)
- allUnits (List<CombatUnit>) — all combatants
- turnOrder (Queue<CombatUnit>) — sorted by initiative
- currentUnit (CombatUnit) — whose turn it is
- selectedAbility (AbilitySO) — what the current unit wants to do
- selectedTarget (CombatUnit) — who they're targeting
- turnNumber (int)
- encounterData (EncounterSO) — the current encounter definition

Core Methods:
- void StartCombat(EncounterSO encounter, List<CombatUnit> party)
  → Creates enemy CombatUnits from encounter data
  → Rolls initiative for everyone
  → Sorts turn order
  → Transitions to first turn

- void BeginTurn()
  → Dequeues next unit
  → If dead/unconscious, skip to next
  → Tick cooldowns and status effects
  → If player: transition to PlayerChooseAction, emit event for UI
  → If enemy: call AI decision maker

- void OnPlayerActionSelected(AbilitySO ability)
  → Store selected ability
  → Transition to PlayerChooseTarget (or skip if self-target)

- void OnPlayerTargetSelected(CombatUnit target)
  → Store target
  → Call ResolveAction

- void ResolveAction()
  → Use DamageCalculator to resolve the attack/heal/ability
  → Emit result events (for UI to animate)
  → Transition to TurnEnd

- void EndTurn()
  → Check if any units died this turn
  → Transition to CheckEndCondition

- void CheckEndCondition()
  → All enemies dead? → Victory
  → All party dead? → Defeat
  → Otherwise → BeginTurn with next unit

Events (C# events for UI to subscribe to):
- OnCombatStarted(List<CombatUnit> party, List<CombatUnit> enemies)
- OnInitiativeRolled(List<(CombatUnit unit, int roll)> results)
- OnTurnStarted(CombatUnit activeUnit)
- OnPlayerActionNeeded(CombatUnit activeUnit, List<RuntimeAbility> availableAbilities)
- OnPlayerTargetNeeded(CombatUnit activeUnit, AbilitySO ability, List<CombatUnit> validTargets)
- OnActionResolved(AttackResult or HealResult — use a base CombatResult class)
- OnUnitDamaged(CombatUnit unit, int damage)
- OnUnitHealed(CombatUnit unit, int amount)
- OnUnitDied(CombatUnit unit)
- OnCriticalFail(CombatUnit unit) — triggers Stick Break animation
- OnNarratorQuip(string message)
- OnCombatEnded(bool victory)

This class should NOT know about UI. It only emits events. The UI subscribes and reacts.

Store the complete design to memory as "state-machine-design".
```

---

### TASK 2.5 — Enemy AI
**Role:** DESIGNER → ENGINEER
**Depends on:** `combat-unit-design`, `enemy-so-design`, `state-machine-design`
**Store to memory:** `enemy-ai-design`, `enemy-ai-file`

**DESIGNER prompt:**
```
Design the EnemyAI static class that decides what an enemy does on its turn.

Retrieve "combat-unit-design", "enemy-so-design", and "state-machine-design" from memory.

This is a static utility class. Namespace: StickersGate.Combat

Method:
- static (AbilitySO chosenAbility, CombatUnit chosenTarget) DecideAction(CombatUnit enemy, List<CombatUnit> allAllies, List<CombatUnit> allEnemies, EnemySO enemyData)

Decision logic (priority order):

1. SELF-PRESERVATION: If enemy HP < retreatThreshold AND canFlee → attempt flee (50% chance, DEX save)
2. HEALING: If enemy has a heal ability, it's off cooldown, AND own HP < 40% → heal self
3. TARGET SELECTION based on aiTargetPreference:
   - LowestHP: pick the alive player character with lowest current HP
   - HighestThreat: pick the player character that dealt most damage last round (needs a simple damage tracker)
   - Healers: prioritize Stickleric class, then lowest HP
   - Random: pick any alive player character at random
4. ABILITY SELECTION: Pick the highest-damage ability that's off cooldown. If all on cooldown, use basic attack (index 0, which should always have 0 cooldown).
5. AGGRESSION MODIFIER: Roll a random float. If it's > aiAggression, pick a weaker/safer ability instead of the strongest one. This adds unpredictability.

Add some randomness — enemies shouldn't be perfectly optimal. Maybe 20% of the time they make a suboptimal choice for flavor.

Store design to memory as "enemy-ai-design".
```

---

## PHASE 3: Combat UI
*Visual layer that subscribes to combat events and renders everything.*

---

### TASK 3.1 — Combat Scene Layout
**Role:** DESIGNER → ENGINEER
**Depends on:** `state-machine-design`, `project-structure`
**Store to memory:** `combat-layout-design`

**DESIGNER prompt:**
```
Design the visual layout of the combat screen for Stickler's Gate. This is a landscape mobile game.

The layout should feel like a simplified Baldur's Gate combat screen, but with stick figures on a parchment/notebook paper background.

Screen Layout (landscape, 16:9):

TOP STRIP (10% height):
- Initiative order bar — shows small circular portraits of all combatants in turn order
- Active combatant is highlighted/enlarged
- Dead units are crossed out

MAIN AREA (60% height):
- Left side: Party stick figures (up to 4), arranged in their combat rows
- Right side: Enemy stick figures, arranged in their combat rows
- Background: Simple line-drawn environment (from EncounterSO)
- Floating damage/heal numbers appear here
- Dice roll animation appears center-screen when rolling

NARRATOR BAR (10% height):
- Horizontal text strip between battlefield and action menu
- Typewriter-style text for narrator quips
- Parchment/scroll aesthetic

BOTTOM PANEL (20% height):
- Current unit info: name, HP bar, class icon
- Action buttons: [Attack] [Abilities] [Items] [Defend] [Flee]
- When "Abilities" is tapped, buttons swap to show ability list
- Target selection: when choosing a target, enemies/allies get tap targets (highlighted circles)

Color Palette:
- Background: warm parchment (#f4e4c1) with faint ruled lines
- UI panels: darker parchment (#d4c4a1) with sketchy borders
- Text: dark ink (#2a2a2a)
- HP bars: green (#4caf50) → yellow (#ffc107) → red (#f44336)
- Damage numbers: red (#e63946)
- Heal numbers: green (#2a9d8f)
- Critical hit: gold (#ffd700) with larger font
- Critical fail: purple (#7b2cbf)

Font: Something that looks handwritten — like "Indie Flower", "Amatic SC", or "Patrick Hand" from Google Fonts

Store to memory as "combat-layout-design".
```

---

### TASK 3.2 — Combat UI Controller
**Role:** ENGINEER
**Depends on:** `combat-layout-design`, `state-machine-design`, `coding-conventions`
**Store to memory:** `combat-ui-file`

**ENGINEER prompt:**
```
Retrieve "combat-layout-design", "state-machine-design", and "coding-conventions" from memory.

Create the following UI controller scripts:

1. Assets/_Project/Scripts/UI/CombatUIManager.cs
   - MonoBehaviour that subscribes to all CombatStateMachine events
   - References to all UI elements (assigned in inspector)
   - Routes events to the appropriate UI sub-components
   - Handles button clicks and forwards player choices back to CombatStateMachine

2. Assets/_Project/Scripts/UI/InitiativeBarUI.cs
   - Displays turn order as horizontal list of unit icons
   - Highlights active unit
   - Marks dead units

3. Assets/_Project/Scripts/UI/ActionMenuUI.cs
   - Shows action buttons during player turn
   - Swaps to ability list when "Abilities" tapped
   - Grays out abilities on cooldown
   - Hides during enemy turns

4. Assets/_Project/Scripts/UI/HealthBarUI.cs
   - Reusable component for any unit's health bar
   - Animated smooth HP changes using DOTween
   - Color transitions based on HP percentage

5. Assets/_Project/Scripts/UI/FloatingTextUI.cs
   - Spawns floating damage/heal numbers above units
   - Different colors/sizes for normal hit, crit, miss, heal
   - Floats upward and fades out

6. Assets/_Project/Scripts/UI/NarratorUI.cs
   - Displays narrator text with typewriter effect
   - Queue system for multiple messages
   - Auto-advances after delay

7. Assets/_Project/Scripts/UI/DiceRollUI.cs
   - Visual dice roll animation
   - Shows the d20 spinning and landing on result
   - Displays modifier breakdown: "14 + 3 (STR) = 17 vs AC 13 — HIT!"

All UI scripts should:
- Use TextMeshPro for all text
- Use [SerializeField] for inspector references
- Not contain game logic — only display logic
- Use DOTween for animations

Namespace: StickersGate.UI

Store all file paths to memory as "combat-ui-file".
```

---

### TASK 3.3 — Stick Figure Sprites
**Role:** DESIGNER → ENGINEER
**Depends on:** `combat-layout-design`
**Store to memory:** `sprite-design`

**DESIGNER prompt:**
```
Design the Phase 1 stick figure sprite specifications for Stickler's Gate.

These are intentionally simple — circle head, line body, line limbs. Each class should be instantly recognizable by a simple accessory.

Define sprites needed for each of the 4 classes:

STICKNIGHT (Fighter):
- Distinguishing feature: small rectangle on left arm (shield) + line with crossguard (sword)
- Idle: standing straight, shield forward
- Attack: sword arm swung forward, lunging pose
- Hurt: leaning backward, lines for impact
- Death: fallen flat, X eyes

STICKWIZARD (Wizard):
- Distinguishing feature: triangle hat (wizard hat) + wavy lines at hand (magic)
- Idle: standing with staff (long line with circle top)
- Attack: both arms forward, star burst at hands
- Hurt: hat flying off slightly
- Death: crumpled with hat over body

STICKROGUE (Rogue):
- Distinguishing feature: hood (half circle over head) + small triangles (daggers)
- Idle: slight crouch, daggers at sides
- Attack: lunging forward with crossed daggers
- Hurt: spinning backward
- Death: dramatic sprawl

STICKLERIC (Cleric):
- Distinguishing feature: halo (circle above head) + plus/cross symbol on body
- Idle: hands together in prayer pose
- Attack: arms raised, radiating lines
- Hurt: halo tilting
- Death: halo floating above fallen body

ENEMIES:
- Goblin: shorter stick figure, pointy triangle ears, hunched
- Skeleton: stick figure but with visible ribcage lines and no circle head (skull shape)
- Ogre: larger/thicker stick figure, club (thick line)

Each sprite should be:
- Black lines on transparent background
- Approximately 128x128 pixels
- Simple enough to draw programmatically if needed (as an alternative to pre-drawn sprites)

Store to memory as "sprite-design".
```

**ENGINEER prompt:**
```
Retrieve "sprite-design" from memory.

For Milestone 1, we'll generate stick figures programmatically using Unity's LineRenderer or a simple runtime texture drawing approach. This avoids needing an artist for Phase 1.

Create:
Assets/_Project/Scripts/Characters/StickFigureRenderer.cs

This MonoBehaviour draws a stick figure using Unity's LineRenderer (or GL lines, or UI Image manipulation — pick the approach that's simplest for 2D).

Approach: Use a set of LineRenderers as children of a parent GameObject. Each body part (head, torso, left arm, right arm, left leg, right leg) is a separate LineRenderer.

Include:
- Method to set pose (idle, attack, hurt, death) by adjusting line positions
- Method to set class accessories (add shield, hat, daggers, halo based on ClassType)
- Method to set color tint (for enemy differentiation)
- Simple animation support: lerp between poses over time using DOTween

Create a prefab structure:
- StickFigure (parent)
  - Head (LineRenderer, circle approximated with many points)
  - Body (LineRenderer)
  - LeftArm (LineRenderer)
  - RightArm (LineRenderer)
  - LeftLeg (LineRenderer)
  - RightLeg (LineRenderer)
  - Accessory (LineRenderer, class-specific)

Store file path to memory as "stick-figure-renderer".
```

---

### TASK 3.4 — Narrator System
**Role:** DESIGNER → ENGINEER
**Depends on:** `coding-conventions`
**Store to memory:** `narrator-design`, `narrator-file`

**DESIGNER prompt:**
```
Design the narrator quip system for Stickler's Gate. The narrator is a Stanley Parable-style omniscient voice that comments on combat events.

Write 5-8 quips for EACH of these event types:

CRITICAL HIT quips (when a player crits):
- e.g., "Oh, BEAUTIFULLY done. I felt that from here."

CRITICAL FAIL quips (when anyone rolls a 1):
- e.g., "A natural 1. You know, some would call that... statistically inevitable."

PLAYER MISS quips:
- e.g., "You swung with conviction. Conviction, sadly, does not add to your attack roll."

ENEMY MISS quips:
- e.g., "The goblin swings wildly and misses. Even by goblin standards, embarrassing."

KILL quips (when player kills an enemy):
- e.g., "And just like that, a stick figure returns to the great sketchpad in the sky."

OVERKILL quips (when damage greatly exceeds remaining HP):
- e.g., "That was... excessive. It was already dead after the first 20 damage."

PLAYER DEATH quips (when a party member goes down):
- e.g., "Your friend collapses. Dramatically. They practiced this."

HEAL quips:
- e.g., "The Stickleric waves their hands and somehow biology works again."

COMBAT START quips:
- e.g., "Roll for initiative! Or, as I like to call it, roll for who gets to suffer first."

VICTORY quips:
- e.g., "Against all odds — and my personal expectations — you've won."

DEFEAT quips:
- e.g., "Well. That happened. Shall we pretend it didn't and try again?"

LOW HP WARNING quips (when a party member drops below 25% HP):
- e.g., "Your fighter is looking a bit... minimalist. Even for a stick figure."

Make the humor consistent: dry, slightly condescending, but ultimately rooting for the player. The narrator has opinions and isn't afraid to share them.

Store all quips to memory as "narrator-quips".
```

---

## PHASE 4: Content Creation
*Define the actual classes, enemies, and test encounter using the systems we built.*

---

### TASK 4.1 — Define the Four Classes (ScriptableObject Data)
**Role:** DESIGNER
**Depends on:** `class-so-design`, `ability-so-design`, `game-enums`
**Store to memory:** `class-definitions`

**DESIGNER prompt:**
```
Define the complete stats and abilities for all 4 Stickler's Gate classes at level 1. These will become ScriptableObject asset data in Unity.

Retrieve "class-so-design", "ability-so-design", and "game-enums" from memory.

For each class, provide all field values that the CharacterClassSO and AbilitySO need. Balance them so that a party of all 4 can reasonably defeat the test encounter (3 goblins).

Use D&D 5e as a reference but simplify. Standard array for stats: 16, 14, 13, 12, 10, 8 distributed differently per class.

Define all abilities with exact dice, damage types, cooldowns, and quips.

Classes to define:
1. Sticknight (Fighter) — tanky melee, high HP, high AC, STR-based
2. Stickwizard (Wizard) — glass cannon, low HP, INT-based spells
3. Stickrogue (Rogue) — burst damage, DEX-based, sneak attack mechanic
4. Stickleric (Cleric) — healer/support, WIS-based, can also deal radiant damage

For each class provide:
- All stat values
- Base HP and AC
- Hit dice
- 2 basic abilities with complete AbilitySO field values
- Preferred combat row
- Flavor quote

Store the complete definitions to memory as "class-definitions".
```

---

### TASK 4.2 — Define Enemies and Test Encounter
**Role:** DESIGNER
**Depends on:** `enemy-so-design`, `encounter-so-design`, `class-definitions`
**Store to memory:** `enemy-definitions`, `test-encounter`

**DESIGNER prompt:**
```
Define 3 enemy types and 1 test encounter for Milestone 1 of Stickler's Gate.

Retrieve "enemy-so-design", "encounter-so-design", and "class-definitions" from memory.

ENEMIES:

1. Stick Goblin (Minion tier)
   - Weak, dies fast, attacks recklessly
   - Low HP, low AC, decent DEX
   - One ability: "Rusty Shiv" — 1d4 + DEX slashing
   - AI: Random targeting, high aggression, no fleeing
   - XP: 25, Gold: 1-5
   - Intro quip, death quip

2. Stick Skeleton (Standard tier)
   - Medium stats, resistant to piercing (flavor only for now)
   - Two abilities: "Bone Club" (1d6 + STR bludgeoning), "Bone Throw" (1d4 + DEX ranged)
   - AI: LowestHP targeting, medium aggression
   - XP: 50, Gold: 3-8

3. Stick Ogre (Elite tier)
   - High HP, high STR, low DEX, low INT
   - Two abilities: "Greatclub" (2d8 + STR bludgeoning), "Ground Slam" (1d10 + STR, hits all front row)
   - AI: HighestThreat targeting, high aggression, no fleeing
   - XP: 200, Gold: 15-30

TEST ENCOUNTER: "Ambush at the Inkwell"
- 3 Stick Goblins (all front row)
- Difficulty: Easy
- Victory condition: Defeat all enemies
- Background: a spilled inkwell in a notebook margin
- Narrator intro: 2-3 funny lines about goblins being "barely drawn"
- Victory narration: congratulatory but backhanded
- Defeat narration: sympathetic but amused

Balance check: A level 1 party of all 4 classes should win this fight roughly 80% of the time with reasonable play.

Store to memory as "enemy-definitions" and "test-encounter".
```

---

## PHASE 5: Integration & Testing
*Wire everything together into a playable combat encounter.*

---

### TASK 5.1 — Combat Scene Wiring
**Role:** ENGINEER
**Depends on:** All Phase 1-4 tasks
**Store to memory:** `combat-scene-setup`

**ENGINEER prompt:**
```
Retrieve "state-machine-design", "combat-ui-file", "combat-layout-design", "stick-figure-renderer", "class-definitions", "enemy-definitions", and "test-encounter" from memory.

Create the Combat scene setup:

1. Create a GameBootstrapper.cs (MonoBehaviour) that:
   - Creates 4 player CombatUnits (one of each class, level 1)
   - Loads the test EncounterSO
   - Passes both to CombatStateMachine.StartCombat()
   - This is temporary — will be replaced by proper game flow later

2. Set up the Combat scene hierarchy:
   - Canvas (Screen Space - Camera, landscape 1920x1080 reference)
     - InitiativeBar (top)
     - BattlefieldArea (center)
       - PartyContainer (left)
       - EnemyContainer (right)
     - NarratorBar (middle)
     - ActionPanel (bottom)
   - CombatManager (empty GO with CombatStateMachine)
   - GameBootstrapper (empty GO)

3. Wire all UI references in CombatUIManager
4. Create the EncounterSO asset for "Ambush at the Inkwell"
5. Create all CharacterClassSO assets
6. Create all AbilitySO assets
7. Create all EnemySO assets

Document the exact scene hierarchy and component assignments.
Store to memory as "combat-scene-setup".
```

---

### TASK 5.2 — Full Integration Test
**Role:** TESTER
**Depends on:** `combat-scene-setup`
**Store to memory:** `integration-test-results`

**TESTER prompt:**
```
Retrieve "combat-scene-setup", "state-machine-design", and "test-encounter" from memory.

Write a comprehensive test plan for the Milestone 1 combat prototype. This covers both automated tests and manual play-testing steps.

AUTOMATED TESTS (EditMode):
1. CombatUnit creation from CharacterClassSO produces correct stats
2. CombatUnit creation from EnemySO produces correct stats
3. Initiative sorting produces correct turn order
4. Combat ends in Victory when all enemies reach 0 HP
5. Combat ends in Defeat when all party members reach 0 HP
6. Death saving throws stabilize at 3 successes
7. Death saving throws kill at 3 failures
8. Abilities go on cooldown after use and tick down correctly
9. Status effects expire after correct number of turns

MANUAL PLAYTEST CHECKLIST:
1. [ ] Combat starts and initiative is rolled visibly
2. [ ] Turn order bar shows correct sequence
3. [ ] Player can tap Attack and select an enemy target
4. [ ] Dice roll animation plays and shows result
5. [ ] Damage is calculated correctly (verify with manual math)
6. [ ] Health bars animate smoothly
7. [ ] Floating damage numbers appear
8. [ ] Enemy turns execute automatically with visible actions
9. [ ] Narrator quips appear for hits, misses, crits, kills
10. [ ] Critical hit on nat 20 doubles dice (verify damage)
11. [ ] Critical fail on nat 1 triggers Stick Break flavor
12. [ ] All 4 class abilities work correctly
13. [ ] Cooldown abilities gray out and count down
14. [ ] Combat ends with victory screen after killing all goblins
15. [ ] Combat ends with defeat screen if all party members die
16. [ ] Touch input works correctly (no accidental taps)
17. [ ] UI is readable and properly laid out in landscape
18. [ ] No errors in Unity console during full combat

BALANCE OBSERVATIONS:
- Record average combat length (in turns)
- Record party HP remaining at victory
- Note any abilities that feel too strong or too weak
- Check if any class feels useless

Store test plan and any findings to memory as "integration-test-results".
```

---

### TASK 5.3 — Polish Pass
**Role:** REVIEWER → ENGINEER
**Depends on:** `integration-test-results`
**Store to memory:** `polish-notes`

**REVIEWER prompt:**
```
Retrieve all memory keys from Phases 1-5.

Conduct a full review of the Milestone 1 codebase:

CODE QUALITY:
- Do all files follow "coding-conventions"?
- Are there any MonoBehaviours doing logic they shouldn't?
- Is the event-driven architecture consistent? (state machine emits, UI subscribes)
- Are there any tight couplings that will cause problems in Milestone 2?
- Are ScriptableObjects used correctly? (no runtime mutation of SO data)

ARCHITECTURE:
- Can we add a new class without modifying existing code? (open/closed principle)
- Can we add a new enemy type without modifying existing code?
- Can we add a new ability type without modifying existing code?
- Is the save/load path clear for Milestone 2?

PERFORMANCE:
- Any per-frame allocations in combat loop?
- Any unnecessary GetComponent calls?
- UI rebuilds minimized?

MOBILE READINESS:
- Touch targets large enough (minimum 44x44 points)?
- Text readable at mobile resolution?
- No reliance on hover states?

Compile all feedback into actionable items for the engineer.
Store to memory as "polish-notes".
```

---

## Summary: Task Dependency Graph

```
PHASE 0 (parallel):
  0.1 Project Setup ──────────────────────┐
  0.2 Coding Conventions ─────────────────┤
                                          ▼
PHASE 1 (mostly parallel):
  1.1 Enums & Constants ──┬──► 1.2 AbilitySO
                          ├──► 1.3 CharacterClassSO (needs 1.2)
                          ├──► 1.4 EnemySO (needs 1.2)
                          └──► 1.5 EncounterSO (needs 1.4)

PHASE 2 (sequential with some parallel):
  2.1 DiceRoller ─────────┐
  2.2 CombatUnit ─────────┼──► 2.3 DamageCalculator
                          │         │
                          └─────────┼──► 2.4 CombatStateMachine
                                    └──► 2.5 EnemyAI

PHASE 3 (parallel, after Phase 2):
  3.1 Combat Layout Design ──► 3.2 Combat UI Controller
  3.3 Stick Figure Sprites     3.4 Narrator System

PHASE 4 (parallel, after Phase 1):
  4.1 Class Definitions
  4.2 Enemy + Encounter Definitions

PHASE 5 (sequential, after all above):
  5.1 Scene Wiring ──► 5.2 Integration Test ──► 5.3 Polish
```

---

## Feeding This to Your Pipeline

**Recommended approach:**

1. Feed Phase 0 tasks first (can be parallel)
2. Feed Phase 1 tasks (1.1 first, then 1.2-1.5 can parallel after 1.1)
3. Feed Phase 4 tasks (can run parallel with Phase 2 since they're design-only)
4. Feed Phase 2 tasks (2.1 and 2.2 parallel, then 2.3, then 2.4 and 2.5)
5. Feed Phase 3 tasks (can mostly parallel)
6. Feed Phase 5 tasks (strictly sequential)

**Each task prompt includes:**
- Which agent role should handle it
- What memory keys to retrieve for context
- What to produce
- What to store back to memory

**Total: 20 tasks across 4 roles to complete Milestone 1.**

---

*This document covers Milestone 1 only. Milestones 2-5 will get their own task breakdowns when we're ready.*
