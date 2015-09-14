using System;
using System.Text.RegularExpressions;
using Omnifactotum.Annotations;

namespace MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.Parsing
{
    internal static class RegexHelper
    {
        #region Public Methods

        [NotNull]
        public static Group GetSucceeded(
            [NotNull] this GroupCollection groupCollection,
            [NotNull] string groupName)
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
            if (!group.Success)
            {
                throw new InvalidOperationException($"The group '{groupName}' was supposed to succeed.");
            }

            return group;
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