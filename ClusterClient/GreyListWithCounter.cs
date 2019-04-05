using System.Collections.Concurrent;
using System.Collections.Generic;

namespace ClusterClient
{
    public class GreyListWithCounter<T> : ConcurrentDictionary<T, int>
    {
        public void DecreaseCounters()
        {
            lock (this)
            {
                var keysToRemove = new List<T>();
                foreach (var key in Keys)
                {
                    var value = this[key];
                    if (value <= 1)
                        keysToRemove.Add(key);
                    else
                        this[key] = value - 1;
                }

                foreach (var key in keysToRemove) TryRemove(key, out _);
            }
        }
    }
}
