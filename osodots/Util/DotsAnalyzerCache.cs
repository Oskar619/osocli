using osodots.Model;
using System;
using System.Collections.Generic;
using System.Runtime.Caching;

namespace osodots.Util
{
    public class DotsAnalyzerCache
    {
        private readonly MemoryCache cache;
        private readonly CacheItemPolicy defaultCachePolicy;
        public DotsAnalyzerCache()
        {
            cache = MemoryCache.Default;
            defaultCachePolicy = new CacheItemPolicy
            {
                SlidingExpiration = TimeSpan.FromHours(1)
            };
        }

        public bool IsCached(string cacheId)
        {
            return cache.Contains(cacheId);
        }

        public void SetAnalyzedData(string cacheId, Dictionary<string, DotsProperties> data)
        {
            var cachePolicy = new CacheItemPolicy
            {
                SlidingExpiration = TimeSpan.FromDays(1)
            };

            if (cache.Contains(cacheId))
            {
                cache.Remove(cacheId);
            }
            cache.Add(cacheId, data, cachePolicy);
        }

        public void AppendAnalyzedData(string cacheId, Dictionary<string, DotsProperties> data)
        {
            if (cache.Contains(cacheId))
            {
                var cacheItem = cache.GetCacheItem(cacheId);
                var cachedAnalyzedData = (Dictionary<string, DotsProperties>)cacheItem.Value;
                foreach(var analyzedFile in data.Keys)
                {
                    if (!cachedAnalyzedData.ContainsKey(analyzedFile))
                    {
                        cachedAnalyzedData.Add(analyzedFile, data[analyzedFile]);
                    }
                    else
                    {
                        cachedAnalyzedData[analyzedFile] = data[analyzedFile];
                    }
                }
                cache.Set(cacheItem, defaultCachePolicy);
            }
        }

        public Dictionary<string, DotsProperties> GetCachedAnalyzedData(string cacheId)
        {
            if (cache.Contains(cacheId))
            {
                var result = (Dictionary<string, DotsProperties>) cache.Get(cacheId);
                return result;
            }
            return null;
        }
    }
}
