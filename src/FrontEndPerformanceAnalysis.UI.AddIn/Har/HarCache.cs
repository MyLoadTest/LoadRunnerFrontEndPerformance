using System;
using System.Linq;
using System.Runtime.Serialization;

namespace MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.Har
{
    [DataContract]
    internal sealed class HarCache
    {
        #region Public Properties

        [DataMember(Name = "beforeRequest")]
        public HarCacheInfo BeforeRequest
        {
            get;
            set;
        }

        [DataMember(Name = "afterRequest")]
        public HarCacheInfo AfterRequest
        {
            get;
            set;
        }

        #endregion
    }
}