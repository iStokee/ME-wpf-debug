using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Security.Policy;
using System.Windows.Media;
using MESharp.API;
using MESharp.ViewModels;

namespace MESharp.Models
{
	/// <summary>
	/// ViewModel wrapper for one skill, exposing XP metrics to the UI.
	/// </summary>
	public class SkillModel : INotifyPropertyChanged
	{
		private readonly SkillSession _session;
		public string Name { get; }
		public string Level { get; private set; }
		public int LevelValue { get; private set; }
		public int Xp { get; private set; }
		public int XpGained { get; private set; }
		public double XpPerHour { get; private set; }
		public int XpToNext { get; private set; }
		public string EtaString { get; private set; }
		public double EtaMinutes { get; private set; }
		public bool IsActive => XpGained > 0;
		public Brush PrimaryBrush { get; }
		public Brush SecondaryBrush { get; }

		public SkillModel(SkillName name, SkillSession session)
		{
			Name = name.ToString();
			_session = session;

			// Map to your primary and secondary colors
			var colorPair = ColorMappings.GetColorPair(Name);
			PrimaryBrush = new SolidColorBrush(
				(Color)ColorConverter.ConvertFromString(colorPair.Item1)
			);
			SecondaryBrush = new SolidColorBrush(
				(Color)ColorConverter.ConvertFromString(colorPair.Item2)
			);

			Update();
		}

		/// <summary>
		/// Pull fresh stats from native API and notify UI.
		/// </summary>
		public void Update()
		{
			var skillEnum = (SkillName)Enum.Parse(typeof(SkillName), Name);
			var snapshot = Skills.Get(skillEnum);

			Xp = snapshot.Xp;
			LevelValue = snapshot.CurrentLevel;
			Level = LevelValue.ToString();
			XpGained = _session.GetXpGained(skillEnum);
			XpPerHour = _session.GetXpPerHour(skillEnum);
			XpToNext = Skills.GetXpToNextLevel(snapshot);

			var eta = _session.GetTimeToNextLevel(skillEnum);
			EtaString = eta == TimeSpan.MaxValue ? "--" : eta.ToString(@"hh\:mm\:ss");
			EtaMinutes = eta == TimeSpan.MaxValue ? double.MaxValue : eta.TotalMinutes;

			// Notify property changes
			OnPropertyChanged(nameof(Level));
			OnPropertyChanged(nameof(LevelValue));
			OnPropertyChanged(nameof(Xp));
			OnPropertyChanged(nameof(XpGained));
			OnPropertyChanged(nameof(XpPerHour));
			OnPropertyChanged(nameof(XpToNext));
			OnPropertyChanged(nameof(EtaString));
			OnPropertyChanged(nameof(EtaMinutes));
			OnPropertyChanged(nameof(IsActive));
		}

		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged([CallerMemberName] string propName = null)
			=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));

	}
}
