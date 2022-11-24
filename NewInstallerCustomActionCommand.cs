using System.Management.Automation;

namespace PSMSI
{
    [Cmdlet(VerbsCommon.New, "InstallerCustomAction")]
    public class NewInstallerCustomActionCommand : PSCmdlet
    {
        [Parameter(Mandatory = true, ParameterSetName = "fileid")]
        public string FileId { get; set; }
        [Parameter]
        public SwitchParameter CheckReturnValue { get; set; }
        [Parameter]
        public SwitchParameter RunOnInstall { get; set; }
        [Parameter]
        public SwitchParameter RunOnUninstall { get; set; }
        [Parameter]
        public string Arguments { get; set; }
        [Parameter]
        public string ScriptArguments { get; set; }
        protected override void ProcessRecord()
        {
            WriteObject(new Models.CustomAction
            {
                FileId = FileId,
                RunOnInstall = RunOnInstall,
                RunOnUninstall = RunOnUninstall,
                CheckReturnValue = CheckReturnValue,
                Arguments = Arguments,
                ScriptArguments = ScriptArguments
            });
        }
    }
}
