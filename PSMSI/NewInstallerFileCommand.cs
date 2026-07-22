using System;
using System.IO;
using System.Management.Automation;

namespace PSMSI
{
    [Cmdlet(VerbsCommon.New, "InstallerFile")]
    public class NewInstallerFileCommand : PSCmdlet
    {
        [Parameter()]
        public string Id { get; set; }
        [Parameter(Mandatory = true, ValueFromPipeline = true, Position = 0)]
        public string Source { get; set; }

        protected override void ProcessRecord()
        {
            if (!MyInvocation.BoundParameters.ContainsKey("Id"))
            {
                Id = "fil" + Guid.NewGuid().ToString("n");
            }

            Source = base.GetUnresolvedProviderPathFromPSPath(Source);


            WriteObject(new Models.File
            {
                Id = Id,
                Source = Source
            });
        }
    }
}
