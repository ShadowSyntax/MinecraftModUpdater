using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;

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
                if (process?.ExitCode == 0)
                {
                    ReportStatus("Java found in system PATH.");
                    return true;
                }

                string[] commonJavaPaths = {
                    @"C:\Program Files\Eclipse Adoptium",
                    @"C:\Program Files\Java",
                    @"C:\Program Files (x86)\Java"
                };

                foreach (string basePath in commonJavaPaths)
                {
                    if (Directory.Exists(basePath))
                    {
                        var javaDirs = Directory.GetDirectories(basePath, "*jre*", SearchOption.TopDirectoryOnly)
                                              .Concat(Directory.GetDirectories(basePath, "*jdk*", SearchOption.TopDirectoryOnly));

                        foreach (string javaDir in javaDirs)
                        {
                            string javaExe = Path.Combine(javaDir, "bin", "java.exe");
                            if (File.Exists(javaExe))
                            {
                                ReportStatus($"Java found at: {javaExe}");
                                return true;
                            }
                        }
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private bool IsForgeInstalled(string forgeVersion)
        {
            try
            {
                string versionsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft", "versions");
                string forgePath = Path.Combine(versionsDir, $"forge-{forgeVersion}");
                bool isInstalled = Directory.Exists(forgePath);

                if (isInstalled)
                {
                    ReportStatus($"Forge {forgeVersion} found at: {forgePath}");
                }

                return isInstalled;
            }
            catch
            {
                return false;
            }
        }

        private async Task<string> DownloadFileAsync(string url, string fileName, string description)
        {
            string filePath = Path.Combine(Path.GetTempPath(), fileName);

            ReportStatus($"Downloading {description}...");

            using (HttpClient client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromMinutes(10); 

                try
                {
                    using var response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode();

                    using var stream = await response.Content.ReadAsStreamAsync();
                    using var file = File.Create(filePath);
                    await stream.CopyToAsync(file);

                    ReportStatus($"{description} download completed.");
                }
                catch (HttpRequestException ex)
                {
                    throw new Exception($"Failed to download {description}: {ex.Message}");
                }
                catch (TaskCanceledException ex)
                {
                    throw new Exception($"Download of {description} timed out: {ex.Message}");
                }
            }

            return filePath;
        }

        private async Task InstallJavaAsync()
        {
            string javaUrl = "https://github.com/adoptium/temurin17-binaries/releases/download/jdk-17.0.8+7/OpenJDK17U-jre_x64_windows_hotspot_17.0.8_7.msi";

            string javaInstallerPath = await DownloadFileAsync(javaUrl, "temurin-jre17.msi", "Java JRE 17");

            ReportStatus("Installing Java silently...");
            var javaInstall = Process.Start(new ProcessStartInfo
            {
                FileName = "msiexec",
                Arguments = $"/i \"{javaInstallerPath}\" /quiet /norestart",
                UseShellExecute = false,
                CreateNoWindow = true
            });

            if (javaInstall == null)
                throw new Exception("Failed to start Java installer process.");

            javaInstall.WaitForExit();

            if (javaInstall.ExitCode != 0)
            {
                throw new Exception($"Java installation failed with exit code: {javaInstall.ExitCode}");
            }

            ReportStatus("Java installed successfully.");

            try
            {
                File.Delete(javaInstallerPath);
                ReportStatus("Cleaned up Java installer file.");
            }
            catch
            {

            }
        }

        private async Task InstallForgeAsync()
        {
            // Forge 1.12.2 - Latest recommended version (14.23.5.2859)
            string forgeUrl = "https://maven.minecraftforge.net/net/minecraftforge/forge/1.12.2-14.23.5.2859/forge-1.12.2-14.23.5.2859-installer.jar";
            string forgeInstallerPath = await DownloadFileAsync(forgeUrl, "forge-1.12.2-installer.jar", "Forge 1.12.2 installer");

            System.Windows.Forms.MessageBox.Show(
                "The Forge 1.12.2 installer will now open.\n\n" +
                "Please:\n" +
                "1. Select 'Install client'\n" +
                "2. Keep the default Minecraft directory\n" +
                "3. Click 'OK' to install\n" +
                "4. Wait for installation to complete\n\n" +
                "Note: This will install Forge for Minecraft 1.12.2\n\n" +
                "Click OK here to continue...",
                "Forge 1.12.2 Installer Instructions",
                System.Windows.Forms.MessageBoxButtons.OK,
                System.Windows.Forms.MessageBoxIcon.Information);

            ReportStatus("Launching Forge 1.12.2 installer GUI for client install...");

            var forgeInstall = Process.Start(new ProcessStartInfo
            {
                FileName = "java",
                Arguments = $"-jar \"{forgeInstallerPath}\"",
                UseShellExecute = true,
                WorkingDirectory = Path.GetTempPath()
            });

            if (forgeInstall == null)
                throw new Exception("Failed to launch Forge installer.");

            ReportStatus("Waiting for Forge 1.12.2 installation to complete...");
            ReportStatus("Please complete the installation in the Forge installer window...");

            forgeInstall.WaitForExit();

            if (forgeInstall.ExitCode == 0)
            {
                ReportStatus("Forge 1.12.2 installation completed successfully.");
                ReportStatus("You can now launch Minecraft with the 'forge' profile.");
            }
            else
            {
                ReportStatus($"Forge installer exited with code: {forgeInstall.ExitCode}");
                ReportStatus("Installation may have failed or was cancelled by user.");
            }

            // Clean up installer file
            try
            {
                File.Delete(forgeInstallerPath);
                ReportStatus("Cleaned up Forge installer file.");
            }
            catch (Exception ex)
            {
                ReportStatus($"Warning: Could not delete installer file - {ex.Message}");
            }
        }
    }
}