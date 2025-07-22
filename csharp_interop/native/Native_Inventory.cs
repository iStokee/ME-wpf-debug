using System.Runtime.InteropServices;

namespace csharp_interop.native
{
	internal static class Native_Inventory
	{
		//[StructLayout(LayoutKind.Sequential)]
		
		//public struct InventoryItem
		//{
		//	public int id;
		//	[MarshalAs(UnmanagedType.BStr)]
		//	public string name;
		//	public int amount;
		//	public int slot;
		//	public int xp;

		//	public InventoryItem(int id = 0, string name = "", int amount = 0, int slot = 0, int xp = -1)
		//	{
		//		this.id = id;
		//		this.name = name;
		//		this.amount = amount;
		//		this.slot = slot;
		//		this.xp = xp;
		//	}
		//}

		[StructLayout(LayoutKind.Sequential)]
		public struct SlotData
		{
			public int id;
			[MarshalAs(UnmanagedType.LPStr)] public string name;
			public ulong amount;
			public int slot;
			public int xp;
		}

		private const string Dll = "XInput1_4_inject.dll";

		// ——— Bools ———
		[DllImport(Dll)]
		private static extern int Inventory_IsOpen();
		public static bool IsOpen() => Inventory_IsOpen() != 0;

		[DllImport(Dll)]
		private static extern int Inventory_IsFull();
		public static bool IsFull() => Inventory_IsFull() != 0;

		[DllImport(Dll)]
		private static extern int Inventory_IsEmpty();
		public static bool IsEmpty() => Inventory_IsEmpty() != 0;

		[DllImport(Dll)]
		private static extern int Inventory_IsItemSelected();
		public static bool IsItemSelected() => Inventory_IsItemSelected() != 0;

		// ——— Contains ———
		[DllImport(Dll)]
		private static extern int Inventory_ContainsByID(int itemId);
		public static bool ContainsByID(int id) => Inventory_ContainsByID(id) != 0;

		[DllImport(Dll, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		private static extern int Inventory_ContainsByName([MarshalAs(UnmanagedType.LPStr)] string name);
		public static bool ContainsByName(string name) => Inventory_ContainsByName(name) != 0;

		[DllImport(Dll)]
		private static extern int Inventory_ContainsAllByID([In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] int[] items, int count);
		public static bool ContainsAllByID(int[] ids) =>
			ids != null && ids.Length > 0 && Inventory_ContainsAllByID(ids, ids.Length) != 0;

		[DllImport(Dll)]
		private static extern int Inventory_ContainsAnyByID([In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] int[] items, int count);
		public static bool ContainsAnyByID(int[] ids) =>
			ids != null && ids.Length > 0 && Inventory_ContainsAnyByID(ids, ids.Length) != 0;

		[DllImport(Dll)]
		private static extern int Inventory_ContainsOnlyByID([In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] int[] items, int count);
		public static bool ContainsOnlyByID(int[] ids) =>
			ids != null && ids.Length > 0 && Inventory_ContainsOnlyByID(ids, ids.Length) != 0;

		// ——— Amount & XP ———
		[DllImport(Dll)]
		private static extern ulong Inventory_GetItemAmountByID(int itemId);
		public static ulong GetItemAmountByID(int id) => Inventory_GetItemAmountByID(id);

		[DllImport(Dll, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		private static extern ulong Inventory_GetItemAmountByName([MarshalAs(UnmanagedType.LPStr)] string name);
		public static ulong GetItemAmountByName(string name) => Inventory_GetItemAmountByName(name);

		[DllImport(Dll)]
		private static extern int Inventory_GetItemXpByID(int itemId);
		public static int GetItemXpByID(int id) => Inventory_GetItemXpByID(id);

		[DllImport(Dll, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		private static extern int Inventory_GetItemXpByName([MarshalAs(UnmanagedType.LPStr)] string name);
		public static int GetItemXpByName(string name) => Inventory_GetItemXpByName(name);

		// ——— Actions ———
		[DllImport(Dll)]
		private static extern int Inventory_EatByID(int itemId);
		public static bool EatByID(int id) => Inventory_EatByID(id) != 0;

		[DllImport(Dll)]
		private static extern int Inventory_DropByID(int itemId);
		public static bool DropByID(int id) => Inventory_DropByID(id) != 0;

		[DllImport(Dll)]
		private static extern int Inventory_UseByID(int itemId);
		public static bool UseByID(int id) => Inventory_UseByID(id) != 0;

		[DllImport(Dll)]
		private static extern int Inventory_EquipByID(int itemId);
		public static bool EquipByID(int id) => Inventory_EquipByID(id) != 0;

		[DllImport(Dll)]
		private static extern int Inventory_RubByID(int itemId);
		public static bool RubByID(int id) => Inventory_RubByID(id) != 0;

		[DllImport(Dll)]
		private static extern int Inventory_NoteItemByID(int itemId);
		public static bool NoteItemByID(int id) => Inventory_NoteItemByID(id) != 0;

		[DllImport(Dll, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		private static extern int Inventory_EatByName([MarshalAs(UnmanagedType.LPStr)] string name);
		public static bool EatByName(string name) => Inventory_EatByName(name) != 0;

		[DllImport(Dll, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		private static extern int Inventory_DropByName([MarshalAs(UnmanagedType.LPStr)] string name);
		public static bool DropByName(string name) => Inventory_DropByName(name) != 0;

		[DllImport(Dll, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		private static extern int Inventory_UseByName([MarshalAs(UnmanagedType.LPStr)] string name);
		public static bool UseByName(string name) => Inventory_UseByName(name) != 0;

		[DllImport(Dll, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		private static extern int Inventory_EquipByName([MarshalAs(UnmanagedType.LPStr)] string name);
		public static bool EquipByName(string name) => Inventory_EquipByName(name) != 0;

		[DllImport(Dll, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		private static extern int Inventory_RubByName([MarshalAs(UnmanagedType.LPStr)] string name);
		public static bool RubByName(string name) => Inventory_RubByName(name) != 0;

		[DllImport(Dll, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		private static extern int Inventory_NoteItemByName([MarshalAs(UnmanagedType.LPStr)] string name);
		public static bool NoteItemByName(string name) => Inventory_NoteItemByName(name) != 0;

		[DllImport(Dll)]
		private static extern int Inventory_UseItemOnItemByID(int id1, int id2);
		public static bool UseItemOnItemByID(int id1, int id2) => Inventory_UseItemOnItemByID(id1, id2) != 0;

		[DllImport(Dll, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		private static extern int Inventory_UseItemOnItemByName(
			[MarshalAs(UnmanagedType.LPStr)] string n1,
			[MarshalAs(UnmanagedType.LPStr)] string n2);
		public static bool UseItemOnItemByName(string a, string b) =>
			Inventory_UseItemOnItemByName(a, b) != 0;

		[DllImport(Dll)]
		private static extern int Inventory_DoActionByID(int itemId, int action, int offset);
		public static bool DoAction(int id, int action, int offset) =>
			Inventory_DoActionByID(id, action, offset) != 0;

		[DllImport(Dll, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		private static extern int Inventory_DoActionByName(
			[MarshalAs(UnmanagedType.LPStr)] string name,
			int action,
			int offset);
		public static bool DoAction(string name, int action, int offset) =>
			Inventory_DoActionByName(name, action, offset) != 0;

		// ——— Inventory Info ———
		[DllImport(Dll)]
		private static extern int Inventory_FreeSpaces();
		public static int FreeSpaces() => Inventory_FreeSpaces();

		// Get all items
		[DllImport(Dll)]
		private static extern nint Inventory_GetItems(out int outCount);
		public static SlotData[] GetItems()
		{
			nint ptr = Inventory_GetItems(out int count);
			if (ptr == nint.Zero || count <= 0) return Array.Empty<SlotData>();

			var size = Marshal.SizeOf<SlotData>();
			var result = new SlotData[count];
			for (int i = 0; i < count; i++)
			{
				result[i] = Marshal.PtrToStructure<SlotData>(ptr + i * size)!;
			}
			Marshal.FreeCoTaskMem(ptr);
			return result;
		}

		// Get items by ID or Name
		[DllImport(Dll)]
		private static extern nint Inventory_GetItemByID(int itemId, out int outCount);
		public static SlotData[] GetItemByID(int id) => MarshalSlotArray(Inventory_GetItemByID(id, out var c), c);

		[DllImport(Dll, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		private static extern nint Inventory_GetItemByName([MarshalAs(UnmanagedType.LPStr)] string name, out int outCount);
		public static SlotData[] GetItemByName(string name) => MarshalSlotArray(Inventory_GetItemByName(name, out var c), c);

		private static SlotData[] MarshalSlotArray(nint ptr, int count)
		{
			if (ptr == nint.Zero || count <= 0) return Array.Empty<SlotData>();
			var size = Marshal.SizeOf<SlotData>();
			var result = new SlotData[count];
			for (int i = 0; i < count; i++)
			{
				result[i] = Marshal.PtrToStructure<SlotData>(ptr + i * size)!;
			}
			Marshal.FreeCoTaskMem(ptr);
			return result;
		}

		// Slot-by-slot
		[DllImport(Dll)]
		private static extern int Inventory_GetSlotData(int slot, out SlotData data);
		public static bool TryGetSlotData(int slot, out SlotData data) =>
			Inventory_GetSlotData(slot, out data) != 0;

	}
}
