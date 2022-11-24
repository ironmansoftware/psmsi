using System.Collections.Generic;

namespace PSMSI.Models
{
    public class Directory
    {
        public string Name { get; set; }
        public string Id { get; set; }
        public bool Configurable { get; set; }
        public IEnumerable<object> Content { get; set; }
    }
}
