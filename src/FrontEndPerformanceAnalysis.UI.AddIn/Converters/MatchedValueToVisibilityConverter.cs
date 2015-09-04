using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.Converters
{
    internal class MatchedValueToVisibilityConverter<TValue> : IValueConverter
    {
        #region Constructors

        public MatchedValueToVisibilityConverter()
        {
            MatchedValueVisibility = Visibility.Visible;
            UnmatchedValueVisibility = Visibility.Collapsed;
        }

        #endregion

        #region Public Properties

        public Visibility MatchedValueVisibility
        {
            get;
            set;
        }

        public Visibility UnmatchedValueVisibility
        {
            get;
            set;
        }

        #endregion

        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var castValue = (TValue)value;
            var castParameter = (TValue)parameter;
            var matches = EqualityComparer<TValue>.Default.Equals(castValue, castParameter);
            return matches ? MatchedValueVisibility : UnmatchedValueVisibility;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        #endregion
    }
}