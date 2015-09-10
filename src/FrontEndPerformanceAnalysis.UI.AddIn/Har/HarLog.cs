using System;
using System.Linq;
using System.Runtime.Serialization;

namespace MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.Har
{
    [DataContract]
    internal sealed class HarLog
    {
        #region Constants and Fields

        public static readonly string DefaultHarVersion = "1.2";

        #endregion

        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="HarLog"/> class.
        /// </summary>
        public HarLog()
        {
            Version = DefaultHarVersion;
        }

        #endregion

        #region Public Properties

        [DataMember(Name = "version")]
        public string Version
        {
            get;
            set;
        }

        [DataMember(Name = "creator")]
        public HarCreator Creator
        {
            get;
            set;
        }

        [DataMember(Name = "pages")]
        public HarPage[] Pages
        {
            get;
            set;
        }

        [DataMember(Name = "entries")]
        public HarEntry[] Entries
        {
            get;
            set;
        }

        #endregion
    }
}