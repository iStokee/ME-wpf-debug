using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace csharp_interop.native
{
	[StructLayout(LayoutKind.Sequential)]
	public struct WPoint { public int x, y, z; }

	[StructLayout(LayoutKind.Sequential)]
	public struct FFPoint { public float x, y, z; }

	internal static class Native_Player
	{
		private const string Dll = "XInput1_4_inject.dll";

		[DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
		public static extern bool LP_IsLoggedIn();

		[DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
		public static extern WPoint LP_GetTilePos();

		[DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
		public static extern FFPoint LP_GetExactPos();

		[DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
		public static extern bool LP_IsMoving();

		[DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
		public static extern int LP_GetAnimation();

		[DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
		public static extern bool LP_IsInCombat();

		[DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
		public static extern int LP_GetHoverProgress();

		[DllImport(Dll, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		public static extern bool LP_GetInteractingWith(
			[Out, MarshalAs(UnmanagedType.LPStr, SizeParamIndex = 1)]
			string outName, int bufLen);

		[DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
		public static extern int LP_GetInteractingWithId();

		[DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
		public static extern float LP_DistanceTo(WPoint tile);
	}
}