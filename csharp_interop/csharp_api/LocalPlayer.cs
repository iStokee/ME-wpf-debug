using csharp_interop.native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MESharp.API
{
	public static class LocalPlayer
	{
		public static bool IsLoggedIn() => Native_Player.LP_IsLoggedIn();
		public static (int x, int y, int z) GetTilePosition()
		{
			var p = Native_Player.LP_GetTilePos();
			return (p.x, p.y, p.z);
		}

		public static (float x, float y, float z) GetExactPosition()
		{
			var p = Native_Player.LP_GetExactPos();
			return (p.x, p.y, p.z);
		}

		public static bool IsMoving() => Native_Player.LP_IsMoving();
		public static int GetAnimation() => Native_Player.LP_GetAnimation();
		public static bool IsInCombat() => Native_Player.LP_IsInCombat();
		public static int GetHoverProgress() => Native_Player.LP_GetHoverProgress();

		public static string GetInteractingWith()
		{
			var buf = new string('\0', 64);
			if (!Native_Player.LP_GetInteractingWith(buf, buf.Length))
				return string.Empty;
			return buf.TrimEnd('\0');
		}

		public static int GetInteractingWithId() => Native_Player.LP_GetInteractingWithId();

		public static float DistanceTo(int x, int y, int z)
			=> Native_Player.LP_DistanceTo(new WPoint { x = x, y = y, z = z });
	}
}