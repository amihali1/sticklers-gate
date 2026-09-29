# CONTENT — Stickler's Gate

## Character Classes

### Sticknight (Fighter)
- **Role:** Tank / Melee DPS
- **Stats:** STR 16, DEX 13, CON 14, INT 10, WIS 12, CHA 8
- **HP:** d10 hit die, base HP 44 (at level 1: 10 + CON mod)
- **AC:** 16
- **Movement:** 3 tiles
- **Flavor:** "Hits things. Gets hit. Hits things harder."
- **Abilities:**
  - Greatsword Slash: 2d6 + STR slashing, range 1, no cooldown
  - Shield Bash: 1d4 + STR bludgeoning, range 1, target makes STR save or Stunned 1 turn, 2 turn cooldown
  - (Level 3) Action Surge: 3d6 + STR slashing, range 1, 3 turn cooldown
  - (Level 5) Champion's Strike: 4d6 + STR slashing, range 1, auto-crit if target below 25% HP, 4 turn cooldown

### Stickwizard (Wizard)
- **Role:** Ranged AoE DPS
- **Stats:** STR 8, DEX 14, CON 12, INT 18, WIS 13, CHA 10
- **HP:** d6 hit die, base HP 26
- **AC:** 12
- **Movement:** 2 tiles
- **Flavor:** "Glass cannon. Emphasis on glass."
- **Abilities:**
  - Fire Bolt: 2d10 + INT fire, range 5, no cooldown
  - Ray of Frost: 1d8 + INT cold, range 4, target speed reduced (flavor), no cooldown
  - (Level 3) Fireball: 8d6 fire, hits AllEnemies, range 0 (AoE), 4 turn cooldown
  - (Level 5) Disintegrate: 10d6 + 40 force, range 6, single target, 5 turn cooldown

### Stickrogue (Rogue)
- **Role:** Burst DPS / Utility
- **Stats:** STR 10, DEX 18, CON 12, INT 13, WIS 10, CHA 14
- **HP:** d8 hit die, base HP 33
- **AC:** 15
- **Movement:** 3 tiles
- **Flavor:** "Steals your gold. Then your heart. Then your gold again."
- **Abilities:**
  - Sneak Attack: 3d6 + DEX piercing, range 1, no cooldown
  - Cunning Dodge: Self-buff, range 0, +4 AC for 1 turn, 2 turn cooldown
  - (Level 3) Assassinate: 6d6 + DEX piercing, range 1, 4 turn cooldown
  - (Level 5) Shadow Step: 4d6 + DEX piercing, range 1, + applies Invisible for 1 turn, 4 turn cooldown

### Stickleric (Cleric)
- **Role:** Healer / Support
- **Stats:** STR 14, DEX 10, CON 14, INT 10, WIS 18, CHA 13
- **HP:** d8 hit die, base HP 38
- **AC:** 18
- **Movement:** 2 tiles
- **Flavor:** "Prays for you. Judges you. Heals you anyway."
- **Abilities:**
  - Sacred Flame: 2d8 + WIS radiant, range 4, no cooldown
  - Cure Wounds: heals 2d8 + 4, range 4, single ally, 2 turn cooldown
  - (Level 3) Guiding Bolt: 4d6 + WIS radiant, range 5, 3 turn cooldown
  - (Level 5) Mass Heal: heals 3d8 + WIS mod, range 0 (AoE), AllAllies, 5 turn cooldown

**Status:** Implemented as SO assets (PR #9)

---

## Enemies

### Stick Goblin (Minion)
- **Stats:** HP 12, AC 11, STR 8, DEX 14, CON 10, INT 8, WIS 8, CHA 6
- **Movement:** 3 tiles
- **Ability:** Rusty Shiv — 1d4 + DEX slashing, range 1
- **AI:** Random targeting, aggression 0.9, can't flee
- **Rewards:** 25 XP, 1-5 gold
- **Intro:** "Three goblins emerge from behind an inkblot. They look barely drawn."
- **Death:** "The goblin dissolves back into the margin. It was barely here to begin with."

### Stick Skeleton (Standard)
- **Stats:** HP 26, AC 13, STR 12, DEX 14, CON 12, INT 6, WIS 8, CHA 4
- **Movement:** 2 tiles
- **Abilities:** Bone Club (1d6 + STR bludgeoning, range 1), Bone Throw (1d4 + DEX piercing, range 3)
- **AI:** LowestHP targeting, aggression 0.7, can't flee
- **Rewards:** 50 XP, 3-8 gold
- **Intro:** "A skeleton rattles into view. It's just a stick figure with extra lines, really."
- **Death:** "The skeleton collapses into a pile of straight lines."

### Stick Ogre (Elite)
- **Stats:** HP 68, AC 11, STR 19, DEX 8, CON 16, INT 5, WIS 7, CHA 4
- **Movement:** 2 tiles
- **Abilities:** Greatclub (2d8 + STR bludgeoning, range 1), Ground Slam (1d10 + STR bludgeoning, AllEnemies, range 0)
- **AI:** HighestThreat targeting, aggression 0.95, can't flee
- **Rewards:** 200 XP, 15-30 gold
- **Intro:** "An ogre lumbers forward. It's drawn with a thicker pen. You can tell it's important."
- **Death:** "The ogre falls with a tremor that shakes the very notebook."

**Status:** Implemented as SO assets (PR #9)

### Stick Bandit (Standard) — *Added Milestone 2*
- **Stats:** HP 22, AC 13, STR 11, DEX 14, CON 12, INT 10, WIS 10, CHA 10
- **Movement:** 2 tiles
- **Abilities:** Crossbow Shot (1d8+1 piercing, range 4), Dagger Slash (1d4+2 piercing, range 1)
- **AI:** LowestHP targeting, aggression 0.8, retreat at 15% HP, can flee
- **Rewards:** 50 XP, 5-15 gold

### Stick Spider (Standard) — *Added Milestone 2*
- **Stats:** HP 26, AC 14, STR 12, DEX 16, CON 12, INT 2, WIS 10, CHA 4
- **Movement:** 3 tiles
- **Abilities:** Venomous Bite (1d6 piercing, range 1, + Poison 3t, CON DC 12), Web Spit (Stun 1t, range 3, DEX DC 11, 3t CD), Poison Spit (1d4 piercing AllEnemies, range 0, + Poison 2t, CON DC 11, 4t CD)
- **AI:** Healers targeting, aggression 0.85, can't flee
- **Rewards:** 75 XP, 0-3 gold

### Stick Necromancer (Elite) — *Added Milestone 2*
- **Stats:** HP 45, AC 12, STR 8, DEX 10, CON 14, INT 16, WIS 12, CHA 12
- **Movement:** 2 tiles
- **Abilities:** Dark Bolt (2d6+2 necrotic, range 5), Life Drain (1d8 necrotic, range 4, 2t CD)
- **AI:** HighestThreat targeting, aggression 0.7, retreat at 30% HP, can flee
- **Rewards:** 150 XP, 10-25 gold

**Status:** Implemented as SO assets (Milestone 2)

---

## Test Encounter: "Ambush at the Inkwell"
- **Grid:** 8×6
- **Enemies:** 3x Stick Goblin
- **Difficulty:** Easy
- **Victory:** Defeat all enemies
- **Intro narration:**
  - "You approach the Inkwell Tavern. Well, 'tavern' is generous. It's a circle with a door."
  - "Three goblins emerge. They were hiding behind an apostrophe."
  - "Roll for initiative. And try to look heroic — someone might be watching."
- **Victory narration:**
  - "Against three goblins. Truly, bards will sing of this day. Quietly."
- **Defeat narration:**
  - "You lost to three goblins. I'm not angry, I'm just... no, I'm angry."

**Status:** Implemented as SO asset (PR #9)

## Encounter: "The Bandit Crossing" — *Added Milestone 2*
- **Grid:** 10×6
- **Enemies:** 2x Stick Bandit, 1x Stick Spider
- **Difficulty:** Medium
- **Victory:** Defeat all enemies

## Encounter: "The Necromancer's Study" — *Added Milestone 2*
- **Grid:** 8×8
- **Enemies:** 2x Stick Skeleton "Bone Guard", 1x Stick Necromancer
- **Difficulty:** Hard
- **Victory:** Defeat all enemies

**Status:** Implemented as SO assets (Milestone 2)

---

## Grid Values (Phase 9)

**Movement speed (tiles/turn):** Sticknight 3, Stickwizard 2, Stickrogue 4, Stickleric 3 · Goblin 4, Bandit 3, Spider 4, Skeleton 3, Ogre 2, Necromancer 2.

**Ability range (Manhattan tiles; AoE/self ignore range):**
| Range | Abilities |
|---|---|
| 1 (melee/touch) | Greatsword Slash, Champion's Strike, Shield Bash, Action Surge, Sneak Attack, Assassinate, Cure Wounds, Bone Club, Dagger Slash, Greatclub, Rusty Shiv, Venomous Bite |
| 3 | Shadow Step, Life Drain, Web Spit |
| 4 | Ray of Frost, Disintegrate, Guiding Bolt, Sacred Flame, Bone Throw |
| 5 | Fire Bolt, Crossbow Shot, Dark Bolt |

**Layouts** (`Data/Encounters/Layouts/`, all 8×6, row 0 = top). Party spawns: Sticknight (2,2), Stickwizard (0,3), Stickrogue (2,3), Stickleric (1,2).
- **Ambush at the Inkwell:** ink puddle (Difficult) at (4,2) (4,3) (5,2); goblins fan out at (6,0) (7,3) (5,5).
- **The Bandit Crossing:** river (Water) down column 4 with a bridge at rows 2–3, reeds (Difficult) at (3,0) (5,5); bandits hold the bridge at (6,2) (6,3), spider at (5,0).
- **The Necromancer's Study:** bookshelves (Abyss, impassable) down column 4 leaving a gap at rows 2–3, bone piles (Difficult) at (6,1) (6,4); Bone Guards at the gap (5,2) (5,3), necromancer at (7,2).

## Narrator Quip Pool

*To be written in full during Phase 3 Task 3.4. Core categories:*
- Critical hit, critical fail, player miss, enemy miss
- Kill, overkill, player death
- Heal, combat start, victory, defeat, low HP warning

**Status:** Not written yet

---

## Story (High Level)

**Setting:** Sticklandia — a world on notebook paper. The city of Stickler's Gate guards an ancient portal to the margins.

**Threat:** The Erasure — a force literally erasing the world. Stick figures vanishing, villages reduced to blank paper.

**Act 1 — "Lines in the Sand":** Arrive at Stickler's Gate as a nobody. Prove yourself, recruit companions, survive the Erased Ones.

**Key NPCs:**
- The Narrator (Stanley Parable meets BG3, opinionated, unreliable)
- The Doodler (Withers equivalent — redraws the dead)

**BG3 Parody Beats:** Stick figure romance (holding lines), camp around a triangle fire, The Smudge (tadpole equivalent granting dark power)

**Status:** High-level only, not detailed yet
