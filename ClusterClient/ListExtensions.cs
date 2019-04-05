using System;
using System.Collections.Generic;

namespace ClusterClient
{
    public static class ListExtensions
    {
        private static readonly Random Rand = new Random();

        public static void Shuffle<T>(this IList<T> list, Random random = null)
        {
            random = random ?? Rand;
            var n = list.Count;
            while (n > 1)
            {
                n--;
                var k = random.Next(n + 1);
                var value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public static IEnumerable<IEnumerable<T>> Split<T>(this IList<T> list, int amountInPart)
        {
            for (var start = 0; start < list.Count; start += amountInPart)
            {
                var end = Math.Min(start + amountInPart, list.Count);
                yield return list.Slice(start, end);
            }
        }

        public static IEnumerable<T> Slice<T>(this IList<T> list, int from, int to)
        {
            for (var i = from; i < to; i++) yield return list[i];
        }
    }
}
