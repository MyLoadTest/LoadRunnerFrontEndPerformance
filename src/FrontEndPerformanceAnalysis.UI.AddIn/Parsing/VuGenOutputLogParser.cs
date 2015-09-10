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
        private const string TimestampGroupName = "Timestamp";
        private const string IPAddressGroupName = "IPAddress";
        private const string PortGroupName = "Port";
        private const string SourceEndpointGroupName = "SourceEndpoint";
        private const string TargetEndpointGroupName = "TargetEndpoint";

        private const string DecimalNumberPattern = @"\d+\.?\d*|\d*\.?\d+";
        private const string IPAddressPattern = @"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}"; // IPv4 only so far
        private const string PortPattern = @"\d+";

        private const string FileAndPositionPrefixPattern = @"^[^\(]+\(\d+\):";

        private static readonly string IPAddressAndPortPattern = $@"{IPAddressPattern}\:{PortPattern}";

        private static readonly Regex IPEndPointRegex = new Regex(
            $@"(?<{IPAddressGroupName}>{IPAddressPattern})\:(?<{PortGroupName}>{PortPattern})",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);

        private static readonly Regex TransactionStartRegex = new Regex(
            $@"{FileAndPositionPrefixPattern} Notify\: Transaction \""(?<{NameGroupName}>[^""]+)\"" started\.$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);

        private static readonly Regex TransactionEndRegex = new Regex(
            $@"{FileAndPositionPrefixPattern} Notify\: Transaction \""(?<{NameGroupName
                }>[^""]+)\"" ended with a \""(?<{StatusGroupName
                }>[^""]+)\"" status \(Duration: (?<{DurationGroupName}>{DecimalNumberPattern}) Wasted Time: (?<{
                WastedTimeGroupName}>{DecimalNumberPattern})\)\.$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);

        private static readonly Regex ConnectedSocketRegex = new Regex(
            $@"{FileAndPositionPrefixPattern} t=(?<{TimestampGroupName}>\d+)ms\: Connected socket \[\d+\] from (?<{
                SourceEndpointGroupName}>{
                IPAddressAndPortPattern}) to (?<{TargetEndpointGroupName}>{IPAddressAndPortPattern}) in (?<{
                DurationGroupName}>\d+) ms\s+\[MsgId\: MMSG\-\d+\]$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);

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

                    var match3 = ConnectedSocketRegex.Match(line);
                    Debug.WriteLine(match3.Success);

                    if (match3.Success)
                    {
                        var sourceEndpointMatch = IPEndPointRegex.Match(match3.Groups[SourceEndpointGroupName].Value);
                        Debug.WriteLine(sourceEndpointMatch.Success);

                        var targetEndpointMatch = IPEndPointRegex.Match(match3.Groups[TargetEndpointGroupName].Value);
                        Debug.WriteLine(targetEndpointMatch.Success);
                    }
                }
            }

            throw new NotImplementedException();
        }

        #endregion
    }
}