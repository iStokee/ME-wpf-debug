using System;

namespace MESharp.ViewModels
{
	public static class ColorMappings
	{
		/// <summary>
		/// Get primary hex color for a skill category.
		/// </summary>
		public static string GetPrimary(string category)
		{
			return GetColorPair(category).Item1;
		}

		/// <summary>
		/// Get secondary hex color for a skill category.
		/// </summary>
		public static string GetSecondary(string category)
		{
			return GetColorPair(category).Item2;
		}

		/// <summary>
		/// Get both primary and secondary colors as a tuple.
		/// </summary>
		public static Tuple<string, string> GetColorPair(string category)
		{
			switch (category)
			{
				case "Agility":
					return Tuple.Create("#25277b", "#822d26");
				case "Archaeology":
					return Tuple.Create("#d2c6b2", "#0c090b");
				case "Construction":
					return Tuple.Create("#7f7463", "#8a590c");
				case "Cooking":
					return Tuple.Create("#58206a", "#76140a");
				case "Crafting":
					return Tuple.Create("#d0b312", "#866347");
				case "Divination":
					return Tuple.Create("#412c96", "#2c968c");
				case "Dungeoneering":
					return Tuple.Create("#de7f44", "#5d2c08");
				case "Farming":
					return Tuple.Create("#849c51", "#28632a");
				case "Firemaking":
					return Tuple.Create("#dabb13", "#e16c14");
				case "Fishing":
					return Tuple.Create("#cdb012", "#7ca1c5");
				case "Fletching":
					return Tuple.Create("#064b4d", "#ceb112");
				case "Herblore":
					return Tuple.Create("#b99f11", "#096e0d");
				case "Hunter":
					return Tuple.Create("#393125", "#6c6545");
				case "Invention":
					return Tuple.Create("#2466b1", "#ab8a23");
				case "Magic":
					return Tuple.Create("#26287f", "#d2cdc6");
				case "Mining":
					return Tuple.Create("#2b2b21", "#487b8a");
				case "Prayer":
					return Tuple.Create("#ffffff", "#ffe118");
				case "Runecrafting":
					return Tuple.Create("#ffe118", "#c4c4b5");
				case "Slayer":
					return Tuple.Create("#661008", "#272424");
				case "Smithing":
					return Tuple.Create("#585644", "#cdb012");
				case "Summoning":
					return Tuple.Create("#8094aa", "#c4a811");
				case "Thieving":
					return Tuple.Create("#2c2827", "#7c4067");
				case "Woodcutting":
					return Tuple.Create("#23552f", "#9b7d40");
				case "Attack":
					return Tuple.Create("#7c1307", "#f9d108");
				case "Strength":
					return Tuple.Create("#115638", "#822e1a");
				case "Defence":
					return Tuple.Create("#4f6198", "#bdc194");
				case "Constitution":
					return Tuple.Create("#ffffff", "#d11800");
				case "Ranged":
					return Tuple.Create("#988770", "#23552f");
				case "Necromancy":
					return Tuple.Create("#6500CA", "#38CDBF");
			}
			return Tuple.Create("#ffffff", "#808080");
		}
	}
}
