using System;

namespace PSMSI.Models
{
    public class Component
    {
        public string Id { get; set; } = "cmd" + System.Guid.NewGuid().ToString("n");
        public string Guid { get; set; } = "*";
    }
}
