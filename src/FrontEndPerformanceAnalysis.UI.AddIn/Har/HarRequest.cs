using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;

namespace MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.Har
{
    [DataContract]
    [DebuggerDisplay("{GetType().Name,nq}. {Method,nq} {Url,nq}")]
    internal sealed class HarRequest : HarMessageBase
    {
        #region Public Properties

        [DataMember(Name = "method")]
        public string Method
        {
            get;
            set;
        }

        [DataMember(Name = "url")]
        public string Url
        {
            get;
            set;
        }

        [DataMember(Name = "queryString")]
        public HarQueryStringParameter[] QueryStringParameters
        {
            get;
            set;
        }

        [DataMember(Name = "postData")]
        public HarPostData PostData
        {
            get;
            set;
        }

        #endregion
    }
}