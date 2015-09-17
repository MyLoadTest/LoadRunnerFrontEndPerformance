using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.Har;
using Omnifactotum;

namespace MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.Parsing
{
    internal sealed class VuGenOutputLogParser : IDisposable
    {
        #region Constants and Fields

        private StreamReader _reader;
        private bool _isDisposed;
        private bool _isParseExecuted;

        #endregion

        #region Constructors

        public VuGenOutputLogParser(string logPath)
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

            return ParseInternal();
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

        private void EnsureNotDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(GetType().GetFullName());
            }
        }

        private TransactionInfo[] ParseInternal()
        {
            var transactionInfos = new List<TransactionInfo>();

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

                    rawLine = _reader.ReadLine();
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
                var transactionEndMatch = line.MatchAgainst(ParsingHelper.TransactionEndRegex);
                if (transactionEndMatch.Success)
                {
                    commitTransaction();
                    continue;
                }

                var transactionStartMatch = line.MatchAgainst(ParsingHelper.TransactionStartRegex);
                if (transactionStartMatch.Success)
                {
                    commitTransaction();

                    var name = transactionStartMatch.GetSucceededGroupValue(ParsingHelper.NameGroupName);
                    transactionInfo = createTransactionInfo(name);
                    continue;
                }

                var connectedSocketMatch = line.MatchAgainst(ParsingHelper.ConnectedSocketRegex);
                if (connectedSocketMatch.Success)
                {
                    var sourceEndpointMatch = connectedSocketMatch
                        .GetSucceededGroupValue(ParsingHelper.SourceEndpointGroupName)
                        .MatchAgainst(ParsingHelper.IPEndPointRegex);

                    var targetEndpointMatch = connectedSocketMatch
                        .GetSucceededGroupValue(ParsingHelper.TargetEndpointGroupName)
                        .MatchAgainst(ParsingHelper.IPEndPointRegex);

                    Debug.WriteLine(sourceEndpointMatch.Value);
                    Debug.WriteLine(targetEndpointMatch.Value);

                    fetchLine();
                    var requestHeadersMarkerMatch = line.MatchAgainst(ParsingHelper.RequestHeadersMarkerRegex);
                    if (!requestHeadersMarkerMatch.Success)
                    {
                        throw new InvalidOperationException($"Request header was expected at line {lineIndex}.");
                    }

                    var url = requestHeadersMarkerMatch.GetSucceededGroupValue(ParsingHelper.UrlGroupName);
                    var size = ParseLong(
                        requestHeadersMarkerMatch.GetSucceededGroupValue(ParsingHelper.SizeGroupName));

                    var frameId =
                        ParseNullableLong(
                            requestHeadersMarkerMatch.GetSucceededGroupValue(ParsingHelper.FrameIdGroupName));

                    var internalId =
                        ParseLong(requestHeadersMarkerMatch.GetSucceededGroupValue(ParsingHelper.InternalIdGroupName));

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
                    var httpRequestLineMatch = line.MatchAgainst(ParsingHelper.HttpRequestLineRegex);
                    if (!httpRequestLineMatch.Success)
                    {
                        //// TODO [vmcl] VuGen output may contain Request-Line carried over to a few lines

                        throw new InvalidOperationException(
                            $"An HTTP Request-Line was expected at line {lineIndex}.");
                    }

                    harRequest.Method = httpRequestLineMatch.GetSucceededGroupValue(ParsingHelper.HttpMethodGroupName);
                    harRequest.HttpVersion =
                        httpRequestLineMatch.GetSucceededGroupValue(ParsingHelper.HttpVersionGroupName);

                    var harHeaders = new List<HarHeader>();

                    //// ReSharper disable LoopVariableIsNeverChangedInsideLoop - False positive
                    while (fetchLine())
                    {
                        //// ReSharper restore LoopVariableIsNeverChangedInsideLoop

                        var headerMatch = line.MatchAgainst(ParsingHelper.HttpHeaderRegex);
                        if (!headerMatch.Success)
                        {
                            break;
                        }

                        var name = headerMatch.GetSucceededGroupValue(ParsingHelper.NameGroupName);
                        var value = headerMatch.GetSucceededGroupValue(ParsingHelper.ValueGroupName);
                        var harHeader = new HarHeader { Name = name, Value = value };
                        harHeaders.Add(harHeader);
                    }

                    harRequest.Headers = harHeaders.ToArray();

                    if (!line.MatchAgainst(ParsingHelper.HttpHeaderEndedRegex).Success)
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

            return transactionInfos.ToArray();
        }

        #endregion
    }
}