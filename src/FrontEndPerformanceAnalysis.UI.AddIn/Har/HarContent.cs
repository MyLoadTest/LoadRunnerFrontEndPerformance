using System;
using System.Linq;
using System.Runtime.Serialization;

namespace MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.Har
{
    [DataContract]
    internal sealed class HarContent
    {
        #region Public Properties

        [DataMember(Name = "size")]
        public long? Size
        {
            get;
            set;
        }

        [DataMember(Name = "compression")]
        public long? SavedByCompression
        {
            get;
            set;
        }

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

        #endregion
    }
}