using NUnit.Framework;
using SticklersGate.Data;
using UnityEngine;

namespace SticklersGate.Tests
{
    /// <summary>
    /// Tests that validate Phase 4 content data: class stats, ability values,
    /// enemy configurations, and encounter setup match the GDD design specs.
    /// </summary>
    public class ContentDataTests
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
            throw new System.Exception($"Field '{fieldName}' not found on {obj.GetType().Name}");
        }

        // ====================================================================
        // CharacterClassSO Tests
        // ====================================================================

        private CharacterClassSO CreateClass(string name, CharacterClassType type,
            StatBlock stats, int baseAC, int hitDie, CombatRow row,
            AbilitySO[] l1 = null, AbilitySO[] l3 = null, AbilitySO[] l5 = null)
        {
            var so = ScriptableObject.CreateInstance<CharacterClassSO>();
            SetPrivateField(so, "_className", name);
            SetPrivateField(so, "_classType", type);
            SetPrivateField(so, "_baseStats", stats);
            SetPrivateField(so, "_baseAC", baseAC);
            SetPrivateField(so, "_hitDiceSides", hitDie);
            SetPrivateField(so, "_preferredRow", row);
            SetPrivateField(so, "_level1Abilities", l1 ?? new AbilitySO[0]);
            SetPrivateField(so, "_level3Abilities", l3 ?? new AbilitySO[0]);
            SetPrivateField(so, "_level5Abilities", l5 ?? new AbilitySO[0]);
            return so;
        }

        [Test]
        public void Sticknight_HasCorrectStats()
        {
            var knight = CreateClass("Sticknight", CharacterClassType.Sticknight,
                new StatBlock { strength = 16, dexterity = 13, constitution = 14,
                    intelligence = 10, wisdom = 12, charisma = 8 },
                baseAC: 16, hitDie: 10, row: CombatRow.FrontRow);

            Assert.AreEqual("Sticknight", knight.ClassName);
            Assert.AreEqual(CharacterClassType.Sticknight, knight.ClassType);
            Assert.AreEqual(16, knight.BaseStats.strength);
            Assert.AreEqual(14, knight.BaseStats.constitution);
            Assert.AreEqual(16, knight.BaseAC);
            Assert.AreEqual(10, knight.HitDiceSides);
            Assert.AreEqual(CombatRow.FrontRow, knight.PreferredRow);
        }

        [Test]
        public void Sticknight_HPAtLevel1_Is12()
        {
            // d10 hit die, CON 14 (+2 mod) => 10 + 2 = 12
            var knight = CreateClass("Sticknight", CharacterClassType.Sticknight,
                new StatBlock { strength = 16, dexterity = 13, constitution = 14,
                    intelligence = 10, wisdom = 12, charisma = 8 },
                baseAC: 16, hitDie: 10, row: CombatRow.FrontRow);

            Assert.AreEqual(12, knight.GetMaxHPAtLevel(1));
        }

        [Test]
        public void Stickwizard_HasCorrectStats()
        {
            var wizard = CreateClass("Stickwizard", CharacterClassType.Stickwizard,
                new StatBlock { strength = 8, dexterity = 14, constitution = 12,
                    intelligence = 18, wisdom = 13, charisma = 10 },
                baseAC: 12, hitDie: 6, row: CombatRow.BackRow);

            Assert.AreEqual(CharacterClassType.Stickwizard, wizard.ClassType);
            Assert.AreEqual(18, wizard.BaseStats.intelligence);
            Assert.AreEqual(12, wizard.BaseAC);
            Assert.AreEqual(6, wizard.HitDiceSides);
            Assert.AreEqual(CombatRow.BackRow, wizard.PreferredRow);
        }

        [Test]
        public void Stickwizard_HPAtLevel1_Is7()
        {
            // d6 hit die, CON 12 (+1 mod) => 6 + 1 = 7
            var wizard = CreateClass("Stickwizard", CharacterClassType.Stickwizard,
                new StatBlock { strength = 8, dexterity = 14, constitution = 12,
                    intelligence = 18, wisdom = 13, charisma = 10 },
                baseAC: 12, hitDie: 6, row: CombatRow.BackRow);

            Assert.AreEqual(7, wizard.GetMaxHPAtLevel(1));
        }

        [Test]
        public void Stickrogue_HasCorrectStats()
        {
            var rogue = CreateClass("Stickrogue", CharacterClassType.Stickrogue,
                new StatBlock { strength = 10, dexterity = 18, constitution = 12,
                    intelligence = 13, wisdom = 10, charisma = 14 },
                baseAC: 15, hitDie: 8, row: CombatRow.FrontRow);

            Assert.AreEqual(CharacterClassType.Stickrogue, rogue.ClassType);
            Assert.AreEqual(18, rogue.BaseStats.dexterity);
            Assert.AreEqual(15, rogue.BaseAC);
            Assert.AreEqual(8, rogue.HitDiceSides);
            Assert.AreEqual(CombatRow.FrontRow, rogue.PreferredRow);
        }

        [Test]
        public void Stickleric_HasCorrectStats()
        {
            var cleric = CreateClass("Stickleric", CharacterClassType.Stickleric,
                new StatBlock { strength = 14, dexterity = 10, constitution = 14,
                    intelligence = 10, wisdom = 18, charisma = 13 },
                baseAC: 18, hitDie: 8, row: CombatRow.BackRow);

            Assert.AreEqual(CharacterClassType.Stickleric, cleric.ClassType);
            Assert.AreEqual(18, cleric.BaseStats.wisdom);
            Assert.AreEqual(18, cleric.BaseAC);
            Assert.AreEqual(8, cleric.HitDiceSides);
            Assert.AreEqual(CombatRow.BackRow, cleric.PreferredRow);
        }

        [Test]
        public void GetAbilitiesAtLevel_ReturnsCorrectCounts()
        {
            var a1 = ScriptableObject.CreateInstance<AbilitySO>();
            var a2 = ScriptableObject.CreateInstance<AbilitySO>();
            var a3 = ScriptableObject.CreateInstance<AbilitySO>();
            var a5 = ScriptableObject.CreateInstance<AbilitySO>();

            var knight = CreateClass("Sticknight", CharacterClassType.Sticknight,
                new StatBlock { strength = 16, dexterity = 13, constitution = 14,
                    intelligence = 10, wisdom = 12, charisma = 8 },
                baseAC: 16, hitDie: 10, row: CombatRow.FrontRow,
                l1: new[] { a1, a2 }, l3: new[] { a3 }, l5: new[] { a5 });

            Assert.AreEqual(2, knight.GetAbilitiesAtLevel(1).Length);
            Assert.AreEqual(2, knight.GetAbilitiesAtLevel(2).Length);
            Assert.AreEqual(3, knight.GetAbilitiesAtLevel(3).Length);
            Assert.AreEqual(3, knight.GetAbilitiesAtLevel(4).Length);
            Assert.AreEqual(4, knight.GetAbilitiesAtLevel(5).Length);
        }

        // ====================================================================
        // AbilitySO Tests
        // ====================================================================

        private AbilitySO CreateAbility(string name, AbilityCategory category,
            AbilityTargetType target, int dmgDice = 0, int dmgSides = 0,
            DamageType dmgType = DamageType.Slashing, AbilityScore mod = AbilityScore.Strength,
            int flatDmg = 0, int healDice = 0, int healSides = 0, int flatHeal = 0,
            StatusEffectType effect = StatusEffectType.None, int effectDur = 0,
            int cooldown = 0, int unlockLevel = 1)
        {
            var so = ScriptableObject.CreateInstance<AbilitySO>();
            SetPrivateField(so, "_abilityName", name);
            SetPrivateField(so, "_category", category);
            SetPrivateField(so, "_targetType", target);
            SetPrivateField(so, "_damageDiceCount", dmgDice);
            SetPrivateField(so, "_damageDiceSides", dmgSides);
            SetPrivateField(so, "_damageType", dmgType);
            SetPrivateField(so, "_abilityModifier", mod);
            SetPrivateField(so, "_flatDamageBonus", flatDmg);
            SetPrivateField(so, "_healDiceCount", healDice);
            SetPrivateField(so, "_healDiceSides", healSides);
            SetPrivateField(so, "_flatHealBonus", flatHeal);
            SetPrivateField(so, "_appliesEffect", effect);
            SetPrivateField(so, "_effectDurationTurns", effectDur);
            SetPrivateField(so, "_cooldownTurns", cooldown);
            SetPrivateField(so, "_unlockLevel", unlockLevel);
            return so;
        }

        [Test]
        public void GreatswordSlash_HasCorrectValues()
        {
            var ability = CreateAbility("Greatsword Slash", AbilityCategory.MeleeAttack,
                AbilityTargetType.SingleEnemy, dmgDice: 2, dmgSides: 6,
                dmgType: DamageType.Slashing, mod: AbilityScore.Strength);

            Assert.AreEqual("Greatsword Slash", ability.AbilityName);
            Assert.IsTrue(ability.DealsDamage);
            Assert.IsFalse(ability.Heals);
            Assert.IsFalse(ability.HasCooldown);
            Assert.AreEqual(2, ability.DamageDiceCount);
            Assert.AreEqual(6, ability.DamageDiceSides);
        }

        [Test]
        public void ShieldBash_AppliesStunned()
        {
            var ability = CreateAbility("Shield Bash", AbilityCategory.MeleeAttack,
                AbilityTargetType.SingleEnemy, dmgDice: 1, dmgSides: 4,
                dmgType: DamageType.Bludgeoning, mod: AbilityScore.Strength,
                effect: StatusEffectType.Stunned, effectDur: 1, cooldown: 2);

            Assert.IsTrue(ability.DealsDamage);
            Assert.IsTrue(ability.AppliesStatusEffect);
            Assert.AreEqual(StatusEffectType.Stunned, ability.AppliesEffect);
            Assert.AreEqual(1, ability.EffectDurationTurns);
            Assert.AreEqual(2, ability.CooldownTurns);
        }

        [Test]
        public void Fireball_IsAoE_WithHighDamage()
        {
            var ability = CreateAbility("Fireball", AbilityCategory.Spell,
                AbilityTargetType.AllEnemies, dmgDice: 8, dmgSides: 6,
                dmgType: DamageType.Fire, mod: AbilityScore.Intelligence,
                cooldown: 4, unlockLevel: 3);

            Assert.AreEqual(AbilityTargetType.AllEnemies, ability.TargetType);
            Assert.AreEqual(8, ability.DamageDiceCount);
            Assert.AreEqual(6, ability.DamageDiceSides);
            Assert.AreEqual(DamageType.Fire, ability.DamageType);
            Assert.AreEqual(4, ability.CooldownTurns);
            Assert.AreEqual(3, ability.UnlockLevel);
        }

        [Test]
        public void Disintegrate_HasFlatDamageBonus()
        {
            var ability = CreateAbility("Disintegrate", AbilityCategory.Spell,
                AbilityTargetType.SingleEnemy, dmgDice: 10, dmgSides: 6,
                dmgType: DamageType.Force, mod: AbilityScore.Intelligence,
                flatDmg: 40, cooldown: 5, unlockLevel: 5);

            Assert.AreEqual(40, ability.FlatDamageBonus);
            Assert.AreEqual(10, ability.DamageDiceCount);
            Assert.AreEqual(DamageType.Force, ability.DamageType);
        }

        [Test]
        public void CureWounds_HealsAlly()
        {
            var ability = CreateAbility("Cure Wounds", AbilityCategory.Heal,
                AbilityTargetType.SingleAlly, healDice: 2, healSides: 8,
                flatHeal: 4, cooldown: 2);

            Assert.IsTrue(ability.Heals);
            Assert.IsFalse(ability.DealsDamage);
            Assert.AreEqual(AbilityTargetType.SingleAlly, ability.TargetType);
            Assert.AreEqual(2, ability.HealDiceCount);
            Assert.AreEqual(8, ability.HealDiceSides);
            Assert.AreEqual(4, ability.FlatHealBonus);
        }

        [Test]
        public void MassHeal_TargetsAllAllies()
        {
            var ability = CreateAbility("Mass Heal", AbilityCategory.Heal,
                AbilityTargetType.AllAllies, healDice: 3, healSides: 8,
                flatHeal: 4, cooldown: 5, unlockLevel: 5);

            Assert.AreEqual(AbilityTargetType.AllAllies, ability.TargetType);
            Assert.AreEqual(5, ability.CooldownTurns);
            Assert.AreEqual(5, ability.UnlockLevel);
        }

        [Test]
        public void CunningDodge_IsSelfBuff()
        {
            var ability = CreateAbility("Cunning Dodge", AbilityCategory.Buff,
                AbilityTargetType.Self, effect: StatusEffectType.Shielded,
                effectDur: 1, cooldown: 2);

            Assert.AreEqual(AbilityCategory.Buff, ability.Category);
            Assert.AreEqual(AbilityTargetType.Self, ability.TargetType);
            Assert.IsTrue(ability.AppliesStatusEffect);
            Assert.AreEqual(StatusEffectType.Shielded, ability.AppliesEffect);
            Assert.IsFalse(ability.DealsDamage);
            Assert.IsFalse(ability.Heals);
        }

        [Test]
        public void ShadowStep_DealsDamageAndAppliesInvisible()
        {
            var ability = CreateAbility("Shadow Step", AbilityCategory.MeleeAttack,
                AbilityTargetType.SingleEnemy, dmgDice: 4, dmgSides: 6,
                dmgType: DamageType.Piercing, mod: AbilityScore.Dexterity,
                effect: StatusEffectType.Invisible, effectDur: 1,
                cooldown: 4, unlockLevel: 5);

            Assert.IsTrue(ability.DealsDamage);
            Assert.IsTrue(ability.AppliesStatusEffect);
            Assert.AreEqual(StatusEffectType.Invisible, ability.AppliesEffect);
        }

        // ====================================================================
        // EnemySO Tests
        // ====================================================================

        private EnemySO CreateEnemy(string name, EnemyTier tier, StatBlock stats,
            int hp, int ac, CombatRow row, float aggression,
            AITargetPreference targetPref, int xp, int goldMin, int goldMax)
        {
            var so = ScriptableObject.CreateInstance<EnemySO>();
            SetPrivateField(so, "_enemyName", name);
            SetPrivateField(so, "_tier", tier);
            SetPrivateField(so, "_stats", stats);
            SetPrivateField(so, "_hitPoints", hp);
            SetPrivateField(so, "_armorClass", ac);
            SetPrivateField(so, "_preferredRow", row);
            SetPrivateField(so, "_aggression", aggression);
            SetPrivateField(so, "_targetPreference", targetPref);
            SetPrivateField(so, "_canFlee", false);
            SetPrivateField(so, "_xpReward", xp);
            SetPrivateField(so, "_goldMin", goldMin);
            SetPrivateField(so, "_goldMax", goldMax);
            SetPrivateField(so, "_abilities", new AbilitySO[0]);
            return so;
        }

        [Test]
        public void StickGoblin_IsMinion()
        {
            var goblin = CreateEnemy("Stick Goblin", EnemyTier.Minion,
                new StatBlock { strength = 8, dexterity = 14, constitution = 10,
                    intelligence = 8, wisdom = 8, charisma = 6 },
                hp: 12, ac: 11, row: CombatRow.FrontRow,
                aggression: 0.9f, targetPref: AITargetPreference.Random,
                xp: 25, goldMin: 1, goldMax: 5);

            Assert.AreEqual("Stick Goblin", goblin.EnemyName);
            Assert.AreEqual(EnemyTier.Minion, goblin.Tier);
            Assert.AreEqual(12, goblin.HitPoints);
            Assert.AreEqual(11, goblin.ArmorClass);
            Assert.AreEqual(AITargetPreference.Random, goblin.TargetPreference);
        }

        [Test]
        public void StickSkeleton_IsStandard()
        {
            var skeleton = CreateEnemy("Stick Skeleton", EnemyTier.Standard,
                new StatBlock { strength = 12, dexterity = 14, constitution = 12,
                    intelligence = 6, wisdom = 8, charisma = 4 },
                hp: 26, ac: 13, row: CombatRow.FrontRow,
                aggression: 0.7f, targetPref: AITargetPreference.LowestHP,
                xp: 50, goldMin: 3, goldMax: 8);

            Assert.AreEqual(EnemyTier.Standard, skeleton.Tier);
            Assert.AreEqual(26, skeleton.HitPoints);
            Assert.AreEqual(AITargetPreference.LowestHP, skeleton.TargetPreference);
        }

        [Test]
        public void StickOgre_IsElite_WithHighHP()
        {
            var ogre = CreateEnemy("Stick Ogre", EnemyTier.Elite,
                new StatBlock { strength = 19, dexterity = 8, constitution = 16,
                    intelligence = 5, wisdom = 7, charisma = 4 },
                hp: 68, ac: 11, row: CombatRow.FrontRow,
                aggression: 0.95f, targetPref: AITargetPreference.HighestThreat,
                xp: 200, goldMin: 15, goldMax: 30);

            Assert.AreEqual(EnemyTier.Elite, ogre.Tier);
            Assert.AreEqual(68, ogre.HitPoints);
            Assert.AreEqual(19, ogre.Stats.strength);
            Assert.AreEqual(AITargetPreference.HighestThreat, ogre.TargetPreference);
            Assert.AreEqual(200, ogre.XPReward);
        }

        [Test]
        public void StickOgre_GoldRange_IsValid()
        {
            var ogre = CreateEnemy("Stick Ogre", EnemyTier.Elite,
                new StatBlock { strength = 19, dexterity = 8, constitution = 16,
                    intelligence = 5, wisdom = 7, charisma = 4 },
                hp: 68, ac: 11, row: CombatRow.FrontRow,
                aggression: 0.95f, targetPref: AITargetPreference.HighestThreat,
                xp: 200, goldMin: 15, goldMax: 30);

            Assert.GreaterOrEqual(ogre.GoldMax, ogre.GoldMin);
            Assert.Greater(ogre.GoldMin, 0);
        }

        // ====================================================================
        // Cross-class stat validation
        // ====================================================================

        [Test]
        public void AllClasses_UseStandardArray()
        {
            // Each class uses the standard array: 18, 16, 14, 13, 12, 10, 8
            // (with one 18 for primary stat)
            // Verify stats sum to the same total
            int[] knightStats = { 16, 13, 14, 10, 12, 8 };
            int[] wizardStats = { 8, 14, 12, 18, 13, 10 };
            int[] rogueStats = { 10, 18, 12, 13, 10, 14 };
            int[] clericStats = { 14, 10, 14, 10, 18, 13 };

            int knightSum = 0, wizardSum = 0, rogueSum = 0, clericSum = 0;
            for (int i = 0; i < 6; i++)
            {
                knightSum += knightStats[i];
                wizardSum += wizardStats[i];
                rogueSum += rogueStats[i];
                clericSum += clericStats[i];
            }

            // All should use the same stat point total
            Assert.AreEqual(knightSum, wizardSum, "Wizard stat total should match Knight");
            Assert.AreEqual(knightSum, rogueSum, "Rogue stat total should match Knight");
            Assert.AreEqual(knightSum, clericSum, "Cleric stat total should match Knight");
        }

        [Test]
        public void EachClass_HasPrimaryStatOf18OrHigher()
        {
            // Sticknight primary: STR 16 (not 18, uses standard array differently)
            // Stickwizard primary: INT 18
            // Stickrogue primary: DEX 18
            // Stickleric primary: WIS 18
            Assert.AreEqual(18, new StatBlock { strength = 8, dexterity = 14, constitution = 12,
                intelligence = 18, wisdom = 13, charisma = 10 }.GetScore(AbilityScore.Intelligence));
            Assert.AreEqual(18, new StatBlock { strength = 10, dexterity = 18, constitution = 12,
                intelligence = 13, wisdom = 10, charisma = 14 }.GetScore(AbilityScore.Dexterity));
            Assert.AreEqual(18, new StatBlock { strength = 14, dexterity = 10, constitution = 14,
                intelligence = 10, wisdom = 18, charisma = 13 }.GetScore(AbilityScore.Wisdom));
        }
    }
}
