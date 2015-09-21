﻿using System;
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

        #endregion

        #region Constants and Fields: Regular Expressions

        public static readonly Regex UtcStartTimeRegex = new Regex(
            $@"UTC \(GMT\) start date\/time\s*:\s*(?<{DateYearGroupName}>{DateYearPattern})\-(?<{DateMonthGroupName}>{
                DateMonthPattern})\-(?<{DateDayGroupName}>{DateDayPattern}) (?<{DateHourGroupName}>{DateHourPattern
                })\:(?<{DateMinuteGroupName}>{DateMinutePattern})\:(?<{DateSecondGroupName}>{DateSecondPattern
                })\s+\[MsgId\: MMSG\-\d+\]$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);

        public static readonly Regex IPEndPointRegex = new Regex(
            $@"(?<{IPAddressGroupName}>{IPAddressPattern})\:(?<{PortGroupName}>{PortPattern})",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);

        public static readonly Regex TransactionStartRegex = new Regex(
            $@"{FileAndPositionPrefixPattern} Notify\: Transaction \""(?<{NameGroupName}>[^""]+)\"" started\.$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);

        public static readonly Regex TransactionEndRegex = new Regex(
            $@"{FileAndPositionPrefixPattern} Notify\: Transaction \""(?<{NameGroupName
                }>[^""]+)\"" ended with a \""(?<{StatusGroupName
                }>[^""]+)\"" status \(Duration: (?<{DurationGroupName}>{DecimalNumberPattern}) Wasted Time: (?<{
                WastedTimeGroupName}>{DecimalNumberPattern})\)\.$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);

        public static readonly Regex ConnectedSocketRegex = new Regex(
            $@"{FileAndPositionPrefixPattern} t=(?<{TimestampGroupName}>\d+)ms\: Connected socket \[(?<{
                SocketIdGroupName}>{SocketIdPattern})\] from (?<{SourceEndpointGroupName}>{IPAddressAndPortPattern
                }) to (?<{TargetEndpointGroupName}>{IPAddressAndPortPattern}) in (?<{DurationGroupName
                }>\d+) ms\s+\[MsgId\: MMSG\-\d+\]$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);

        public static readonly Regex AlreadyConnectedRegex = new Regex(
            $@"{FileAndPositionPrefixPattern} t=(?<{TimestampGroupName}>\d+)ms\: Already connected \[(?<{
                SocketIdGroupName}>{SocketIdPattern})\] to \S*?\s+\[MsgId\: MMSG\-\d+\]$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);

        public static readonly Regex RequestHeadersMarkerRegex = new Regex(
            $@"{FileAndPositionPrefixPattern} t=(?<{TimestampGroupName}>\d+)ms\: (?<{SizeGroupName
                }>\d+)-byte request headers for ""(?<{UrlGroupName}>[^""]+)"" \(RelFrameId\=(?<{FrameIdGroupName
                }>\d*)\, Internal ID\=(?<{InternalIdGroupName}>\d+)\)$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);

        public static readonly Regex MultiLineRegex = new Regex(
            $@"{FileAndPositionPrefixPattern}\s{{5}}(?<{ValueGroupName}>.*)$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);

        public static readonly Regex HttpRequestLineRegex = new Regex(
            $@"^(?<{HttpMethodGroupName}>{HttpRequestLineElementPattern})\s+(?<{
                UrlGroupName}>{HttpRequestLineElementPattern})\s+HTTP/(?<{HttpVersionGroupName}>{
                HttpVersionPattern})\s*$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);

        public static readonly Regex HttpHeaderRegex = new Regex(
            $@"^(?<{NameGroupName}>[^:]+)\s*\:\s*(?<{ValueGroupName}>.*?)$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);

        public static readonly Regex ResponseHeadersMarkerRegex = new Regex(
            $@"{FileAndPositionPrefixPattern} t=(?<{TimestampGroupName}>\d+)ms\: (?<{SizeGroupName
                }>\d+)-byte response headers for ""(?<{UrlGroupName}>[^""]+)"" \(RelFrameId\=(?<{FrameIdGroupName
                }>\d*)\, Internal ID\=(?<{InternalIdGroupName}>\d+)\)$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);

        public static readonly Regex HttpStatusLineRegex = new Regex(
            $@"^\s*HTTP/(?<{HttpVersionGroupName}>{HttpVersionPattern})\s+(?<{HttpStatusCodeGroupName}>{
                HttpStatusCodePattern})\s+(?<{HttpReasonPhraseGroupName}>{HttpReasonPhrasePattern})$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);

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
                throw new InvalidOperationException($"The group '{groupName}' was supposed to succeed.");
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
                throw new ArgumentNullException("value");
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

        #endregion
    }
}