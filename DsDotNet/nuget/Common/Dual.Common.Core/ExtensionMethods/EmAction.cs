using System;

namespace Dual.Common.Core
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
