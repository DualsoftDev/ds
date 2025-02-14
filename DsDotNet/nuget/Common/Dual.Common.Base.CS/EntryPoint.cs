using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dual.Common.Base.CS
{
    public class ModuleInitializer
    {
        public static void Initialize()
        {
            Trace.WriteLine("Dual.Common.Base.CS being initialized..");
            DcApp.Initialize();
            DcLogger.EnableTrace = DcApp.IsDebugVersion || DcApp.IsInUnitTest();
        }
    }
}
