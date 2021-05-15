using osodots.Model;
using System;
using System.Collections.Generic;
using System.Runtime.Caching;
using Microsoft;
using osodots.Cache;

namespace osodots.Util
{
    public class DotsAnalyzerCache
    {
        private readonly MemoryCache cache;
        private readonly CacheItemPolicy defaultCachePolicy;
        private readonly CachedDictionary<string, SystemGroupClassMetadata> CachedGroups;
        private readonly CachedDictionary<string, ComponentMetaData> CachedComponents;
        private readonly CachedDictionary<string, SystemClassMetadata> CachedSystems;
        private readonly CachedDictionary<string, DotsProperties> CachedAnalyzedData;        
        private readonly string GroupsSetName = "Groups";
        private readonly string ComponentSetName = "Components";
        private readonly string SystemSetName = "Systems";
        private readonly string AnalyzedDataDictionaryName = "AnalyzedData";

        private string CacheId { get; set; }

        public DotsAnalyzerCache()
        {
            cache = MemoryCache.Default;
            defaultCachePolicy = new CacheItemPolicy
            {
                SlidingExpiration = TimeSpan.FromHours(1)
            };

            CachedGroups = new CachedSet<string>(CacheId, GroupsSetName, cache, defaultCachePolicy);
            CachedComponents = new CachedSet<string>(CacheId, ComponentSetName, cache, defaultCachePolicy);
            CachedSystems = new CachedSet<string>(CacheId, SystemSetName, cache, defaultCachePolicy);
            CachedAnalyzedData = new CachedDictionary<string, DotsProperties>(CacheId, AnalyzedDataDictionaryName, cache, defaultCachePolicy);
        }

        public void SetCacheId(string cacheId)
        {
            CacheId = cacheId;
        }

        public bool IsEnabled => !string.IsNullOrWhiteSpace(CacheId);

        public IDictionary<string, DotsProperties> Analysis => CachedAnalyzedData.GetTable();
        public ISet<string> Components => CachedComponents.GetTable();
        public ISet<string> Systems => CachedSystems.GetTable();
        public ISet<string> Groups => CachedGroups.GetTable();

        public void SetData(IDictionary<string, DotsProperties> data)
        {
            Requires.NotNull(CacheId, nameof(CacheId));
            var identifiers = ExtractIdentifiers(data);
            CachedAnalyzedData.SetTable(data);
            CachedComponents.SetTable(identifiers.components);
            CachedGroups.SetTable(identifiers.systemGroups);
            CachedSystems.SetTable(identifiers.systems);
        }

        public void AppendData(IDictionary<string, DotsProperties> data)
        {
            Requires.NotNull(CacheId, nameof(CacheId));
            var identifiers = ExtractIdentifiers(data);
            CachedAnalyzedData.Append(data);
            CachedComponents.Append(identifiers.components);
            CachedGroups.Append(identifiers.systemGroups);
            CachedSystems.Append(identifiers.systems);
        }

        public void RemoveData(IDictionary<string, DotsProperties> data)
        {
            Requires.NotNull(CacheId, nameof(CacheId));
            var identifiers = ExtractIdentifiers(data);
            CachedAnalyzedData.Remove(data.Keys);
            CachedComponents.Remove(identifiers.components);
            CachedGroups.Remove(identifiers.systemGroups);
            CachedSystems.Remove(identifiers.systems);
        }

        private static (ISet<string> components, ISet<string> systems, ISet<string> systemGroups) ExtractIdentifiers(IDictionary<string, DotsProperties> data)
        {
            var c = new HashSet<string>();
            var s = new HashSet<string>();
            var sg = new HashSet<string>();
            foreach(var elem in data.Keys)
            {
                var currentProperty = data[elem];
                foreach(var group in currentProperty.SystemGroups.Keys)
                {
                    if (sg.Contains(group))
                    {
                        continue;
                    }
                    sg.Add(group);
                }
                foreach(var system in currentProperty.Systems.Keys)
                {
                    if (s.Contains(system))
                    {
                        continue;
                    }
                    s.Add(system);
                }
                foreach(var component in currentProperty.Components.Keys)
                {
                    if (c.Contains(component))
                    {
                        continue;
                    }
                    c.Add(component);
                }
            }
            return (c, s, sg);
        }
    }
}
