using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using Omnifactotum.Annotations;

namespace MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.Analysis.PageSpeed
{
    [DataContract]
    [DebuggerDisplay("Score = {Score}, Version = {Version}, Locale = {Locale}")]
    internal sealed class PageSpeedOutput: SpecificAnalysisResult
    {
        #region Constants and Fields

        private static readonly DataContractJsonSerializer Serializer =
            new DataContractJsonSerializer(typeof(PageSpeedOutput));

        #endregion

        #region Public Properties

        [DataMember(Name = "locale")]
        public string Locale
        {
            get;
            set;
        }

        [DataMember(Name = "rule_results")]
        public RuleResult[] RuleResults
        {
            get;
            set;
        }

        [DataMember(Name = "score")]
        public decimal Score
        {
            get;
            set;
        }

        [DataMember(Name = "version")]
        public VersionInfo Version
        {
            get;
            set;
        }

        #endregion

        #region Public Methods

        public static PageSpeedOutput Deserialize([NotNull] Stream stream)
        {
            #region Argument Check

            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            #endregion

            return (PageSpeedOutput)Serializer.ReadObject(stream).EnsureNotNull();
        }

        public static PageSpeedOutput DeserializeFromFile([NotNull] string filePath)
        {
            #region Argument Check

            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException(
                    @"The value can be neither empty nor whitespace-only string nor null.",
                    "filePath");
            }

            #endregion

            using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return Deserialize(stream);
            }
        }

        #endregion
    }
}