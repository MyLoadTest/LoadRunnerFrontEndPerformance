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
                }>\d*)\, Internal ID\=(?<{InternalIdGroupName}>\d+)\)$",
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
                TransactionInfo transactionInfo = null;
                var pageUniqueId = 0;
                var implicitTransactionUniqueId = 0;
                List<HarPage> harPages = null;
                List<HarEntry> harEntries = null;
                Dictionary<long, HarEntry> internalIdToHarEntryMap = null;
                HarPage harPage = null;

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

                Func<bool> commitTransaction =
                    () =>
                    {
                        // ReSharper disable once AccessToModifiedClosure - As designed
                        if (transactionInfo == null)
                        {
                            return false;
                        }

                        // ReSharper disable once AccessToModifiedClosure - As designed
                        var harLog = transactionInfo.HarRoot.EnsureNotNull().Log.EnsureNotNull();

                        harLog.Pages = harPages.EnsureNotNull().ToArray();
                        harLog.Entries = harEntries.EnsureNotNull().ToArray();

                        // ReSharper disable once AccessToModifiedClosure - As designed
                        transactionInfos.Add(transactionInfo);

                        transactionInfo = null;
                        harPages = null;
                        harEntries = null;
                        internalIdToHarEntryMap = null;

                        return true;
                    };

                Func<string, TransactionInfo> createTransactionInfo =
                    name =>
                    {
                        var result = new TransactionInfo(name)
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
                        internalIdToHarEntryMap = new Dictionary<long, HarEntry>();

                        return result;
                    };

                Func<TransactionInfo> createImplicitTransactionInfo =
                    () =>
                    {
                        implicitTransactionUniqueId++;
                        var name = $"Implicit transaction {implicitTransactionUniqueId}";
                        return createTransactionInfo(name);
                    };

                Func<TransactionInfo> ensureTransactionInfo =
                    () => transactionInfo ?? (transactionInfo = createImplicitTransactionInfo());

                //// ReSharper disable once LoopVariableIsNeverChangedInsideLoop - False positive
                while (fetchLine())
                {
                    var transactionEndMatch = line.MatchAgainst(TransactionEndRegex);
                    if (transactionEndMatch.Success)
                    {
                        commitTransaction();
                        continue;
                    }

                    var transactionStartMatch = line.MatchAgainst(TransactionStartRegex);
                    if (transactionStartMatch.Success)
                    {
                        commitTransaction();

                        var name = transactionStartMatch.GetSucceededGroupValue(NameGroupName);
                        transactionInfo = createTransactionInfo(name);
                        continue;
                    }

                    var connectedSocketMatch = line.MatchAgainst(ConnectedSocketRegex);
                    if (connectedSocketMatch.Success)
                    {
                        var sourceEndpointMatch =
                            IPEndPointRegex.Match(
                                connectedSocketMatch.GetSucceededGroupValue(SourceEndpointGroupName));

                        var targetEndpointMatch =
                            IPEndPointRegex.Match(
                                connectedSocketMatch.GetSucceededGroupValue(TargetEndpointGroupName));

                        Debug.WriteLine(sourceEndpointMatch.Value);
                        Debug.WriteLine(targetEndpointMatch.Value);

                        fetchLine();
                        var requestHeadersMarkerMatch = line.MatchAgainst(RequestHeadersMarkerRegex);
                        if (!requestHeadersMarkerMatch.Success)
                        {
                            throw new InvalidOperationException($"Request header was expected at line {lineIndex}.");
                        }

                        var url = requestHeadersMarkerMatch.GetSucceededGroupValue(UrlGroupName);
                        var size = ParseLong(requestHeadersMarkerMatch.GetSucceededGroupValue(SizeGroupName));

                        var frameId =
                            ParseNullableLong(requestHeadersMarkerMatch.GetSucceededGroupValue(FrameIdGroupName));

                        var internalId =
                            ParseLong(requestHeadersMarkerMatch.GetSucceededGroupValue(InternalIdGroupName));

                        ensureTransactionInfo();

                        if (frameId.HasValue || harPage == null)
                        {
                            harPage = new HarPage
                            {
                                Id = $"page_{++pageUniqueId}",
                                Title = url
                            };

                            harPages.EnsureNotNull().Add(harPage);
                        }

                        var harRequest = new HarRequest
                        {
                            HeadersSize = size,
                            Url = url
                        };

                        var harEntry = new HarEntry
                        {
                            PageRef = harPage.Id,
                            ////ConnectionId = //// TODO [vmcl] ConnectionId = Port of SourceEndpointGroupName
                            ////ServerIPAddress =  //// TODO [vmcl] ConnectionId = From TargetEndpointGroupName
                            Request = harRequest
                        };

                        harEntries.EnsureNotNull().Add(harEntry);
                        internalIdToHarEntryMap.Add(internalId, harEntry);

                        fetchLine();
                        var httpRequestLineMatch = line.MatchAgainst(HttpRequestLineRegex);
                        if (!httpRequestLineMatch.Success)
                        {
                            //// TODO [vmcl] VuGen output may contain Request-Line carried over to a few lines

                            throw new InvalidOperationException(
                                $"An HTTP Request-Line was expected at line {lineIndex}.");
                        }

                        harRequest.Method = httpRequestLineMatch.GetSucceededGroupValue(HttpMethodGroupName);
                        harRequest.HttpVersion = httpRequestLineMatch.GetSucceededGroupValue(HttpVersionGroupName);

                        var harHeaders = new List<HarHeader>();

                        //// ReSharper disable LoopVariableIsNeverChangedInsideLoop - False positive
                        while (fetchLine())
                        {
                            //// ReSharper restore LoopVariableIsNeverChangedInsideLoop

                            var headerMatch = line.MatchAgainst(HttpHeaderRegex);
                            if (!headerMatch.Success)
                            {
                                break;
                            }

                            var name = headerMatch.GetSucceededGroupValue(NameGroupName);
                            var value = headerMatch.GetSucceededGroupValue(ValueGroupName);
                            var harHeader = new HarHeader { Name = name, Value = value };
                            harHeaders.Add(harHeader);
                        }

                        harRequest.Headers = harHeaders.ToArray();

                        if (!line.MatchAgainst(HttpHeaderEndedRegex).Success)
                        {
                            throw new InvalidOperationException(
                                $"The HTTP headers ended prematurely at line {lineIndex}.");
                        }

                        continue;
                    }

                    //// TODO [vmcl] Parse request body
                    //// TODO [vmcl] Parse response headers
                    //// TODO [vmcl] Parse response body

                    Debug.WriteLine($"[{GetType().GetQualifiedName()}] Skipping line: {rawLine}");
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

        private static long? ParseNullableLong(string value)
        {
            return value.IsNullOrEmpty() ? default(long?) : ParseLong(value);
        }

        private static long ParseLong(string value)
        {
            return long.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
        }

        #endregion
    }
}