using System;
using System.Linq;
using System.Runtime.Serialization;

namespace MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.ObjectMappings
{
    [DataContract]
    internal sealed class RuleInfo
    {
        #region Public Properties

        [DataMember(Name = "rule_name")]
        public string RuleName
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

        [DataMember(Name = "results")]
        public dynamic Results
        {
            get;
            set;
        }

        #endregion
    }
}