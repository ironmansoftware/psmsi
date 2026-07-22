using System;
using System.Linq;
using System.Management.Automation;

namespace PSMSI
{
    [Cmdlet(VerbsCommon.New, "InstallerDirectory", DefaultParameterSetName = "custom")]
    public class NewInstallerDirectoryCommand : PSCmdlet
    {
        [Parameter(Mandatory = true, ParameterSetName = "custom", Position = 0)]
        public string DirectoryName { get; set; }
        [Parameter(Mandatory = true, ParameterSetName = "predefined")]
        [ValidateSet("AdminToolsFolder", "AppDataFolder", "CommonAppDataFolder", "CommonFilesFolder", "CommonFiles64Folder", "CommonFiles6432Folder", "DesktopFolder", "FavoritesFolder", "FontsFolder", "LocalAppDataFolder", "MyPicturesFolder", "PersonalFolder", "ProgramFilesFolder", "ProgramFiles64Folder", "ProgramFiles6432Folder", "ProgramMenuFolder", "SendToFolder", "StartMenuFolder", "StartupFolder", "SystemFolder", "System64Folder", "TemplateFolder", "WindowsFolder")]
        public string PredefinedDirectoryName { get; set; }
        [Parameter(ParameterSetName = "custom")]
        public string Id { get; set; }
        [Parameter(Position = 1)]
        public ScriptBlock Content { get; set; }
        [Parameter(ParameterSetName = "custom")]
        public SwitchParameter Configurable { get; set; }

        protected override void ProcessRecord()
        {
            if (ParameterSetName == "custom")
            {
                if (!MyInvocation.BoundParameters.ContainsKey("Id"))
                {
                    Id = "dir" + Guid.NewGuid().ToString("n");
                }

                WriteObject(new Models.Directory
                {
                    Name = DirectoryName,
                    Id = Id,
                    Content = Content?.Invoke().Select(m => m.BaseObject),
                    Configurable = Configurable
                });
            }
            else
            {
                WriteObject(new Models.Directory
                {
                    Name = PredefinedDirectoryName,
                    Id = PredefinedDirectoryName,
                    Content = Content?.Invoke().Select(m => m.BaseObject)
                });
            }


        }
    }
}
