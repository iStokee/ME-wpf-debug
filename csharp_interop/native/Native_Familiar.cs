// Native_Familiars.cs
using System;
using System.Runtime.InteropServices;

namespace csharp_interop.native
{
	internal static class Native_Familiars
    {
		private const string Dll = "XInput1_4_inject.dll";

		[DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
		public static extern int Familiars_HasFamiliar();

		[DllImport(Dll, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		public static extern IntPtr Familiars_GetName();

		[DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
		public static extern int Familiars_GetTimeRemaining();

		[DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
		public static extern int Familiars_HasScrollsStored();
		[DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
		public static extern int Familiars_GetStoredScrollAmount();

		[DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
		public static extern int Familiars_CanRenew();

		[DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
		public static extern int Familiars_GetSpellPoints();

		[DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
		public static extern int Familiars_GetHealth();

		[DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
		public static extern int Familiars_CastSpecialAttack();
	}
}

