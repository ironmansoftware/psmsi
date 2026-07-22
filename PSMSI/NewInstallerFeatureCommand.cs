using System.Linq;
using System.Management.Automation;

namespace PSMSI
{
    [Cmdlet(VerbsCommon.New, "InstallerFeature")]
    public class NewInstallerFeatureCommand : PSCmdlet
    {
        [Parameter(Mandatory = true, Position = 0)]
        [ValidateLength(1, 38)]
        [ValidatePattern("^[A-Za-z_][A-Za-z0-9_.]*$")]
        public string Id { get; set; }

        [Parameter]
        public string Title { get; set; }

        [Parameter]
        public string Description { get; set; }

        [Parameter]
        [ValidateSet("Install", "Absent", "CompleteOnly", "Disabled")]
        public string DefaultState { get; set; } = "Install";

        [Parameter]
        [ValidateSet("Expand", "Collapse", "Hidden")]
        public string Display { get; set; } = "Collapse";

        [Parameter]
        public SwitchParameter Required { get; set; }

        [Parameter]
        [ValidateLength(1, 72)]
        [ValidatePattern("^[A-Za-z_][A-Za-z0-9_.]*$")]
        public string ConfigurableDirectoryId { get; set; }

        [Parameter(Position = 1)]
        public ScriptBlock Content { get; set; }

        protected override void ProcessRecord()
        {
            WriteObject(new Models.Feature
            {
                Id = Id,
                Title = string.IsNullOrEmpty(Title) ? Id : Title,
                Description = Description,
                DefaultState = DefaultState,
                Display = Display,
                Required = Required,
                ConfigurableDirectoryId = ConfigurableDirectoryId,
                Content = Content?.Invoke().Select(m => m.BaseObject)
            });
        }
    }
}
