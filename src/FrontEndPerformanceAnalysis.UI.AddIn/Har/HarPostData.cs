using System;
using System.Linq;
using System.Runtime.Serialization;

namespace MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.Har
{
    [DataContract]
    internal sealed class HarPostData
    {
        #region Public Properties

        [DataMember(Name = "mimeType")]
        public string MimeType
        {
            get;
            set;
        }

        [DataMember(Name = "text")]
        public string Text
        {
            get;
            set;
        }

        [DataMember(Name = "params")]
        public HarPostDataParameter[] Parameters
        {
            get;
            set;
        }

        #endregion
    }
}