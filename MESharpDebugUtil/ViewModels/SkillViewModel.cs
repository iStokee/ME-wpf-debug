using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Security.Policy;
using System.Windows.Media;
using MESharp.API;

namespace MESharp.ViewModels
{
	/// <summary>
	/// ViewModel wrapper for one skill, exposing XP metrics to the UI.
	/// </summary>
	public class SkillViewModel : INotifyPropertyChanged
	{
		private readonly SkillSession _session;
		public string Name { get; }
		public string Level { get; set; }
		public int Xp { get; private set; }
		public int XpGained { get; private set; }
		public double XpPerHour { get; private set; }
		public int XpToNext { get; private set; }
		public string EtaString { get; private set; }
		public Brush PrimaryBrush { get; }
		public Brush SecondaryBrush { get; }

		public SkillViewModel(SkillName name, SkillSession session)
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
			Level = snapshot.CurrentLevel.ToString();
			XpGained = _session.GetXpGained(skillEnum);
			XpPerHour = _session.GetXpPerHour(skillEnum);
			XpToNext = Skills.GetXpToNextLevel(snapshot);

			var eta = _session.GetTimeToNextLevel(skillEnum);
			EtaString = eta == TimeSpan.MaxValue ? "--" : eta.ToString(@"hh\:mm");

			// Notify property changes
			OnPropertyChanged(nameof(Xp));
			OnPropertyChanged(nameof(XpGained));
			OnPropertyChanged(nameof(XpPerHour));
			OnPropertyChanged(nameof(XpToNext));
			OnPropertyChanged(nameof(EtaString));
		}

		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged([CallerMemberName] string propName = null)
			=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));

	}
}