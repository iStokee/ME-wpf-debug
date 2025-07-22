using csharp_interop.native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static csharp_interop.native.Native_Inventory;

namespace MESharp.API
{
	/// <summary>
	/// High-level Inventory API.
	/// </summary>
	public static class Inventory
	{
		/// <summary>
		/// Represents one slot in the inventory.
		/// </summary>
		public class Item
		{
			internal SlotData _raw;

			internal Item(SlotData raw) => _raw = raw;

			/// <summary>Item Id</summary>
			public int Id => _raw.id;
			/// <summary>Item Name</summary>
			public string Name => _raw.name;
			/// <summary>Stack count</summary>
			public ulong Amount => _raw.amount;
			/// <summary>Slot index (0–27)</summary>
			public int Slot => _raw.slot;
			/// <summary>XP (if any)</summary>
			public int Exp => _raw.xp;

			/// <summary>Use this item on another.</summary>
			public bool UseOn(Item other)
			{
				if (!DoAction(1)) return false;
				Thread.Sleep(RandomMs(150, 300));
				return other.DoAction(99);
			}

			/// <summary>Do a menu-action on this item (1-based)</summary>
			public bool DoAction(int menuIndex)
			{
				if (Id < 0) return false;
				return Native_Inventory.DoAction(Id, menuIndex, Slot);
			}

			private static int RandomMs(int lo, int hi) => new Random().Next(lo, hi);
		}

		/// <summary>True if the inventory UI is currently open</summary>
		public static bool IsOpen => Native_Inventory.IsOpen();
		/// <summary>True if completely full</summary>
		public static bool IsFull => Native_Inventory.IsFull();
		/// <summary>True if no items present</summary>
		public static bool IsEmpty => Native_Inventory.IsEmpty();
		/// <summary>Number of free slots</summary>
		public static int FreeSlots => Native_Inventory.FreeSpaces();

		/// <summary>Get every slot (empty or not).</summary>
		public static List<Item> GetAll()
			=> Native_Inventory.GetItems()
						.Select(raw => new Item(raw))
						.ToList();

		/// <summary>Find items by exact ID.</summary>
		public static List<Item> FindById(int id)
			=> GetAll().Where(i => i.Id == id).ToList();

		/// <summary>Find items whose name contains this substring (case-insensitive).</summary>
		public static List<Item> FindByName(string substring)
			=> GetAll()
			  .Where(i => i.Name?.IndexOf(substring, StringComparison.OrdinalIgnoreCase) >= 0)
			  .ToList();

		/// <summary>Check whether the inventory contains a given ID.</summary>
		public static bool ContainsId(int id) => Native_Inventory.ContainsByID(id);
		/// <summary>Check whether the inventory contains any of these IDs.</summary>
		public static bool ContainsAny(params int[] ids) => Native_Inventory.ContainsAnyByID(ids);
		/// <summary>Check whether the inventory contains all of these IDs.</summary>
		public static bool ContainsAll(params int[] ids) => Native_Inventory.ContainsAllByID(ids);

		/// <summary>Get the total count of an item by ID.</summary>
		public static ulong CountOf(int id) => Native_Inventory.GetItemAmountByID(id);
		/// <summary>Get the total count of an item by name.</summary>
		public static ulong CountOf(string name) => Native_Inventory.GetItemAmountByName(name);

		// ——— Eat ———
		public static bool Eat(int id) => Native_Inventory.EatByID(id);
		public static bool Eat(string name) => Native_Inventory.EatByName(name);

		// ——— Drop ———
		public static bool Drop(int id) => Native_Inventory.DropByID(id);
		public static bool Drop(string name) => Native_Inventory.DropByName(name);

		// ——— Use ———
		public static bool Use(int id) => Native_Inventory.UseByID(id);
		public static bool Use(string name) => Native_Inventory.UseByName(name);

		// ——— Equip ———
		public static bool Equip(int id) => Native_Inventory.EquipByID(id);
		public static bool Equip(string name) => Native_Inventory.EquipByName(name);

		// ——— Note ———
		public static bool Note(int id) => Native_Inventory.NoteItemByID(id);
		public static bool Note(string name) => Native_Inventory.NoteItemByName(name);


		// ——— Contains ———
		public static bool Contains(int id) => Native_Inventory.ContainsByID(id);
		public static bool Contains(string name) => Native_Inventory.ContainsByName(name);
	}
}