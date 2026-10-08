using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using SticklersGate.Combat;
using SticklersGate.Data;

namespace SticklersGate.Tests
{
    /// <summary>
    /// Factory helpers for mechanics tests: build ScriptableObjects in memory
    /// (via reflection on [SerializeField] fields) and wrap them as CombatUnits.
    /// </summary>
    public static class TestUnits
    {
        /// <summary>Neutral 10s across the board (all modifiers 0).</summary>
        public static StatBlock FlatStats() => new StatBlock
        {
            strength = 10, dexterity = 10, constitution = 10,
            intelligence = 10, wisdom = 10, charisma = 10
        };

        public static AbilitySO Ability(string name = "Test", AbilityCategory category = AbilityCategory.MeleeAttack,
            AbilityTargetType targetType = AbilityTargetType.SingleEnemy, int dice = 1, int sides = 6,
            DamageType damageType = DamageType.Slashing, int range = 1)
        {
            var so = ScriptableObject.CreateInstance<AbilitySO>();
            Set(so, "_abilityName", name);
            Set(so, "_category", category);
            Set(so, "_targetType", targetType);
            Set(so, "_damageDiceCount", dice);
            Set(so, "_damageDiceSides", sides);
            Set(so, "_damageType", damageType);
            Set(so, "_range", range);
            return so;
        }

        public static CombatUnit Player(string name, GridPosition pos, int movement = 3, int hp = 30, int ac = 12)
        {
            var so = ScriptableObject.CreateInstance<CharacterClassSO>();
            Set(so, "_className", name);
            Set(so, "_baseAC", ac);
            Set(so, "_hitDiceSides", hp);
            Set(so, "_baseStats", FlatStats());
            Set(so, "_movementSpeed", movement);
            Set(so, "_level1Abilities", new AbilitySO[0]);
            var unit = CombatUnit.FromPlayerCharacter(name, so, 1);
            unit.Position = pos;
            return unit;
        }

        public static CombatUnit Enemy(string name, GridPosition pos, int movement = 3, int hp = 30, int ac = 12,
            params AbilitySO[] abilities)
        {
            var so = ScriptableObject.CreateInstance<EnemySO>();
            Set(so, "_enemyName", name);
            Set(so, "_hitPoints", hp);
            Set(so, "_armorClass", ac);
            Set(so, "_stats", FlatStats());
            Set(so, "_movementSpeed", movement);
            Set(so, "_abilities", abilities ?? new AbilitySO[0]);
            var unit = CombatUnit.FromEnemy(so, name);
            unit.Position = pos;
            return unit;
        }

        /// <summary>Set a private serialized field anywhere in the type hierarchy.</summary>
        public static void Set(object target, string field, object value)
        {
            var type = target.GetType();
            while (type != null)
            {
                var info = type.GetField(field, BindingFlags.NonPublic | BindingFlags.Instance);
                if (info != null)
                {
                    info.SetValue(target, value);
                    return;
                }
                type = type.BaseType;
            }
            Assert.Fail($"Field '{field}' not found on {target.GetType().Name}.");
        }
    }
}
