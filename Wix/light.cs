using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace PSMSI.Wix
{
    public class Light
    {
        public ToolResult Run(LightOptions options)
        {
            var assemblyBasePath = Path.GetDirectoryName(GetType().GetTypeInfo().Assembly.Location);
            var lightexe = Path.Combine(assemblyBasePath, "wix", "bin", "light.exe");

            if (!File.Exists(lightexe))
            {
                throw new System.Exception($"File does not exist: {lightexe}");
            }

            var outputFileName = Path.Combine(options.OutputDirectory, options.MsiFileName);

            var light = new Process();
            light.StartInfo = new ProcessStartInfo();
            light.StartInfo.FileName = lightexe;
            light.StartInfo.Arguments = $"-v -sw1076 -spdb -ext WixUIExtension -out \"{outputFileName}\" \"{options.WixObjFile}\" -b \"{options.OutputDirectory.TrimEnd('\\')}\" -sice:ICE91 -sice:ICE69 -sice:ICE38 -sice:ICE57 -sice:ICE64 -sice:ICE204 -sice:ICE80";
            light.StartInfo.RedirectStandardOutput = true;
            light.StartInfo.RedirectStandardError = true;
            light.StartInfo.UseShellExecute = false;
            light.Start();

            var toolResult = new ToolResult();

            toolResult.Output = light.StandardOutput.ReadToEnd();
            toolResult.Error = light.StandardError.ReadToEnd();

            light.WaitForExit();

            toolResult.Success = File.Exists(Path.Combine(options.OutputDirectory, outputFileName));

            return toolResult;
        }
    }

    public class LightOptions
    {
        public string WixObjFile { get; set; }
        public string OutputDirectory { get; set; }
        public string MsiFileName { get; set; }

    }
}
