using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PSMSI.Wix
{
    public class Candle
    {
        public ToolResult Run(CandleOption options)
        {
            var assemblyBasePath = Path.GetDirectoryName(GetType().GetTypeInfo().Assembly.Location);
            var candleexe = Path.Combine(assemblyBasePath, "wix", "bin", "candle.exe");

            if (!File.Exists(candleexe))
            {
                throw new Exception($"File does not exist: {candleexe}");
            }

            var process = new Process();
            process.StartInfo = new ProcessStartInfo();
            process.StartInfo.FileName = candleexe;
            process.StartInfo.Arguments = $"\"{options.WxsFile}\" -out \"{options.WxsObjFile}\" -ext WixUtilExtension.dll";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.Start();

            var toolResult = new ToolResult();

            toolResult.Output = process.StandardOutput.ReadToEnd();
            toolResult.Error = process.StandardError.ReadToEnd();

            process.WaitForExit();

            toolResult.Success = process.ExitCode == 0;

            return toolResult;
        }
    }

    public class CandleOption
    {
        public string WxsFile { get; set; }
        public string WxsObjFile { get; set; }
    }
}
