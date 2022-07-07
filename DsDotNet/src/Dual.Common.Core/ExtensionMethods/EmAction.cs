using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dsu.Common.Utilities.Core.ExtensionMethods
{
    public static class EmAction
    {
        public static void TryDo(this Action action, Action<Exception> onFailure, bool rethrow=false)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                onFailure(ex);
                if (rethrow)
                    throw;
            }
        }
    }
}
