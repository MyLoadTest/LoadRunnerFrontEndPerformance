using System;
using System.Linq;
using System.Runtime.Serialization;

namespace MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.Har
{
    [DataContract]
    internal sealed class HarResponse : HarMessageBase
    {
        #region Public Properties

        [DataMember(Name = "status")]
        public int? Status
        {
            get;
            set;
        }

        [DataMember(Name = "statusText")]
        public string StatusText
        {
            get;
            set;
        }

        [DataMember(Name = "content")]
        public HarContent Content
        {
            get;
            set;
        }

        [DataMember(Name = "redirectURL")]
        public string RedirectUrl
        {
            get;
            set;
        }

        #endregion
    }
}