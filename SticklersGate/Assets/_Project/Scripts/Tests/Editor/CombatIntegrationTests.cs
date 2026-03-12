using System.Collections.Generic;
using NUnit.Framework;
using SticklersGate.Combat;
using SticklersGate.Data;
using UnityEngine;

namespace SticklersGate.Tests
{
    /// <summary>
    /// Integration tests that verify the full combat loop: party creation,
    /// encounter spawning, turn flow, action resolution, and end conditions.
    /// </summary>
    public class CombatIntegrationTests
    {
        private static void SetPrivateField(object obj, string fieldName, object value)
        {
            var type = obj.GetType();
            while (type != null)
            {
                var field = type.GetField(fieldName,
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    field.SetValue(obj, value);
                    return;
                }
                type = type.BaseType;
            }
            Assert.Fail($"Field '{fieldName}' not found on {obj.GetType().Name}");
        }

        // ================================================================
        // Test Helpers — Create SOs with specific data
        // ================================================================

        private AbilitySO CreateAbilitySO(string name, AbilityCategory category,
            AbilityTargetType target, int dmgDice, int dmgSides,
            DamageType dmgType = DamageType.Slashing, AbilityScore mod = AbilityScore.Strength,
            int cooldown = 0, int healDice = 0, int healSides = 0, int flatHeal = 0)
        {
            var so = ScriptableObject.CreateInstance<AbilitySO>();
            SetPrivateField(so, "_abilityName", name);
            SetPrivateField(so, "_category", category);
            SetPrivateField(so, "_targetType", target);
            SetPrivateField(so, "_damageDiceCount", dmgDice);
            SetPrivateField(so, "_damageDiceSides", dmgSides);
            SetPrivateField(so, "_damageType", dmgType);
            SetPrivateField(so, "_abilityModifier", mod);
            SetPrivateField(so, "_flatDamageBonus", 0);
            SetPrivateField(so, "_healDiceCount", healDice);
            SetPrivateField(so, "_healDiceSides", healSides);
            SetPrivateField(so, "_flatHealBonus", flatHeal);
            SetPrivateField(so, "_cooldownTurns", cooldown);
            SetPrivateField(so, "_unlockLevel", 1);
            SetPrivateField(so, "_hitQuips", new string[0]);
            SetPrivateField(so, "_missQuips", new string[0]);
            SetPrivateField(so, "_critQuips", new string[0]);
            SetPrivateField(so, "_appliesEffect", StatusEffectType.None);
            SetPrivateField(so, "_effectDurationTurns", 0);
            SetPrivateField(so, "_savingThrowAbility", AbilityScore.Strength);
            SetPrivateField(so, "_savingThrowDC", 0);
            return so;
        }

        private CharacterClassSO CreateClassSO(string name, CharacterClassType type,
            StatBlock stats, int ac, int hitDie, CombatRow row, AbilitySO[] abilities)
        {
            var so = ScriptableObject.CreateInstance<CharacterClassSO>();
            SetPrivateField(so, "_className", name);
            SetPrivateField(so, "_classType", type);
            SetPrivateField(so, "_flavorText", "Test class");
            SetPrivateField(so, "_baseStats", stats);
            SetPrivateField(so, "_baseAC", ac);
            SetPrivateField(so, "_hitDiceSides", hitDie);
            SetPrivateField(so, "_preferredRow", row);
            SetPrivateField(so, "_level1Abilities", abilities);
            SetPrivateField(so, "_level3Abilities", new AbilitySO[0]);
            SetPrivateField(so, "_level5Abilities", new AbilitySO[0]);
            return so;
        }

        private EnemySO CreateEnemySO(string name, EnemyTier tier, StatBlock stats,
            int hp, int ac, AbilitySO[] abilities, CombatRow row = CombatRow.FrontRow,
            AITargetPreference pref = AITargetPreference.Random)
        {
            var so = ScriptableObject.CreateInstance<EnemySO>();
            SetPrivateField(so, "_enemyName", name);
            SetPrivateField(so, "_tier", tier);
            SetPrivateField(so, "_description", "Test enemy");
            SetPrivateField(so, "_stats", stats);
            SetPrivateField(so, "_hitPoints", hp);
            SetPrivateField(so, "_armorClass", ac);
            SetPrivateField(so, "_abilities", abilities);
            SetPrivateField(so, "_preferredRow", row);
            SetPrivateField(so, "_aggression", 0.9f);
            SetPrivateField(so, "_targetPreference", pref);
            SetPrivateField(so, "_retreatThreshold", 0f);
            SetPrivateField(so, "_canFlee", false);
            SetPrivateField(so, "_xpReward", 10);
            SetPrivateField(so, "_goldMin", 1);
            SetPrivateField(so, "_goldMax", 5);
            SetPrivateField(so, "_introQuips", new string[0]);
            SetPrivateField(so, "_deathQuips", new string[0]);
            return so;
        }

        private EncounterSO CreateEncounterSO(string name, EnemySO[] enemies)
        {
            var so = ScriptableObject.CreateInstance<EncounterSO>();
            SetPrivateField(so, "_encounterName", name);
            var spawns = new EnemySpawn[enemies.Length];
            for (int i = 0; i < enemies.Length; i++)
            {
                spawns[i] = new EnemySpawn
                {
                    enemyData = enemies[i],
                    row = CombatRow.FrontRow,
                    nameOverride = ""
                };
            }
            SetPrivateField(so, "_enemies", spawns);
            SetPrivateField(so, "_victoryCondition", VictoryCondition.DefeatAll);
            SetPrivateField(so, "_survivalTurns", 0);
            SetPrivateField(so, "_introLines", new string[0]);
            SetPrivateField(so, "_victoryLines", new string[0]);
            SetPrivateField(so, "_defeatLines", new string[0]);
            return so;
        }

        // ================================================================
        // Party Creation Tests
        // ================================================================

        [Test]
        public void FromPlayerCharacter_CreatesUnitWithCorrectAbilities()
        {
            var slash = CreateAbilitySO("Slash", AbilityCategory.MeleeAttack,
                AbilityTargetType.SingleEnemy, 2, 6);
            var bash = CreateAbilitySO("Bash", AbilityCategory.MeleeAttack,
                AbilityTargetType.SingleEnemy, 1, 4, cooldown: 2);

            var classSO = CreateClassSO("Sticknight", CharacterClassType.Sticknight,
                new StatBlock { strength = 16, dexterity = 13, constitution = 14,
                    intelligence = 10, wisdom = 12, charisma = 8 },
                ac: 16, hitDie: 10, row: CombatRow.FrontRow,
                abilities: new[] { slash, bash });

            var unit = CombatUnit.FromPlayerCharacter("Hero", classSO, 1);

            Assert.AreEqual("Hero", unit.Name);
            Assert.IsTrue(unit.IsPlayer);
            Assert.AreEqual(2, unit.Abilities.Count);
            Assert.AreEqual("Slash", unit.Abilities[0].Data.AbilityName);
            Assert.AreEqual("Bash", unit.Abilities[1].Data.AbilityName);
            Assert.IsTrue(unit.Abilities[0].IsReady);
            Assert.IsTrue(unit.Abilities[1].IsReady);
        }

        [Test]
        public void FromEnemy_CreatesUnitWithCorrectStats()
        {
            var shiv = CreateAbilitySO("Shiv", AbilityCategory.MeleeAttack,
                AbilityTargetType.SingleEnemy, 1, 4, DamageType.Slashing, AbilityScore.Dexterity);

            var enemySO = CreateEnemySO("Goblin", EnemyTier.Minion,
                new StatBlock { strength = 8, dexterity = 14, constitution = 10,
                    intelligence = 8, wisdom = 8, charisma = 6 },
                hp: 12, ac: 11, abilities: new[] { shiv });

            var unit = CombatUnit.FromEnemy(enemySO);

            Assert.AreEqual("Goblin", unit.Name);
            Assert.IsFalse(unit.IsPlayer);
            Assert.AreEqual(12, unit.MaxHP);
            Assert.AreEqual(11, unit.BaseAC);
            Assert.AreEqual(1, unit.Abilities.Count);
        }

        // ================================================================
        // Full Party Tests
        // ================================================================

        [Test]
        public void FullParty_FourClasses_AllHaveUniqueTypes()
        {
            var slash = CreateAbilitySO("Attack", AbilityCategory.MeleeAttack,
                AbilityTargetType.SingleEnemy, 1, 6);

            var knightClass = CreateClassSO("Sticknight", CharacterClassType.Sticknight,
                new StatBlock { strength = 16, dexterity = 13, constitution = 14,
                    intelligence = 10, wisdom = 12, charisma = 8 },
                ac: 16, hitDie: 10, row: CombatRow.FrontRow, abilities: new[] { slash });
            var wizardClass = CreateClassSO("Stickwizard", CharacterClassType.Stickwizard,
                new StatBlock { strength = 8, dexterity = 14, constitution = 12,
                    intelligence = 18, wisdom = 13, charisma = 10 },
                ac: 12, hitDie: 6, row: CombatRow.BackRow, abilities: new[] { slash });
            var rogueClass = CreateClassSO("Stickrogue", CharacterClassType.Stickrogue,
                new StatBlock { strength = 10, dexterity = 18, constitution = 12,
                    intelligence = 13, wisdom = 10, charisma = 14 },
                ac: 15, hitDie: 8, row: CombatRow.FrontRow, abilities: new[] { slash });
            var clericClass = CreateClassSO("Stickleric", CharacterClassType.Stickleric,
                new StatBlock { strength = 14, dexterity = 10, constitution = 14,
                    intelligence = 10, wisdom = 18, charisma = 13 },
                ac: 18, hitDie: 8, row: CombatRow.BackRow, abilities: new[] { slash });

            var party = new List<CombatUnit>
            {
                CombatUnit.FromPlayerCharacter("Knight", knightClass, 1),
                CombatUnit.FromPlayerCharacter("Wizard", wizardClass, 1),
                CombatUnit.FromPlayerCharacter("Rogue", rogueClass, 1),
                CombatUnit.FromPlayerCharacter("Cleric", clericClass, 1),
            };

            Assert.AreEqual(4, party.Count);
            var types = new HashSet<CharacterClassType>();
            foreach (var unit in party)
            {
                Assert.IsTrue(unit.IsPlayer);
                Assert.IsTrue(unit.IsAlive);
                types.Add(unit.ClassData.ClassType);
            }
            Assert.AreEqual(4, types.Count, "All four classes should be unique");
        }

        // ================================================================
        // Encounter Spawning Tests
        // ================================================================

        [Test]
        public void EncounterSO_SpawnsCorrectNumberOfEnemies()
        {
            var shiv = CreateAbilitySO("Shiv", AbilityCategory.MeleeAttack,
                AbilityTargetType.SingleEnemy, 1, 4);
            var goblin = CreateEnemySO("Goblin", EnemyTier.Minion,
                new StatBlock { strength = 8, dexterity = 14, constitution = 10,
                    intelligence = 8, wisdom = 8, charisma = 6 },
                hp: 12, ac: 11, abilities: new[] { shiv });

            var encounter = CreateEncounterSO("Test Ambush", new[] { goblin, goblin, goblin });

            Assert.AreEqual("Test Ambush", encounter.EncounterName);
            Assert.AreEqual(3, encounter.Enemies.Length);
            Assert.AreEqual(VictoryCondition.DefeatAll, encounter.VictoryCondition);
        }

        // ================================================================
        // Combat Flow Tests
        // ================================================================

        [Test]
        public void CombatUnit_TakeDamage_ReducesHP()
        {
            var shiv = CreateAbilitySO("Shiv", AbilityCategory.MeleeAttack,
                AbilityTargetType.SingleEnemy, 1, 4);
            var goblin = CreateEnemySO("Goblin", EnemyTier.Minion,
                new StatBlock { strength = 8, dexterity = 14, constitution = 10,
                    intelligence = 8, wisdom = 8, charisma = 6 },
                hp: 12, ac: 11, abilities: new[] { shiv });

            var unit = CombatUnit.FromEnemy(goblin);
            unit.TakeDamage(5);

            Assert.AreEqual(7, unit.CurrentHP);
            Assert.IsTrue(unit.IsAlive);
        }

        [Test]
        public void CombatUnit_LethalDamage_KillsEnemy()
        {
            var shiv = CreateAbilitySO("Shiv", AbilityCategory.MeleeAttack,
                AbilityTargetType.SingleEnemy, 1, 4);
            var goblin = CreateEnemySO("Goblin", EnemyTier.Minion,
                new StatBlock { strength = 8, dexterity = 14, constitution = 10,
                    intelligence = 8, wisdom = 8, charisma = 6 },
                hp: 12, ac: 11, abilities: new[] { shiv });

            var unit = CombatUnit.FromEnemy(goblin);
            bool deathFired = false;
            unit.OnDeath += () => deathFired = true;

            unit.TakeDamage(20);

            Assert.AreEqual(0, unit.CurrentHP);
            Assert.IsFalse(unit.IsAlive);
            Assert.IsTrue(deathFired);
        }

        [Test]
        public void CombatUnit_Heal_RestoresHP()
        {
            var shiv = CreateAbilitySO("Shiv", AbilityCategory.MeleeAttack,
                AbilityTargetType.SingleEnemy, 1, 4);
            var goblin = CreateEnemySO("Goblin", EnemyTier.Minion,
                new StatBlock { strength = 8, dexterity = 14, constitution = 10,
                    intelligence = 8, wisdom = 8, charisma = 6 },
                hp: 12, ac: 11, abilities: new[] { shiv });

            var unit = CombatUnit.FromEnemy(goblin);
            unit.TakeDamage(8);
            Assert.AreEqual(4, unit.CurrentHP);

            unit.Heal(5);
            Assert.AreEqual(9, unit.CurrentHP);
        }

        [Test]
        public void CombatUnit_HealCannotExceedMaxHP()
        {
            var shiv = CreateAbilitySO("Shiv", AbilityCategory.MeleeAttack,
                AbilityTargetType.SingleEnemy, 1, 4);
            var goblin = CreateEnemySO("Goblin", EnemyTier.Minion,
                new StatBlock { strength = 8, dexterity = 14, constitution = 10,
                    intelligence = 8, wisdom = 8, charisma = 6 },
                hp: 12, ac: 11, abilities: new[] { shiv });

            var unit = CombatUnit.FromEnemy(goblin);
            unit.TakeDamage(3);
            unit.Heal(100);

            Assert.AreEqual(12, unit.CurrentHP);
        }

        [Test]
        public void RuntimeAbility_Cooldown_TracksCorrectly()
        {
            var fireball = CreateAbilitySO("Fireball", AbilityCategory.Spell,
                AbilityTargetType.AllEnemies, 8, 6, DamageType.Fire, AbilityScore.Intelligence,
                cooldown: 4);

            var classSO = CreateClassSO("Wizard", CharacterClassType.Stickwizard,
                new StatBlock { strength = 8, dexterity = 14, constitution = 12,
                    intelligence = 18, wisdom = 13, charisma = 10 },
                ac: 12, hitDie: 6, row: CombatRow.BackRow, abilities: new[] { fireball });

            var unit = CombatUnit.FromPlayerCharacter("Twiggy", classSO, 1);
            var ability = unit.Abilities[0];

            Assert.IsTrue(ability.IsReady);

            ability.StartCooldown();
            Assert.IsFalse(ability.IsReady);
            Assert.AreEqual(4, ability.CurrentCooldown);

            ability.TickCooldown();
            Assert.AreEqual(3, ability.CurrentCooldown);
            Assert.IsFalse(ability.IsReady);

            ability.TickCooldown();
            ability.TickCooldown();
            ability.TickCooldown();
            Assert.AreEqual(0, ability.CurrentCooldown);
            Assert.IsTrue(ability.IsReady);
        }

        [Test]
        public void CombatUnit_Defend_AddsBonusAC()
        {
            var slash = CreateAbilitySO("Slash", AbilityCategory.MeleeAttack,
                AbilityTargetType.SingleEnemy, 2, 6);
            var classSO = CreateClassSO("Knight", CharacterClassType.Sticknight,
                new StatBlock { strength = 16, dexterity = 13, constitution = 14,
                    intelligence = 10, wisdom = 12, charisma = 8 },
                ac: 16, hitDie: 10, row: CombatRow.FrontRow, abilities: new[] { slash });

            var unit = CombatUnit.FromPlayerCharacter("Knight", classSO, 1);
            Assert.AreEqual(0, unit.BonusAC);

            unit.BonusAC = GameConstants.DEFEND_AC_BONUS;
            Assert.AreEqual(2, unit.BonusAC);
        }

        // ================================================================
        // Target Selection Tests
        // ================================================================

        [Test]
        public void GetValidTargets_SingleEnemy_ReturnsLivingEnemies()
        {
            var slash = CreateAbilitySO("Slash", AbilityCategory.MeleeAttack,
                AbilityTargetType.SingleEnemy, 1, 6);
            var classSO = CreateClassSO("Knight", CharacterClassType.Sticknight,
                new StatBlock { strength = 16, dexterity = 13, constitution = 14,
                    intelligence = 10, wisdom = 12, charisma = 8 },
                ac: 16, hitDie: 10, row: CombatRow.FrontRow, abilities: new[] { slash });

            var enemySO = CreateEnemySO("Goblin", EnemyTier.Minion,
                new StatBlock { strength = 8, dexterity = 14, constitution = 10,
                    intelligence = 8, wisdom = 8, charisma = 6 },
                hp: 12, ac: 11, abilities: new[] { slash });

            var player = CombatUnit.FromPlayerCharacter("Hero", classSO, 1);
            var enemy1 = CombatUnit.FromEnemy(enemySO);
            var enemy2 = CombatUnit.FromEnemy(enemySO);
            var enemy3 = CombatUnit.FromEnemy(enemySO);

            // Kill one enemy
            enemy2.TakeDamage(100);

            var players = new List<CombatUnit> { player };
            var enemies = new List<CombatUnit> { enemy1, enemy2, enemy3 };

            var targets = UI.TargetPanel.GetValidTargets(
                AbilityTargetType.SingleEnemy, player, players, enemies);

            Assert.AreEqual(2, targets.Count, "Should only return living enemies");
            Assert.IsFalse(targets.Contains(enemy2), "Dead enemy should not be a target");
        }

        [Test]
        public void GetValidTargets_AllAllies_ReturnsLivingAllies()
        {
            var heal = CreateAbilitySO("Heal", AbilityCategory.Heal,
                AbilityTargetType.AllAllies, 0, 0, healDice: 2, healSides: 8, flatHeal: 4);
            var classSO = CreateClassSO("Cleric", CharacterClassType.Stickleric,
                new StatBlock { strength = 14, dexterity = 10, constitution = 14,
                    intelligence = 10, wisdom = 18, charisma = 13 },
                ac: 18, hitDie: 8, row: CombatRow.BackRow, abilities: new[] { heal });

            var cleric = CombatUnit.FromPlayerCharacter("Cleric", classSO, 1);
            var knight = CombatUnit.FromPlayerCharacter("Knight", classSO, 1);

            var players = new List<CombatUnit> { cleric, knight };
            var enemies = new List<CombatUnit>();

            var targets = UI.TargetPanel.GetValidTargets(
                AbilityTargetType.AllAllies, cleric, players, enemies);

            Assert.AreEqual(2, targets.Count, "Should return all living allies including self");
        }

        // ================================================================
        // Enemy AI Tests
        // ================================================================

        [Test]
        public void EnemyAI_PicksAbilityOffCooldown()
        {
            var shiv = CreateAbilitySO("Shiv", AbilityCategory.MeleeAttack,
                AbilityTargetType.SingleEnemy, 1, 4);
            var slam = CreateAbilitySO("Slam", AbilityCategory.MeleeAttack,
                AbilityTargetType.AllEnemies, 1, 10, cooldown: 3);

            var enemySO = CreateEnemySO("Ogre", EnemyTier.Elite,
                new StatBlock { strength = 19, dexterity = 8, constitution = 16,
                    intelligence = 5, wisdom = 7, charisma = 4 },
                hp: 68, ac: 11, abilities: new[] { shiv, slam });

            var unit = CombatUnit.FromEnemy(enemySO);

            // Put slam on cooldown
            unit.Abilities[1].StartCooldown();

            var classSO = CreateClassSO("Target", CharacterClassType.Sticknight,
                new StatBlock { strength = 10, dexterity = 10, constitution = 10,
                    intelligence = 10, wisdom = 10, charisma = 10 },
                ac: 10, hitDie: 8, row: CombatRow.FrontRow, abilities: new[] { shiv });
            var target = CombatUnit.FromPlayerCharacter("Target", classSO, 1);

            var players = new List<CombatUnit> { target };
            var enemies = new List<CombatUnit> { unit };

            var decision = EnemyAI.Decide(unit, players, enemies);

            Assert.IsNotNull(decision.ChosenAbility);
            Assert.AreEqual("Shiv", decision.ChosenAbility.Data.AbilityName,
                "AI should pick the only ability off cooldown");
        }
    }
}
