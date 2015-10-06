using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;

namespace MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.Analysis.PageSpeed
{
    [DataContract]
    [DebuggerDisplay("[{GetType().Name,nq}] Header = {Header}, Urls.Length = {Urls?.Length}")]
    internal sealed class UrlBlock
    {
        #region Public Properties

        [DataMember(Name = "header")]
        public FormattedString Header
        {
            get;
            set;
        }

        [DataMember(Name = "urls")]
        public UrlData[] Urls
        {
            get;
            set;
        }

        #endregion
    }
}