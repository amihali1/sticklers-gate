# CONTENT — Stickler's Gate

## Character Classes

### Sticknight (Fighter)
- **Role:** Tank / Melee DPS
- **Stats:** STR 16, DEX 13, CON 14, INT 10, WIS 12, CHA 8
- **HP:** d10 hit die, base HP 44 (at level 1: 10 + CON mod)
- **AC:** 16
- **Row:** Front
- **Flavor:** "Hits things. Gets hit. Hits things harder."
- **Abilities:**
  - Greatsword Slash: 2d6 + STR slashing, no cooldown
  - Shield Bash: 1d4 + STR bludgeoning, target makes STR save or Stunned 1 turn, 2 turn cooldown
  - (Level 3) Action Surge: 3d6 + STR slashing, 3 turn cooldown
  - (Level 5) Champion's Strike: 4d6 + STR slashing, auto-crit if target below 25% HP, 4 turn cooldown

### Stickwizard (Wizard)
- **Role:** Ranged AoE DPS
- **Stats:** STR 8, DEX 14, CON 12, INT 18, WIS 13, CHA 10
- **HP:** d6 hit die, base HP 26
- **AC:** 12
- **Row:** Back
- **Flavor:** "Glass cannon. Emphasis on glass."
- **Abilities:**
  - Fire Bolt: 2d10 + INT fire, no cooldown
  - Ray of Frost: 1d8 + INT cold, target speed reduced (flavor), no cooldown
  - (Level 3) Fireball: 8d6 fire, hits AllEnemies, 4 turn cooldown
  - (Level 5) Disintegrate: 10d6 + 40 force, single target, 5 turn cooldown

### Stickrogue (Rogue)
- **Role:** Burst DPS / Utility
- **Stats:** STR 10, DEX 18, CON 12, INT 13, WIS 10, CHA 14
- **HP:** d8 hit die, base HP 33
- **AC:** 15
- **Row:** Front
- **Flavor:** "Steals your gold. Then your heart. Then your gold again."
- **Abilities:**
  - Sneak Attack: 3d6 + DEX piercing, no cooldown
  - Cunning Dodge: Self-buff, +4 AC for 1 turn, 2 turn cooldown
  - (Level 3) Assassinate: 6d6 + DEX piercing, 4 turn cooldown
  - (Level 5) Shadow Step: 4d6 + DEX piercing + applies Invisible for 1 turn, 4 turn cooldown

### Stickleric (Cleric)
- **Role:** Healer / Support
- **Stats:** STR 14, DEX 10, CON 14, INT 10, WIS 18, CHA 13
- **HP:** d8 hit die, base HP 38
- **AC:** 18
- **Row:** Back
- **Flavor:** "Prays for you. Judges you. Heals you anyway."
- **Abilities:**
  - Sacred Flame: 2d8 + WIS radiant, no cooldown
  - Cure Wounds: heals 2d8 + 4, single ally, 2 turn cooldown
  - (Level 3) Guiding Bolt: 4d6 + WIS radiant, 3 turn cooldown
  - (Level 5) Mass Heal: heals 3d8 + WIS mod, AllAllies, 5 turn cooldown

**Status:** Implemented as SO assets (PR #9)

---

## Enemies

### Stick Goblin (Minion)
- **Stats:** HP 12, AC 11, STR 8, DEX 14, CON 10, INT 8, WIS 8, CHA 6
- **Ability:** Rusty Shiv — 1d4 + DEX slashing
- **AI:** Random targeting, aggression 0.9, can't flee
- **Rewards:** 25 XP, 1-5 gold
- **Intro:** "Three goblins emerge from behind an inkblot. They look barely drawn."
- **Death:** "The goblin dissolves back into the margin. It was barely here to begin with."

### Stick Skeleton (Standard)
- **Stats:** HP 26, AC 13, STR 12, DEX 14, CON 12, INT 6, WIS 8, CHA 4
- **Abilities:** Bone Club (1d6 + STR bludgeoning), Bone Throw (1d4 + DEX piercing, ranged)
- **AI:** LowestHP targeting, aggression 0.7, can't flee
- **Rewards:** 50 XP, 3-8 gold
- **Intro:** "A skeleton rattles into view. It's just a stick figure with extra lines, really."
- **Death:** "The skeleton collapses into a pile of straight lines."

### Stick Ogre (Elite)
- **Stats:** HP 68, AC 11, STR 19, DEX 8, CON 16, INT 5, WIS 7, CHA 4
- **Abilities:** Greatclub (2d8 + STR bludgeoning), Ground Slam (1d10 + STR bludgeoning, AllEnemies)
- **AI:** HighestThreat targeting, aggression 0.95, can't flee
- **Rewards:** 200 XP, 15-30 gold
- **Intro:** "An ogre lumbers forward. It's drawn with a thicker pen. You can tell it's important."
- **Death:** "The ogre falls with a tremor that shakes the very notebook."

**Status:** Implemented as SO assets (PR #9)

---

## Test Encounter: "Ambush at the Inkwell"
- **Enemies:** 3x Stick Goblin (all front row)
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

---

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
