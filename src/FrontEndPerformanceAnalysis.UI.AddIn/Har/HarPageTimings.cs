using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;

namespace MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.Har
{
    [DataContract]
    [DebuggerDisplay("{GetType().Name,nq}. OnLoad = {OnLoad}, OnContentLoad = {OnContentLoad}")]
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