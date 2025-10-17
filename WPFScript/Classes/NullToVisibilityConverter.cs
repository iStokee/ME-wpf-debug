using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MESharp.Converters
{
	/// <summary>Not Null → Visible, Null → Collapsed</summary>
	public class NullToVisibilityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
			=> value != null
				? Visibility.Visible
				: Visibility.Collapsed;

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
			=> throw new NotImplementedException();
	}

	/// <summary>Null → Visible, Not Null → Collapsed</summary>
	public class InverseNullToVisibilityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
			=> value == null
				? Visibility.Visible
				: Visibility.Collapsed;

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
			=> throw new NotImplementedException();
	}
}
