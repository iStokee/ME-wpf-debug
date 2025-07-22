// Equipment.cs
using System;
using System.Runtime.InteropServices;

namespace csharp_interop.native
{
	internal static class Native_Equipment
	{
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
		public struct EquipmentItem
		{
			public int id;
			[MarshalAs(UnmanagedType.LPStr)] public string name;
			public int slot;
			public int amount;
			public int xp;
		}

		private const string Dll = "XInput1_4_inject.dll";

		// ——— Bools ———
		[DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
		private static extern int Equipment_IsOpen();
		public static bool IsOpen() => Equipment_IsOpen() != 0;

		[DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
		private static extern int Equipment_OpenInterface();
		public static bool OpenInterface() => Equipment_OpenInterface() != 0;

		[DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
		private static extern int Equipment_IsEmpty();
		public static bool IsEmpty() => Equipment_IsEmpty() != 0;

		[DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
		private static extern int Equipment_IsFull();
		public static bool IsFull() => Equipment_IsFull() != 0;

		// ——— Contains ———
		// containsByID
		[DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
		private static extern int Equipment_ContainsByID(int itemID);
		public static bool ContainsByID(int id) => Equipment_ContainsByID(id) != 0;

		// containsByName
		[DllImport(Dll, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		private static extern int Equipment_ContainsByName(string name);
		public static bool ContainsByName(string name) => Equipment_ContainsByName(name) != 0;

		// containsAny
		[DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
		private static extern int Equipment_ContainsAny([In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] int[] items, int count);

		public static bool ContainsAny(params int[] ids)
		{
			if (ids == null || ids.Length == 0)
				return false;

			return Equipment_ContainsAny(ids, ids.Length) != 0;
		}

		// containsAll
		[DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
		private static extern int Equipment_ContainsAll([In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] int[] items, int count);
		public static bool ContainsAll(params int[] ids)
		{
			if (ids == null || ids.Length == 0)
				return false;

			return Equipment_ContainsAll(ids, ids.Length) != 0;
		}

		// containsOnly
		[DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
		private static extern int Equipment_ContainsOnly([In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] int[] items, int count);
		public static bool ContainsOnly(params int[] ids)
		{
			if (ids == null || ids.Length == 0)
				return false;

			return Equipment_ContainsOnly(ids, ids.Length) != 0;
		}

		// ——— Actions ———
		[DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
		private static extern int Equipment_UnequipByID(int itemID);
		public static bool UnequipByID(int id) => Equipment_UnequipByID(id) != 0;

		[DllImport(Dll, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		private static extern int Equipment_UnequipByName(string name);
		public static bool UnequipByName(string name) => Equipment_UnequipByName(name) != 0;

		[DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
		private static extern int Equipment_DoAction(int itemID, int action);
		public static bool DoAction(int id, int action) => Equipment_DoAction(id, action) != 0;

		// ——— Get single values ———
		[DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
		private static extern int Equipment_GetItemID(int slot);
		public static int GetItemID(int slot) => Equipment_GetItemID(slot);

		[DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
		private static extern int Equipment_GetItemXp(int slot);
		public static int GetItemXp(int slot) => Equipment_GetItemXp(slot);

		// ——— Slot Data ———
		[DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
		private static extern EquipmentItem Equipment_GetSlotData(int slot);
		public static EquipmentItem GetSlotData(int slot) => Equipment_GetSlotData(slot);

		// ——— All Items ———
		[DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
		private static extern nint Equipment_GetItems(out int count);

		public static EquipmentItem[] GetItems()
		{
			var ptr = Equipment_GetItems(out int count);
			if (ptr == nint.Zero || count <= 0)
				return Array.Empty<EquipmentItem>();

			var size = Marshal.SizeOf<EquipmentItem>();
			var items = new EquipmentItem[count];

			for (int i = 0; i < count; i++)
			{
				items[i] = Marshal.PtrToStructure<EquipmentItem>(
					ptr + i * size
				)!;
			}

			// free each string and the block itself
			for (int i = 0; i < count; i++)
			{
				if (items[i].name != null)
				{
					Marshal.FreeCoTaskMem(
						Marshal.StringToCoTaskMemAnsi(items[i].name)
					);
				}
			}
			Marshal.FreeCoTaskMem(ptr);

			return items;
		}
	}
}
