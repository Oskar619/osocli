using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace osodots.Model
{
    public class DotsProperties
    {
        public string FriendlyName { get; set; }
        public Dictionary<string, SystemClassMetadata> Systems { get; set; }
        public Dictionary<string, ComponentMetaData> Components { get; set; }
        public Dictionary<string, SystemGroupClassMetadata> SystemGroups { get; set; }
        public Dictionary<string, AuthoringComponentMetadata> AuthoringComponents { get; set; }

        [JsonIgnore]
        public bool HasAnyDotsData => Systems.Any() || Components.Any() || SystemGroups.Any() || AuthoringComponents.Any();
    }
}
