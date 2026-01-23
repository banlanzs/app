// Copyright (C) 2024 Stratum Contributors
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Globalization;
using System.Windows.Data;
using Stratum.Desktop.Services;

namespace Stratum.Desktop.Converters
{
    public class IconSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not ValidatorDisplayMode displayMode)
                return 44.0;

            var sizeType = parameter as string ?? "Container";
            return GetBaseSize(displayMode, sizeType);
        }

        private double GetBaseSize(ValidatorDisplayMode displayMode, string sizeType)
        {
            return displayMode switch
            {
                ValidatorDisplayMode.Compact => sizeType == "Image" ? 20.0 : 32.0,
                ValidatorDisplayMode.Tile => sizeType == "Image" ? 32.0 : 48.0,
                _ => sizeType == "Image" ? 28.0 : 44.0
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
