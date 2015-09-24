using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using Omnifactotum.Annotations;

namespace MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.Parsing
{
    internal static class ParsingHelper
    {
        #region Constants and Fields: Group Names

        public const string DateYearGroupName = "DateYear";
        public const string DateMonthGroupName = "DateMonth";
        public const string DateDayGroupName = "DateDay";
        public const string DateHourGroupName = "DateHour";
        public const string DateMinuteGroupName = "DateMinute";
        public const string DateSecondGroupName = "DateSecond";

        public const string NameGroupName = "Name";
        public const string ValueGroupName = "Value";

        public const string StatusGroupName = "Status";
        public const string DurationGroupName = "Duration";
        public const string WastedTimeGroupName = "WastedTime";
        public const string TimestampGroupName = "Timestamp";
        public const string IPAddressGroupName = "IPAddress";
        public const string PortGroupName = "Port";
        public const string SourceEndpointGroupName = "SourceEndpoint";
        public const string TargetEndpointGroupName = "TargetEndpoint";
        public const string SizeGroupName = "Size";
        public const string FrameIdGroupName = "FrameId";
        public const string InternalIdGroupName = "InternalId";
        public const string UrlGroupName = "Url";
        public const string SocketIdGroupName = "SocketId";

        public const string HttpMethodGroupName = "HttpMethod";
        public const string HttpVersionGroupName = "HttpVersion";
        public const string HttpStatusCodeGroupName = "HttpStatusCode";
        public const string HttpReasonPhraseGroupName = "HttpReasonPhrase";

        public const string ResponseBodyTypeGroupName = "ResponseBodyType";

        #endregion

        #region Constants and Fields: General

        private const RegexOptions DefaultRegexOptions =
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture;

        #endregion

        #region Constants and Fields: Patterns

        private const string DateYearPattern = @"\d{4}";
        private const string DateMonthPattern = @"\d{2}";
        private const string DateDayPattern = @"\d{2}";
        private const string DateHourPattern = @"\d{2}";
        private const string DateMinutePattern = @"\d{2}";
        private const string DateSecondPattern = @"\d{2}";

        private const string DecimalNumberPattern = @"\d+\.?\d*|\d*\.?\d+";
        private const string SocketIdPattern = @"\d+";
        private const string IPAddressPattern = @"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}"; // IPv4 only so far
        private const string PortPattern = @"\d+";

        private const string FileAndPositionPrefixPattern = @"^[^\(]+\(\d+\):";

        private const string HttpVersionPattern = @"\d+\.\d+";

        private const string HttpRequestLineElementPattern = @"\S+";
        private const string HttpStatusCodePattern = @"\d{3}";
        private const string HttpReasonPhrasePattern = @".*";

        private static readonly string IPAddressAndPortPattern = $@"{IPAddressPattern}\:{PortPattern}";

        private const string ResponseBodyTypePatternDefault = "";
        private const string ResponseBodyTypePatternChunked = "chunked ";
        private const string ResponseBodyTypePatternEncoded = "ENCODED ";
        private const string ResponseBodyTypePatternDecoded = "DECODED ";

        #endregion

        #region Constants and Fields: Regular Expressions

        public static readonly Regex UtcStartTimeRegex = new Regex(
            $@"UTC \(GMT\) start date\/time\s*:\s*(?<{DateYearGroupName}>{DateYearPattern})\-(?<{DateMonthGroupName}>{
                DateMonthPattern})\-(?<{DateDayGroupName}>{DateDayPattern}) (?<{DateHourGroupName}>{DateHourPattern
                })\:(?<{DateMinuteGroupName}>{DateMinutePattern})\:(?<{DateSecondGroupName}>{DateSecondPattern
                })\s+\[MsgId\: MMSG\-\d+\]$",
            DefaultRegexOptions);

        public static readonly Regex IPEndPointRegex = new Regex(
            $@"(?<{IPAddressGroupName}>{IPAddressPattern})\:(?<{PortGroupName}>{PortPattern})",
            DefaultRegexOptions);

        public static readonly Regex TransactionStartRegex = new Regex(
            $@"{FileAndPositionPrefixPattern} Notify\: Transaction \""(?<{NameGroupName}>[^""]+)\"" started\.$",
            DefaultRegexOptions);

        public static readonly Regex TransactionEndRegex = new Regex(
            $@"{FileAndPositionPrefixPattern} Notify\: Transaction \""(?<{NameGroupName
                }>[^""]+)\"" ended with a \""(?<{StatusGroupName
                }>[^""]+)\"" status \(Duration: (?<{DurationGroupName}>{DecimalNumberPattern}) Wasted Time: (?<{
                WastedTimeGroupName}>{DecimalNumberPattern})\)\.$",
            DefaultRegexOptions);

        public static readonly Regex ConnectedSocketRegex = new Regex(
            $@"{FileAndPositionPrefixPattern} t=(?<{TimestampGroupName}>\d+)ms\: Connected socket \[(?<{
                SocketIdGroupName}>{SocketIdPattern})\] from (?<{SourceEndpointGroupName}>{IPAddressAndPortPattern
                }) to (?<{TargetEndpointGroupName}>{IPAddressAndPortPattern}) in (?<{DurationGroupName
                }>\d+) ms\s+\[MsgId\: MMSG\-\d+\]$",
            DefaultRegexOptions);

        public static readonly Regex AlreadyConnectedRegex = new Regex(
            $@"{FileAndPositionPrefixPattern} t=(?<{TimestampGroupName}>\d+)ms\: Already connected \[(?<{
                SocketIdGroupName}>{SocketIdPattern})\] to \S*?\s+\[MsgId\: MMSG\-\d+\]$",
            DefaultRegexOptions);

        public static readonly Regex RequestHeadersMarkerRegex = new Regex(
            $@"{FileAndPositionPrefixPattern} t=(?<{TimestampGroupName}>\d+)ms\: (?<{SizeGroupName
                }>\d+)\-byte request headers for ""(?<{UrlGroupName}>[^""]+)"" \(RelFrameId\=(?<{FrameIdGroupName
                }>\d*)\, Internal ID\=(?<{InternalIdGroupName}>\d+)\)$",
            DefaultRegexOptions);

        public static readonly Regex MultiLineRegex = new Regex(
            $@"{FileAndPositionPrefixPattern}\s{{5}}(?<{ValueGroupName}>.*)$",
            DefaultRegexOptions);

        public static readonly Regex HttpRequestLineRegex = new Regex(
            $@"^(?<{HttpMethodGroupName}>{HttpRequestLineElementPattern})\s+(?<{
                UrlGroupName}>{HttpRequestLineElementPattern})\s+HTTP/(?<{HttpVersionGroupName}>{
                HttpVersionPattern})\s*$",
            DefaultRegexOptions);

        public static readonly Regex HttpHeaderRegex = new Regex(
            $@"^(?<{NameGroupName}>[^:]+)\s*\:\s*(?<{ValueGroupName}>.*?)$",
            DefaultRegexOptions);

        public static readonly Regex ResponseHeadersMarkerRegex = new Regex(
            $@"{FileAndPositionPrefixPattern} t=(?<{TimestampGroupName}>\d+)ms\: (?<{SizeGroupName
                }>\d+)\-byte response headers for ""(?<{UrlGroupName}>[^""]+)"" \(RelFrameId\=(?<{FrameIdGroupName
                }>\d*)\, Internal ID\=(?<{InternalIdGroupName}>\d+)\)$",
            DefaultRegexOptions);

        public static readonly Regex HttpStatusLineRegex = new Regex(
            $@"^\s*HTTP/(?<{HttpVersionGroupName}>{HttpVersionPattern})\s+(?<{HttpStatusCodeGroupName}>{
                HttpStatusCodePattern})\s+(?<{HttpReasonPhraseGroupName}>{HttpReasonPhrasePattern})$",
            DefaultRegexOptions);

        public static readonly Regex EncodedResponseBodyReceivedRegex = new Regex(
            $@"{FileAndPositionPrefixPattern} t=(?<{TimestampGroupName}>\d+)ms\: (?<{SizeGroupName}>\d+)\-byte (?<{
                ResponseBodyTypeGroupName}>{ResponseBodyTypePatternEncoded})response body received for ""(?<{
                UrlGroupName}>[^""]+)"" \(RelFrameId\=(?<{FrameIdGroupName}>\d*)\, Internal ID\=(?<{
                InternalIdGroupName}>\d+)\)$",
            DefaultRegexOptions);

        public static readonly Regex ResponseBodyMarkerRegex = new Regex(
            $@"{FileAndPositionPrefixPattern} t=(?<{TimestampGroupName}>\d+)ms\: (?<{SizeGroupName
                }>\d+)\-byte (?<{ResponseBodyTypeGroupName}>{ResponseBodyTypePatternEncoded}|{
                ResponseBodyTypePatternDecoded}|{ResponseBodyTypePatternChunked}|{ResponseBodyTypePatternDefault
                })response body for ""(?<{UrlGroupName}>[^""]+)"" \(RelFrameId\=(?<{FrameIdGroupName
                }>\d*)\, Internal ID\=(?<{InternalIdGroupName}>\d+)\)$",
            DefaultRegexOptions);

        public static readonly Regex RequestDoneRegex = new Regex(
            $@"{FileAndPositionPrefixPattern} t=(?<{TimestampGroupName}>\d+)ms\: Request done ""(?<{UrlGroupName
                }>[^""]+)""\s+\[MsgId\: MMSG\-\d+\]$",
            DefaultRegexOptions);

        #endregion

        #region Constants and Fields

        private static readonly Dictionary<ResponseBodyType, string> ResponseBodyTypeToPatternMap =
            new Dictionary<ResponseBodyType, string>
            {
                { ResponseBodyType.Default, ResponseBodyTypePatternDefault },
                { ResponseBodyType.Chunked, ResponseBodyTypePatternChunked },
                { ResponseBodyType.Encoded, ResponseBodyTypePatternEncoded },
                { ResponseBodyType.Decoded, ResponseBodyTypePatternDecoded }
            };

        private static readonly Dictionary<string, ResponseBodyType> PatternToResponseBodyTypeMap =
            ResponseBodyTypeToPatternMap.ReverseDictionary(StringComparer.OrdinalIgnoreCase);

        #endregion

        #region Public Methods

        [NotNull]
        public static Group GetSucceeded([NotNull] this GroupCollection groupCollection, [NotNull] string groupName)
        {
            #region Argument Check

            if (groupCollection == null)
            {
                throw new ArgumentNullException(nameof(groupCollection));
            }

            if (groupName == null)
            {
                throw new ArgumentNullException(nameof(groupName));
            }

            #endregion

            var group = groupCollection[groupName].EnsureNotNull();
            if (!@group.Success)
            {
                throw new InvalidOperationException($@"The group '{groupName}' was supposed to succeed.");
            }

            return @group;
        }

        [NotNull]
        public static Group GetSucceededGroup([NotNull] this Match match, [NotNull] string groupName)
        {
            #region Argument Check

            if (match == null)
            {
                throw new ArgumentNullException(nameof(match));
            }

            if (!match.Success)
            {
                throw new ArgumentException(@"The specified match was supposed to succeed.", nameof(match));
            }

            #endregion

            return match.Groups.GetSucceeded(groupName);
        }

        [NotNull]
        public static string GetSucceededGroupValue([NotNull] this Match match, [NotNull] string groupName)
        {
            return match.GetSucceededGroup(groupName).Value.EnsureNotNull();
        }

        [NotNull]
        public static Match MatchAgainst([CanBeNull] this string input, [NotNull] Regex regex)
        {
            #region Argument Check

            if (regex == null)
            {
                throw new ArgumentNullException(nameof(regex));
            }

            #endregion

            return input == null ? Match.Empty : regex.Match(input).EnsureNotNull();
        }

        [NotNull]
        public static IPEndPoint ParseEndPoint([NotNull] this string input)
        {
            #region Argument Check

            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            #endregion

            var match = input.MatchAgainst(IPEndPointRegex);
            if (!match.Success)
            {
                throw new ArgumentException(
                    $"The input string \"{input}\" does not represent a valid IP endpoint.",
                    nameof(input));
            }

            var ipAddressString = match.GetSucceededGroupValue(IPAddressGroupName);
            var portString = match.GetSucceededGroupValue(PortGroupName);

            var ipAddress = IPAddress.Parse(ipAddressString);
            var port = int.Parse(portString, NumberStyles.Integer, CultureInfo.InvariantCulture);

            var result = new IPEndPoint(ipAddress, port);
            return result;
        }

        [NotNull]
        public static string UnescapeLogString([NotNull] this string value)
        {
            #region Argument Check

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            #endregion

            return Regex.Unescape(value); // Seems to be a good conversion method at the moment
        }

        public static long? ParseNullableLong(this string value)
        {
            return value.IsNullOrEmpty() ? default(long?) : ParseLong(value);
        }

        public static long ParseLong([NotNull] this string value)
        {
            return long.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
        }

        public static int ParseInt([NotNull] this string value)
        {
            return int.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
        }

        public static ResponseBodyType GetResponseBodyType([NotNull] Match match)
        {
            #region Argument Check

            if (match == null)
            {
                throw new ArgumentNullException(nameof(match));
            }

            #endregion

            var responseBodyTypeString = match.GetSucceededGroupValue(ResponseBodyTypeGroupName);
            return PatternToResponseBodyTypeMap[responseBodyTypeString];
        }

        public static TimeSpan GetTimeOffset([NotNull] Match match)
        {
            #region Argument Check

            if (match == null)
            {
                throw new ArgumentNullException("match");
            }

            #endregion

            var result = TimeSpan.FromMilliseconds(match.GetSucceededGroupValue(TimestampGroupName).ParseLong());
            return result;
        }

        #endregion
    }
}