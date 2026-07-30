using NUnit.Framework;
using UnityEngine;
using SticklersGate.Combat;
using SticklersGate.Data;

namespace SticklersGate.Tests
{
    /// <summary>
    /// Tests for grid-aware enemy movement (EnemyAI.DecideMovement): melee closes
    /// to adjacency, ranged holds at weapon range, edge cases return current tile.
    /// </summary>
    public class EnemyAIMovementTests
    {
        private CombatGrid _grid;

        [SetUp]
        public void SetUp()
        {
            _grid = new CombatGrid(8, 6);
        }

        private static AbilitySO MakeAbility(int range)
        {
            var so = ScriptableObject.CreateInstance<AbilitySO>();
            SetField(so, "_abilityName", "Attack");
            SetField(so, "_targetType", AbilityTargetType.SingleEnemy);
            SetField(so, "_damageDiceCount", 1);
            SetField(so, "_damageDiceSides", 6);
            SetField(so, "_range", range);
            return so;
        }

        private static CombatUnit MakeEnemy(int movementSpeed, int abilityRange)
        {
            var so = ScriptableObject.CreateInstance<EnemySO>();
            SetField(so, "_enemyName", "Mover");
            SetField(so, "_hitPoints", 20);
            SetField(so, "_armorClass", 12);
            SetField(so, "_stats", new StatBlock
            {
                strength = 10, dexterity = 10, constitution = 10,
                intelligence = 10, wisdom = 10, charisma = 10
            });
            SetField(so, "_abilities", new[] { MakeAbility(abilityRange) });
            SetField(so, "_movementSpeed", movementSpeed);
            return CombatUnit.FromEnemy(so);
        }

        private CombatUnit PlaceEnemy(int movementSpeed, int abilityRange, GridPosition pos)
        {
            var unit = MakeEnemy(movementSpeed, abilityRange);
            unit.Position = pos;
            _grid.PlaceOccupant(unit, pos);
            return unit;
        }

        private CombatUnit PlaceTarget(GridPosition pos)
        {
            var unit = MakeEnemy(3, 1); // reuse factory; only Position/alive matter
            unit.Position = pos;
            _grid.PlaceOccupant(unit, pos);
            return unit;
        }

        [Test]
        public void Melee_ClosesTowardTarget()
        {
            var enemy = PlaceEnemy(movementSpeed: 3, abilityRange: 1, new GridPosition(0, 0));
            var target = PlaceTarget(new GridPosition(5, 0));

            var dest = EnemyAI.DecideMovement(enemy, _grid, new System.Collections.Generic.List<CombatUnit> { target });

            // Range-1 target 5 away, 3 movement → closest reachable footing is distance 2.
            Assert.AreEqual(2, dest.ManhattanDistanceTo(target.Position));
            Assert.Less(dest.ManhattanDistanceTo(target.Position),
                        enemy.Position.ManhattanDistanceTo(target.Position));
        }

        [Test]
        public void Melee_StopsAdjacentWhenClose()
        {
            var enemy = PlaceEnemy(movementSpeed: 3, abilityRange: 1, new GridPosition(0, 0));
            var target = PlaceTarget(new GridPosition(2, 0));

            var dest = EnemyAI.DecideMovement(enemy, _grid, new System.Collections.Generic.List<CombatUnit> { target });

            // Range-1 melee wants distance 1 (adjacent). Which adjacent tile is
            // order-dependent, so assert the resulting distance, not the tile.
            Assert.AreEqual(1, dest.ManhattanDistanceTo(target.Position));
        }

        [Test]
        public void Ranged_HoldsAtWeaponRange()
        {
            // Ranged (range 4) starting too close (distance 2) should back off to
            // sit at distance 4 (kite) rather than close in.
            var enemy = PlaceEnemy(movementSpeed: 3, abilityRange: 4, new GridPosition(0, 0));
            var target = PlaceTarget(new GridPosition(2, 0));

            var dest = EnemyAI.DecideMovement(enemy, _grid, new System.Collections.Generic.List<CombatUnit> { target });

            Assert.AreEqual(4, dest.ManhattanDistanceTo(target.Position),
                "Ranged unit should reposition to its weapon range");
        }

        [Test]
        public void NoLivingTargets_StaysPut()
        {
            var enemy = PlaceEnemy(movementSpeed: 3, abilityRange: 1, new GridPosition(1, 1));
            var target = PlaceTarget(new GridPosition(5, 0));
            target.Die();

            var dest = EnemyAI.DecideMovement(enemy, _grid, new System.Collections.Generic.List<CombatUnit> { target });

            Assert.AreEqual(enemy.Position, dest);
        }

        [Test]
        public void NullGrid_StaysPut()
        {
            var enemy = MakeEnemy(3, 1);
            enemy.Position = new GridPosition(2, 2);
            var target = MakeEnemy(3, 1);
            target.Position = new GridPosition(5, 5);

            var dest = EnemyAI.DecideMovement(enemy, null, new System.Collections.Generic.List<CombatUnit> { target });

            Assert.AreEqual(enemy.Position, dest);
        }

        private static void SetField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(field, $"Field {fieldName} not found.");
            field.SetValue(obj, value);
        }
    }
}
