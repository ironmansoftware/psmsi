using SixLabors.ImageSharp;
using System.IO;
using System.Management.Automation;

namespace PSMSI
{
    [Cmdlet(VerbsCommon.New, "InstallerUserInterface")]
    public class NewInstallerUserInterface : PSCmdlet
    {
        [Parameter]
        public string Eula { get; set; }
        [Parameter]
        public string TopBanner { get; set; }
        [Parameter]
        public string WelcomeAndCompletionBackground { get; set; }
        [Parameter]
        public string ExclamationIcon { get; set; }
        [Parameter]
        public string InformationIcon { get; set; }
        [Parameter]
        public string NewIcon { get; set; }
        [Parameter]
        public string UpIcon { get; set; }
        [Parameter]
        public string ExitDialogText { get; set; }

        protected override void ProcessRecord()
        {
            if (Eula != null)
            {
                Eula = GetUnresolvedProviderPathFromPSPath(Eula);
                if (!File.Exists(Eula))
                {
                    WriteWarning("-EULA specified but the file does not exist.");
                }
            }

            if (TopBanner != null)
            {
                TopBanner = GetUnresolvedProviderPathFromPSPath(TopBanner);
                if (!File.Exists(TopBanner))
                {
                    WriteWarning("-TopBanner specified but the file does not exist.");
                }
            }

            if (WelcomeAndCompletionBackground != null)
            {
                WelcomeAndCompletionBackground = GetUnresolvedProviderPathFromPSPath(WelcomeAndCompletionBackground);
                if (!File.Exists(WelcomeAndCompletionBackground))
                {
                    WriteWarning("-WelcomeAndCompletionBackground specified but the file does not exist.");
                }
            }

            if (ExclamationIcon != null)
            {
                ExclamationIcon = GetUnresolvedProviderPathFromPSPath(ExclamationIcon);
                if (!File.Exists(ExclamationIcon))
                {
                    WriteWarning("-ExclamationIcon specified but the file does not exist.");
                }
            }

            if (InformationIcon != null)
            {
                InformationIcon = GetUnresolvedProviderPathFromPSPath(InformationIcon);
                if (!File.Exists(InformationIcon))
                {
                    WriteWarning("-InformationIcon specified but the file does not exist.");
                }
            }

            if (NewIcon != null)
            {
                NewIcon = GetUnresolvedProviderPathFromPSPath(NewIcon);
                if (!File.Exists(NewIcon))
                {
                    WriteWarning("-NewIcon specified but the file does not exist.");
                }
            }

            if (UpIcon != null)
            {
                UpIcon = GetUnresolvedProviderPathFromPSPath(UpIcon);
                if (!File.Exists(UpIcon))
                {
                    WriteWarning("-UpIcon specified but the file does not exist.");
                }
            }

            TestImageSize(TopBanner, nameof(TopBanner), 58, 493);
            TestImageSize(WelcomeAndCompletionBackground, nameof(WelcomeAndCompletionBackground), 312, 493);
            TestImageSize(ExclamationIcon, nameof(ExclamationIcon), 32, 32);
            TestImageSize(InformationIcon, nameof(InformationIcon), 32, 32);
            TestImageSize(NewIcon, nameof(NewIcon), 16, 16);
            TestImageSize(UpIcon, nameof(UpIcon), 16, 16);

            WriteObject(new Models.UserInterface
            {
                Eula = Eula,
                TopBanner = TopBanner,
                WelcomeAndCompletionBackground = WelcomeAndCompletionBackground,
                ExclamationIcon = ExclamationIcon,
                InformationIcon = InformationIcon,
                NewIcon = NewIcon,
                UpIcon = UpIcon,
                ExitDialogText = ExitDialogText
            });
        }

        private void TestImageSize(string file, string name, int height, int width)
        {
            if (file == null) return;

            try
            {
                var image = Image.Load(file);
                if (image.Height != height)
                {
                    WriteWarning($"The {name} image's recommended height is {height} pixels but the image specified is {image.Height}.");
                }

                if (image.Width != width)
                {
                    WriteWarning($"The {name} image's recommended width is {width} pixels but the image specified is {image.Width}.");
                }
            }
            catch
            {

            }
        }
    }
}
