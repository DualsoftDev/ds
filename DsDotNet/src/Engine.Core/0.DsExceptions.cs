using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core
{
    public class DsException: Exception
    {
        public DsException(string message)
            : base(message)
        {
        }
    }
}
