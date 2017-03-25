using System.Collections.Generic;
using System;

namespace GrokDotNet
{
    public class GrokParseResponse {
        public TimeSpan TimeTaken { get; set; }
        public List<Tuple<string, object>> Captures = new List<Tuple<string, object>>();
    }
}