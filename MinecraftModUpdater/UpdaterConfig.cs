using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace MinecraftModUpdater
{
    public class UpdaterConfig
    {
        public string GitHubRepoUrl { get; set; } = "https://github.com/ShadowSyntax/MinecraftModUpdater.git";
        public string MinecraftModsPath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft", "mods");
        public string TempDownloadPath { get; set; } = Path.Combine(Path.GetTempPath(), "minecraft_mod_update");

        // URL for the modpack rar file
        public string ModpackRarUrl { get; set; } = "https://github.com/ShadowSyntax/MinecraftModUpdater/raw/master/Modpack.rar";

        // Base URL for multi-part modpack files
        public string ModpackPartBaseUrl { get; set; } = "https://github.com/ShadowSyntax/MinecraftModUpdater/raw/master/modpack.part{0}.rar";

        // Local path for the downloaded rar file
        public string LocalModpackRarPath => Path.Combine(TempDownloadPath, "Modpack.rar");

        public void EnsureTempFolderExists()
        {
            if (!Directory.Exists(TempDownloadPath))
            {
                Directory.CreateDirectory(TempDownloadPath);
            }
        }

        /// <summary>
        /// Gets the URL for a specific modpack part
        /// </summary>
        /// <param name="partNumber">The part number (1, 2, 3, etc.)</param>
        /// <returns>The URL for the specified part</returns>
        public string GetModpackPartUrl(int partNumber)
        {
            return string.Format(ModpackPartBaseUrl, partNumber);
        }

        /// <summary>
        /// Gets the local file path for a specific modpack part
        /// </summary>
        /// <param name="partNumber">The part number (1, 2, 3, etc.)</param>
        /// <returns>The local file path for the specified part</returns>
        public string GetLocalModpackPartPath(int partNumber)
        {
            return Path.Combine(TempDownloadPath, $"modpack.part{partNumber}.rar");
        }

        /// <summary>
        /// Checks if the modpack rar file exists on GitHub
        /// </summary>
        public async Task<bool> CheckModpackExistsAsync()
        {
            using (var httpClient = new HttpClient())
            {
                try
                {
                    httpClient.Timeout = TimeSpan.FromSeconds(10);
                    var response = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, ModpackRarUrl));
                    return response.IsSuccessStatusCode;
                }
                catch
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Downloads the modpack rar file from GitHub
        /// </summary>
        public async Task DownloadModpackAsync(IProgress<int> progress = null)
        {
            EnsureTempFolderExists();

            using (var httpClient = new HttpClient())
            {
                httpClient.Timeout = TimeSpan.FromMinutes(10);

                using (var response = await httpClient.GetAsync(ModpackRarUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();

                    var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                    var downloadedBytes = 0L;

                    using (var contentStream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = new FileStream(LocalModpackRarPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                    {
                        var buffer = new byte[8192];
                        int bytesRead;

                        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, bytesRead);
                            downloadedBytes += bytesRead;

                            if (totalBytes > 0 && progress != null)
                            {
                                var progressPercentage = (int)((downloadedBytes * 100L) / totalBytes);
                                progress.Report(progressPercentage);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Cleans up the downloaded modpack rar file
        /// </summary>
        public void CleanupModpack()
        {
            try
            {
                if (File.Exists(LocalModpackRarPath))
                {
                    File.Delete(LocalModpackRarPath);
                }

                // Clean up temp directory if empty
                if (Directory.Exists(TempDownloadPath))
                {
                    var remainingFiles = Directory.GetFiles(TempDownloadPath);
                    if (remainingFiles.Length == 0)
                    {
                        Directory.Delete(TempDownloadPath);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not clean up files - {ex.Message}");
            }
        }
    }
}