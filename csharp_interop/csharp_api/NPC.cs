// File: MESharp.API/NPC.cs

using System;
using System.Collections.Generic;
using System.Linq;
using csharp_interop.native;
using static csharp_interop.native.Native_NPC;    // your P/Invoke definitions for Native_NPC

namespace MESharp.API
{
	public static class Npcs
	{
		// --- native-route constants ---
		public const int InteractNPC_route = 1648;
		public const int AttackNPC_route = 1744;
		public const int InteractNPC_route2 = 1856;
		public const int InteractNPC_route3 = 1952;
		public const int InteractNPC_route4 = 2032;

		/// <summary>
		/// Fetches *all* NPCs currently loaded in the scene.
		/// </summary>
		public static List<Npc> GetAll()
		{
			var raw = Native_NPC.GetAll();                // AllObject[]
			return raw.Select(o => new Npc(o)).ToList();
		}

		/// <summary>
		/// Finds all NPCs whose Name matches exactly.
		/// </summary>
		public static List<Npc> ByName(string name)
		{
			var raw = Native_NPC.FindByName(name);        // AllObject[]
			return raw.Select(o => new Npc(o)).ToList();
		}

		/// <summary>
		/// Performs DoAction_NPC (ID-based) on the first matching NPC.
		/// </summary>
		public static bool DoActionByIds(
			IEnumerable<int> ids,
			int actionIndex,
			int offset = 0,
			int maxDistance = int.MaxValue,
			bool ignoreStar = false,
			int minHealth = 0)
		{
			var idArr = ids.ToArray();
			return Native_NPC.DoAction_NPC(
				actionIndex,
				offset,
				idArr,
				idArr.Length,
				maxDistance,
				ignoreStar,
				minHealth
			);
		}

		/// <summary>
		/// Performs DoAction_NPC_str (name-based) on the first matching NPC.
		/// </summary>
		public static bool DoActionByNames(
			IEnumerable<string> names,
			int actionIndex,
			int offset = 0,
			int maxDistance = int.MaxValue,
			bool ignoreStar = false,
			int minHealth = 0)
		{
			var nameArr = names.ToArray();
			return Native_NPC.DoAction_NPC_str(
				actionIndex,
				offset,
				nameArr,
				nameArr.Length,
				maxDistance,
				ignoreStar,
				minHealth
			);
		}

		/// <summary>
		/// Convenience: look up a single NPC by its numeric ID.
		/// </summary>
		public static Npc GetById(int id) =>
			GetAll().FirstOrDefault(n => n.Id == id);


		public class Npc
		{
			// --- exposed properties ---
			public int Id { get; }
			public string Name { get; }
			public int Health { get; }
			public int X { get; }
			public int Y { get; }
			public float Distance { get; }
			public int Animation { get; }

			internal Npc(AllObject o)
			{
				Id        = o.Id;
				Name      = o.Name;
				Health    = o.Life;
				X         = o.Tile_XYZ.x;
				Y         = o.Tile_XYZ.y;
				Distance  = o.Distance;
				Animation = o.Anim;
			}

			/// <summary>
			/// Interact with this NPC via its Name (menuIndex 0–10).
			/// </summary>
			public bool DoAction(
				int actionIndex,
				int offset = 0,
				int maxDistance = int.MaxValue,
				bool ignoreStar = false,
				int minHealth = 0,
				bool waitMove = false,
				bool waitNpcDead = false)
			{
				// 1) Try by name
				var ok = Npcs.DoActionByNames(
					new[] { Name },
					actionIndex,
					offset,
					maxDistance,
					ignoreStar,
					minHealth
				);

				if (!ok) return false;

				// 2) Optionally wait for movement to finish
				if (waitMove)
					while (LocalPlayer.IsMoving())
						System.Threading.Thread.Sleep(20);

				// 3) Optionally wait for NPC death
				if (waitNpcDead)
					while (RefreshHealth() > 0)
						System.Threading.Thread.Sleep(50);

				return true;
			}

			private int RefreshHealth()
			{
				var fresh = Npcs.GetById(Id);
				return fresh?.Health ?? 0;
			}
		}
	}
}
