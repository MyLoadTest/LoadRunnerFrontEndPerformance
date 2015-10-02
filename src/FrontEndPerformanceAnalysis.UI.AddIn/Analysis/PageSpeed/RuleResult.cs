using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;

namespace MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.Analysis.PageSpeed
{
    [DataContract]
    [DebuggerDisplay("{RuleName,nq} : {RuleImpact}{Experimental ? \" (experimental)\" : string.Empty,nq}")]
    internal sealed class RuleResult
    {
        #region Public Properties

        [DataMember(Name = "rule_name")]
        public string RuleName
        {
            get;
            set;
        }

        [DataMember(Name = "localized_rule_name")]
        public string LocalizedRuleName
        {
            get;
            set;
        }

        [DataMember(Name = "experimental")]
        public bool Experimental
        {
            get;
            set;
        }

        [DataMember(Name = "rule_impact")]
        public decimal RuleImpact
        {
            get;
            set;
        }

        [DataMember(Name = "summary_line")]
        public FormattedString SummaryLine
        {
            get;
            set;
        }

        [DataMember(Name = "url_blocks")]
        public UrlBlock[] UrlBlocks
        {
            get;
            set;
        }

        #endregion
    }
}