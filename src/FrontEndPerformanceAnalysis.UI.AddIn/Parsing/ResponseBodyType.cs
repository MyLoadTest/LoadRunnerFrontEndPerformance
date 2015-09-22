using System;
using System.Linq;

namespace MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.Parsing
{
    internal enum ResponseBodyType
    {
        Default,
        Chunked,
        Encoded,
        Decoded
    }
}