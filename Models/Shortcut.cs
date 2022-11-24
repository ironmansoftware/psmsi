using System.IO;

namespace PSMSI.Models
{
    public class Shortcut
    {
        public string FileId { get; set; }
        public string DirectoryId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string IconPath { get; set; }
        public string WorkingDirectory { get; set; }
        public string Arguments { get; set; }
        public string Show { get; set; }
    }
}
