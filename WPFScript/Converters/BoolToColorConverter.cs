using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace MESharp.Converters
{
	/// <summary>
	/// Converts a boolean value to a color brush.
	/// True = Green, False = Red
	/// </summary>
	public class BoolToColorConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is bool boolValue)
			{
				return boolValue ? Brushes.LightGreen : Brushes.LightCoral;
			}
			return Brushes.Gray;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
