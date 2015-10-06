using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;

namespace MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.Analysis.PageSpeed
{
    [DataContract]
    [DebuggerDisplay("[{GetType().Name,nq}] Result = {Result}")]
    internal sealed class UrlData
    {
        #region Public Properties

        [DataMember(Name = "result")]
        public FormattedString Result
        {
            get;
            set;
        }

        #endregion
    }
}