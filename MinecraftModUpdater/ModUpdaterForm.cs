using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
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

                using var stream = assembly.GetManifestResourceStream("MinecraftModUpdater.minecraftapp.ico");
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

                string downloadUrl = config.ModpackRarUrl;
                string tempDownloadPath = config.LocalModpackRarPath;
                string modsFolder = @"C:\Users\Default.DESKTOP-R9B8UHT\Desktop\Miecraft server\mods";

                config.EnsureTempFolderExists();

                using (HttpClient client = new HttpClient())
                using (var response = await client.GetAsync(downloadUrl))
                {
                    response.EnsureSuccessStatusCode();
                    using (var stream = await response.Content.ReadAsStreamAsync())
                    using (var fs = new FileStream(tempDownloadPath, FileMode.Create))
                    {
                        await stream.CopyToAsync(fs);
                    }
                }

                LogToConsole("Modpack downloaded. Extracting...");

                if (!Directory.Exists(modsFolder))
                    Directory.CreateDirectory(modsFolder);

                using (var archive = RarArchive.Open(tempDownloadPath))
                {
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
                                continue;
                            }

                            using (var entryStream = entry.OpenEntryStream())
                            using (var destinationStream = new FileStream(destinationFilePath, FileMode.CreateNew))
                            {
                                await entryStream.CopyToAsync(destinationStream);
                            }

                            LogToConsole($"Installed mod: {fileName}");
                        }
                    }
                }

                LogToConsole($"Mods installed to: {modsFolder}");
                statusLabel.Text = "Admin mod installation complete.";
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
    }
}