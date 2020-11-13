using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SpaServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace ViteHosted
{
    public static class VueHelper
    {
        // done message of 'npm run serve' command.
        private static string DoneMessage { get; } = "Dev server running at:";

        /// <summary>
        /// Adds Connection to Vite Hosted VueApplication
        /// configured per <seealso cref="SpaOptions"/> on the <paramref name="spa"/>.
        /// NOTE: (this will create devcert.pfx and vite.config.js in your Vue Application on first run)
        /// </summary>
        /// <param name="spa"></param>
        public static void UseViteDevelopmentServer(this ISpaBuilder spa)
        {
            // Default HostingPort
            if (spa.Options.DevServerPort == 0)
                spa.Options.DevServerPort = 3000;

            var devServerEndpoint = new Uri($"https://localhost:{spa.Options.DevServerPort}");
            var loggerFactory = spa.ApplicationBuilder.ApplicationServices.GetService<ILoggerFactory>();
            var webHostEnvironment = spa.ApplicationBuilder.ApplicationServices.GetService<IWebHostEnvironment>();
            var logger = loggerFactory.CreateLogger("Vue");

            // Is this already running
            bool IsRunning = IPGlobalProperties.GetIPGlobalProperties()
                .GetActiveTcpListeners()
                .Select(x => x.Port)
                .Contains(spa.Options.DevServerPort);

            if (!IsRunning)
            {
                var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

                // export dev cert
                var tempDir = webHostEnvironment.ContentRootPath;
                var tempPfx = Path.Combine(tempDir, spa.Options.SourcePath, "devcert.pfx");
                var tempConfig = Path.Combine(tempDir, spa.Options.SourcePath, "vite.config.js");

                if (!File.Exists(tempPfx) || !File.Exists(tempConfig))
                {
                    var pfxPassword = Guid.NewGuid().ToString("N");
                    logger.LogInformation($"Exporting dotnet dev cert to {tempPfx} for Vite");
                    logger.LogDebug($"Export password: {pfxPassword}");
                    var certExport = new ProcessStartInfo
                    {
                        FileName = isWindows ? "cmd" : "dotnet",
                        Arguments = $"{(isWindows ? "/c dotnet " : "")}dev-certs https -v -ep {tempPfx} -p {pfxPassword}",
                        RedirectStandardError = true,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                    };
                    var exportProcess = Process.Start(certExport);
                    exportProcess.WaitForExit();
                    if (exportProcess.ExitCode == 0)
                    {
                        logger.LogInformation(exportProcess.StandardOutput.ReadToEnd());
                    }
                    else
                    {
                        logger.LogError(exportProcess.StandardError.ReadToEnd());
                    }

                    // create config
                    File.WriteAllText(tempConfig, $"export default {{\r\nhttps:true,\r\nhttpsOptions: {{\r\npfx: '{Path.GetFileName(tempPfx)}',\r\npassphrase: '{pfxPassword}'\r\n}}\r\n}}");
                    logger.LogInformation($"Creating Vite config: {tempConfig}");
                }

                // launch vue.js development server
                var processInfo = new ProcessStartInfo
                {
                    FileName = isWindows ? "cmd" : "npm",
                    Arguments = $"{(isWindows ? "/c npm " : "")}run dev",
                    WorkingDirectory = spa.Options.SourcePath,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                };
                var process = Process.Start(processInfo);
                var tcs = new TaskCompletionSource<int>();
                _ = Task.Run(() =>
                {
                    try
                    {
                        string line;
                        while ((line = process.StandardOutput.ReadLine()?.Trim()) != null)
                        {
                            if (!String.IsNullOrEmpty(line))
                            {
                                logger.LogInformation(line);
                                if (!tcs.Task.IsCompleted && line.Contains(DoneMessage))
                                {
                                    tcs.SetResult(1);
                                }
                            }
                        }
                    }
                    catch (EndOfStreamException ex)
                    {
                        logger.LogError(ex.ToString());
                        tcs.SetException(new InvalidOperationException("'npm run dev' failed.", ex));
                    }
                });
                _ = Task.Run(() =>
                {
                    try
                    {
                        string line;
                        while ((line = process.StandardError.ReadLine()?.Trim()) != null)
                        {
                            logger.LogError(line);
                        }
                    }
                    catch (EndOfStreamException ex)
                    {
                        logger.LogError(ex.ToString());
                        tcs.SetException(new InvalidOperationException("'npm run dev' failed.", ex));
                    }
                });

                if (!tcs.Task.Wait(spa.Options.StartupTimeout))
                {
                    throw new TimeoutException();
                }
            }
            spa.UseProxyToSpaDevelopmentServer(devServerEndpoint);
        }
    }
}
