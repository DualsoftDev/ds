using System.Collections.Generic;
using System.Linq;
#if NET
namespace Dual.Common.Base.CS
{
    public static class EmConsecutive
    {
        private static bool _isConsecutive(dynamic x, dynamic y) => (x == y) || (x == y - 1);

        /// <summary>
        /// http://sagarkhyaju.blogspot.kr/2014/01/split-array-with-linq-to-group.html
        /// </summary>
        public static IEnumerable<List<T>> ToConsecutiveGroups<T>(this IEnumerable<T> source)
        {
            using (var iterator = source.GetEnumerator())
            {
                if (iterator.MoveNext())
                {
                    T current = iterator.Current;
                    List<T> group = new List<T> { current };

                    while (iterator.MoveNext())
                    {
                        T next = iterator.Current;
                        if (!_isConsecutive(current, next))
                        {
                            yield return group;
                            group = new List<T>();
                        }

                        current = next;
                        group.Add(current);
                    }

                    if (group.Any())
                        yield return group;
                }
            }
        }

        //[TestMethod]
        //private void TestConsecutive()
        //{
        //    var listOfInt = new List<int> { 1, 2, 3, 4, 7, 8, 12, 13, 14 };
        //    var result = listOfInt.ToConsecutiveGroups();
        //    ==>  { {1, 2, 3, 4}, {7, 8}, {12, 13, 14} }
        //}

        private static bool _isSame(dynamic x, dynamic y) => x == y;
        public static IEnumerable<List<T>> ToSameGroups<T>(this IEnumerable<T> source)
        {
            using (var iterator = source.GetEnumerator())
            {
                if (iterator.MoveNext())
                {
                    T current = iterator.Current;
                    List<T> group = new List<T> { current };

                    while (iterator.MoveNext())
                    {
                        T next = iterator.Current;
                        if (!_isSame(current, next))
                        {
                            yield return group;
                            group = new List<T>();
                        }

                        current = next;
                        group.Add(current);
                    }

                    if (group.Any())
                        yield return group;
                }
            }
        }


        /// <summary>
        /// 항목 2개씩 순환 : new[] { 1, 2, 3, 4, 5 }.EnumeratePaired() ==> {{1, 2}, {2, 3}, {3, 4}, {4, 5}, {5, 1}}
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <returns></returns>
        public static IEnumerable<IEnumerable<T>> EnumeratePaired<T>(this IEnumerable<T> source)
        {
            return source.Zip(source.Rotate(1), (a, b) => new[] { a, b });  // ==> {{1, 2}, {2, 3}, {3, 4}, {4, 5}, {5, 1}}
            //return source.Zip(source.Skip(1), (a, b) => new[] { a, b });  // ==> {{1, 2}, {2, 3}, {3, 4}, {4, 5}}
        }

        public static IEnumerable<T> Rotate<T>(this IEnumerable<T> source, int rot)
        {
            return source.Skip(rot)
                  .Take(source.Count() - rot)
                  .Concat(source.Take(rot));
        }

    }
}

#endif //NET