using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace MESharp.Converters
{
	/// <summary>
	/// Converts bool to icon: true = Green ✓, false = Red -
	/// </summary>
	public class BoolToIconConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is bool b)
			{
				return b ? "✓" : "−";
			}
			return "—";
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

	/// <summary>
	/// Converts bool to color: true = Green, false = Red
	/// </summary>
	public class BoolToColorConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is bool b)
			{
				return b ? new SolidColorBrush(Color.FromRgb(46, 204, 113)) : new SolidColorBrush(Color.FromRgb(231, 76, 60));
			}
			return new SolidColorBrush(Colors.Gray);
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
