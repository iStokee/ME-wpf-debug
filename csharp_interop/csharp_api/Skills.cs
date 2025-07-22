using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using csharp_interop.native;

namespace MESharp.API
{
	public enum SkillName
	{
		Attack, Defence, Strength, Constitution, Ranged, Prayer, Magic,
		Cooking, Woodcutting, Fletching, Fishing, Firemaking, Crafting,
		Smithing, Mining, Herblore, Agility, Thieving, Slayer, Farming,
		Runecrafting, Hunter, Construction, Summoning, Dungeoneering,
		Divination, Invention, Archaeology, Necromancy
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
	internal struct Skill_C
	{
		public int interfaceIdx;
		public int id;
		public IntPtr name;
		public int vb;
		public int xp;
		public int currentLevel;
		public int boostedLevel;
	}

	/// <summary>
	/// A small DTO representing one skill’s current snapshot.
	/// </summary>
	public class Skill
	{
		public int InterfaceIdx { get; }
		public int Id { get; }
		public string Name { get; }
		public int Vb { get; }
		public int Xp { get; }
		public int CurrentLevel { get; }
		public int BoostedLevel { get; }

		internal Skill(in Skill_C c)
		{
			InterfaceIdx  = c.interfaceIdx;
			Id            = c.id;
			Name          = Marshal.PtrToStringAnsi(c.name)!;
			Vb            = c.vb;
			Xp            = c.xp;
			CurrentLevel  = c.currentLevel;
			BoostedLevel  = c.boostedLevel;
		}
	}

	public static class Skills
	{
		/// <summary>
		/// Fetch a raw snapshot of one skill.
		/// </summary>
		public static Skill Get(SkillName skillName)
		{
			using var handle = Native_Skills.SK_GetById((int)skillName);
			IntPtr ptr = handle.DangerousGetHandle();
			var c = Marshal.PtrToStructure<Skill_C>(ptr);
			return new Skill(c);
		}

		/// <summary>
		/// All skills at once.
		/// </summary>
		public static IReadOnlyList<Skill> GetAll()
			=> Enum.GetValues<SkillName>()
				   .Select(Get)
				   .ToList();

		/// <summary>
		/// Just the raw XP for a skill.
		/// </summary>
		public static int GetXp(SkillName skillName)
			=> Get(skillName).Xp;

		/// <summary>
		/// How much XP remains until the next level?
		/// </summary>
		public static int GetXpToNextLevel(Skill skill)
		{
			int next = skill.CurrentLevel + 1;
			bool isElite = skill.Id == (int)SkillName.Invention;
			int totalForNext = Native_Skills.SK_XPForLevel(next, isElite ? 1 : 0);
			return Math.Max(totalForNext - skill.Xp, 0);
		}

		/// <summary>
		/// What level corresponds to this total XP?
		/// </summary>
		public static int LevelForXp(int xp, bool elite = false)
			=> GetLevelForXp(xp, elite);

		private static int GetLevelForXp(int xp, bool elite)
			=> typeof(Native_Skills)
			  .GetMethod("SK_XPLevelTable", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
			  .Invoke(null, new object[] { xp, elite ? 1 : 0 }) is int lvl
				? lvl
				: 0;
	}

	/// <summary>
	/// Holds a snapshot of every skill at session start,
	/// and computes gains, rates, ETAs, levels gained, etc.
	/// </summary>
	public class SkillSession
	{
		private readonly DateTime _startTime;
		private readonly Dictionary<SkillName, int> _startXp;
		private readonly Dictionary<SkillName, int> _startLevel;

		public SkillSession()
		{
			_startTime = DateTime.UtcNow;
			_startXp    = Enum.GetValues<SkillName>()
							  .ToDictionary(s => s, s => Skills.GetXp(s));
			_startLevel = Enum.GetValues<SkillName>()
							  .ToDictionary(s => s, s => Skills.Get(s).CurrentLevel);
		}

		/// <summary>Elapsed time since session start.</summary>
		public TimeSpan Elapsed => DateTime.UtcNow - _startTime;

		/// <summary>XP gained so far in this skill.</summary>
		public int GetXpGained(SkillName skill)
			=> Skills.GetXp(skill) - _startXp[skill];

		/// <summary>Levels gained so far in this skill.</summary>
		public int GetLevelsGained(SkillName skill)
			=> Skills.Get(skill).CurrentLevel - _startLevel[skill];

		/// <summary>Average XP/hr so far.</summary>
		public double GetXpPerHour(SkillName skill)
		{
			var gained = GetXpGained(skill);
			return Elapsed.TotalHours > 0
				 ? gained / Elapsed.TotalHours
				 : 0;
		}

		/// <summary>ETA until next level at current rate.</summary>
		public TimeSpan GetTimeToNextLevel(SkillName skill)
		{
			var stat = Skills.Get(skill);
			var toGo = Skills.GetXpToNextLevel(stat);
			var rate = GetXpPerHour(skill);
			return rate <= 0
			   ? TimeSpan.MaxValue
			   : TimeSpan.FromHours(toGo / rate);
		}

		public IReadOnlyDictionary<SkillName, int> AllXpGained() => _startXp.Keys.ToDictionary(s => s, s => GetXpGained(s));
		public IReadOnlyDictionary<SkillName, double> AllXpPerHour() => _startXp.Keys.ToDictionary(s => s, s => GetXpPerHour(s));
		public IReadOnlyDictionary<SkillName, int> AllLevelsGained() => _startLevel.Keys.ToDictionary(s => s, s => GetLevelsGained(s));
	}

	// SafeHandle:
	internal class SafeSkillHandle : SafeHandle
	{
		public SafeSkillHandle() : base(IntPtr.Zero, true) { }
		public override bool IsInvalid => handle == IntPtr.Zero;
		protected override bool ReleaseHandle()
		{
			Native_Skills.SK_Free(handle);
			return true;
		}
	}
}
