using System;
using System.Runtime.InteropServices;
using csharp_interop.native;

namespace MESharp.API
{
	internal static class Native_Skills
	{
		private const string Dll = "XInput1_4_inject.dll";

		[DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SK_IsPanelOpen();

		[DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SK_TogglePanel();

		[DllImport(Dll, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		public static extern int SK_GetSkillXP(string name);

		[DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
		public static extern SafeSkillHandle SK_GetById(int id);

		[DllImport(Dll, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		public static extern SafeSkillHandle SK_GetByName(string name);

		[DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void SK_Free(IntPtr skillPtr);

		[DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SK_XPForLevel(int level, int elite);

		[DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
		private static extern int SK_XPLevelTable(int xp, int elite);
	}
}