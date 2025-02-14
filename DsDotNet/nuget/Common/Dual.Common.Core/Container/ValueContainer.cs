using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dual.Common.Core
{
    /// Reference type 이 아닌, struct type(value type) 에 대한 hashset 지원 class
    public class ValueHashSet<TKey> : HashSet<TKey>
        where TKey : struct
    {
    }

    /// Reference type 이 아닌, struct type(value type) 에 대한 dictionary 지원 class
    public class ValueDictionay<TKey, TValue> : Dictionary<TKey, TValue>
        where TKey : struct
        where TValue : class
    {
    }
}
