using System;
using System.Linq;
using System.Runtime.Serialization;

namespace MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.Har
{
    [DataContract]
    internal sealed class HarPostDataParameter : HarNameValueObject
    {
        #region Public Properties

        [DataMember(Name = "fileName")]
        public string FileName
        {
            get;
            set;
        }

        [DataMember(Name = "contentType")]
        public string ContentType
        {
            get;
            set;
        }

        #endregion
    }
}