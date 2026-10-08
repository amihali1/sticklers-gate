using System.Collections.Generic;
using NUnit.Framework;
using SticklersGate.Combat;
using SticklersGate.Data;

namespace SticklersGate.Tests
{
    /// <summary>
    /// Tests for grid areas of effect (sphere / emanation), friendly fire
    /// filtering, and enemy AI area scoring.
    /// </summary>
    public class AreaTargetingTests
    {
        private CombatGrid _grid;

        [SetUp]
        public void SetUp()
        {
            _grid = new CombatGrid(8, 6);
        }

        private static AbilitySO Sphere(int radius, int range, AreaAffects affects = AreaAffects.AllCreatures)
        {
            var a = TestUnits.Ability("Sphere", AbilityCategory.Spell, AbilityTargetType.Point, 2, 6, DamageType.Fire, range);
            TestUnits.Set(a, "_areaShape", AreaShape.Sphere);
            TestUnits.Set(a, "_areaRadius", radius);
            TestUnits.Set(a, "_areaAffects", affects);
            return a;
        }

        private static AbilitySO Emanation(int radius, AreaAffects affects = AreaAffects.AllCreatures)
        {
            var a = TestUnits.Ability("Aura", AbilityCategory.MeleeAttack, AbilityTargetType.Self, 1, 10, DamageType.Bludgeoning, 0);
            TestUnits.Set(a, "_areaShape", AreaShape.Emanation);
            TestUnits.Set(a, "_areaRadius", radius);
            TestUnits.Set(a, "_areaAffects", affects);
            return a;
        }

        [Test]
        public void SphereTiles_IncludeCenterAndRadius()
        {
            var caster = TestUnits.Player("Wiz", new GridPosition(0, 0));
            var tiles = AreaTargeting.GetAreaTiles(_grid, Sphere(1, 6), caster, new GridPosition(4, 3));

            Assert.AreEqual(5, tiles.Count);
            CollectionAssert.Contains(tiles, new GridPosition(4, 3));
            CollectionAssert.Contains(tiles, new GridPosition(5, 3));
        }

        [Test]
        public void EmanationTiles_SurroundCasterButExcludeIt()
        {
            var caster = TestUnits.Enemy("Ogre", new GridPosition(4, 3));
            var tiles = AreaTargeting.GetAreaTiles(_grid, Emanation(1), caster, new GridPosition(0, 0));

            Assert.AreEqual(4, tiles.Count);
            CollectionAssert.DoesNotContain(tiles, new GridPosition(4, 3));
        }

        [Test]
        public void IsValidCenter_RespectsRangeAndBounds()
        {
            var caster = TestUnits.Player("Wiz", new GridPosition(0, 0));
            var ability = Sphere(2, 4);

            Assert.IsTrue(AreaTargeting.IsValidCenter(_grid, ability, caster, new GridPosition(2, 2)));
            Assert.IsTrue(AreaTargeting.IsValidCenter(_grid, ability, caster, new GridPosition(0, 0)), "Can center on self.");
            Assert.IsFalse(AreaTargeting.IsValidCenter(_grid, ability, caster, new GridPosition(3, 2)), "Distance 5 > range 4.");
            Assert.IsFalse(AreaTargeting.IsValidCenter(_grid, ability, caster, new GridPosition(-1, 0)));
        }

        [Test]
        public void Sphere_HitsAlliesInTheBlast_FriendlyFire()
        {
            var wizard = TestUnits.Player("Wiz", new GridPosition(0, 2));
            var knight = TestUnits.Player("Knight", new GridPosition(4, 2));
            var goblin = TestUnits.Enemy("Goblin", new GridPosition(5, 2));
            var farGoblin = TestUnits.Enemy("Far", new GridPosition(7, 5));

            var hit = AreaTargeting.GetAffectedUnits(Sphere(1, 6), wizard, new GridPosition(5, 2),
                new List<CombatUnit> { wizard, knight, goblin, farGoblin });

            CollectionAssert.AreEquivalent(new[] { knight, goblin }, hit);
        }

        [Test]
        public void Sphere_CanCatchTheCaster()
        {
            var wizard = TestUnits.Player("Wiz", new GridPosition(2, 2));
            var hit = AreaTargeting.GetAffectedUnits(Sphere(1, 6), wizard, new GridPosition(3, 2),
                new List<CombatUnit> { wizard });

            Assert.Contains(wizard, hit);
        }

        [Test]
        public void AlliesOnly_SkipsEnemiesAndReachesDownedAllies()
        {
            var heal = Sphere(2, 4, AreaAffects.AlliesOnly);
            TestUnits.Set(heal, "_damageDiceCount", 0);
            TestUnits.Set(heal, "_healDiceCount", 3);
            TestUnits.Set(heal, "_healDiceSides", 8);

            var cleric = TestUnits.Player("Cleric", new GridPosition(1, 1));
            var downed = TestUnits.Player("Downed", new GridPosition(2, 1));
            downed.TakeDamage(999);
            var goblin = TestUnits.Enemy("Goblin", new GridPosition(2, 2));

            var hit = AreaTargeting.GetAffectedUnits(heal, cleric, new GridPosition(2, 1),
                new List<CombatUnit> { cleric, downed, goblin });

            CollectionAssert.AreEquivalent(new[] { cleric, downed }, hit);
        }

        [Test]
        public void DamagingArea_SkipsDownedAndDeadUnits()
        {
            var wizard = TestUnits.Player("Wiz", new GridPosition(0, 0));
            var downed = TestUnits.Player("Downed", new GridPosition(4, 2));
            downed.TakeDamage(999);
            var dead = TestUnits.Enemy("Dead", new GridPosition(5, 2));
            dead.Die();

            var hit = AreaTargeting.GetAffectedUnits(Sphere(1, 6), wizard, new GridPosition(4, 2),
                new List<CombatUnit> { downed, dead });

            Assert.IsEmpty(hit);
        }

        [Test]
        public void Emanation_NeverHitsTheCaster()
        {
            var ogre = TestUnits.Enemy("Ogre", new GridPosition(3, 3));
            var knight = TestUnits.Player("Knight", new GridPosition(3, 2));

            var hit = AreaTargeting.GetAffectedUnits(Emanation(1), ogre, ogre.Position,
                new List<CombatUnit> { ogre, knight });

            CollectionAssert.AreEquivalent(new[] { knight }, hit);
        }

        [Test]
        public void AllyAura_CoversTheCasterAndAllies()
        {
            var paladin = TestUnits.Player("Paladin", new GridPosition(3, 3));
            var cleric = TestUnits.Player("Cleric", new GridPosition(3, 1));
            var goblin = TestUnits.Enemy("Goblin", new GridPosition(4, 3));

            var hit = AreaTargeting.GetAffectedUnits(Emanation(2, AreaAffects.AlliesOnly), paladin, paladin.Position,
                new List<CombatUnit> { paladin, cleric, goblin });

            CollectionAssert.AreEquivalent(new[] { paladin, cleric }, hit);
        }

        [Test]
        public void AI_SphereAimsAtTheBiggestCluster()
        {
            var spider = TestUnits.Enemy("Spider", new GridPosition(7, 0));
            var lone = TestUnits.Player("Lone", new GridPosition(7, 3));
            var a = TestUnits.Player("A", new GridPosition(5, 1));
            var b = TestUnits.Player("B", new GridPosition(5, 2));

            int value = EnemyAI.BestAreaValue(spider, Sphere(1, 4), new List<CombatUnit> { lone, a, b },
                new List<CombatUnit> { spider }, out var aimAt);

            Assert.AreEqual(2, value);
            Assert.That(aimAt == a || aimAt == b);
        }

        [Test]
        public void AI_SphereSubtractsFriendsCaught()
        {
            var spider = TestUnits.Enemy("Spider", new GridPosition(7, 0));
            var goblin = TestUnits.Enemy("Goblin", new GridPosition(5, 2));
            var knight = TestUnits.Player("Knight", new GridPosition(5, 1));

            int value = EnemyAI.BestAreaValue(spider, Sphere(1, 4), new List<CombatUnit> { knight },
                new List<CombatUnit> { spider, goblin }, out _);

            Assert.LessOrEqual(value, 0, "Hitting one foe and one friend isn't worth it.");
        }

        [Test]
        public void AI_EmanationWithNoFoesInReach_IsWorthless()
        {
            var ogre = TestUnits.Enemy("Ogre", new GridPosition(7, 5));
            var knight = TestUnits.Player("Knight", new GridPosition(0, 0));

            int value = EnemyAI.BestAreaValue(ogre, Emanation(1), new List<CombatUnit> { knight },
                new List<CombatUnit> { ogre }, out var aimAt);

            Assert.AreEqual(0, value);
            Assert.IsNull(aimAt);
        }
    }
}
