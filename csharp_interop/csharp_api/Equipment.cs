using System;
using System.Collections.Generic;
using csharp_interop.native;

namespace MESharp.API
{
	/// <summary>
	/// High-level Equipment API facade.
	/// </summary>
	public static class Equipment
	{
		/// <summary>
		/// Represents a single equipment slot item.
		/// </summary>
		public class Item
		{
			public int Id { get; }
			public string Name { get; }
			public int Slot { get; }
			public int Amount { get; }
			public int Xp { get; }

			internal Item(Native_Equipment.EquipmentItem native)
			{
				Id = native.id;
				Name = native.name;
				Slot = native.slot;
				Amount = native.amount;
				Xp = native.xp;
			}
		}

		// ——— Interface Control ———
		public static bool IsOpen() => Native_Equipment.IsOpen();
		public static bool OpenInterface() => Native_Equipment.OpenInterface();
		public static bool IsEmpty() => Native_Equipment.IsEmpty();
		public static bool IsFull() => Native_Equipment.IsFull();

		// ——— Contains Checks ———
		public static bool ContainsById(int id) => Native_Equipment.ContainsByID(id);
		public static bool ContainsByName(string name) => Native_Equipment.ContainsByName(name);
		public static bool ContainsAny(params int[] ids) => Native_Equipment.ContainsAny(ids);
		public static bool ContainsAll(params int[] ids) => Native_Equipment.ContainsAll(ids);
		public static bool ContainsOnly(params int[] ids) => Native_Equipment.ContainsOnly(ids);

		// ——— Actions ———
		public static bool UnequipById(int id) => Native_Equipment.UnequipByID(id);
		public static bool UnequipByName(string name) => Native_Equipment.UnequipByName(name);
		public static bool DoAction(int id, int action) => Native_Equipment.DoAction(id, action);

		// ——— Single-Slot Data ———
		public static int GetItemId(int slot) => Native_Equipment.GetItemID(slot);
		public static int GetItemXp(int slot) => Native_Equipment.GetItemXp(slot);
		public static Item GetSlotData(int slot)
		{
			var native = Native_Equipment.GetSlotData(slot);
			return new Item(native);
		}

		// ——— Bulk Retrieval ———
		public static IReadOnlyList<Item> GetAllItems()
		{
			var nativeArr = Native_Equipment.GetItems();
			var list = new List<Item>(nativeArr.Length);
			foreach (var n in nativeArr)
				list.Add(new Item(n));
			return list;
		}
	}
}
