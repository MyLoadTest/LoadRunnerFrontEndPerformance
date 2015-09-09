using System;
using System.Linq;
using System.Runtime.Serialization;

namespace MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.Har
{
    [DataContract]
    internal sealed class HarCookie : HarNameValueObject
    {
        #region Public Properties

        [DataMember(Name = "path")]
        public string Path
        {
            get;
            set;
        }

        [DataMember(Name = "domain")]
        public string Domain
        {
            get;
            set;
        }

        [DataMember(Name = "expires")]
        public string ExpiresAsString
        {
            get;
            set;
        }

        [DataMember(Name = "httpOnly")]
        public bool? HttpOnly
        {
            get;
            set;
        }

        [DataMember(Name = "secure")]
        public bool? Secure
        {
            get;
            set;
        }

        #endregion
    }
}