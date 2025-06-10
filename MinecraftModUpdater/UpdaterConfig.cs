using System;
using System.IO;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace MinecraftModUpdater
{
    public class UpdaterConfig
    {
        public string GitHubRepoUrl { get; set; } = "https://github.com/ShadowSyntax/MinecraftModUpdater.git";
        public string MinecraftModsPath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft", "mods");
        public string TempDownloadPath { get; set; } = Path.Combine(Path.GetTempPath(), "minecraft_mod_update");

        // Base URL for modpack parts
        public string ModpackBaseUrl { get; set; } = "https://github.com/ShadowSyntax/MinecraftModUpdater/raw/master/";

        // Pattern for modpack part files
        public string ModpackPartPattern { get; set; } = "Modpack.part{0}.rar";

        public void EnsureTempFolderExists()
        {
            if (!Directory.Exists(TempDownloadPath))
            {
                Directory.CreateDirectory(TempDownloadPath);
            }
        }

        /// <summary>
        /// Gets the local path for a specific modpack part
        /// </summary>
        public string GetLocalModpackPartPath(int partNumber)
        {
            string fileName = string.Format(ModpackPartPattern, partNumber);
            return Path.Combine(TempDownloadPath, fileName);
        }

        /// <summary>
        /// Gets the download URL for a specific modpack part
        /// </summary>
        public string GetModpackPartUrl(int partNumber)
        {
            string fileName = string.Format(ModpackPartPattern, partNumber);
            return ModpackBaseUrl + fileName;
        }

        /// <summary>
        /// Discovers all available modpack parts by checking URLs
        /// </summary>
        public async Task<List<int>> DiscoverModpackPartsAsync()
        {
            var availableParts = new List<int>();

            using (var httpClient = new HttpClient())
            {
                // Set a reasonable timeout
                httpClient.Timeout = TimeSpan.FromSeconds(10);

                // Check for parts starting from 1
                for (int i = 1; i <= 20; i++) // Limit to 20 parts max to avoid infinite loop
                {
                    try
                    {
                        string url = GetModpackPartUrl(i);

                        // Send HEAD request to check if file exists without downloading
                        var response = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, url));

                        if (response.IsSuccessStatusCode)
                        {
                            availableParts.Add(i);
                            Console.WriteLine($"Found modpack part {i}");
                        }
                        else
                        {
                            // If we get a 404, assume no more parts exist
                            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                            {
                                break;
                            }
                        }
                    }
                    catch (HttpRequestException)
                    {
                        // Network error or timeout, assume no more parts
                        break;
                    }
                    catch (TaskCanceledException)
                    {
                        // Timeout, assume no more parts
                        break;
                    }
                }
            }

            return availableParts;
        }

        /// <summary>
        /// Gets all local modpack part file paths
        /// </summary>
        public List<string> GetAllLocalModpackParts(List<int> partNumbers)
        {
            var paths = new List<string>();
            foreach (int partNumber in partNumbers)
            {
                paths.Add(GetLocalModpackPartPath(partNumber));
            }
            return paths;
        }

        /// <summary>
        /// Cleans up all downloaded modpack parts
        /// </summary>
        public void CleanupModpackParts(List<int> partNumbers)
        {
            foreach (int partNumber in partNumbers)
            {
                try
                {
                    string filePath = GetLocalModpackPartPath(partNumber);
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Could not delete part {partNumber} - {ex.Message}");
                }
            }
        }
    }
}