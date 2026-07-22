using System;
using System.IO;

namespace PSMSI.Models
{
    public class Icon
    {
        public string Id { get; set; } = "ico" + Guid.NewGuid().ToString("n");
        public FileInfo SourceFile { get; set; }
    }
}
