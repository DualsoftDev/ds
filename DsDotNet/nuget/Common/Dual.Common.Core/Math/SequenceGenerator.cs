using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace Dual.Common.Core
{
    public static class SequenceGenerator
    {
        public static IEnumerable<double> GenerateAccelDecelFactors(int step)
        {
            Contract.Requires(step > 0);
            int[] acceleration = Enumerable.Range(1, step).Select(n => n < step / 2 ? n * n : (step - n) * (step - n)).ToArray();
            var sum = acceleration.Sum();
            for ( int i = 0; i < step; i++ )
                yield return 1.0 * acceleration.Take(i + 1).Sum() / sum;
        }
    }
}
