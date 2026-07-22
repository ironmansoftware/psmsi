using System;
using System.Collections.Generic;
using System.IO;

namespace PSMSI.Models
{
    public class Installer
    {
        public string ProductName { get; set; }
        public string Description { get; set; }
        public Version Version { get; set; }
        public string Manufacturer { get; set; }
        public Guid UpgradeCode { get; set; }
        public string Platform { get; set; }
        public DirectoryInfo OutputDirectory { get; set; }
        public IEnumerable<object> Content { get; set; }
        public string HelpLink { get; set; }
        public string AboutLink { get; set; }
        public bool RequiresElevation { get; set; }
        public UserInterface UserInterface { get; set; }
        public FileInfo AddRemoveProgramsIcon { get; set; }
        public CustomAction[] CustomActions { get; set; }
        public string ProductId { get; set; } = "*";
    }
}
