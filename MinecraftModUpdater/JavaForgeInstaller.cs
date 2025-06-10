using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace MinecraftModUpdater
{
    public class JavaForgeInstaller
    {
        public event EventHandler<string>? StatusUpdated;

        private void ReportStatus(string message)
        {
            StatusUpdated?.Invoke(this, message);
        }

        public async Task InstallJavaAndForgeAsync()
        {
            try
            {
                ReportStatus("Starting Java and Forge installation...");

                if (IsJavaInstalled())
                {
                    ReportStatus("Java is already installed. Skipping Java installation.");
                }
                else
                {
                    await InstallJavaAsync();
                }

                if (IsForgeInstalled("1.21-47.0.0"))
                {
                    ReportStatus("Forge 1.21-47.0.0 is already installed. Skipping Forge installation.");
                }
                else
                {
                    await InstallForgeAsync();
                }

                ReportStatus("Installation process completed.");
            }
            catch (Exception ex)
            {
                ReportStatus($"Error: {ex.Message}");
                throw;
            }
        }

        private bool IsJavaInstalled()
        {
            try
            {
                var process = Process.Start(new ProcessStartInfo
                {
                    FileName = "java",
                    Arguments = "-version",
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                });

                process?.WaitForExit(3000);
                return process?.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        private bool IsForgeInstalled(string forgeVersion)
        {
            string versionsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft", "versions");
            string forgePath = Path.Combine(versionsDir, $"forge-{forgeVersion}");
            return Directory.Exists(forgePath);
        }

        private async Task InstallJavaAsync()
        {
            string javaUrl = "https://github.com/adoptium/temurin17-binaries/releases/download/jdk-17.0.8+7/OpenJDK17U-jre_x64_windows_hotspot_17.0.8_7.msi";
            string javaInstallerPath = Path.Combine(Path.GetTempPath(), "temurin-jre17.msi");

            ReportStatus("Downloading Java...");
            using (HttpClient client = new HttpClient())
            using (var stream = await client.GetStreamAsync(javaUrl))
            using (var file = File.Create(javaInstallerPath))
            {
                await stream.CopyToAsync(file);
            }
            ReportStatus("Java download completed.");

            ReportStatus("Installing Java silently...");
            var javaInstall = Process.Start(new ProcessStartInfo
            {
                FileName = "msiexec",
                Arguments = $"/i \"{javaInstallerPath}\" /quiet /norestart",
                UseShellExecute = false
            });
            if (javaInstall == null)
                throw new Exception("Failed to start Java installer process.");

            javaInstall.WaitForExit();

            if (javaInstall.ExitCode != 0)
                throw new Exception("Java installation failed.");
            ReportStatus("Java installed successfully.");
        }

        private async Task InstallForgeAsync()
        {
            string forgeUrl = "https://files.minecraftforge.net/maven/net/minecraftforge/forge/1.21-47.0.0/forge-1.21-47.0.0-installer.jar";
            string forgeInstallerPath = Path.Combine(Path.GetTempPath(), "forge-installer.jar");

            ReportStatus("Downloading Forge 1.21 installer...");
            using (HttpClient client = new HttpClient())
            using (var stream = await client.GetStreamAsync(forgeUrl))
            using (var file = File.Create(forgeInstallerPath))
            {
                await stream.CopyToAsync(file);
            }
            ReportStatus("Forge download completed.");

            System.Windows.Forms.MessageBox.Show(
                "Please pick Forge 1.21 Client install in the installer window that will appear next.",
                "Forge Installer",
                System.Windows.Forms.MessageBoxButtons.OK,
                System.Windows.Forms.MessageBoxIcon.Information);

            ReportStatus("Launching Forge installer GUI for client install...");
            var forgeInstall = Process.Start(new ProcessStartInfo
            {
                FileName = "java",
                Arguments = $"-jar \"{forgeInstallerPath}\"",
                UseShellExecute = true,
                WorkingDirectory = Path.GetTempPath()
            });

            if (forgeInstall == null)
                throw new Exception("Failed to launch Forge installer.");
        }

    }
}
