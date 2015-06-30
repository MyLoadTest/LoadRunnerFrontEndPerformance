using System;
using System.Linq;

namespace MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.Controls
{
    internal static class ControlItem
    {
        #region Public Methods

        public static ControlItem<T> Create<T>(T value, string text)
        {
            return new ControlItem<T>(value, text);
        }

        public static ControlItem<T> Create<T>(T value)
        {
            return new ControlItem<T>(value);
        }

        #endregion
    }
}