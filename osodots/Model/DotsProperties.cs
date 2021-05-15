using System.Text.Json.Serialization;

namespace osodots.Model
{
    public class DotsProperties
    {
        public SystemClassMetadata[] Systems { get; set; }
        public ComponentMetaData[] Components { get; set; }
        public SystemGroupClassMetadata[] SystemGroups { get; set; }
        public AuthoringComponentMetadata[] AuthoringComponents { get; set; }

        [JsonIgnore]
        public bool HasAnyDotsData => Systems.Length > 0 || Components.Length > 0 || SystemGroups.Length > 0 || AuthoringComponents.Length > 0;
    }
}
