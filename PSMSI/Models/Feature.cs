using System.Collections.Generic;

namespace PSMSI.Models
{
    public class Feature
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string DefaultState { get; set; }
        public string Display { get; set; }
        public bool Required { get; set; }
        public string ConfigurableDirectoryId { get; set; }
        public IEnumerable<object> Content { get; set; }
    }
}
