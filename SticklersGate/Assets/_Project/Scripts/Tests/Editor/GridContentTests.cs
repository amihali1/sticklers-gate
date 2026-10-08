using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using SticklersGate.Combat;
using SticklersGate.Data;

namespace SticklersGate.Tests
{
    /// <summary>
    /// Integration tests over the real content assets: every encounter has a
    /// playable grid layout, abilities and units carry sensible grid values, and
    /// enemies actually engage the party on each battlefield.
    /// </summary>
    public class GridContentTests
    {
        private const string DATA_ROOT = "Assets/_Project/Data";

        /// <summary>5e touch spells: spell attacks that legitimately reach only adjacent tiles.</summary>
        private static readonly HashSet<string> TOUCH_SPELLS = new HashSet<string> { "Vampiric Touch" };

        /// <summary>Enemies must be able to attack someone within this many rounds.</summary>
        private const int MAX_ROUNDS_TO_ENGAGE = 4;

        private static List<T> LoadAll<T>() where T : UnityEngine.Object
        {
            return AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { DATA_ROOT })
                .Select(guid => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(asset => asset != null)
                .ToList();
        }

        private static IEnumerable<EncounterSO> Encounters() => LoadAll<EncounterSO>();

        [Test]
        public void ContentAssets_AreFound()
        {
            Assert.GreaterOrEqual(LoadAll<EncounterSO>().Count, 3, "Expected at least 3 encounters.");
            Assert.GreaterOrEqual(LoadAll<CharacterClassSO>().Count, 4, "Expected at least 4 classes.");
            Assert.IsNotEmpty(LoadAll<AbilitySO>());
            Assert.IsNotEmpty(LoadAll<EnemySO>());
        }

        [Test]
        public void EveryEncounter_HasGridLayout()
        {
            foreach (var encounter in Encounters())
            {
                Assert.IsNotNull(encounter.GridLayout, $"{encounter.name} has no GridLayoutSO.");
            }
        }

        [Test]
        public void EveryLayout_FitsGridLimits()
        {
            foreach (var encounter in Encounters())
            {
                var layout = encounter.GridLayout;
                Assert.That(layout.GridWidth, Is.InRange(1, GameConstants.MAX_GRID_WIDTH), encounter.name);
                Assert.That(layout.GridHeight, Is.InRange(1, GameConstants.MAX_GRID_HEIGHT), encounter.name);
                Assert.DoesNotThrow(() => CombatGrid.FromLayout(layout), $"{encounter.name}: terrain out of bounds.");
            }
        }

        [Test]
        public void EveryLayout_HasEnoughSpawnsForPartyAndEnemies()
        {
            foreach (var encounter in Encounters())
            {
                var layout = encounter.GridLayout;
                Assert.GreaterOrEqual(layout.PlayerSpawns.Length, GameConstants.MAX_PARTY_SIZE,
                    $"{encounter.name}: not enough player spawns.");
                Assert.GreaterOrEqual(layout.EnemySpawns.Length, encounter.Enemies.Length,
                    $"{encounter.name}: not enough enemy spawns.");
            }
        }

        [Test]
        public void EveryLayout_SpawnsAreDistinctPassableTiles()
        {
            foreach (var encounter in Encounters())
            {
                var layout = encounter.GridLayout;
                var grid = CombatGrid.FromLayout(layout);
                var seen = new HashSet<GridPosition>();

                foreach (var spawn in layout.PlayerSpawns.Concat(layout.EnemySpawns))
                {
                    Assert.IsTrue(grid.IsPassableTerrain(spawn), $"{encounter.name}: spawn {spawn} is out of bounds or impassable.");
                    Assert.IsTrue(seen.Add(spawn), $"{encounter.name}: spawn {spawn} is used twice.");
                }
            }
        }

        [Test]
        public void EveryLayout_PlayersCanReachEveryEnemySpawn()
        {
            foreach (var encounter in Encounters())
            {
                var layout = encounter.GridLayout;
                var grid = CombatGrid.FromLayout(layout);

                foreach (var player in layout.PlayerSpawns)
                {
                    foreach (var enemy in layout.EnemySpawns)
                    {
                        Assert.IsNotNull(Pathfinder.FindPath(grid, player, enemy),
                            $"{encounter.name}: no path from player spawn {player} to enemy spawn {enemy}.");
                    }
                }
            }
        }

        [Test]
        public void EveryAbility_HasPositiveRange()
        {
            foreach (var ability in LoadAll<AbilitySO>())
            {
                Assert.GreaterOrEqual(ability.Range, 1, $"{ability.name} has range < 1.");
            }
        }

        [Test]
        public void SingleTargetRangedAndSpellAbilities_ReachBeyondMelee()
        {
            foreach (var ability in LoadAll<AbilitySO>())
            {
                bool rangedOrSpell = ability.Category == AbilityCategory.RangedAttack ||
                                     ability.Category == AbilityCategory.Spell;
                if (!rangedOrSpell || ability.TargetType != AbilityTargetType.SingleEnemy) continue;
                if (TOUCH_SPELLS.Contains(ability.AbilityName)) continue;

                Assert.Greater(ability.Range, GameConstants.DEFAULT_MELEE_RANGE,
                    $"{ability.name} is a ranged/spell attack but only reaches melee range.");
            }
        }

        [Test]
        public void NoAbility_UsesWholeSideTargeting()
        {
            // Grid combat: areas have a range and a shape; nothing hits "all enemies" anywhere on the map
            foreach (var ability in LoadAll<AbilitySO>())
            {
                Assert.AreNotEqual(AbilityTargetType.AllEnemies, ability.TargetType, ability.name);
                Assert.AreNotEqual(AbilityTargetType.AllAllies, ability.TargetType, ability.name);
            }
        }

        [Test]
        public void AreaAbilities_HaveConsistentShapeTargetAndRadius()
        {
            foreach (var ability in LoadAll<AbilitySO>())
            {
                if (ability.TargetType == AbilityTargetType.Point)
                {
                    Assert.AreEqual(AreaShape.Sphere, ability.AreaShape, $"{ability.name}: Point targets a sphere.");
                    Assert.GreaterOrEqual(ability.Range, 1, $"{ability.name}: sphere needs a range.");
                }

                if (ability.AreaShape == AreaShape.Emanation)
                    Assert.AreEqual(AbilityTargetType.Self, ability.TargetType, $"{ability.name}: emanations center on the user.");

                if (ability.IsArea)
                    Assert.GreaterOrEqual(ability.AreaRadius, 1, $"{ability.name}: area radius must be at least 1.");
            }
        }

        [Test]
        public void SaveForHalfAbilities_HaveASavingThrow()
        {
            foreach (var ability in LoadAll<AbilitySO>())
            {
                if (ability.SaveForHalfDamage)
                    Assert.Greater(ability.SavingThrowDC, 0, $"{ability.name}: save-for-half needs a DC.");
            }
        }

        [Test]
        public void EveryClassAndEnemy_HasPositiveMovementSpeed()
        {
            foreach (var cls in LoadAll<CharacterClassSO>())
                Assert.GreaterOrEqual(cls.MovementSpeed, 1, $"{cls.name} cannot move.");
            foreach (var enemy in LoadAll<EnemySO>())
                Assert.GreaterOrEqual(enemy.MovementSpeed, 1, $"{enemy.name} cannot move.");
        }

        [Test]
        public void EveryEncounter_EnemiesEngagePartyWithinFewRounds()
        {
            var classes = LoadAll<CharacterClassSO>().Take(GameConstants.MAX_PARTY_SIZE).ToList();

            foreach (var encounter in Encounters())
            {
                var layout = encounter.GridLayout;
                var grid = CombatGrid.FromLayout(layout);

                var party = new List<CombatUnit>();
                for (int i = 0; i < classes.Count; i++)
                {
                    var unit = CombatUnit.FromPlayerCharacter(classes[i].ClassName, classes[i], 1);
                    Place(grid, unit, layout.PlayerSpawns[i]);
                    party.Add(unit);
                }

                var enemies = new List<CombatUnit>();
                for (int i = 0; i < encounter.Enemies.Length; i++)
                {
                    var unit = CombatUnit.FromEnemy(encounter.Enemies[i].enemyData, encounter.Enemies[i].nameOverride);
                    Place(grid, unit, layout.EnemySpawns[i]);
                    enemies.Add(unit);
                }

                // Party holds still; enemies use their real movement AI each round.
                var engaged = new HashSet<CombatUnit>();
                for (int round = 0; round < MAX_ROUNDS_TO_ENGAGE && engaged.Count < enemies.Count; round++)
                {
                    foreach (var enemy in enemies)
                    {
                        if (engaged.Contains(enemy)) continue;

                        var destination = EnemyAI.DecideMovement(enemy, grid, party);
                        grid.MoveOccupant(enemy, destination);
                        enemy.Position = destination;

                        if (CanAttackAnyone(enemy, party)) engaged.Add(enemy);
                    }
                }

                foreach (var enemy in enemies)
                {
                    Assert.IsTrue(engaged.Contains(enemy),
                        $"{encounter.name}: {enemy.Name} never got a party member in range within {MAX_ROUNDS_TO_ENGAGE} rounds.");
                }
            }
        }

        private static void Place(CombatGrid grid, CombatUnit unit, GridPosition pos)
        {
            unit.Position = pos;
            grid.PlaceOccupant(unit, pos);
        }

        private static bool CanAttackAnyone(CombatUnit enemy, List<CombatUnit> party)
        {
            foreach (var ability in enemy.Abilities)
            {
                // AoE ignores range, so only single-target attacks prove engagement
                if (ability.Data.TargetType != AbilityTargetType.SingleEnemy) continue;

                foreach (var target in party)
                {
                    if (DamageCalculator.IsInRange(enemy, target, ability.Data)) return true;
                }
            }
            return false;
        }
    }
}
