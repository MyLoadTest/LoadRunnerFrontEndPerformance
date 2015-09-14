﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;

namespace MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.ObjectMappings.PageSpeed
{
    [DataContract]
    [DebuggerDisplay("Format = {Format}")]
    internal sealed class FormattedString
    {
        #region Public Properties

        [DataMember(Name = "format")]
        public string Format
        {
            get;
            set;
        }

        [DataMember(Name = "args")]
        public FormattedStringArg[] Args
        {
            get;
            set;
        }

        #endregion
    }
}