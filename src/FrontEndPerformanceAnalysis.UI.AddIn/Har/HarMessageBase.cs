using System;
using System.Linq;
using System.Runtime.Serialization;

namespace MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.Har
{
    [DataContract]
    internal abstract class HarMessageBase
    {
        #region Public Properties

        [DataMember(Name = "httpVersion")]
        public string HttpVersion
        {
            get;
            set;
        }

        [DataMember(Name = "cookies")]
        public HarCookie[] Cookies
        {
            get;
            set;
        }

        [DataMember(Name = "headers")]
        public HarHeader[] Headers
        {
            get;
            set;
        }

        [DataMember(Name = "headersSize")]
        public long? HeadersSize
        {
            get;
            set;
        }

        [DataMember(Name = "bodySize")]
        public long? BodySize
        {
            get;
            set;
        }

        #endregion
    }
}