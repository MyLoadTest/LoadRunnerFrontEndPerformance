using System;
using System.Text.RegularExpressions;
using Omnifactotum.Annotations;

namespace MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.Parsing
{
    internal static class ParsingHelper
    {
        #region Constants and Fields

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
        public const string HttpMethodGroupName = "HttpMethod";
        public const string HttpVersionGroupName = "HttpVersion";

        private const string DecimalNumberPattern = @"\d+\.?\d*|\d*\.?\d+";
        private const string IPAddressPattern = @"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}"; // IPv4 only so far
        private const string PortPattern = @"\d+";

        private const string FileAndPositionPrefixPattern = @"^[^\(]+\(\d+\):";

        private const string HttpRequestLineElementPattern = @"\S+";
        private const string HttpRequestLineVersionPattern = @"\d+\.\d+";

        private static readonly string IPAddressAndPortPattern = $@"{IPAddressPattern}\:{PortPattern}";

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
            $@"{FileAndPositionPrefixPattern} t=(?<{TimestampGroupName}>\d+)ms\: Connected socket \[\d+\] from (?<{
                SourceEndpointGroupName}>{IPAddressAndPortPattern}) to (?<{TargetEndpointGroupName}>{
                IPAddressAndPortPattern}) in (?<{DurationGroupName}>\d+) ms\s+\[MsgId\: MMSG\-\d+\]$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);

        public static readonly Regex RequestHeadersMarkerRegex = new Regex(
            $@"{FileAndPositionPrefixPattern} t=(?<{TimestampGroupName}>\d+)ms\: (?<{SizeGroupName
                }>\d+)-byte request headers for ""(?<{UrlGroupName}>[^""]+)"" \(RelFrameId\=(?<{FrameIdGroupName
                }>\d*)\, Internal ID\=(?<{InternalIdGroupName}>\d+)\)$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);

        public static readonly Regex HttpRequestLineRegex = new Regex(
            $@"{FileAndPositionPrefixPattern}\s{{5}}(?<{HttpMethodGroupName}>{HttpRequestLineElementPattern})\s+(?<{
                UrlGroupName}>{HttpRequestLineElementPattern})\s+HTTP/(?<{HttpVersionGroupName}>{
                HttpRequestLineVersionPattern})\s*$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);

        public static readonly Regex HttpHeaderRegex = new Regex(
            $@"{FileAndPositionPrefixPattern}\s{{5}}(?<{NameGroupName}>[^:]+)\s*\:\s*(?<{ValueGroupName}>.*?)\r\n$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);

        public static readonly Regex HttpHeaderEndedRegex = new Regex(
            $@"{FileAndPositionPrefixPattern}\s{{5}}\r\n$",
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

        #endregion
    }
}