﻿using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace dCom.Converters
{
	public class StringToVisibilityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return string.IsNullOrEmpty(value as string) ? Visibility.Collapsed : Visibility.Visible;
		}

		// TODO
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}