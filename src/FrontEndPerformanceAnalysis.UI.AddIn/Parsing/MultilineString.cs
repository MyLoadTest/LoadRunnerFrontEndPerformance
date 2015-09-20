using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Omnifactotum;
using Omnifactotum.Annotations;

namespace MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.Parsing
{
    [DebuggerDisplay(
        "{GetType().Name,nq}. LineIndexRange = {LineIndexRange.ToString()}"
            + ", Lines.Count = {Lines?.Count}")]
    internal sealed class MultilineString
    {
        #region Constructors

        public MultilineString([CanBeNull] string value, ValueRange<int> lineIndexRange)
        {
            Lines = SplitIntoLines(value).AsReadOnly();
            LineIndexRange = lineIndexRange;
        }

        #endregion

        #region Public Properties

        [NotNull]
        public ReadOnlyCollection<string> Lines
        {
            get;
        }

        public ValueRange<int> LineIndexRange
        {
            get;
        }

        #endregion

        #region Private Methods

        private static string[] SplitIntoLines([CanBeNull] string value)
        {
            if (value == null)
            {
                return new string[0];
            }

            if (value == string.Empty)
            {
                return string.Empty.AsArray();
            }

            var unescapedValue = value.UnescapeLogString();

            var lines = new List<string>();
            using (var stringReader = new StringReader(unescapedValue))
            {
                string line;
                while ((line = stringReader.ReadLine()) != null)
                {
                    lines.Add(line);
                }
            }

            return lines.ToArray();
        }

        #endregion
    }
}