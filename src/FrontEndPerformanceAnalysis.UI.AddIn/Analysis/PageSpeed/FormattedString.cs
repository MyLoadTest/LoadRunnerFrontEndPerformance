using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;

namespace MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.Analysis.PageSpeed
{
    [DataContract]
    [DebuggerDisplay("{GetType().Name,nq}. Format = {Format}")]
    internal sealed class FormattedString
    {
        #region Constants and Fields

        private const string BeginHyperlinkPrefix = "BEGIN_";
        private const string EndHyperlinkPrefix = "END_";
        private const string HyperlinkArgumentType = "HYPERLINK";

        private const string ArgumentGroupName = "arg";

        private static readonly Regex ArgumentRegex = new Regex(
            $@"\{{\{{(?<{ArgumentGroupName}>[^{{}}]+)\}}\}}",
            RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.Singleline);

        private static readonly Regex BeginHyperlinkRegex = new Regex(
            $@"{BeginHyperlinkPrefix}(?<{ArgumentGroupName}>.*)",
            RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.Singleline);

        private static readonly Regex EndHyperlinkRegex = new Regex(
            $@"{EndHyperlinkPrefix}(?<{ArgumentGroupName}>.*)",
            RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.Singleline);

        #endregion

        #region Public Properties

        [DataMember(Name = "format")]
        public string Format
        {
            get;
            set;
        }

        [DataMember(Name = "args")]
        public FormattedStringArg[] Args
        {
            get;
            set;
        }

        #endregion

        #region Public Methods

        public override string ToString()
        {
            var format = Format;
            if (format.IsNullOrWhiteSpace())
            {
                return string.Empty;
            }

            var matches = ArgumentRegex.Matches(format).Cast<Match>().ToArray();
            if (matches.Length == 0)
            {
                return format;
            }

            var argumentMap = Args
                .AvoidNull()
                .Where(obj => obj?.PlaceholderKey != null)
                .ToDictionary(obj => obj.PlaceholderKey);

            var resultBuilder = new StringBuilder();
            var index = 0;
            foreach (var match in matches)
            {
                var plainTextLength = match.Index - index;
                if (plainTextLength > 0)
                {
                    var plainSubstring = format.Substring(index, plainTextLength);
                    resultBuilder.Append(plainSubstring);
                }

                index = match.Index + match.Length;

                var key = match.GetSucceededGroupValue(ArgumentGroupName);
                var argument = argumentMap.GetValueOrDefault(key);
                if (argument == null)
                {
                    var beginHyperlinkMatch = key.MatchAgainst(BeginHyperlinkRegex);
                    if (beginHyperlinkMatch.Success)
                    {
                        var innerKey = beginHyperlinkMatch.GetSucceededGroupValue(ArgumentGroupName);
                        var innerArgument = argumentMap.GetValueOrDefault(innerKey);
                        if (innerArgument != null && innerArgument.Type == HyperlinkArgumentType)
                        {
                            // For now, just skipping start of URL
                            continue;
                        }
                    }
                    else
                    {
                        var endHyperlinkMatch = key.MatchAgainst(EndHyperlinkRegex);
                        if (endHyperlinkMatch.Success)
                        {
                            var innerKey = endHyperlinkMatch.GetSucceededGroupValue(ArgumentGroupName);
                            var innerArgument = argumentMap.GetValueOrDefault(innerKey);
                            if (innerArgument != null && innerArgument.Type == HyperlinkArgumentType)
                            {
                                // For now, just representing URL as a plain text
                                resultBuilder.Append($@" ({innerArgument.LocalizedValue})");
                                continue;
                            }
                        }
                    }

                    resultBuilder.Append($@"{{{{{key}:Warning:ArgumentNotFound!}}}}");
                    continue;
                }

                resultBuilder.Append(argument.LocalizedValue);
            }

            var remainingPlainTextLength = format.Length - index;
            if (remainingPlainTextLength > 0)
            {
                var plainSubstring = format.Substring(index, remainingPlainTextLength);
                resultBuilder.Append(plainSubstring);
            }

            return resultBuilder.ToString();
        }

        #endregion
    }
}