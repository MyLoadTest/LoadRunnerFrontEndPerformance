using System;
using System.Globalization;
using System.Linq;
using MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.Properties;

namespace MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn
{
    internal static class LocalHelper
    {
        #region Public Methods

        public static string GetTranslation(this Enum value)
        {
            #region Argument Check

            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            #endregion

            var name = string.Format(
                CultureInfo.InvariantCulture,
                "Enum_{0}_{1}",
                value.GetType().GetQualifiedName(),
                value.GetName());

            var translation = Resources.ResourceManager.GetString(name);

            if (translation.IsNullOrWhiteSpace())
            {
                translation = value.GetName();
            }

            return translation;
        }

        public static byte[] GetTestHarFile(string harName)
        {
            #region Argument Check

            if (string.IsNullOrWhiteSpace(harName))
            {
                throw new ArgumentException(
                    @"The value can be neither empty nor whitespace-only string nor null.",
                    "harName");
            }

            #endregion

            var name = string.Format(CultureInfo.InvariantCulture, "HAR_{0}", harName);
            return (byte[])Resources.ResourceManager.GetObject(name).EnsureNotNull();
        }

        #endregion
    }
}