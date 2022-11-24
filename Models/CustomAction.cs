using System;

namespace PSMSI.Models
{
    public class CustomAction
    {
        public string Id { get; private set; } = "CA" + Guid.NewGuid().ToString("N");
        public string FileId { get; set; }
        public bool CheckReturnValue { get; set; }
        public bool RunOnInstall { get; set; }
        public bool RunOnUninstall { get; set; }
        public string Arguments { get; set; }
        public string ScriptArguments { get; set; }
    }
}
