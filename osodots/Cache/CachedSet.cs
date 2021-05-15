using System.Collections.Generic;
using System.Runtime.Caching;

namespace osodots.Cache
{
    public class CachedSet<T>
        where T : class
    {
        private readonly string cacheId;
        private readonly MemoryCache cache;
        private readonly CacheItemPolicy cacheItemPolicy;
        public CachedSet(string dbId, string tableName, MemoryCache cache, CacheItemPolicy cacheItemPolicy)
        {
            cacheId = $"{dbId}_{tableName}";
            this.cache = cache;
            this.cacheItemPolicy = cacheItemPolicy;
        }

        public ISet<T> GetTable()
        {
            if (cache.Contains(cacheId))
            {
                return (ISet<T>)cache.Get(cacheId);
            }
            return null;
        }

        public void SetTable(ISet<T> table)
        {
            cache.Set(cacheId, table, cacheItemPolicy);
        }

        public void Append(IEnumerable<T> elements)
        {
            var table = GetTable() ?? new HashSet<T>();
            foreach(var tableElem in elements)
            {
                if (!table.Contains(tableElem))
                {
                    table.Add(tableElem);
                }
            }
            cache.Set(cacheId, table, cacheItemPolicy);
        }

        public void Remove(IEnumerable<T> elements)
        {
            var table = GetTable();
            if(table == null)
            {
                return;
            }
            foreach(var tableElem in elements)
            {
                if (!table.Contains(tableElem))
                {
                    continue;
                }

                table.Remove(tableElem);
            }

            cache.Set(cacheId, table, cacheItemPolicy);
        }
    }
}
