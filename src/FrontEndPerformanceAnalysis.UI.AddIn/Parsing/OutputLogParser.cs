using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.Har;
using Omnifactotum;
using Omnifactotum.Annotations;

namespace MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.Parsing
{
    internal sealed class OutputLogParser : IDisposable
    {
        #region Constants and Fields

        private StreamReader _reader;
        private bool _isDisposed;
        private bool _isParseExecuted;

        private TransactionInfo _transactionInfo;
        private int _pageUniqueId;
        private int _implicitTransactionUniqueId;
        private List<HarPage> _harPages;
        private List<HarEntry> _harEntries;
        private Dictionary<long, HarEntry> _internalIdToHarEntryMap;
        private Dictionary<string, HarEntry> _urlToOpenRequestHarEntryMap;
        private HarPage _harPage;
        private string _line;
        private int _lineIndex;
        private bool _skipFetchOnce;
        private List<TransactionInfo> _transactionInfos;
        private Dictionary<int, SocketData> _socketIdToDataMap;
        private DateTimeOffset? _scriptStartTime;

        #endregion

        #region Constructors

        public OutputLogParser(string logPath)
        {
            if (string.IsNullOrWhiteSpace(logPath))
            {
                throw new ArgumentException(
                    @"The value can be neither empty nor whitespace-only string nor null.",
                    nameof(logPath));
            }

            if (!File.Exists(logPath))
            {
                throw new ArgumentException($"The output log file \"{logPath}\" is not found.", nameof(logPath));
            }

            _reader = new StreamReader(logPath);
        }

        #endregion

        #region Public Methods

        public TransactionInfo[] Parse()
        {
            EnsureNotDisposed();

            if (_isParseExecuted)
            {
                throw new InvalidOperationException("The parsing has already been executed.");
            }

            _isParseExecuted = true;

            ParseInternal();

            var result = _transactionInfos.EnsureNotNull().ToArray();
            _transactionInfos = null;
            return result;
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            Factotum.DisposeAndNull(ref _reader);
            _isDisposed = true;
        }

        #endregion

        #region Private Methods

        private static HarHeader[] ParseHttpHeaders(MultilineString multilineString)
        {
            var resultList = new List<HarHeader>();

            //// Starting from 1 since the 0th line is either HTTP Request-Line or HTTP Status-Line
            for (var index = 1; index < multilineString.Lines.Count; index++)
            {
                var line = multilineString.Lines[index];
                if (line.IsNullOrEmpty())
                {
                    continue;
                }

                var headerMatch = line.MatchAgainst(ParsingHelper.HttpHeaderRegex);
                if (!headerMatch.Success)
                {
                    throw new InvalidOperationException(
                        $@"HTTP Headers are expected at lines {
                            multilineString.LineIndexRange.Lower}-{multilineString.LineIndexRange.Upper}.");
                }

                var name = headerMatch.GetSucceededGroupValue(ParsingHelper.NameGroupName);
                var value = headerMatch.GetSucceededGroupValue(ParsingHelper.ValueGroupName);

                var harHeader = new HarHeader { Name = name, Value = value };
                resultList.Add(harHeader);
            }

            return resultList.ToArray();
        }

        private void EnsureNotDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(GetType().GetFullName());
            }
        }

        private DateTimeOffset GetTime([NotNull] Match match)
        {
            var timeOffset = ParsingHelper.GetTimeOffset(match);

            var result = _scriptStartTime.EnsureNotNull() + timeOffset;
            return result;
        }

        private bool FetchLine()
        {
            if (_skipFetchOnce)
            {
                if (_lineIndex == 0)
                {
                    throw new InvalidOperationException(
                        "The fetch of a line is not supposed to be skipped at the very beginning.");
                }

                _skipFetchOnce = false;
                return _line != null;
            }

            _line = _reader.ReadLine();
            if (_line == null)
            {
                return false;
            }

            _lineIndex++;

            return true;
        }

        [NotNull]
        private MultilineString FetchMultipleLines()
        {
            var stringBuilder = new StringBuilder();

            var startLineIndex = _lineIndex + 1;

            int? endLineIndex = null;
            while (FetchLine())
            {
                endLineIndex = _lineIndex;

                var multilineMatch = _line.MatchAgainst(ParsingHelper.MultiLineRegex);
                if (!multilineMatch.Success)
                {
                    _skipFetchOnce = true;
                    break;
                }

                var value = multilineMatch.GetSucceededGroupValue(ParsingHelper.ValueGroupName);
                stringBuilder.Append(value);
            }

            return endLineIndex.HasValue
                ? new MultilineString(
                    stringBuilder.ToString(),
                    ValueRange.Create(startLineIndex, endLineIndex.Value))
                : new MultilineString(null, ValueRange.Create(startLineIndex, startLineIndex));
        }

        private void CommitTransactionIfPending()
        {
            if (_transactionInfo == null)
            {
                return;
            }

            var harLog = _transactionInfo.HarRoot.EnsureNotNull().Log.EnsureNotNull();

            _harEntries.DoForEach(entry => entry.ComputeTime());

            harLog.Pages = _harPages.EnsureNotNull().ToArray();
            harLog.Entries = _harEntries.EnsureNotNull().ToArray();

            _transactionInfos.Add(_transactionInfo);

            _transactionInfo = null;

            _harPages = null;
            _harEntries = null;
            _internalIdToHarEntryMap = null;
            _urlToOpenRequestHarEntryMap = null;
            _socketIdToDataMap = null;
        }

        private TransactionInfo CreateTransaction(string name)
        {
            if (!_scriptStartTime.HasValue)
            {
                throw new InvalidOperationException(
                    $@"The script's start date/time was supposed to be encountered before transaction is started (line {
                        _lineIndex}).");
            }

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

            _harPages = new List<HarPage>();
            _harEntries = new List<HarEntry>();
            _internalIdToHarEntryMap = new Dictionary<long, HarEntry>();
            _urlToOpenRequestHarEntryMap = new Dictionary<string, HarEntry>(StringComparer.Ordinal);
            _socketIdToDataMap = new Dictionary<int, SocketData>();

            return result;
        }

        private TransactionInfo CreateImplicitTransactionInfo()
        {
            _implicitTransactionUniqueId++;
            var name = $"Implicit transaction {_implicitTransactionUniqueId}";
            return CreateTransaction(name);
        }

        private void EnsureTransactionInfo()
        {
            if (_transactionInfo == null)
            {
                _transactionInfo = CreateImplicitTransactionInfo();
            }
        }

        private void AddHarEntry(HarEntry harEntry, long internalId, string url)
        {
            #region Argument Check

            if (harEntry == null)
            {
                throw new ArgumentNullException(nameof(harEntry));
            }

            if (string.IsNullOrWhiteSpace(url))
            {
                throw new ArgumentException(
                    @"The value can be neither empty nor whitespace-only string nor null.",
                    nameof(url));
            }

            #endregion

            _harEntries.EnsureNotNull().Add(harEntry);
            _internalIdToHarEntryMap.EnsureNotNull().Add(internalId, harEntry);
            _urlToOpenRequestHarEntryMap.EnsureNotNull().Add(url, harEntry);
        }

        private void FetchRequestHeaders(
            long connectTimeInMilliseconds,
            IPEndPoint sourceEndpoint,
            IPEndPoint targetEndpoint)
        {
            FetchLine();
            var match = _line.MatchAgainst(ParsingHelper.RequestHeadersMarkerRegex);
            if (!match.Success)
            {
                throw new InvalidOperationException($"Request header was expected at line {_lineIndex}.");
            }

            var url = match.GetSucceededGroupValue(ParsingHelper.UrlGroupName);
            var size = match.GetSucceededGroupValue(ParsingHelper.SizeGroupName).ParseLong();
            var frameId = match.GetSucceededGroupValue(ParsingHelper.FrameIdGroupName).ParseNullableLong();
            var internalId = match.GetSucceededGroupValue(ParsingHelper.InternalIdGroupName).ParseLong();

            var startTime = GetTime(match);

            if (frameId.HasValue || _harPage == null)
            {
                _harPage = new HarPage
                {
                    Id = $"page_{++_pageUniqueId}",
                    Title = url,
                    StartedDateTime = startTime
                };

                _harPages.EnsureNotNull().Add(_harPage);
            }

            var harRequest = new HarRequest
            {
                HeadersSize = size,
                Url = url,
                BodySize = 0
            };

            var harEntryTimings = new HarEntryTimings
            {
                Blocked = HarConstants.NotApplicableTiming,
                Dns = HarConstants.NotApplicableTiming,
                Connect = connectTimeInMilliseconds,
                Ssl = HarConstants.NotApplicableTiming,
                Send = 0 //// Currently, it's technically impossible to determine the 'send' timing value
            };

            var harEntry = new HarEntry
            {
                PageRef = _harPage.Id,
                ConnectionId = sourceEndpoint.Port.ToString(CultureInfo.InvariantCulture),
                ServerIPAddress = targetEndpoint.Address.ToString(),
                StartedDateTime = startTime,
                Request = harRequest,
                Response = new HarResponse { BodySize = 0, Content = new HarContent() },
                Timings = harEntryTimings
            };

            AddHarEntry(harEntry, internalId, url);

            var multilineString = FetchMultipleLines();

            var requestLineString = multilineString.Lines.FirstOrDefault();
            var httpRequestLineMatch = requestLineString.MatchAgainst(ParsingHelper.HttpRequestLineRegex);
            if (!httpRequestLineMatch.Success)
            {
                throw new InvalidOperationException(
                    $"An HTTP Request-Line was expected at line {multilineString.LineIndexRange.Lower}.");
            }

            harRequest.Method = httpRequestLineMatch.GetSucceededGroupValue(ParsingHelper.HttpMethodGroupName);
            harRequest.HttpVersion =
                httpRequestLineMatch.GetSucceededGroupValue(ParsingHelper.HttpVersionGroupName);

            var harHeaders = ParseHttpHeaders(multilineString);
            harRequest.Headers = harHeaders;
        }

        private void ProcessResponseHeaders(Match match)
        {
            var size = match.GetSucceededGroupValue(ParsingHelper.SizeGroupName).ParseLong();
            var internalId = match.GetSucceededGroupValue(ParsingHelper.InternalIdGroupName).ParseLong();

            EnsureTransactionInfo();

            if (_harPage == null)
            {
                throw new InvalidOperationException(
                    $"Page could not be determined for the response headers at line {_lineIndex}.");
            }

            var harEntry = _internalIdToHarEntryMap.EnsureNotNull().GetValueOrDefault(internalId);
            if (harEntry == null)
            {
                throw new InvalidOperationException(
                    $"Cannot find an entry with Internal ID = {internalId} (line {_lineIndex}).");
            }

            var harResponse = harEntry.Response.EnsureNotNull();
            harResponse.HeadersSize = size;

            var timings = harEntry.Timings.EnsureNotNull();
            var time = GetTime(match);
            var waitTimeSpan = time - harEntry.StartedDateTime.EnsureNotNull();
            timings.Wait = (decimal)waitTimeSpan.TotalMilliseconds;

            var multilineString = FetchMultipleLines();

            var statusLineString = multilineString.Lines.FirstOrDefault();

            var statusLineMatch = statusLineString.MatchAgainst(ParsingHelper.HttpStatusLineRegex);
            if (!statusLineMatch.Success)
            {
                throw new InvalidOperationException(
                    $"An HTTP Status-Line was expected at line {multilineString.LineIndexRange.Lower}.");
            }

            var httpVersion = statusLineMatch.GetSucceededGroupValue(ParsingHelper.HttpVersionGroupName);
            var statusCode = statusLineMatch.GetSucceededGroupValue(ParsingHelper.HttpStatusCodeGroupName).ParseInt();
            var statusText = statusLineMatch.GetSucceededGroupValue(ParsingHelper.HttpReasonPhraseGroupName);

            harResponse.HttpVersion = httpVersion;
            harResponse.Status = statusCode;
            harResponse.StatusText = statusText;

            var harHeaders = ParseHttpHeaders(multilineString);
            harResponse.Headers = harHeaders;
        }

        private void ProcessResponseBody([NotNull] Match responseBodyMatch)
        {
            //// TODO [vmcl] Parse response body (content)

            var responseBodyType = ParsingHelper.GetResponseBodyType(responseBodyMatch);
            if (responseBodyType == ResponseBodyType.Decoded)
            {
                //// TODO [vmcl] Determine HarContent.SavedByCompression
                return;
            }

            var size = responseBodyMatch.GetSucceededGroupValue(ParsingHelper.SizeGroupName).ParseLong();
            var internalId = responseBodyMatch.GetSucceededGroupValue(ParsingHelper.InternalIdGroupName).ParseLong();

            var harEntry = _internalIdToHarEntryMap.EnsureNotNull().GetValueOrDefault(internalId);
            if (harEntry == null)
            {
                throw new InvalidOperationException(
                    $"Cannot find an entry with Internal ID = {internalId} (line {_lineIndex}).");
            }

            var harResponse = harEntry.Response.EnsureNotNull();
            harResponse.BodySize = harResponse.BodySize.GetValueOrDefault() + size;
        }

        private void ParseInternal()
        {
            _transactionInfos = new List<TransactionInfo>();

            _transactionInfo = null;
            _pageUniqueId = 0;
            _implicitTransactionUniqueId = 0;
            _harPages = null;
            _harEntries = null;
            _internalIdToHarEntryMap = null;
            _harPage = null;

            _line = null;
            _lineIndex = 0;
            _skipFetchOnce = false;
            _scriptStartTime = null;

            while (FetchLine())
            {
                var utcStartTimeMatch = _line.MatchAgainst(ParsingHelper.UtcStartTimeRegex);
                if (utcStartTimeMatch.Success)
                {
                    if (_scriptStartTime.HasValue)
                    {
                        throw new InvalidOperationException(@"The script's start date/time was already found.");
                    }

                    var year = utcStartTimeMatch.GetSucceededGroupValue(ParsingHelper.DateYearGroupName).ParseInt();
                    var month = utcStartTimeMatch.GetSucceededGroupValue(ParsingHelper.DateMonthGroupName).ParseInt();
                    var day = utcStartTimeMatch.GetSucceededGroupValue(ParsingHelper.DateDayGroupName).ParseInt();

                    var hour = utcStartTimeMatch.GetSucceededGroupValue(ParsingHelper.DateHourGroupName).ParseInt();
                    var minute =
                        utcStartTimeMatch.GetSucceededGroupValue(ParsingHelper.DateMinuteGroupName).ParseInt();
                    var second =
                        utcStartTimeMatch.GetSucceededGroupValue(ParsingHelper.DateSecondGroupName).ParseInt();

                    _scriptStartTime = new DateTimeOffset(year, month, day, hour, minute, second, TimeSpan.Zero);
                    continue;
                }

                var transactionEndMatch = _line.MatchAgainst(ParsingHelper.TransactionEndRegex);
                if (transactionEndMatch.Success)
                {
                    CommitTransactionIfPending();
                    continue;
                }

                var transactionStartMatch = _line.MatchAgainst(ParsingHelper.TransactionStartRegex);
                if (transactionStartMatch.Success)
                {
                    CommitTransactionIfPending();

                    var name = transactionStartMatch.GetSucceededGroupValue(ParsingHelper.NameGroupName);
                    _transactionInfo = CreateTransaction(name);
                    continue;
                }

                var connectedSocketMatch = _line.MatchAgainst(ParsingHelper.ConnectedSocketRegex);
                if (connectedSocketMatch.Success)
                {
                    var socketId =
                        connectedSocketMatch.GetSucceededGroupValue(ParsingHelper.SocketIdGroupName).ParseInt();

                    var sourceEndpoint = connectedSocketMatch
                        .GetSucceededGroupValue(ParsingHelper.SourceEndpointGroupName)
                        .ParseEndPoint();

                    var targetEndpoint = connectedSocketMatch
                        .GetSucceededGroupValue(ParsingHelper.TargetEndpointGroupName)
                        .ParseEndPoint();

                    var connectTimeInMilliseconds =
                        connectedSocketMatch.GetSucceededGroupValue(ParsingHelper.DurationGroupName).ParseLong();

                    var socketData = new SocketData
                    {
                        SourceEndpoint = sourceEndpoint,
                        TargetEndpoint = targetEndpoint
                    };

                    EnsureTransactionInfo();

                    _socketIdToDataMap.EnsureNotNull().Add(socketId, socketData);

                    FetchRequestHeaders(connectTimeInMilliseconds, sourceEndpoint, targetEndpoint);
                    continue;
                }

                var alreadyConnectedMatch = _line.MatchAgainst(ParsingHelper.AlreadyConnectedRegex);
                if (alreadyConnectedMatch.Success)
                {
                    var socketId =
                        alreadyConnectedMatch.GetSucceededGroupValue(ParsingHelper.SocketIdGroupName).ParseInt();

                    EnsureTransactionInfo();

                    var socketData = _socketIdToDataMap.EnsureNotNull().GetValueOrDefault(socketId);
                    if (socketData == null)
                    {
                        throw new InvalidOperationException(
                            $"Socket data for the already connected socket {socketId} was not found.");
                    }

                    FetchRequestHeaders(0, socketData.SourceEndpoint, socketData.TargetEndpoint);
                    continue;
                }

                var responseHeadersMarkerMatch = _line.MatchAgainst(ParsingHelper.ResponseHeadersMarkerRegex);
                if (responseHeadersMarkerMatch.Success)
                {
                    ProcessResponseHeaders(responseHeadersMarkerMatch);
                    continue;
                }

                var encodedResponseBodyReceivedMatch =
                    _line.MatchAgainst(ParsingHelper.EncodedResponseBodyReceivedRegex);
                if (encodedResponseBodyReceivedMatch.Success)
                {
                    ProcessResponseBody(encodedResponseBodyReceivedMatch);
                    continue;
                }

                var responseBodyMarkerRegexMatch = _line.MatchAgainst(ParsingHelper.ResponseBodyMarkerRegex);
                if (responseBodyMarkerRegexMatch.Success)
                {
                    ProcessResponseBody(responseBodyMarkerRegexMatch);
                    continue;
                }

                //// TODO [vmcl] Parse request body

                var requestDoneMatch = _line.MatchAgainst(ParsingHelper.RequestDoneRegex);
                if (requestDoneMatch.Success)
                {
                    var url = requestDoneMatch.GetSucceededGroupValue(ParsingHelper.UrlGroupName);

                    var harEntry = _urlToOpenRequestHarEntryMap.EnsureNotNull().GetValueOrDefault(url);
                    if (harEntry == null)
                    {
                        throw new InvalidOperationException(
                            $@"Cannot find a corresponding entry for the completed request ""{url}"" (line {_lineIndex
                                }).");
                    }

                    var doneTime = GetTime(requestDoneMatch);
                    var elapsed = doneTime - harEntry.StartedDateTime.EnsureNotNull();

                    var timings = harEntry.Timings.EnsureNotNull();
                    timings.Receive = (decimal)elapsed.TotalMilliseconds - timings.Wait.GetValueOrDefault();

                    continue;
                }

                Debug.WriteLine($"[{GetType().GetQualifiedName()}] Skipping line: {_line}");
            }
        }

        #endregion

        #region SocketData Class

        private sealed class SocketData
        {
            #region Public Properties

            public IPEndPoint SourceEndpoint
            {
                get;
                set;
            }

            public IPEndPoint TargetEndpoint
            {
                get;
                set;
            }

            #endregion
        }

        #endregion
    }
}