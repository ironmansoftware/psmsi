using System.Management.Automation;

namespace PSMSI
{
    [Cmdlet(VerbsCommon.New, "InstallerShortcut")]
    public class NewInstallerShortcutCommand : PSCmdlet
    {
        [Parameter(Mandatory = true, ParameterSetName = "File")]
        public string FileId { get; set; }
        [Parameter(Mandatory = true, ParameterSetName = "Dir")]
        public string DirectoryId { get; set; }
        [Parameter(Mandatory = true)]
        public string Name { get; set; }
        [Parameter()]
        public string Description { get; set; }
        [Parameter()]
        public string IconPath { get; set; }

        [Parameter()]
        public string WorkingDirectoryId { get; set; }

        [Parameter()]
        public string Arguments { get; set; }

        [Parameter]
        [ValidateSet("normal", "minimized", "maximized")]
        public string Show { get; set; } = "normal";

        protected override void ProcessRecord()
        {
            if (!string.IsNullOrEmpty(IconPath))
            {
                IconPath = base.GetUnresolvedProviderPathFromPSPath(IconPath);
            }

            WriteObject(new Models.Shortcut
            {
                FileId = FileId,
                DirectoryId = DirectoryId,
                Name = Name,
                Description = Description,
                IconPath = IconPath,
                WorkingDirectory = WorkingDirectoryId,
                Arguments = Arguments,
                Show = Show
            });
        }
    }
}
