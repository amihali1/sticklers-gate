# STICKLER'S GATE — Game Design Document

**Version:** 0.1 | **Last Updated:** March 9, 2026 | **Status:** Pre-Production

---

## 1. Executive Summary

**Stickler's Gate** is a turn-based RPG for Android and iOS, built in Unity (C#). It is a loving parody of Baldur's Gate featuring stick-figure characters, D&D 5e-inspired mechanics, and a tone that blends genuine stakes with sharp humor. Players assemble a party of stick-figure adventurers, navigate a branching narrative, and engage in tactical turn-based combat across a fantasy world drawn in a deliberately minimalist art style.

**Tagline:** *"Roll for initiative. Pray for mercy."*

### Core Pillars

1. **Authentic D&D Feel** — d20 rolls, ability scores, AC, saving throws, advantage/disadvantage. Players who know tabletop RPGs should feel at home.
2. **Genuine Stakes, Funny Delivery** — The world is in real danger. The dialogue just happens to be hilarious. Characters die permanently. The narrator roasts you for bad rolls.
3. **Stick-Figure Charm** — Simple art that gets the job done. Expressive through animation and effects rather than detail. Evolves over time as we layer on gear visibility, particle effects, and environmental flair.
4. **Mobile-First Design** — Landscape orientation, touch-friendly UI, sessions that work in 5-minute bursts or hour-long deep dives.

---

## 2. Technical Foundation

### Platform & Engine

| Item | Choice |
|------|--------|
| Engine | Unity 2022 LTS (or latest LTS at dev start) |
| Language | C# |
| Rendering | Unity 2D (URP — Universal Render Pipeline) |
| Target Platforms | Android (API 24+), iOS (14+) |
| Orientation | Landscape only |
| Target Resolution | 1920×1080 base, responsive scaling |
| Min Spec | ~2GB RAM, OpenGL ES 3.0 / Metal |

### Architecture Overview

The project follows a **data-driven architecture** using Unity's ScriptableObjects for all game data (classes, enemies, items, spells, encounters). This means designers and writers can tweak balance and content without touching code.

**Key Architectural Patterns:**

- **ScriptableObject Data Layer** — All stats, items, abilities, enemy definitions, and encounter configs live in SO assets. No magic numbers in code.
- **State Machine for Game Flow** — MainMenu → CharacterCreation → WorldMap → Exploration → Combat → Dialogue → Camp. Each state is a self-contained scene or overlay.
- **Event-Driven Communication** — Systems talk through a central EventBus (ScriptableObject-based events). Combat doesn't know about UI. UI listens to events.
- **Command Pattern for Combat** — Every combat action is a serializable Command object. This enables undo (future), replay, and clean AI decision-making.
- **MVC for UI** — UI elements never hold game state. They read from models and dispatch player intentions.

### Project Folder Structure

```
Assets/
├── _Project/
│   ├── Scenes/
│   │   ├── MainMenu.unity
│   │   ├── CharacterCreation.unity
│   │   ├── WorldMap.unity
│   │   ├── Combat.unity
│   │   └── Dialogue.unity
│   ├── Scripts/
│   │   ├── Core/           # GameManager, EventBus, SaveSystem, Enums
│   │   ├── Data/           # ScriptableObject definitions
│   │   ├── Combat/         # TurnManager, CombatActions, AI, DiceRoller
│   │   ├── Characters/     # CharacterSheet, StatBlock, Inventory
│   │   ├── UI/             # All UI controllers and views
│   │   ├── Dialogue/       # DialogueRunner, ChoiceHandler
│   │   ├── World/          # WorldMap, LocationNodes, RandomEncounters
│   │   └── Utils/          # Extensions, helpers, constants
│   ├── Data/               # ScriptableObject instances
│   │   ├── Classes/        # FighterClass.asset, WizardClass.asset, etc.
│   │   ├── Enemies/        # GoblinData.asset, DragonData.asset, etc.
│   │   ├── Items/          # Weapons, armor, potions
│   │   ├── Abilities/      # Attack, spell, and skill definitions
│   │   └── Encounters/     # Pre-built encounter configurations
│   ├── Art/
│   │   ├── Characters/     # Stick figure sprites and animations
│   │   ├── UI/             # Buttons, panels, frames
│   │   ├── Environments/   # Background art, tileable pieces
│   │   └── Effects/        # Particle textures, hit flashes
│   ├── Audio/
│   │   ├── Music/
│   │   └── SFX/
│   └── Prefabs/
│       ├── Characters/
│       ├── UI/
│       └── Effects/
└── Plugins/                # Third-party (DOTween, etc.)
```

---

## 3. Game Systems Design

### 3.1 Ability Scores (D&D 5e Inspired)

Six core stats, each generating a modifier: `modifier = floor((score - 10) / 2)`

| Stat | Abbr | Used For |
|------|------|----------|
| Strength | STR | Melee attacks, carrying capacity, Athletics checks |
| Dexterity | DEX | Ranged attacks, AC (light armor), initiative, Stealth |
| Constitution | CON | Hit points per level, Concentration saves |
| Intelligence | INT | Wizard spellcasting, Arcana/Investigation checks |
| Wisdom | WIS | Cleric spellcasting, Perception, Insight checks |
| Charisma | CHA | Dialogue persuasion, Bard/Warlock casting, Intimidation |

### 3.2 Character Classes (Launch Set)

Four classes at launch, each with a distinct combat role and personality archetype:

**Fighter** — "Hits things. Gets hit. Hits things harder."

- Hit Die: d10 | Primary: STR | Secondary: CON
- Armor: Heavy (high base AC)
- Role: Frontline tank/damage dealer
- Signature: Action Surge (extra action, cooldown-based)
- Personality Archetype: Earnest hero who takes everything too seriously

**Wizard** — "Glass cannon. Emphasis on glass."

- Hit Die: d6 | Primary: INT | Secondary: DEX
- Armor: None (low base AC)
- Role: AoE damage, battlefield control
- Signature: Fireball (8d6, hits all enemies, long cooldown)
- Personality Archetype: Smug know-it-all who panic-screams when hit

**Rogue** — "Steals your gold. Then your heart. Then your gold again."

- Hit Die: d8 | Primary: DEX | Secondary: CHA
- Armor: Light
- Role: High single-target damage, evasion
- Signature: Sneak Attack (bonus damage if advantage), Assassinate (huge burst, long cooldown)
- Personality Archetype: Fourth-wall breaker, comments on game mechanics

**Cleric** — "The only one keeping this party alive, and nobody thanks them."

- Hit Die: d8 | Primary: WIS | Secondary: CON
- Armor: Heavy (shield)
- Role: Healer, support, off-damage
- Signature: Cure Wounds (healing), Guiding Bolt (damage + grants advantage to next attack)
- Personality Archetype: Passive-aggressive healer who keeps a grudge list

### 3.3 Combat System (D&D 5e Simplified)

**Initiative:** Each combatant rolls d20 + DEX modifier. Turn order is highest to lowest. Ties broken by DEX score, then alphabetically (stick figures don't have complex tiebreakers).

**Turn Structure:** On each turn, a character can take ONE action:

- **Attack** — Roll d20 + ability modifier + proficiency vs target AC. Hit = roll damage dice. Miss = 0 damage. Nat 20 = critical hit (double damage dice). Nat 1 = critical miss (humiliating narrator commentary).
- **Cast Spell** — Same as attack but uses spellcasting ability. Some spells force saving throws instead of attack rolls.
- **Use Item** — Consume a potion, throw an alchemist fire, etc.
- **Defend** — Gain +2 AC until next turn. Narrator mocks your cowardice.
- **Flee** — DEX check (DC 12). Success = escape combat. Failure = lose turn, enemies get free attacks, narrator is disappointed.

**Damage & HP:**

- Each class has a hit die that determines HP per level
- HP = (hit die max at level 1) + CON modifier per level
- At 0 HP: character is downed. They get 3 death saving throws (d20, 10+ = success, below 10 = fail). Three successes = stabilized. Three failures = permadeath. Nat 20 = revive with 1 HP (narrator is shocked).
- Permadeath is real. Dead stick figures get a memorial screen.

**Advantage / Disadvantage:**

- Advantage: Roll 2d20, take the higher. Granted by flanking, stealth, status effects, etc.
- Disadvantage: Roll 2d20, take the lower. Caused by status effects, bad positioning, being on fire.

**Armor Class (AC):**

- Base AC depends on armor type: None (10 + DEX), Light (12 + DEX), Medium (14 + DEX max 2), Heavy (16-18, no DEX)
- Shields add +2
- Spells and buffs can modify

**Status Effects:**

| Effect | Mechanical Impact | Visual |
|--------|-------------------|--------|
| Poisoned | Disadvantage on attacks | Green tint, wavy lines |
| Burning | 1d6 damage per turn start | Orange flicker particles |
| Stunned | Skip turn | Stars circling head |
| Blessed | +1d4 to attacks and saves | Golden glow |
| Frightened | Disadvantage, can't move closer | Shaking animation |
| Prone | Disadvantage on attacks, melee attacks against have advantage | Stick figure horizontal |

### 3.4 Encounter Design

Encounters are defined as ScriptableObjects containing:

- Enemy list (with positions on a grid or formation)
- Difficulty rating (Easy / Medium / Hard / Deadly, per D&D CR guidelines simplified)
- Terrain modifiers (cover, hazards, elevation)
- Loot table
- Optional pre-combat dialogue
- Optional narrator commentary triggers

**Combat Arena:** A simple 2D battlefield. Party on the left, enemies on the right. No grid movement in v1 — positioning is abstracted into "front row" and "back row." Front row characters can melee. Back row characters get +2 AC vs melee but must use ranged attacks.

### 3.5 Leveling & Progression

- XP-based leveling (not milestone, because we want the numbers going up dopamine)
- Level cap: 10 (for manageable scope)
- Each level grants: HP increase (hit die + CON mod), possible ability score improvement, new abilities at key levels
- Level thresholds: 0, 300, 900, 2700, 6500, 14000, 23000, 34000, 48000, 64000

### 3.6 Narrator System

A persistent, sarcastic narrator comments on:

- Combat results (especially critical hits and misses)
- Player decisions in dialogue
- Party deaths ("And so falls [Name], who never did learn to dodge.")
- Idle behavior (if player takes too long on a turn)
- Loading screens (flavor text and bad advice)

The narrator is implemented as a top-of-screen text overlay with typewriter animation. Narrator lines are stored in ScriptableObjects tagged by trigger type.

---

## 4. Story & World

### Setting: The Stick Realms

A world where everyone is a stick figure and nobody acknowledges it. The environments are sketched in a "notebook margin" style — simple lines with occasional splashes of color for emphasis.

### Main Plot (Three Acts)

**Act 1: The Gate Opens** — The city of Stickburgh is threatened when an ancient portal (the titular Stickler's Gate) begins opening, releasing increasingly dangerous creatures. Your character is a nobody who happens to be nearby when the first monsters emerge. You're "volunteered" by the city guard to investigate.

**Act 2: The Companions** — Travel across the Stick Realms recruiting companions, each with their own questline and personality. Discover that the Gate was sealed by a legendary hero called "The Stickler" centuries ago, and only artifacts from their journey can re-seal it.

**Act 3: Through the Gate** — Enter the Gate itself, face the final dungeon, confront the entity trying to break through. Multiple endings based on player choices throughout the game.

### Companion Characters (4 recruitable, pick 3 for your party of 4)

| Name | Class | Personality | Recruitment |
|------|-------|-------------|-------------|
| Ser Sticksworth | Fighter | Overly formal knight who speaks entirely in chivalric code. Confused by casual speech. | Defends a bridge from a troll (who is just a big stick figure). |
| Twiggy | Wizard | Conspiracy theorist. Believes the world is actually 3D and they're trapped in a flat dimension. | Found in a tower "researching the truth." |
| Slick | Rogue | Con artist with a heart of gold (which they stole). Constantly tries to sell you your own items. | Caught picking your pocket. Offers to join instead of going to jail. |
| Sister Splinter | Cleric | Exhausted healer who has seen too much. Keeps a list of everyone who didn't say thank you. | At a temple, debating whether to heal the next person who asks. |

---

## 5. Milestone Roadmap

### Milestone 1: Combat Prototype (Weeks 1–4)

**Goal:** A fully playable combat encounter with one party member vs one enemy, demonstrating all core D&D mechanics.

**Deliverables:**

- Unity project setup with folder structure and architecture scaffolding
- ScriptableObject data layer: CharacterClassSO, AbilitySO, EnemySO
- Core dice rolling system (d20, damage dice, modifiers, advantage/disadvantage)
- Turn manager (initiative, turn order, turn cycling)
- Combat state machine (PlayerTurn → EnemyTurn → CheckVictory → Repeat)
- Basic enemy AI (attack highest/lowest HP, random ability selection)
- Placeholder stick figure sprites (static poses: idle, attack, hit, death)
- Combat UI: HP bars, turn order display, action buttons, damage numbers
- D20 roll animation (show the roll result with drama)
- Narrator text for combat events (hit, miss, crit, death)
- Victory/defeat screens

### Milestone 2: Full Party Combat (Weeks 5–8)

**Goal:** 4v4 combat with all four classes, front/back row, items, and status effects.

**Deliverables:**

- Party management (4 characters, each with class abilities)
- Multi-target abilities (Fireball hits all enemies)
- Healing spells and support abilities
- Front row / back row positioning with tactical tradeoffs
- Status effects system (poisoned, burning, stunned, blessed, frightened, prone)
- Consumable items (health potions, antidotes, thrown weapons)
- Enemy variety (at least 6 enemy types with distinct behaviors)
- Improved enemy AI (target selection based on class, threat, HP)
- Death saving throws system
- Encounter difficulty scaling

### Milestone 3: Character Creation & Progression (Weeks 9–12)

**Goal:** Players create their character, choose a class, and level up.

**Deliverables:**

- Character creation screen (name, class selection, stat overview)
- Ability score generation (standard array or point buy)
- XP and leveling system
- Per-level ability unlocks
- Ability score improvements at key levels
- Character sheet UI (stats, equipment, abilities)
- Equipment system (weapons, armor, accessories affect stats)
- Basic inventory management

### Milestone 4: Dialogue & Narrative (Weeks 13–16)

**Goal:** Branching dialogue with skill checks and companion recruitment.

**Deliverables:**

- Dialogue system with branching choices
- Skill check integration in dialogue (Persuasion, Intimidation, etc.)
- Companion recruitment conversations
- Companion approval system (they react to your choices)
- Narrator commentary on dialogue choices
- Dialogue data format (JSON or ScriptableObject-based)
- Portrait/expression system for dialogue (stick figure faces)

### Milestone 5: World Map & Exploration (Weeks 17–20)

**Goal:** A traversable world map connecting locations with random encounters.

**Deliverables:**

- World map screen with location nodes
- Travel between locations (with travel events / random encounters)
- Location scenes (town, dungeon entrance, camp, etc.)
- Random encounter tables per region
- Shop system (buy/sell gear in towns)
- Rest/camp system (heal, talk to companions, prepare)
- Save/load system

### Milestone 6: Act 1 Complete (Weeks 21–26)

**Goal:** A polished, playable first act with 2–3 hours of content.

**Deliverables:**

- Act 1 story content (all dialogue, encounters, events)
- All 4 companion recruitment quests
- 3–4 dungeon encounters with mini-bosses
- Act 1 boss fight
- Sound effects and music (placeholder/royalty-free initially)
- Tutorial flow for new players
- Mobile optimization pass (performance, touch targets, battery)
- Playtesting and balance pass

### Milestone 7+: Acts 2 & 3, Polish, Launch

- Act 2 and 3 content
- Art evolution (gear visible on stick figures, environment detail)
- Particle effects and animation polish
- Original music and sound design
- Accessibility features
- Monetization strategy (if any — one-time purchase recommended for RPGs)
- App Store / Google Play submission

---

## 6. Art Direction

### Phase 1: Minimal Stick Figures

- Black lines on simple backgrounds
- Characters are classic stick figures: circle head, line body, line limbs
- Differentiated by: color tint per class, simple weapon/hat silhouettes, size variation for enemies
- Animations: simple tweens (bob idle, swing attack, flash on hit, collapse on death)
- UI: Dark parchment theme with hand-drawn style borders

### Phase 2: Expressive Stick Figures (Post-Milestone 3)

- Facial expressions (dot eyes, simple mouth shapes)
- Visible gear (sword shape, shield shape, hat shape as overlays)
- Emote animations (confusion "?", anger "!", heart for healing)

### Phase 3: Full Charm (Post-Milestone 5)

- Particle effects (fire, sparkles, blood splatters as confetti)
- Environmental art (sketched trees, buildings, dungeon walls)
- Screen shake, hit stop, and juice effects
- Parallax scrolling backgrounds

---

## 7. Technical Notes for Development

### Unity-Specific Guidance

- **ScriptableObjects are your best friend.** Every piece of game data (class stats, enemy definitions, ability parameters, loot tables) should be a SO. This enables rapid iteration without recompilation.
- **Use the Component pattern aggressively.** A character in combat has: SpriteRenderer, Animator, CharacterStats (MonoBehaviour reading from SO), CombatActor, HealthBar (UI). Keep components focused.
- **Addressables for content loading.** Not needed at first, but plan for it. When Act 2+ arrives, you don't want everything in memory.
- **TextMeshPro for all text.** Unity's legacy text is garbage. TMPro ships with Unity now.
- **DOTween for animations.** The free version handles 90% of what you'll need for UI tweens, damage numbers, dice rolls. Install via Asset Store or OpenUPM.
- **Input System:** Use Unity's new Input System package for touch + editor testing with mouse.

### Data-Driven Enemies (Example ScriptableObject)

```csharp
[CreateAssetMenu(fileName = "NewEnemy", menuName = "SticklerGate/EnemyData")]
public class EnemyDataSO : ScriptableObject
{
    public string enemyName;
    public Sprite portrait;
    public int hitPoints;
    public int armorClass;
    public StatBlock stats;  // STR, DEX, CON, INT, WIS, CHA
    public AbilitySO[] abilities;
    public int challengeRating;
    public int xpReward;
    public LootTableSO lootTable;
    [TextArea] public string[] narratorDeathLines;
}
```

### Save System Strategy

- Use JSON serialization for save files
- Save: party state, inventory, world map progress, quest flags, companion approval
- Store in Application.persistentDataPath (works on both Android and iOS)
- Consider cloud save via Unity Gaming Services (later milestone)

---

## 8. Open Questions for Future Sessions

- Should permadeath be toggleable (story mode vs classic mode)?
- Do we want a difficulty selector (or narrator-voiced difficulty, e.g., "Stickler Mode")?
- Multiplayer? (Probably not for v1, but worth discussing)
- Monetization: One-time purchase? Free with optional cosmetics? Ads? (Recommendation: paid, no ads, for an RPG audience)
- Should companions have romance options? (On-brand for a BG parody, but scope concern)

---

*"The Gate opens. Your stick figure stands at the threshold. A narrator you never asked for clears their throat. Roll for initiative."*
