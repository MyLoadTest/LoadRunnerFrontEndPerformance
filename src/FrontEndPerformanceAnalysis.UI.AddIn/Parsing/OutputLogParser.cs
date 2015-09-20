﻿using System;
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
        private HarPage _harPage;
        private string _line;
        private int _lineIndex;
        private bool _skipFetchOnce;
        private List<TransactionInfo> _transactionInfos;
        private Dictionary<int, SocketData> _socketIdToDataMap;

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

        private static long? ParseNullableLong(string value)
        {
            return value.IsNullOrEmpty() ? default(long?) : ParseLong(value);
        }

        private static long ParseLong(string value)
        {
            return long.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
        }

        private static int ParseInt(string value)
        {
            return int.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
        }

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

            harLog.Pages = _harPages.EnsureNotNull().ToArray();
            harLog.Entries = _harEntries.EnsureNotNull().ToArray();

            _transactionInfos.Add(_transactionInfo);

            _transactionInfo = null;

            _harPages = null;
            _harEntries = null;
            _internalIdToHarEntryMap = null;
            _socketIdToDataMap = null;
        }

        private TransactionInfo CreateTransaction(string name)
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

            _harPages = new List<HarPage>();
            _harEntries = new List<HarEntry>();
            _internalIdToHarEntryMap = new Dictionary<long, HarEntry>();
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

        private void AddHarEntry(long internalId, HarEntry harEntry)
        {
            if (harEntry == null)
            {
                throw new ArgumentNullException(nameof(harEntry));
            }

            _harEntries.EnsureNotNull().Add(harEntry);
            _internalIdToHarEntryMap.EnsureNotNull().Add(internalId, harEntry);
        }

        private void FetchRequestHeaders(IPEndPoint sourceEndpoint, IPEndPoint targetEndpoint)
        {
            FetchLine();
            var match = _line.MatchAgainst(ParsingHelper.RequestHeadersMarkerRegex);
            if (!match.Success)
            {
                throw new InvalidOperationException($"Request header was expected at line {_lineIndex}.");
            }

            var url = match.GetSucceededGroupValue(ParsingHelper.UrlGroupName);
            var size = ParseLong(match.GetSucceededGroupValue(ParsingHelper.SizeGroupName));
            var frameId = ParseNullableLong(match.GetSucceededGroupValue(ParsingHelper.FrameIdGroupName));
            var internalId = ParseLong(match.GetSucceededGroupValue(ParsingHelper.InternalIdGroupName));

            if (frameId.HasValue || _harPage == null)
            {
                _harPage = new HarPage
                {
                    Id = $"page_{++_pageUniqueId}",
                    Title = url
                };

                _harPages.EnsureNotNull().Add(_harPage);
            }

            var harRequest = new HarRequest
            {
                HeadersSize = size,
                Url = url
            };

            var harEntry = new HarEntry
            {
                PageRef = _harPage.Id,
                ConnectionId = sourceEndpoint.Port.ToString(CultureInfo.InvariantCulture),
                ServerIPAddress = targetEndpoint.Address.ToString(),
                Request = harRequest,
                Response = new HarResponse()
            };

            AddHarEntry(internalId, harEntry);

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
            var size = ParseLong(match.GetSucceededGroupValue(ParsingHelper.SizeGroupName));
            var internalId = ParseLong(match.GetSucceededGroupValue(ParsingHelper.InternalIdGroupName));

            EnsureTransactionInfo();

            if (_harPage == null)
            {
                throw new InvalidOperationException(
                    $"Page could not be determined for the response headers at line {_lineIndex}.");
            }

            var harEntry = _internalIdToHarEntryMap.GetValueOrDefault(internalId);
            if (harEntry == null)
            {
                throw new InvalidOperationException(
                    $"Cannot find an entry with Internal ID = {internalId} (line {_lineIndex}).");
            }

            var harResponse = harEntry.Response.EnsureNotNull();
            harResponse.HeadersSize = size;

            var multilineString = FetchMultipleLines();

            var statusLineString = multilineString.Lines.FirstOrDefault();

            var statusLineMatch = statusLineString.MatchAgainst(ParsingHelper.HttpStatusLineRegex);
            if (!statusLineMatch.Success)
            {
                throw new InvalidOperationException(
                    $"An HTTP Status-Line was expected at line {multilineString.LineIndexRange.Lower}.");
            }

            var httpVersion = statusLineMatch.GetSucceededGroupValue(ParsingHelper.HttpVersionGroupName);
            var statusCode = ParseInt(statusLineMatch.GetSucceededGroupValue(ParsingHelper.HttpStatusCodeGroupName));
            var statusText = statusLineMatch.GetSucceededGroupValue(ParsingHelper.HttpReasonPhraseGroupName);

            harResponse.HttpVersion = httpVersion;
            harResponse.Status = statusCode;
            harResponse.StatusText = statusText;

            var harHeaders = ParseHttpHeaders(multilineString);
            harResponse.Headers = harHeaders;
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

            while (FetchLine())
            {
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
                        ParseInt(connectedSocketMatch.GetSucceededGroupValue(ParsingHelper.SocketIdGroupName));

                    var sourceEndpoint = connectedSocketMatch
                        .GetSucceededGroupValue(ParsingHelper.SourceEndpointGroupName)
                        .ParseEndPoint();

                    var targetEndpoint = connectedSocketMatch
                        .GetSucceededGroupValue(ParsingHelper.TargetEndpointGroupName)
                        .ParseEndPoint();

                    var socketData = new SocketData
                    {
                        SourceEndpoint = sourceEndpoint,
                        TargetEndpoint = targetEndpoint
                    };

                    EnsureTransactionInfo();

                    _socketIdToDataMap.EnsureNotNull().Add(socketId, socketData);

                    FetchRequestHeaders(sourceEndpoint, targetEndpoint);
                    continue;
                }

                var alreadyConnectedMatch = _line.MatchAgainst(ParsingHelper.AlreadyConnectedRegex);
                if (alreadyConnectedMatch.Success)
                {
                    var socketId =
                        ParseInt(alreadyConnectedMatch.GetSucceededGroupValue(ParsingHelper.SocketIdGroupName));

                    EnsureTransactionInfo();

                    var socketData = _socketIdToDataMap.EnsureNotNull().GetValueOrDefault(socketId);
                    if (socketData == null)
                    {
                        throw new InvalidOperationException(
                            $"Socket data for the already connected socket {socketId} was not found.");
                    }

                    FetchRequestHeaders(socketData.SourceEndpoint, socketData.TargetEndpoint);
                    continue;
                }

                var responseHeadersMarkerMatch = _line.MatchAgainst(ParsingHelper.ResponseHeadersMarkerRegex);
                if (responseHeadersMarkerMatch.Success)
                {
                    ProcessResponseHeaders(responseHeadersMarkerMatch);
                    continue;
                }

                //// TODO [vmcl] Parse request body
                //// TODO [vmcl] Parse response headers
                //// TODO [vmcl] Parse response body

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