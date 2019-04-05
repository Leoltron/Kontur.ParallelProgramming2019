using System.Collections.Generic;
using System.Linq;

namespace ClusterClient
{
    public static class DictExtensions
    {
        public static IEnumerable<(TKey Key, TValue Value)> GetEntries<TKey, TValue>(
            this IDictionary<TKey, TValue> dictionary)
        {
            return dictionary.Keys.Select(k => (k, dictionary[k]));
        }
    }
}
