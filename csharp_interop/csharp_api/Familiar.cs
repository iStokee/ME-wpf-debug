using System;
using System.Runtime.InteropServices;
using csharp_interop.native;

namespace MESharp.API
{
	public static class Familiar
	{
		public static bool HasFamiliar() => Native_Familiars.Familiars_HasFamiliar() != 0;

		public static string GetName()
		{
			var ptr = Native_Familiars.Familiars_GetName();
			if (ptr == IntPtr.Zero) return string.Empty;
			string s = Marshal.PtrToStringAnsi(ptr)!;
			Marshal.FreeCoTaskMem(ptr);
			return s;
		}

		public static int GetTimeRemaining() => Native_Familiars.Familiars_GetTimeRemaining();
		public static bool HasScrollsStored() => Native_Familiars.Familiars_HasScrollsStored() != 0;
		public static int GetStoredScrollAmount() => Native_Familiars.Familiars_GetStoredScrollAmount();
		public static bool CanRenew() => Native_Familiars.Familiars_CanRenew() != 0;
		public static int GetSpellPoints() => Native_Familiars.Familiars_GetSpellPoints();
		public static int GetHealth() => Native_Familiars.Familiars_GetHealth();
		public static bool CastSpecialAttack() => Native_Familiars.Familiars_CastSpecialAttack() != 0;
	}
}