using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.Properties;
using Omnifactotum.Annotations;

namespace MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn
{
    internal static class LocalHelper
    {
        #region Constants and Fields

        private static readonly HashSet<char> InvalidPathChars = Path.GetInvalidPathChars().ToHashSet();

        #endregion

        #region Public Methods

        public static string GetTranslation([NotNull] this Enum value)
        {
            #region Argument Check

            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            #endregion

            var name = $"Enum_{value.GetType().GetQualifiedName()}_{value.GetName()}";

            var translation = Resources.ResourceManager.GetString(name);

            if (translation.IsNullOrWhiteSpace())
            {
                translation = value.GetName();
            }

            return translation;
        }

        public static bool PathHasInvalidChars([NotNull] string path)
        {
            #region Argument Check

            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException(@"The value can be neither empty string nor null.", "path");
            }

            #endregion

            var chars = new HashSet<char>(path);
            chars.IntersectWith(InvalidPathChars);
            return chars.Count != 0;
        }

        public static void KillNoThrow([NotNull] this Process process)
        {
            #region Argument Check

            if (process == null)
            {
                throw new ArgumentNullException("process");
            }

            #endregion

            try
            {
                process.Kill();
            }
            catch (Exception)
            {
                // Nothing to do
            }
        }

        public static string ToUtcIso8601String(this DateTimeOffset value)
        {
            return value.UtcDateTime.ToString("O");
        }

        public static DateTimeOffset? ParseIso8601DateTime([CanBeNull] this string value)
        {
            if (value.IsNullOrWhiteSpace())
            {
                return null;
            }

            return DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        }

        public static Dictionary<TValue, TKey> ReverseDictionary<TKey, TValue>(
            [NotNull] this Dictionary<TKey, TValue> dictionary,
            [CanBeNull] IEqualityComparer<TValue> equalityComparer)
        {
            #region Argument Check

            if (dictionary == null)
            {
                throw new ArgumentNullException(nameof(dictionary));
            }

            #endregion

            return dictionary.ToDictionary(
                pair => pair.Value,
                pair => pair.Key,
                equalityComparer ?? EqualityComparer<TValue>.Default);
        }

        public static Dictionary<TValue, TKey> ReverseDictionary<TKey, TValue>(
            [NotNull] this Dictionary<TKey, TValue> dictionary)
        {
            return dictionary.ReverseDictionary(null);
        }

        #endregion
    }
}