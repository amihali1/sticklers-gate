#!/usr/bin/env python3
"""Generate Milestone 2 enemy ability, enemy, and encounter assets."""

import os
import uuid

BASE = "SticklersGate/Assets/_Project/Data"
ABILITY_SCRIPT_GUID = "da4d00a4c02545b42850efa556de8129"
ENEMY_SCRIPT_GUID = "8081346e19510ad4d8af056daa33d856"
ENCOUNTER_SCRIPT_GUID = "2162f34773fb5b84fa0651fd187cfe11"

def gen_guid():
    return uuid.uuid4().hex

def write_meta(path, guid):
    meta = f"""fileFormatVersion: 2
guid: {guid}
NativeFormatImporter:
  externalObjects: {{}}
  mainObjectFileID: 11400000
  userData:
  assetBundleName:
  assetBundleVariant:
"""
    with open(path, 'w', newline='\n') as f:
        f.write(meta)

def write_asset(path, content):
    with open(path, 'w', newline='\n') as f:
        f.write(content)

def ability_header(script_guid, name):
    return f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 0}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {script_guid}, type: 3}}
  m_Name: {name}
  m_EditorClassIdentifier:"""

def enemy_header(name):
    return f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 0}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {ENEMY_SCRIPT_GUID}, type: 3}}
  m_Name: {name}
  m_EditorClassIdentifier:"""

# Track GUIDs for cross-references
guids = {}

# ============================================================
# NEW ENEMY ABILITIES
# ============================================================

abilities_dir = os.path.join(BASE, "Abilities", "Enemies")

# --- Crossbow Shot (Stick Bandit) ---
g = gen_guid()
guids["CrossbowShot"] = g
write_meta(os.path.join(abilities_dir, "CrossbowShot.asset.meta"), g)
write_asset(os.path.join(abilities_dir, "CrossbowShot.asset"), f"""{ability_header(ABILITY_SCRIPT_GUID, "CrossbowShot")}
  _abilityName: Crossbow Shot
  _category: 1
  _targetType: 0
  _description: A bolt from across the page. Bandits love ranged attacks - less chance of getting drawn on.
  _damageDiceCount: 1
  _damageDiceSides: 8
  _damageType: 1
  _abilityModifier: 1
  _flatDamageBonus: 1
  _healDiceCount: 0
  _healDiceSides: 0
  _flatHealBonus: 0
  _appliesEffect: 0
  _effectDurationTurns: 0
  _savingThrowAbility: 0
  _savingThrowDC: 0
  _cooldownTurns: 0
  _unlockLevel: 1
  _hitQuips:
  - The bolt finds its mark. The bandit reloads with practiced indifference.
  - A solid hit! The bandit nods approvingly at their own work.
  _missQuips:
  - The bolt embeds itself in the margin. Close, but no gold.
  - A miss! The bandit curses and fumbles for another bolt.
  _critQuips:
  - Critical shot! The bolt pins your shadow to the page!
""")

# --- Dagger Slash (Stick Bandit) ---
g = gen_guid()
guids["DaggerSlash"] = g
write_meta(os.path.join(abilities_dir, "DaggerSlash.asset.meta"), g)
write_asset(os.path.join(abilities_dir, "DaggerSlash.asset"), f"""{ability_header(ABILITY_SCRIPT_GUID, "DaggerSlash")}
  _abilityName: Dagger Slash
  _category: 0
  _targetType: 0
  _description: Quick and dirty. Just like the bandit.
  _damageDiceCount: 1
  _damageDiceSides: 4
  _damageType: 1
  _abilityModifier: 1
  _flatDamageBonus: 2
  _healDiceCount: 0
  _healDiceSides: 0
  _flatHealBonus: 0
  _appliesEffect: 0
  _effectDurationTurns: 0
  _savingThrowAbility: 0
  _savingThrowDC: 0
  _cooldownTurns: 0
  _unlockLevel: 1
  _hitQuips:
  - The bandit slashes with a dagger that's seen better days. So has your HP.
  - A quick cut! The bandit grins. They have terrible dental hygiene.
  _missQuips:
  - The dagger misses by a line width. Literally.
  - The bandit lunges and whiffs. Even stick figures can be clumsy.
  _critQuips:
  - Critical dagger! The bandit found a gap in your... existence.
""")

# --- Venomous Bite (Stick Spider) ---
g = gen_guid()
guids["VenomousBite"] = g
write_meta(os.path.join(abilities_dir, "VenomousBite.asset.meta"), g)
write_asset(os.path.join(abilities_dir, "VenomousBite.asset"), f"""{ability_header(ABILITY_SCRIPT_GUID, "VenomousBite")}
  _abilityName: Venomous Bite
  _category: 0
  _targetType: 0
  _description: Eight legs of nope deliver a package of regret.
  _damageDiceCount: 1
  _damageDiceSides: 6
  _damageType: 1
  _abilityModifier: 1
  _flatDamageBonus: 0
  _healDiceCount: 0
  _healDiceSides: 0
  _flatHealBonus: 0
  _appliesEffect: 1
  _effectDurationTurns: 3
  _savingThrowAbility: 2
  _savingThrowDC: 12
  _cooldownTurns: 0
  _unlockLevel: 1
  _hitQuips:
  - The spider sinks its fangs in. You feel unwell. Very, very unwell.
  - Bitten! The venom burns through your ink like acid through paper.
  _missQuips:
  - The spider lunges but misses. All eight legs trip over each other.
  - You dodge the fangs! The spider hisses. Spiders can hiss now, apparently.
  _critQuips:
  - Critical bite! The spider gets ALL the venom in. Every. Drop.
""")

# --- Web Spit (Stick Spider) ---
g = gen_guid()
guids["WebSpit"] = g
write_meta(os.path.join(abilities_dir, "WebSpit.asset.meta"), g)
write_asset(os.path.join(abilities_dir, "WebSpit.asset"), f"""{ability_header(ABILITY_SCRIPT_GUID, "WebSpit")}
  _abilityName: Web Spit
  _category: 5
  _targetType: 0
  _description: Sticky, gross, and surprisingly accurate for something with no nose.
  _damageDiceCount: 0
  _damageDiceSides: 0
  _damageType: 0
  _abilityModifier: 1
  _flatDamageBonus: 0
  _healDiceCount: 0
  _healDiceSides: 0
  _flatHealBonus: 0
  _appliesEffect: 2
  _effectDurationTurns: 1
  _savingThrowAbility: 1
  _savingThrowDC: 11
  _cooldownTurns: 3
  _unlockLevel: 1
  _hitQuips:
  - You're wrapped in silk. It's not the luxury spa kind.
  _missQuips:
  - The web misses! It sticks to the scenery instead. Gross.
  _critQuips: []
""")

# --- Dark Bolt (Stick Necromancer) ---
g = gen_guid()
guids["DarkBolt"] = g
write_meta(os.path.join(abilities_dir, "DarkBolt.asset.meta"), g)
write_asset(os.path.join(abilities_dir, "DarkBolt.asset"), f"""{ability_header(ABILITY_SCRIPT_GUID, "DarkBolt")}
  _abilityName: Dark Bolt
  _category: 2
  _targetType: 0
  _description: A bolt of concentrated darkness. The necromancer insists it's 'artisanal evil.'
  _damageDiceCount: 2
  _damageDiceSides: 6
  _damageType: 7
  _abilityModifier: 3
  _flatDamageBonus: 2
  _healDiceCount: 0
  _healDiceSides: 0
  _flatHealBonus: 0
  _appliesEffect: 0
  _effectDurationTurns: 0
  _savingThrowAbility: 0
  _savingThrowDC: 0
  _cooldownTurns: 0
  _unlockLevel: 1
  _hitQuips:
  - Necrotic energy slams into you. It smells like old ink and regret.
  - A dark bolt strikes true. The necromancer cackles. They all cackle.
  _missQuips:
  - The dark bolt fizzles into nothing. Even evil has off days.
  - The bolt misses! The necromancer scowls and starts sketching another one.
  _critQuips:
  - Critical dark bolt! The necromancer's eyes glow with satisfaction. And evil. Mostly evil.
""")

# --- Life Drain (Stick Necromancer) ---
g = gen_guid()
guids["LifeDrain"] = g
write_meta(os.path.join(abilities_dir, "LifeDrain.asset.meta"), g)
write_asset(os.path.join(abilities_dir, "LifeDrain.asset"), f"""{ability_header(ABILITY_SCRIPT_GUID, "LifeDrain")}
  _abilityName: Life Drain
  _category: 2
  _targetType: 0
  _description: Steals your life force. The necromancer considers this 'recycling.'
  _damageDiceCount: 1
  _damageDiceSides: 8
  _damageType: 7
  _abilityModifier: 3
  _flatDamageBonus: 0
  _healDiceCount: 0
  _healDiceSides: 0
  _flatHealBonus: 0
  _appliesEffect: 0
  _effectDurationTurns: 0
  _savingThrowAbility: 0
  _savingThrowDC: 0
  _cooldownTurns: 2
  _unlockLevel: 1
  _hitQuips:
  - The necromancer drains your essence. They look healthier already.
  - Life force stolen! The necromancer's lines darken while yours fade.
  _missQuips:
  - The drain fizzles. Your life force was too stubborn to leave.
  - Failed! You cling to your vitality. The necromancer pouts.
  _critQuips:
  - Critical drain! The necromancer practically glows with stolen life!
""")

# --- Poison Spit (Stick Spider, AoE) ---
g = gen_guid()
guids["PoisonSpit"] = g
write_meta(os.path.join(abilities_dir, "PoisonSpit.asset.meta"), g)
write_asset(os.path.join(abilities_dir, "PoisonSpit.asset"), f"""{ability_header(ABILITY_SCRIPT_GUID, "PoisonSpit")}
  _abilityName: Poison Spit
  _category: 2
  _targetType: 2
  _description: The spider sprays venom at everyone. It's not picky about who it hits.
  _damageDiceCount: 1
  _damageDiceSides: 4
  _damageType: 1
  _abilityModifier: 1
  _flatDamageBonus: 0
  _healDiceCount: 0
  _healDiceSides: 0
  _flatHealBonus: 0
  _appliesEffect: 1
  _effectDurationTurns: 2
  _savingThrowAbility: 2
  _savingThrowDC: 11
  _cooldownTurns: 4
  _unlockLevel: 1
  _hitQuips:
  - Venom sprays everywhere! The spider has terrible aim but great coverage.
  _missQuips:
  - The poison spit goes wide. Even the spider looks surprised.
  _critQuips:
  - Critical spray! Everyone gets a generous helping of spider venom!
""")

# ============================================================
# NEW ENEMIES
# ============================================================

enemies_dir = os.path.join(BASE, "Enemies")

# --- Stick Bandit ---
g = gen_guid()
guids["StickBandit"] = g
write_meta(os.path.join(enemies_dir, "StickBandit.asset.meta"), g)
write_asset(os.path.join(enemies_dir, "StickBandit.asset"), f"""{enemy_header("StickBandit")}
  _enemyName: Stick Bandit
  _tier: 1
  _description: A stick figure in a hood. The hood is just a triangle, but the menace is real.
  _stats:
    strength: 11
    dexterity: 14
    constitution: 12
    intelligence: 10
    wisdom: 10
    charisma: 10
  _hitPoints: 22
  _armorClass: 13
  _abilities:
  - {{fileID: 11400000, guid: {guids["CrossbowShot"]}, type: 2}}
  - {{fileID: 11400000, guid: {guids["DaggerSlash"]}, type: 2}}
  _preferredRow: 1
  _aggression: 0.8
  _targetPreference: 1
  _retreatThreshold: 0.15
  _canFlee: 1
  _xpReward: 50
  _goldMin: 5
  _goldMax: 15
  _idleSprite: {{fileID: 0}}
  _attackSprite: {{fileID: 0}}
  _hitSprite: {{fileID: 0}}
  _deathSprite: {{fileID: 0}}
  _introQuips:
  - A bandit steps out from behind a semicolon. They demand your gold.
  - Bandits! They look like they were drawn in a hurry. Probably were.
  _deathQuips:
  - The bandit crumples. Their triangle hood falls off. Just a circle now.
  - The bandit is erased from the narrative. No refunds.
""")

# --- Stick Spider ---
g = gen_guid()
guids["StickSpider"] = g
write_meta(os.path.join(enemies_dir, "StickSpider.asset.meta"), g)
write_asset(os.path.join(enemies_dir, "StickSpider.asset"), f"""{enemy_header("StickSpider")}
  _enemyName: Stick Spider
  _tier: 1
  _description: Eight lines pretending to be legs. The artist clearly has arachnophobia.
  _stats:
    strength: 12
    dexterity: 16
    constitution: 12
    intelligence: 2
    wisdom: 10
    charisma: 4
  _hitPoints: 26
  _armorClass: 14
  _abilities:
  - {{fileID: 11400000, guid: {guids["VenomousBite"]}, type: 2}}
  - {{fileID: 11400000, guid: {guids["WebSpit"]}, type: 2}}
  - {{fileID: 11400000, guid: {guids["PoisonSpit"]}, type: 2}}
  _preferredRow: 0
  _aggression: 0.85
  _targetPreference: 3
  _retreatThreshold: 0
  _canFlee: 0
  _xpReward: 75
  _goldMin: 0
  _goldMax: 3
  _idleSprite: {{fileID: 0}}
  _attackSprite: {{fileID: 0}}
  _hitSprite: {{fileID: 0}}
  _deathSprite: {{fileID: 0}}
  _introQuips:
  - A spider crawls out of the margin. It has eight legs. You counted. Twice.
  - Something skitters across the page. You wish it hadn't.
  _deathQuips:
  - The spider curls into a ball and rolls off the page. Good riddance.
  - Eight legs twitch one last time. The narrator shudders.
""")

# --- Stick Necromancer ---
g = gen_guid()
guids["StickNecromancer"] = g
write_meta(os.path.join(enemies_dir, "StickNecromancer.asset.meta"), g)
write_asset(os.path.join(enemies_dir, "StickNecromancer.asset"), f"""{enemy_header("StickNecromancer")}
  _enemyName: Stick Necromancer
  _tier: 2
  _description: A stick figure in a dark robe. The robe is just scribbled shading, but the dark magic is real.
  _stats:
    strength: 8
    dexterity: 10
    constitution: 14
    intelligence: 16
    wisdom: 12
    charisma: 12
  _hitPoints: 45
  _armorClass: 12
  _abilities:
  - {{fileID: 11400000, guid: {guids["DarkBolt"]}, type: 2}}
  - {{fileID: 11400000, guid: {guids["LifeDrain"]}, type: 2}}
  _preferredRow: 1
  _aggression: 0.7
  _targetPreference: 2
  _retreatThreshold: 0.3
  _canFlee: 1
  _xpReward: 150
  _goldMin: 10
  _goldMax: 25
  _idleSprite: {{fileID: 0}}
  _attackSprite: {{fileID: 0}}
  _hitSprite: {{fileID: 0}}
  _deathSprite: {{fileID: 0}}
  _introQuips:
  - A robed figure emerges from the shadows. Well, from slightly darker lines.
  - The necromancer appears! They're carrying a book titled 'Evil for Beginners.'
  _deathQuips:
  - The necromancer collapses. Their dark robe turns out to just be pencil smudges.
  - The necromancer's spirit departs. Ironic, given their profession.
""")

# ============================================================
# NEW ENCOUNTER: The Bandit Crossing
# ============================================================

encounters_dir = os.path.join(BASE, "Encounters")
g = gen_guid()
guids["BanditCrossing"] = g
write_meta(os.path.join(encounters_dir, "BanditCrossing.asset.meta"), g)
write_asset(os.path.join(encounters_dir, "BanditCrossing.asset"), f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 0}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {ENCOUNTER_SCRIPT_GUID}, type: 3}}
  m_Name: BanditCrossing
  m_EditorClassIdentifier:
  _encounterName: The Bandit Crossing
  _enemies:
  - enemyData: {{fileID: 11400000, guid: {guids["StickBandit"]}, type: 2}}
    row: 1
    nameOverride:
  - enemyData: {{fileID: 11400000, guid: {guids["StickBandit"]}, type: 2}}
    row: 0
    nameOverride:
  - enemyData: {{fileID: 11400000, guid: {guids["StickSpider"]}, type: 2}}
    row: 0
    nameOverride:
  _victoryCondition: 0
  _survivalTurns: 0
  _background: {{fileID: 0}}
  _introLines:
  - The road narrows between two paragraphs. Classic ambush territory.
  - Bandits emerge from the margins! And is that... a spider? Great. Just great.
  - They want your gold. You don't have gold. This is going to be awkward.
  _victoryLines:
  - The bandits are dealt with! The spider too. Your therapist bill just went up.
  - Victory! The road is clear. Your party smells like spider venom, but you'll live.
  _defeatLines:
  - Robbed and web-wrapped. The narrator would laugh if it weren't so sad.
  - Defeated by bandits and a spider. The memoir writes itself.
""")

# --- Second new encounter: Necromancer's Study ---
g = gen_guid()
guids["NecromancersStudy"] = g
write_meta(os.path.join(encounters_dir, "NecromancersStudy.asset.meta"), g)

# Get existing skeleton GUID
skeleton_guid = None
skeleton_meta_path = os.path.join(enemies_dir, "StickSkeleton.asset.meta")
if not os.path.exists(skeleton_meta_path):
    raise FileNotFoundError(f"StickSkeleton.asset.meta not found at {skeleton_meta_path}")
with open(skeleton_meta_path, 'r') as f:
    for line in f:
        if line.startswith("guid:"):
            skeleton_guid = line.split(":")[1].strip()
            break
if skeleton_guid is None:
    raise ValueError(f"Could not parse GUID from {skeleton_meta_path}")

write_asset(os.path.join(encounters_dir, "NecromancersStudy.asset"), f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 0}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {ENCOUNTER_SCRIPT_GUID}, type: 3}}
  m_Name: NecromancersStudy
  m_EditorClassIdentifier:
  _encounterName: The Necromancer's Study
  _enemies:
  - enemyData: {{fileID: 11400000, guid: {skeleton_guid}, type: 2}}
    row: 0
    nameOverride: Bone Guard
  - enemyData: {{fileID: 11400000, guid: {skeleton_guid}, type: 2}}
    row: 0
    nameOverride: Bone Guard
  - enemyData: {{fileID: 11400000, guid: {guids["StickNecromancer"]}, type: 2}}
    row: 1
    nameOverride:
  _victoryCondition: 0
  _survivalTurns: 0
  _background: {{fileID: 0}}
  _introLines:
  - You enter a room full of scribbled arcane circles. Someone's been busy.
  - A necromancer stands behind two skeletal guards. They're reading from 'Raising the Dead for Dummies.'
  - The skeletons rattle menacingly. The necromancer sighs. 'Do I have to do everything myself?'
  _victoryLines:
  - The necromancer falls! The skeletons collapse into a pile of lines.
  - Victory! You've defeated dark magic with the power of hitting things.
  _defeatLines:
  - The necromancer adds you to their collection. How embarrassing.
  - Defeated. The necromancer was not, in fact, a beginner.
""")

print("Generated assets:")
for name, guid in guids.items():
    print(f"  {name}: {guid}")
print(f"\nTotal: {len(guids)} assets")
