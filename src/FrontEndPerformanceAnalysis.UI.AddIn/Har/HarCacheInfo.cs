using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;

namespace MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.Har
{
    [DataContract]
    [DebuggerDisplay("{GetType().Name,nq}. ETag = {ETag}, HitCount = {HitCount}")]
    internal sealed class HarCacheInfo
    {
        #region Public Properties

        [DataMember(Name = "expires")]
        public string ExpiresAsString
        {
            get;
            set;
        }

        [DataMember(Name = "lastAccess")]
        public string LastAccessAsString
        {
            get;
            set;
        }

        [DataMember(Name = "eTag")]
        public string ETag
        {
            get;
            set;
        }

        [DataMember(Name = "hitCount")]
        public long? HitCount
        {
            get;
            set;
        }

        #endregion
    }
}