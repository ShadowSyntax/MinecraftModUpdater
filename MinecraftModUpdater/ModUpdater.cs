using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using SharpCompress.Archives;
using SharpCompress.Archives.Rar;
using SharpCompress.Common;

namespace MinecraftModUpdater
{
    public class ModUpdater : IDisposable
    {
        private readonly UpdaterConfig config;
        private readonly HttpClient httpClient;

        public event EventHandler<int> ProgressChanged;
        public event EventHandler<string> StatusChanged;

        public ModUpdater()
        {
            config = new UpdaterConfig();
            httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "MinecraftModUpdater/1.0");
        }

        public async Task UpdateModsAsync()
        {
            try
            {
                OnStatusChanged("Preparing mod update...");
                OnProgressChanged(5);

                config.EnsureTempFolderExists();
                EnsureModsFolderExists();

                await DownloadModPackAsync();
                OnProgressChanged(60);

                await InstallModsAsync();
                OnProgressChanged(90);

                CleanupTempFiles();
                OnProgressChanged(100);

                OnStatusChanged("Mod update completed successfully!");
                OnStatusChanged($"Mods installed to: {config.MinecraftModsPath}");
            }
            catch (Exception ex)
            {
                OnStatusChanged($"Update failed: {ex.Message}");
                throw;
            }
        }

        private void EnsureModsFolderExists()
        {
            if (!Directory.Exists(config.MinecraftModsPath))
            {
                Directory.CreateDirectory(config.MinecraftModsPath);
                OnStatusChanged($".minecraft/mods folder created at: {config.MinecraftModsPath}");
            }
            else
            {
                OnStatusChanged($".minecraft/mods folder found at: {config.MinecraftModsPath}");
            }
        }

        private async Task DownloadModPackAsync()
        {
            OnStatusChanged("Downloading mod pack...");

            var downloadPath = config.LocalModpackRarPath;

            using (var response = await httpClient.GetAsync(config.ModpackRarUrl, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();

                using (var contentStream = await response.Content.ReadAsStreamAsync())
                using (var fileStream = new FileStream(downloadPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    var totalBytes = response.Content.Headers.ContentLength ?? 0;
                    var totalBytesRead = 0L;
                    var buffer = new byte[8192];
                    int bytesRead;

                    while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesRead);
                        totalBytesRead += bytesRead;

                        if (totalBytes > 0)
                        {
                            var progress = (int)((totalBytesRead * 50) / totalBytes + 10); // 10-60%
                            OnProgressChanged(progress);
                        }
                    }
                }
            }

            OnStatusChanged("Mod pack downloaded successfully.");
        }

        private async Task InstallModsAsync()
        {
            OnStatusChanged("Installing new mods...");

            using (var archive = RarArchive.Open(config.LocalModpackRarPath))
            {
                var installed = 0;
                var skipped = 0;

                foreach (var entry in archive.Entries)
                {
                    if (!entry.IsDirectory && entry.Key.EndsWith(".jar", StringComparison.OrdinalIgnoreCase))
                    {
                        string modName = Path.GetFileName(entry.Key);
                        string destinationPath = Path.Combine(config.MinecraftModsPath, modName);

                        if (File.Exists(destinationPath))
                        {
                            OnStatusChanged($"Skipped (already exists): {modName}");
                            skipped++;
                            continue;
                        }

                        entry.WriteToFile(destinationPath, new ExtractionOptions { Overwrite = false });
                        OnStatusChanged($"Installed: {modName}");
                        installed++;

                        await Task.Delay(100);
                    }
                }

                if (installed == 0 && skipped > 0)
                {
                    OnStatusChanged("All mods are already installed. No new mods added.");
                }
                else if (installed == 0)
                {
                    throw new Exception("No .jar mod files found in the archive.");
                }
                else
                {
                    OnStatusChanged($"Installed {installed} new mod(s). Skipped {skipped} already present.");
                }
            }
        }

        private void CleanupTempFiles()
        {
            OnStatusChanged("Cleaning up temporary files...");
            if (Directory.Exists(config.TempDownloadPath))
            {
                Directory.Delete(config.TempDownloadPath, true);
            }
        }

        private void OnProgressChanged(int progress)
        {
            ProgressChanged?.Invoke(this, Math.Max(0, Math.Min(100, progress)));
        }

        private void OnStatusChanged(string status)
        {
            StatusChanged?.Invoke(this, status);
        }

        public void Dispose()
        {
            httpClient?.Dispose();
        }
    }
}
