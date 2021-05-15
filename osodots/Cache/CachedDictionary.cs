using System.Collections.Generic;
using System.Runtime.Caching;

namespace osodots.Cache
{
    public class CachedDictionary<TKey, TVal>
    {
        private readonly string cacheId;
        private readonly MemoryCache cache;
        private readonly CacheItemPolicy cacheItemPolicy;
        public CachedDictionary(string dbId, string tableName, MemoryCache cache, CacheItemPolicy cacheItemPolicy)
        {
            cacheId = $"{dbId}_{tableName}";
            this.cache = cache;
            this.cacheItemPolicy = cacheItemPolicy;
        }

        public IDictionary<TKey, TVal> GetTable()
        {
            if (cache.Contains(cacheId))
            {
                return (IDictionary<TKey, TVal>) cache.Get(cacheId);
            }
            return null;
        }

        public void SetTable(IDictionary<TKey, TVal> table)
        {
            cache.Set(cacheId, table, cacheItemPolicy);
        }

        public void Append(IDictionary<TKey, TVal> elements)
        {
            var table = GetTable() ?? new Dictionary<TKey, TVal>();
            foreach (var tableElem in elements)
            {
                if (!table.Contains(tableElem))
                {
                    table.Add(tableElem);
                }
            }
            cache.Set(cacheId, table, cacheItemPolicy);
        }

        public void Remove(IEnumerable<TKey> elements)
        {
            var table = GetTable();
            if (table == null)
            {
                return;
            }
            foreach (var tableElem in elements)
            {
                if (!table.ContainsKey(tableElem))
                {
                    continue;
                }

                table.Remove(tableElem);
            }

            cache.Set(cacheId, table, cacheItemPolicy);
        }
    }
}
