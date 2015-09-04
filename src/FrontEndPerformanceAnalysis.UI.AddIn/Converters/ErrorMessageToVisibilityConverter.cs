using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.Converters
{
    internal sealed class ErrorMessageToVisibilityConverter : IValueConverter
    {
        #region Constructors

        public ErrorMessageToVisibilityConverter()
        {
            EmptyValueVisibility = Visibility.Collapsed;
            NonEmptyValueVisibility = Visibility.Visible;
        }

        #endregion

        #region Public Properties

        public Visibility EmptyValueVisibility
        {
            get;
            set;
        }

        public Visibility NonEmptyValueVisibility
        {
            get;
            set;
        }

        #endregion

        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var castValue = (string)value;
            return castValue.IsNullOrEmpty() ?  EmptyValueVisibility: NonEmptyValueVisibility;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        #endregion
    }
}