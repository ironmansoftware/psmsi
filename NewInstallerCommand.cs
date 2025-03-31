using PSMSI.Models;
using PSMSI.Wix;
using PSMSI.Xml;
using System;
using System.IO;
using System.Linq;
using System.Management.Automation;

namespace PSMSI
{
    [Cmdlet(VerbsCommon.New, "Installer")]
    public class NewInstallerCommand : PSCmdlet
    {
        [Parameter(Mandatory = true)]
        public string ProductName { get; set; }
        [Parameter()]
        public string Description { get; set; }
        [Parameter()]
        public Version Version { get; set; } = new Version(1, 0);
        [Parameter()]
        public string Manufacturer { get; set; } = "Ironman Software, LLC";
        [Parameter(Mandatory = true)]
        public Guid UpgradeCode { get; set; }

        [Parameter]
        public string ProductId { get; set; } = "*";

        [Parameter()]
        [ValidateSet("x86", "x64", "ia64", "arm", "intel", "intel64")]
        public string Platform { get; set; } = "x86";
        [Parameter(Mandatory = true)]
        public DirectoryInfo OutputDirectory { get; set; }
        [Parameter(Mandatory = true)]
        public ScriptBlock Content { get; set; }
        [Parameter()]
        public string HelpLink { get; set; }
        [Parameter()]
        public string AboutLink { get; set; }
        [Parameter()]
        public SwitchParameter RequiresElevation { get; set; }
        [Parameter()]
        public UserInterface UserInterface { get; set; }
        [Parameter()]
        public FileInfo AddRemoveProgramsIcon { get; set; }
        [Parameter()]
        public CustomAction[] CustomAction { get; set; }

        protected override void ProcessRecord()
        {
            var installer = new Models.Installer
            {
                ProductName = ProductName,
                Description = Description,
                Version = Version,
                Manufacturer = Manufacturer,
                UpgradeCode = UpgradeCode,
                Platform = Platform,
                OutputDirectory = OutputDirectory,
                Content = Content?.Invoke().Select(m => m.BaseObject),
                HelpLink = HelpLink,
                AboutLink = AboutLink,
                RequiresElevation = RequiresElevation,
                UserInterface = UserInterface,
                AddRemoveProgramsIcon = AddRemoveProgramsIcon,
                CustomActions = CustomAction,
                ProductId = ProductId
            };

            if (!OutputDirectory.Exists)
            {
                OutputDirectory.Create();
            }

            var wxsFile = Path.Combine(OutputDirectory.FullName, $"{ProductName}.{Version}.{Platform}.wxs");

            var generator = new WixXmlGenerator();
            var document = generator.Generate(installer);
            document.Save(wxsFile);

            var wxsObjFile = Path.Combine(OutputDirectory.FullName, $"{ProductName}.{Version}.{Platform}.wxsobj");
            var msiFile = Path.Combine(OutputDirectory.FullName, $"{ProductName}.{Version}.{Platform}.msi");

            var candle = new Candle();
            var candleOptions = new CandleOption
            {
                WxsFile = wxsFile,
                WxsObjFile = wxsObjFile
            };

            var result = candle.Run(candleOptions);
            if (!result.Success)
            {
                throw new Exception(result.Output);
            }
            else
            {
                WriteVerbose(result.Output);
            }

            var light = new Light();
            var lightOptions = new LightOptions
            {
                MsiFileName = msiFile,
                OutputDirectory = OutputDirectory.FullName,
                WixObjFile = wxsObjFile
            };

            result = light.Run(lightOptions);

            if (!result.Success)
            {
                throw new Exception(result.Output);
            }
            else
            {
                WriteVerbose(result.Output);
            }


        }
    }
}
