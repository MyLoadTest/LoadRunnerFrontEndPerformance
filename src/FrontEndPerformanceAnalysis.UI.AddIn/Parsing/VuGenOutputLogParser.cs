using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.Har;

namespace MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.Parsing
{
    internal sealed class VuGenOutputLogParser
    {
        #region Constants and Fields

        private const string NameGroupName = "Name";
        private const string StatusGroupName = "Status";
        private const string DurationGroupName = "Duration";
        private const string WastedTimeGroupName = "WastedTime";

        private static readonly Regex TransactionStartRegex = new Regex(
            $@"Notify: Transaction \""(?<{NameGroupName}>[^""]+)\"" started\.",
            RegexOptions.Compiled);

        private static readonly Regex TransactionEndRegex = new Regex(
            $@"Notify: Transaction \""(?<{NameGroupName}>[^""]+)\"" ended with a \""(?<{StatusGroupName
                }>[^""]+)\"" status \(Duration: (?<{DurationGroupName}>\d+\.?\d*|\d*\.?\d+) Wasted Time: (?<{
                WastedTimeGroupName}>\d+\.?\d*|\d*\.?\d+)\)\.",
            RegexOptions.Compiled);

        private readonly string _logPath;

        #endregion

        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="VuGenOutputLogParser"/> class.
        /// </summary>
        public VuGenOutputLogParser(string logPath)
        {
            if (string.IsNullOrWhiteSpace(logPath))
            {
                throw new ArgumentException(
                    @"The value can be neither empty nor whitespace-only string nor null.",
                    nameof(logPath));
            }

            _logPath = logPath;
        }

        #endregion

        #region Public Methods

        public HarRoot Parse()
        {
            if (!File.Exists(_logPath))
            {
                throw new FileNotFoundException("The output log file is not found.", _logPath);
            }

            using (var reader = new StreamReader(_logPath))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    var match1 = TransactionStartRegex.Match(line);
                    Debug.WriteLine(match1.Success);

                    var match2 = TransactionEndRegex.Match(line);
                    Debug.WriteLine(match2.Success);
                }
            }

            throw new NotImplementedException();
        }

        #endregion
    }
}