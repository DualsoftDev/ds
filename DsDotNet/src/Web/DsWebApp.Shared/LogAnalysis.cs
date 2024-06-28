using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DsWebApp.Shared
{
    public class Span
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
    }

    public class FqdnSpan : Span
    {
        public string Fqdn { get; set; }
    }

    public class CallSpan : FqdnSpan
    {
    }

    public class RealSpan : FqdnSpan
    {
        public CallSpan[] CallSpans { get; set; }
    }

    public class SystemSpan : FqdnSpan
    {
        public RealSpan[] RealSpans { get; set; }
    }
}
