using System;
using System.Linq;
using System.Runtime.Serialization;

namespace MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.Har
{
    [DataContract]
    internal sealed class HarPageTimings
    {
        #region Public Properties

        [DataMember(Name = "onContentLoad")]
        public decimal? OnContentLoad
        {
            get;
            set;
        }

        [DataMember(Name = "onLoad")]
        public decimal? OnLoad
        {
            get;
            set;
        }

        #endregion
    }
}