using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using SharpCompress.Archives;
using SharpCompress.Archives.Rar;
using SharpCompress.Common;

namespace MinecraftModUpdater
{
    public partial class ModUpdaterForm : Form
    {
        private ModUpdater modUpdater;
        private JavaForgeInstaller? javaForgeInstaller;
        private readonly UpdaterConfig config = new UpdaterConfig();

        public ModUpdaterForm()
        {
            InitializeComponent();

            // Load embedded icon
            LoadEmbeddedIcon();

            modUpdater = new ModUpdater();
            modUpdater.ProgressChanged += OnProgressChanged;
            modUpdater.StatusChanged += OnStatusChanged;

            installJavaForgeButton.Click += async (s, e) => await OnInstallJavaForgeButtonClicked();
            adminInstallButton.Click += new EventHandler(OnAdminInstallButtonClicked);
        }

        private void LoadEmbeddedIcon()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();

                // Debug: List all embedded resources
                var resourceNames = assembly.GetManifestResourceNames();
                foreach (var name in resourceNames)
                {
                    System.Diagnostics.Debug.WriteLine($"Found resource: {name}");
                }

                using var stream = assembly.GetManifestResourceStream("MinecraftModUpdater.minecraft.ico");
                if (stream != null)
                {
                    System.Diagnostics.Debug.WriteLine("Icon stream loaded successfully");
                    this.Icon = new Icon(stream);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Icon stream is null - resource not found");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load embedded icon: {ex.Message}");
            }
        }

        private async void UpdateButton_Click(object sender, EventArgs e)
        {
            ClearConsole();
            await UpdateMods();
        }

        private async Task UpdateMods()
        {
            try
            {
                updateButton.Enabled = false;
                progressBar.Value = 0;
                await modUpdater.UpdateModsAsync();
            }
            catch (Exception ex)
            {
                LogToConsole($"Error: {ex.Message}");
                MessageBox.Show($"Update failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                updateButton.Enabled = true;
            }
        }

        private void OnProgressChanged(object sender, int progress)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => progressBar.Value = progress));
            }
            else
            {
                progressBar.Value = progress;
            }
        }

        private void OnStatusChanged(object sender, string status)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() =>
                {
                    statusLabel.Text = status;
                    LogToConsole(status);
                }));
            }
            else
            {
                statusLabel.Text = status;
                LogToConsole(status);
            }
        }

        private void LogToConsole(string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => LogToConsole(message)));
                return;
            }

            consoleOutput.AppendText($"[SHC] {message}\r\n");
            consoleOutput.ScrollToCaret();
        }

        private void ClearConsole()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(ClearConsole));
                return;
            }

            consoleOutput.Clear();
            consoleOutput.AppendText("=== Mod Updater Console ===\r\n");
        }

        private async Task OnInstallJavaForgeButtonClicked()
        {
            ClearConsole();
            try
            {
                installJavaForgeButton.Enabled = false;

                javaForgeInstaller = new JavaForgeInstaller();
                javaForgeInstaller.StatusUpdated += (s, status) =>
                {
                    if (InvokeRequired)
                    {
                        Invoke(new Action(() =>
                        {
                            statusLabel.Text = status;
                            LogToConsole(status);
                        }));
                    }
                    else
                    {
                        statusLabel.Text = status;
                        LogToConsole(status);
                    }
                };

                await javaForgeInstaller.InstallJavaAndForgeAsync();

                MessageBox.Show("Java and Forge installed successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                LogToConsole($"Error: {ex.Message}");
                MessageBox.Show($"Failed to install Java/Forge: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                installJavaForgeButton.Enabled = true;
            }
        }

        private async void OnAdminInstallButtonClicked(object? sender, EventArgs e)
        {
            ClearConsole();
            try
            {
                adminInstallButton.Enabled = false;
                statusLabel.Text = "Starting admin mod installation...";
                LogToConsole("Starting admin mod installation...");

                string modsFolder = @"C:\Users\Default.DESKTOP-R9B8UHT\Desktop\Miecraft server\mods";

                config.EnsureTempFolderExists();

                // Discover and download all modpack parts
                var availableParts = await DiscoverAndDownloadModPackPartsAsync();

                LogToConsole("All modpack parts downloaded. Extracting...");

                if (!Directory.Exists(modsFolder))
                    Directory.CreateDirectory(modsFolder);

                await ExtractModsFromPartsAsync(availableParts, modsFolder);

                LogToConsole($"Admin mod installation completed successfully!");
                LogToConsole($"Mods installed to: {modsFolder}");
                statusLabel.Text = "Admin mod installation complete.";

                // Cleanup downloaded parts
                CleanupDownloadedParts(availableParts);
            }
            catch (Exception ex)
            {
                LogToConsole($"Admin install failed: {ex.Message}");
                MessageBox.Show($"Admin install failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                adminInstallButton.Enabled = true;
            }
        }

        private async Task<List<int>> DiscoverAndDownloadModPackPartsAsync()
        {
            LogToConsole("Discovering modpack parts...");

            var availableParts = await DiscoverModpackPartsAsync();

            if (availableParts.Count == 0)
            {
                throw new Exception("No modpack parts found on GitHub repository.");
            }

            LogToConsole($"Found {availableParts.Count} modpack part(s) to download.");

            // Download all parts
            for (int i = 0; i < availableParts.Count; i++)
            {
                int partNumber = availableParts[i];
                await DownloadModPackPartAsync(partNumber, i + 1, availableParts.Count);
            }

            LogToConsole($"All {availableParts.Count} modpack parts downloaded successfully.");
            return availableParts;
        }

        private async Task<List<int>> DiscoverModpackPartsAsync()
        {
            var availableParts = new List<int>();

            using (var httpClient = new HttpClient())
            {
                httpClient.Timeout = TimeSpan.FromSeconds(10);

                // Check for parts starting from 1
                for (int i = 1; i <= 20; i++) // Limit to 20 parts max
                {
                    try
                    {
                        string url = config.GetModpackPartUrl(i);

                        // Send HEAD request to check if file exists
                        var response = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, url));

                        if (response.IsSuccessStatusCode)
                        {
                            availableParts.Add(i);
                            LogToConsole($"Found modpack part {i}");
                        }
                        else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                        {
                            break;
                        }
                    }
                    catch (HttpRequestException)
                    {
                        break;
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                }
            }

            return availableParts;
        }

        private async Task DownloadModPackPartAsync(int partNumber, int currentPart, int totalParts)
        {
            string url = config.GetModpackPartUrl(partNumber);
            string downloadPath = config.GetLocalModpackPartPath(partNumber);
            string fileName = Path.GetFileName(downloadPath);

            LogToConsole($"Downloading {fileName} ({currentPart}/{totalParts})...");

            using (var httpClient = new HttpClient())
            using (var response = await httpClient.GetAsync(url))
            {
                response.EnsureSuccessStatusCode();

                using (var stream = await response.Content.ReadAsStreamAsync())
                using (var fs = new FileStream(downloadPath, FileMode.Create))
                {
                    await stream.CopyToAsync(fs);
                }
            }

            LogToConsole($"Downloaded {fileName} successfully.");
        }

        private async Task ExtractModsFromPartsAsync(List<int> availableParts, string modsFolder)
        {
            // Get the first part path - RAR will automatically handle the other parts
            var firstPartPath = config.GetLocalModpackPartPath(availableParts.First());

            // Validate that all parts exist
            foreach (int partNumber in availableParts)
            {
                string partPath = config.GetLocalModpackPartPath(partNumber);
                if (!File.Exists(partPath))
                {
                    throw new FileNotFoundException($"Modpack part {partNumber} not found at: {partPath}");
                }
            }

            // Validate RAR signature on first part
            if (!ValidateRarFile(firstPartPath))
            {
                throw new Exception("Invalid RAR file signature. The downloaded file may be corrupted.");
            }

            LogToConsole($"Extracting multi-part RAR archive ({availableParts.Count} parts)...");

            using (var archive = RarArchive.Open(firstPartPath))
            {
                var installed = 0;
                var skipped = 0;
                var totalJarFiles = archive.Entries.Count(e => !e.IsDirectory && e.Key.EndsWith(".jar", StringComparison.OrdinalIgnoreCase));

                LogToConsole($"Found {totalJarFiles} mod files in archive.");

                foreach (var entry in archive.Entries)
                {
                    if (!entry.IsDirectory)
                    {
                        string fileName = Path.GetFileName(entry.Key);
                        string destinationFilePath = Path.Combine(modsFolder, fileName);

                        // Skip if the file already exists
                        if (File.Exists(destinationFilePath))
                        {
                            LogToConsole($"Skipped existing mod: {fileName}");
                            skipped++;
                            continue;
                        }

                        try
                        {
                            using (var entryStream = entry.OpenEntryStream())
                            using (var destinationStream = new FileStream(destinationFilePath, FileMode.CreateNew))
                            {
                                await entryStream.CopyToAsync(destinationStream);
                            }

                            LogToConsole($"Installed mod: {fileName}");
                            installed++;
                        }
                        catch (Exception ex)
                        {
                            LogToConsole($"Failed to install {fileName}: {ex.Message}");
                        }
                    }
                }

                if (installed == 0 && skipped > 0)
                {
                    LogToConsole("All mods are already installed. No new mods added.");
                }
                else if (installed == 0)
                {
                    throw new Exception("No mod files found in the archive.");
                }
                else
                {
                    LogToConsole($"Installed {installed} new mod(s). Skipped {skipped} already present.");
                }
            }
        }

        private bool ValidateRarFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath)) return false;

                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    var buffer = new byte[7];
                    if (fs.Read(buffer, 0, 7) < 7) return false;

                    // Check for RAR signature "Rar!\x1a\x07\x00"
                    return buffer[0] == 0x52 && buffer[1] == 0x61 && buffer[2] == 0x72 &&
                           buffer[3] == 0x21 && buffer[4] == 0x1A && buffer[5] == 0x07 &&
                           buffer[6] == 0x00;
                }
            }
            catch
            {
                return false;
            }
        }

        private void CleanupDownloadedParts(List<int> availableParts)
        {
            LogToConsole("Cleaning up downloaded parts...");

            foreach (int partNumber in availableParts)
            {
                try
                {
                    string partPath = config.GetLocalModpackPartPath(partNumber);
                    if (File.Exists(partPath))
                    {
                        File.Delete(partPath);
                    }
                }
                catch (Exception ex)
                {
                    LogToConsole($"Warning: Could not delete part {partNumber} - {ex.Message}");
                }
            }

            // Also try to clean up the temp directory if it's empty
            try
            {
                if (Directory.Exists(config.TempDownloadPath))
                {
                    var remainingFiles = Directory.GetFiles(config.TempDownloadPath);
                    if (remainingFiles.Length == 0)
                    {
                        Directory.Delete(config.TempDownloadPath);
                        LogToConsole("Temporary directory cleaned up.");
                    }
                }
            }
            catch (Exception ex)
            {
                LogToConsole($"Warning: Could not clean up temp directory - {ex.Message}");
            }
        }
    }
}