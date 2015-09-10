using System;
using System.Text.RegularExpressions;
using Omnifactotum.Annotations;

namespace MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.Parsing
{
    internal static class RegexHelper
    {
        #region Public Methods

        public static T EnsureSucceeded<T>([NotNull] this T obj)
            where T : Group
        {
            #region Argument Check

            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }

            #endregion

            if (!obj.Success)
            {
                throw new InvalidOperationException("The specified group was supposed to succeed.");
            }

            return obj;
        }

        #endregion
    }
}