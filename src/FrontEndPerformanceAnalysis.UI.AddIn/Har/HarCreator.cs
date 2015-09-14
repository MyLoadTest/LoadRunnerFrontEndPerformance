﻿using System;
using System.Linq;
using System.Runtime.Serialization;

namespace MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.Har
{
    [DataContract]
    internal sealed class HarCreator
    {
        #region Public Properties

        [DataMember(Name = "name")]
        public string Name
        {
            get;
            set;
        }

        [DataMember(Name = "version")]
        public string Version
        {
            get;
            set;
        }

        #endregion
    }
}