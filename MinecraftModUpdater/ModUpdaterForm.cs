using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Http;
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

            this.Icon = new Icon("minecraft.ico");  

            modUpdater = new ModUpdater();
            modUpdater.ProgressChanged += OnProgressChanged;
            modUpdater.StatusChanged += OnStatusChanged;

            installJavaForgeButton.Click += async (s, e) => await OnInstallJavaForgeButtonClicked();
            adminInstallButton.Click += new EventHandler(OnAdminInstallButtonClicked);
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
                string destinationFolder = @"C:\test"; //update to my file path

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

                if (!Directory.Exists(destinationFolder))
                    Directory.CreateDirectory(destinationFolder);

                using (var archive = RarArchive.Open(tempDownloadPath))
                {
                    foreach (var entry in archive.Entries)
                    {
                        if (!entry.IsDirectory)
                        {
                            entry.WriteToDirectory(destinationFolder, new ExtractionOptions()
                            {
                                ExtractFullPath = true,
                                Overwrite = true
                            });
                        }
                    }
                }

                LogToConsole($"Mods extracted to: {destinationFolder}");
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
