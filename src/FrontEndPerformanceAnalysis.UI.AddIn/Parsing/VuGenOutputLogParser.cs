using System;
using System.Collections.Generic;
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
        private const string SizeGroupName = "Size";
        private const string FrameIdGroupName = "FrameId";
        private const string InternalIdGroupName = "InternalId";
        private const string UrlGroupName = "Url";

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

        private static readonly Regex RequestHeadersMarkerRegex = new Regex(
            $@"{FileAndPositionPrefixPattern} t=(?<{TimestampGroupName}>\d+)ms\: (?<{SizeGroupName
                }>\d+)-byte request headers for ""(?<{UrlGroupName}>[^""]+)"" \(RelFrameId\=(?<{FrameIdGroupName
                }>\d+)\, Internal ID\=(?<{InternalIdGroupName}>\d+)\)$",
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

        #region ParsingState Enumeration

        private enum ParsingState
        {
            Initial,
            TransactionStarted,
            ConnectionOpened
        }

        #endregion

        #region Public Methods

        public TransactionInfo[] Parse()
        {
            if (!File.Exists(_logPath))
            {
                throw new FileNotFoundException("The output log file is not found.", _logPath);
            }

            var transactionInfos = new List<TransactionInfo>();

            using (var reader = new StreamReader(_logPath))
            {
                var parsingState = ParsingState.Initial;
                TransactionInfo transactionInfo = null;

                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    switch (parsingState)
                    {
                        case ParsingState.Initial:
                            {
                                var transactionStartMatch = TransactionStartRegex.Match(line);
                                if (!transactionStartMatch.Success)
                                {
                                    continue;
                                }

                                var name = transactionStartMatch.Groups[NameGroupName].EnsureSucceeded().Value;

                                transactionInfo = new TransactionInfo(name, transactionInfos.Count + 1)
                                {
                                    HarRoot =
                                    {
                                        Log = new HarLog
                                        {
                                            Creator = new HarCreator
                                            {
                                                Name = GetType().GetFullName(),
                                                Version = GetType().Assembly.GetName().Version.ToString()
                                            }
                                        }
                                    }
                                };

                                parsingState = ParsingState.TransactionStarted;
                                continue;
                            }

                        case ParsingState.TransactionStarted:
                            {
                                var connectedSocketMatch = ConnectedSocketRegex.Match(line);
                                if (connectedSocketMatch.Success)
                                {
                                    var sourceEndpointMatch =
                                        IPEndPointRegex.Match(
                                            connectedSocketMatch.Groups[SourceEndpointGroupName].Value);
                                    Debug.WriteLine(sourceEndpointMatch.Value);

                                    var targetEndpointMatch =
                                        IPEndPointRegex.Match(
                                            connectedSocketMatch.Groups[TargetEndpointGroupName].Value);
                                    Debug.WriteLine(targetEndpointMatch.Value);

                                    parsingState = ParsingState.ConnectionOpened;
                                    continue;
                                }
                            }

                            break;

                        case ParsingState.ConnectionOpened:
                            {
                                //// TODO [vmcl] Implement ConnectionOpened case

                                var requestHeadersMarkerMatch = RequestHeadersMarkerRegex.Match(line);
                                if (requestHeadersMarkerMatch.Success)
                                {
                                    Debug.WriteLine(requestHeadersMarkerMatch.Value);
                                    continue;
                                }
                            }

                            break;

                        default:
                            throw parsingState.CreateEnumValueNotImplementedException();
                    }

                    var transactionEndMatch = TransactionEndRegex.Match(line);
                    if (transactionEndMatch.Success)
                    {
                        if (transactionInfo != null)
                        {
                            transactionInfos.Add(transactionInfo);
                            transactionInfo = null;
                        }

                        parsingState = ParsingState.Initial;
                        ////continue;
                    }
                }
            }

            return transactionInfos.ToArray();
        }

        #endregion
    }
}