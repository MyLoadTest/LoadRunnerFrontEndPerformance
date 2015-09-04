using System;
using System.Linq;
using System.Runtime.Serialization;

namespace MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.ObjectMappings
{
    [DataContract]
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