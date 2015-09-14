using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.Har;

namespace MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.Parsing
{
    internal sealed class VuGenOutputLogParser
    {
        #region Constants and Fields

        private const string NameGroupName = "Name";
        private const string ValueGroupName = "Value";
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
        private const string HttpMethodGroupName = "HttpMethod";
        private const string HttpVersionGroupName = "HttpVersion";

        private const string DecimalNumberPattern = @"\d+\.?\d*|\d*\.?\d+";
        private const string IPAddressPattern = @"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}"; // IPv4 only so far
        private const string PortPattern = @"\d+";

        private const string FileAndPositionPrefixPattern = @"^[^\(]+\(\d+\):";

        private const string HttpRequestLineElementPattern = @"\S+";
        private const string HttpRequestLineVersionPattern = @"\d+\.\d+";

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

        private static readonly Regex HttpRequestLineRegex = new Regex(
            $@"{FileAndPositionPrefixPattern}\s{{5}}(?<{HttpMethodGroupName}>{HttpRequestLineElementPattern})\s+(?<{
                UrlGroupName}>{HttpRequestLineElementPattern})\s+HTTP/(?<{HttpVersionGroupName}>{
                HttpRequestLineVersionPattern})\s*$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);

        private static readonly Regex HttpHeaderRegex = new Regex(
            $@"{FileAndPositionPrefixPattern}\s{{5}}(?<{NameGroupName}>[^:]+)\s*\:\s*(?<{ValueGroupName}>.*?)\r\n$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);

        private static readonly Regex HttpHeaderEndedRegex = new Regex(
            $@"{FileAndPositionPrefixPattern}\s{{5}}\r\n$",
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
            ConnectionOpened,
            RequestHeadersSectionStartedExpectingRequestLine,
            RequestHeadersSectionStartedExpectingHeaders,
            RequestHeadersSectionEnded,
            RequestBodySectionStarted, //// TODO [vmcl] RequestBodySectionStarted (eg. for POST request)
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
                var pageId = 0;
                List<HarPage> harPages = null;
                List<HarEntry> harEntries = null;
                HarPage harPage;
                HarEntry harEntry = null;

                string rawLine = null;
                string line = null;
                var lineIndex = 0;
                var skipFetchOnce = false;

                //// ReSharper disable once AccessToDisposedClosure - Executed immediately in scope of 'using' in this case
                Func<bool> fetchLine =
                    () =>
                    {
                        if (skipFetchOnce)
                        {
                            if (lineIndex == 0)
                            {
                                throw new InvalidOperationException(
                                    "The fetch of a line is not supposed to be skipped at the very beginning.");
                            }

                            skipFetchOnce = false;
                            return rawLine != null;
                        }

                        rawLine = reader.ReadLine();
                        if (rawLine == null)
                        {
                            line = null;
                            return false;
                        }

                        line = NormalizeOutputLogString(rawLine);
                        lineIndex++;

                        return true;
                    };

                //// TODO [vmcl] Consider async request/responses

                //// ReSharper disable once LoopVariableIsNeverChangedInsideLoop - False positive
                while (fetchLine())
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

                                var name = transactionStartMatch.Groups.GetSucceeded(NameGroupName).Value;

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

                                harPages = new List<HarPage>();
                                harEntries = new List<HarEntry>();

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
                                            connectedSocketMatch.Groups.GetSucceeded(SourceEndpointGroupName).Value);

                                    var targetEndpointMatch =
                                        IPEndPointRegex.Match(
                                            connectedSocketMatch.Groups.GetSucceeded(TargetEndpointGroupName).Value);

                                    Debug.WriteLine(sourceEndpointMatch.Value);
                                    Debug.WriteLine(targetEndpointMatch.Value);

                                    parsingState = ParsingState.ConnectionOpened;
                                    continue;
                                }
                            }

                            break;

                        case ParsingState.ConnectionOpened:
                            {
                                var requestHeadersMarkerMatch = RequestHeadersMarkerRegex.Match(line);
                                if (requestHeadersMarkerMatch.Success)
                                {
                                    var url = requestHeadersMarkerMatch.Groups.GetSucceeded(UrlGroupName).Value;

                                    var sizeString =
                                        requestHeadersMarkerMatch.Groups.GetSucceeded(SizeGroupName).Value;

                                    var size = long.Parse(
                                        sizeString,
                                        NumberStyles.Integer,
                                        CultureInfo.InvariantCulture);

                                    harPage = new HarPage
                                    {
                                        Id = $"page_{++pageId}",
                                        Title = url
                                    };

                                    harPages.EnsureNotNull().Add(harPage);

                                    harEntry = new HarEntry
                                    {
                                        PageRef = harPage.Id,
                                        ////ConnectionId = //// TODO [vmcl] ConnectionId = Port of SourceEndpointGroupName
                                        ////ServerIPAddress =  //// TODO [vmcl] ConnectionId = From TargetEndpointGroupName
                                        Request = new HarRequest { HeadersSize = size, Url = url }
                                    };

                                    harEntries.EnsureNotNull().Add(harEntry);

                                    parsingState = ParsingState.RequestHeadersSectionStartedExpectingRequestLine;
                                    continue;
                                }
                            }

                            break;

                        case ParsingState.RequestHeadersSectionStartedExpectingRequestLine:
                            {
                                var match = HttpRequestLineRegex.Match(line);
                                if (!match.Success)
                                {
                                    throw new InvalidOperationException(
                                        $"An HTTP Request-Line was expected at line {lineIndex}.");
                                }

                                var httpMethod = match.Groups.GetSucceeded(HttpMethodGroupName).Value;
                                var httpVersion = match.Groups.GetSucceeded(HttpVersionGroupName).Value;

                                var harRequest = harEntry.EnsureNotNull().Request.EnsureNotNull();
                                harRequest.Method = httpMethod;
                                harRequest.HttpVersion = httpVersion;

                                parsingState = ParsingState.RequestHeadersSectionStartedExpectingHeaders;
                                continue;
                            }

                        case ParsingState.RequestHeadersSectionStartedExpectingHeaders:
                            {
                                var harHeaders = new List<HarHeader>();
                                while (true)
                                {
                                    var headerMatch = HttpHeaderRegex.Match(line);
                                    if (!headerMatch.Success)
                                    {
                                        break;
                                    }

                                    var name = headerMatch.Groups.GetSucceeded(NameGroupName).Value;
                                    var value = headerMatch.Groups.GetSucceeded(ValueGroupName).Value;
                                    var harHeader = new HarHeader { Name = name, Value = value };
                                    harHeaders.Add(harHeader);

                                    if (!fetchLine())
                                    {
                                        break;
                                    }
                                }

                                harEntry.EnsureNotNull().Request.EnsureNotNull().Headers = harHeaders.ToArray();

                                if (!line.MatchAgainst(HttpHeaderEndedRegex).Success)
                                {
                                    throw new InvalidOperationException(
                                        $"The HTTP headers ended prematurely at line {lineIndex}.");
                                }

                                parsingState = ParsingState.RequestHeadersSectionEnded;
                                continue;
                            }

                        case ParsingState.RequestHeadersSectionEnded:
                            //// TODO [vmcl] Implement case ParsingState.RequestHeadersSectionEnded
                            break;

                        default:
                            throw parsingState.CreateEnumValueNotImplementedException();
                    }

                    var transactionEndMatch = TransactionEndRegex.Match(line);
                    if (transactionEndMatch.Success)
                    {
                        if (transactionInfo != null)
                        {
                            var harLog = transactionInfo.HarRoot.Log;
                            harLog.Pages = harPages.EnsureNotNull().ToArray();
                            harLog.Entries = harEntries.EnsureNotNull().ToArray();

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

        #region Private Methods

        private static string NormalizeOutputLogString(string value)
        {
            return value?.Replace(@"\r\n", "\r\n");
        }

        #endregion
    }
}