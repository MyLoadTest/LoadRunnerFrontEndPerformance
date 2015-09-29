using System;
using System.Linq;

namespace MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn
{
    internal static class DescriptiveItem
    {
        #region Public Methods

        public static DescriptiveItem<T> Create<T>(T value, string text)
        {
            return new DescriptiveItem<T>(value, text);
        }

        public static DescriptiveItem<T> Create<T>(T value)
        {
            return new DescriptiveItem<T>(value);
        }

        #endregion
    }
}