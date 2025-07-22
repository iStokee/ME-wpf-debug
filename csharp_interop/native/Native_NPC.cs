// NPC.cs
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace csharp_interop.native
{
	internal static class Native_NPC
	{
		private const string Dll = "XInput1_4_inject.dll";

		[StructLayout(LayoutKind.Sequential)]
		public struct WPoint { public int x, y, z; }

		[StructLayout(LayoutKind.Sequential)]
		public struct FFPoint { public float x, y, z; }

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
		public struct AllObject
		{
			public ulong Mem;
			public int TileX, TileY, TileZ;
			public int Id;
			public int Life;
			public int Anim;
			public int Floor;
			public int Type;
			public float Distance;
			public int Cmb_lv;
			public WPoint Tile_XYZ;
			public FFPoint Pixel_XYZ;
			[MarshalAs(UnmanagedType.LPStr)] public string Name;
			[MarshalAs(UnmanagedType.LPStr)] public string Action;
		}

		#region DllImports
		[DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
		private static extern nint NPC_GetAll(out int count);

		[DllImport(Dll, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		private static extern nint NPC_FindByName(string name, out int count);

		[DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
		public static extern int NPC_DoAction_Str(
			[In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] string[] names,
			int nameCount,
			int action, int offset,
			int maxDistance, bool ignoreStar, int minHealth);

		[DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
		private static extern int NPC_DoAction_Area(
			[In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] int[] ids,
			int idCount,
			int action, int offset,
			int maxDistance, WPoint bottomLeft, WPoint topRight,
			bool ignoreStar, int minHealth);

		[DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
		private static extern AllObject NPC_GetInFocus(int npcId);
		#endregion

		#region Public Wrappers
		public static List<AllObject> GetAll()
		{
			var ptr = NPC_GetAll(out int count);
			return MarshalArray(ptr, count);
		}

		public static List<AllObject> FindByName(string name)
		{
			var ptr = NPC_FindByName(name, out int count);
			return MarshalArray(ptr, count);
		}

		public static bool DoActionByNames(
			string[] names, int action, int offset,
			int maxDistance = int.MaxValue, bool ignoreStar = false, int minHealth = 0)
		{
			return NPC_DoAction_Str(names, names.Length, action, offset, maxDistance, ignoreStar, minHealth) != 0;
		}

		public static bool DoActionInAreaByIds(
			int[] ids, int action, int offset,
			int maxDistance, WPoint bottomLeft, WPoint topRight,
			bool ignoreStar = false, int minHealth = 0)
		{
			return NPC_DoAction_Area(ids, ids.Length, action, offset, maxDistance, bottomLeft, topRight, ignoreStar, minHealth) != 0;
		}

		public static AllObject GetInFocus(int npcId)
			=> NPC_GetInFocus(npcId);

		private static List<AllObject> MarshalArray(nint ptr, int count)
		{
			var list = new List<AllObject>();
			if (ptr == nint.Zero || count <= 0) return list;

			int size = Marshal.SizeOf<AllObject>();
			for (int i = 0; i < count; i++)
			{
				var itemPtr = ptr + i * size;
				var obj = Marshal.PtrToStructure<AllObject>(itemPtr)!;
				list.Add(obj);

				// free inner strings
				var namePtr = Marshal.ReadIntPtr(itemPtr, Marshal.OffsetOf<AllObject>("Name").ToInt32());
				var actionPtr = Marshal.ReadIntPtr(itemPtr, Marshal.OffsetOf<AllObject>("Action").ToInt32());
				if (namePtr   != nint.Zero) Marshal.FreeCoTaskMem(namePtr);
				if (actionPtr != nint.Zero) Marshal.FreeCoTaskMem(actionPtr);
			}
			// free block
			Marshal.FreeCoTaskMem(ptr);
			return list;
		}

		internal static bool DoAction_NPC_str(int actionIndex, int offset, string[] nameArr, int length, int maxDistance, bool ignoreStar, int minHealth)
		{
			if (nameArr == null || nameArr.Length == 0)
				throw new ArgumentException("Name array cannot be null or empty.", nameof(nameArr));
			return NPC_DoAction_Str(nameArr, length, actionIndex, offset, maxDistance, ignoreStar, minHealth) != 0;
		}

		internal static bool DoAction_NPC(int actionIndex, int offset, int[] idArr, int length, int maxDistance, bool ignoreStar, int minHealth)
		{
			if (idArr == null || idArr.Length == 0)
				throw new ArgumentException("ID array cannot be null or empty.", nameof(idArr));
			return NPC_DoAction_Area(idArr, length, actionIndex, offset, maxDistance, new WPoint(), new WPoint(), ignoreStar, minHealth) != 0;
		}
		#endregion
	}
}
