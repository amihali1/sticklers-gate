using NUnit.Framework;
using SticklersGate.Combat;
using SticklersGate.Data;
using UnityEngine;

namespace SticklersGate.Tests
{
    /// <summary>
    /// Unit tests for the Inventory system.
    /// </summary>
    public class InventoryTests
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

        private ItemSO CreateItemSO(string name, ItemType itemType, int healAmount = 0,
            int damageAmount = 0, StatusEffectType removesEffect = StatusEffectType.None)
        {
            var so = ScriptableObject.CreateInstance<ItemSO>();
            SetPrivateField(so, "_itemName", name);
            SetPrivateField(so, "_itemType", itemType);
            SetPrivateField(so, "_rarity", ItemRarity.Common);
            SetPrivateField(so, "_description", "Test item");
            SetPrivateField(so, "_goldValue", 10);
            SetPrivateField(so, "_healAmount", healAmount);
            SetPrivateField(so, "_damageAmount", damageAmount);
            SetPrivateField(so, "_damageType", DamageType.Fire);
            SetPrivateField(so, "_targetType", AbilityTargetType.SingleAlly);
            SetPrivateField(so, "_removesEffect", removesEffect);
            SetPrivateField(so, "_appliesEffect", StatusEffectType.None);
            SetPrivateField(so, "_effectDuration", 0);
            SetPrivateField(so, "_useQuips", new string[0]);
            return so;
        }

        [Test]
        public void Add_IncreasesQuantity()
        {
            var inventory = new Inventory();
            var potion = CreateItemSO("Health Potion", ItemType.Consumable, healAmount: 14);

            inventory.Add(potion, 3);

            Assert.AreEqual(3, inventory.GetQuantity(potion));
        }

        [Test]
        public void Add_StacksMultipleCalls()
        {
            var inventory = new Inventory();
            var potion = CreateItemSO("Health Potion", ItemType.Consumable, healAmount: 14);

            inventory.Add(potion, 2);
            inventory.Add(potion, 1);

            Assert.AreEqual(3, inventory.GetQuantity(potion));
        }

        [Test]
        public void Remove_DecreasesQuantity()
        {
            var inventory = new Inventory();
            var potion = CreateItemSO("Health Potion", ItemType.Consumable, healAmount: 14);

            inventory.Add(potion, 3);
            bool removed = inventory.Remove(potion);

            Assert.IsTrue(removed);
            Assert.AreEqual(2, inventory.GetQuantity(potion));
        }

        [Test]
        public void Remove_LastItem_RemovesFromInventory()
        {
            var inventory = new Inventory();
            var potion = CreateItemSO("Health Potion", ItemType.Consumable, healAmount: 14);

            inventory.Add(potion, 1);
            inventory.Remove(potion);

            Assert.AreEqual(0, inventory.GetQuantity(potion));
            Assert.IsFalse(inventory.Has(potion));
        }

        [Test]
        public void Remove_EmptyInventory_ReturnsFalse()
        {
            var inventory = new Inventory();
            var potion = CreateItemSO("Health Potion", ItemType.Consumable, healAmount: 14);

            Assert.IsFalse(inventory.Remove(potion));
        }

        [Test]
        public void GetConsumables_ReturnsOnlyConsumables()
        {
            var inventory = new Inventory();
            var potion = CreateItemSO("Health Potion", ItemType.Consumable, healAmount: 14);
            var sword = CreateItemSO("Sword", ItemType.Weapon);

            inventory.Add(potion, 2);
            inventory.Add(sword, 1);

            var consumables = inventory.GetConsumables();

            Assert.AreEqual(1, consumables.Count);
            Assert.AreEqual("Health Potion", consumables[0].Item.ItemName);
            Assert.AreEqual(2, consumables[0].Quantity);
        }

        [Test]
        public void Has_ReturnsTrueWhenItemExists()
        {
            var inventory = new Inventory();
            var potion = CreateItemSO("Health Potion", ItemType.Consumable, healAmount: 14);

            inventory.Add(potion, 1);

            Assert.IsTrue(inventory.Has(potion));
        }

        [Test]
        public void ItemSO_Heals_ReturnsCorrectly()
        {
            var potion = CreateItemSO("Health Potion", ItemType.Consumable, healAmount: 14);
            Assert.IsTrue(potion.Heals);
            Assert.IsFalse(potion.DealsDamage);
            Assert.IsTrue(potion.IsConsumable);
        }

        [Test]
        public void ItemSO_Antidote_RemovesPoison()
        {
            var antidote = CreateItemSO("Antidote", ItemType.Consumable,
                removesEffect: StatusEffectType.Poisoned);
            Assert.IsTrue(antidote.RemovesStatus);
            Assert.AreEqual(StatusEffectType.Poisoned, antidote.RemovesEffect);
        }

        [Test]
        public void OnInventoryChanged_FiresOnAddAndRemove()
        {
            var inventory = new Inventory();
            var potion = CreateItemSO("Health Potion", ItemType.Consumable, healAmount: 14);
            int changeCount = 0;
            inventory.OnInventoryChanged += () => changeCount++;

            inventory.Add(potion, 1);
            inventory.Remove(potion);

            Assert.AreEqual(2, changeCount);
        }
    }
}
