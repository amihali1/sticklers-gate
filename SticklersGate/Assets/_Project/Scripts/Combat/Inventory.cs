using System;
using System.Collections.Generic;
using SticklersGate.Data;

namespace SticklersGate.Combat
{
    /// <summary>
    /// Tracks item quantities for the party. Plain C# class — not a MonoBehaviour.
    /// Items are shared across the party (not per-character).
    /// </summary>
    public class Inventory
    {
        private readonly Dictionary<ItemSO, int> _items = new Dictionary<ItemSO, int>();

        /// <summary>
        /// Fired when an item is added or removed.
        /// </summary>
        public event Action OnInventoryChanged;

        /// <summary>
        /// Add items to the inventory.
        /// </summary>
        public void Add(ItemSO item, int quantity = 1)
        {
            if (item == null || quantity <= 0) return;

            if (_items.ContainsKey(item))
                _items[item] += quantity;
            else
                _items[item] = quantity;

            OnInventoryChanged?.Invoke();
        }

        /// <summary>
        /// Remove one of the given item. Returns true if successful.
        /// </summary>
        public bool Remove(ItemSO item)
        {
            if (item == null || !_items.ContainsKey(item) || _items[item] <= 0)
                return false;

            _items[item]--;
            if (_items[item] <= 0)
                _items.Remove(item);

            OnInventoryChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Get the quantity of a specific item.
        /// </summary>
        public int GetQuantity(ItemSO item)
        {
            return item != null && _items.ContainsKey(item) ? _items[item] : 0;
        }

        /// <summary>
        /// Get all consumable items with their quantities.
        /// </summary>
        public List<ItemStack> GetConsumables()
        {
            var result = new List<ItemStack>();
            foreach (var kvp in _items)
            {
                if (kvp.Key.IsConsumable && kvp.Value > 0)
                {
                    result.Add(new ItemStack { Item = kvp.Key, Quantity = kvp.Value });
                }
            }
            return result;
        }

        /// <summary>
        /// Check if the inventory has at least one of the given item.
        /// </summary>
        public bool Has(ItemSO item)
        {
            return GetQuantity(item) > 0;
        }
    }

    /// <summary>
    /// An item with its current quantity in the inventory.
    /// </summary>
    public struct ItemStack
    {
        public ItemSO Item;
        public int Quantity;
    }
}
