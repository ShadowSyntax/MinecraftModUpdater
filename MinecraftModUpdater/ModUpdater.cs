using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using SharpCompress.Archives;
using SharpCompress.Archives.Rar;
using SharpCompress.Common;

namespace MinecraftModUpdater
{
    public class ModUpdater : IDisposable
    {
        private readonly HttpClient httpClient;

        public event EventHandler<int> ProgressChanged;
        public event EventHandler<string> StatusChanged;

        private string MinecraftModsPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            ".minecraft", "mods");

        public ModUpdater()
        {
            httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "MinecraftModUpdater/1.0");
            httpClient.Timeout = TimeSpan.FromMinutes(10);
        }

        // This is the method your form is trying to call
        public async Task UpdateModsAsync()
        {
            await InstallModsFromDesktopAsync();
        }

        public async Task InstallModsFromDesktopAsync()
        {
            try
            {
                OnStatusChanged("Looking for Modpack.rar on desktop...");
                OnProgressChanged(10);

                // Look specifically for Modpack.rar on desktop
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string modpackPath = Path.Combine(desktopPath, "Modpack.rar");

                if (!File.Exists(modpackPath))
                {
                    throw new Exception("Modpack.rar not found on desktop. Please place the modpack file on your desktop.");
                }

                OnStatusChanged("Found Modpack.rar on desktop.");
                OnProgressChanged(20);

                // Ensure mods folder exists
                EnsureModsFolderExists();
                OnProgressChanged(30);

                // Extract and install mods
                await ExtractAndInstallModsFromRarAsync(modpackPath);
                OnProgressChanged(100);

                OnStatusChanged("Mod installation completed successfully!");
                OnStatusChanged($"Mods installed to: {MinecraftModsPath}");
            }
            catch (Exception ex)
            {
                OnStatusChanged($"Installation failed: {ex.Message}");
                throw;
            }
        }

        private void EnsureModsFolderExists()
        {
            if (!Directory.Exists(MinecraftModsPath))
            {
                Directory.CreateDirectory(MinecraftModsPath);
                OnStatusChanged($".minecraft/mods folder created at: {MinecraftModsPath}");
            }
            else
            {
                OnStatusChanged($".minecraft/mods folder found at: {MinecraftModsPath}");
            }
        }

        private async Task ExtractAndInstallModsFromRarAsync(string rarFilePath)
        {
            OnStatusChanged("Extracting mods from Modpack.rar...");

            // Validate RAR file
            bool isValidRar = ValidateRarFile(rarFilePath);
            if (!isValidRar)
            {
                OnStatusChanged("Signature validation failed, attempting to open file anyway...");
                // Don't throw exception yet, try to open the file directly
            }
            else
            {
                OnStatusChanged("RAR file signature validated successfully.");
            }

            try
            {
                using (var archive = RarArchive.Open(rarFilePath))
                {
                    var installed = 0;
                    var skipped = 0;
                    var jarEntries = archive.Entries.Where(e => !e.IsDirectory &&
                        e.Key.EndsWith(".jar", StringComparison.OrdinalIgnoreCase)).ToList();

                    if (jarEntries.Count == 0)
                    {
                        throw new Exception("No .jar mod files found in the RAR archive.");
                    }

                    OnStatusChanged($"Found {jarEntries.Count} mod file(s) in archive.");

                    for (int i = 0; i < jarEntries.Count; i++)
                    {
                        var entry = jarEntries[i];
                        string modName = Path.GetFileName(entry.Key);
                        string destinationPath = Path.Combine(MinecraftModsPath, modName);

                        if (File.Exists(destinationPath))
                        {
                            OnStatusChanged($"Skipped (already exists): {modName}");
                            skipped++;
                        }
                        else
                        {
                            try
                            {
                                entry.WriteToFile(destinationPath, new ExtractionOptions { Overwrite = false });
                                OnStatusChanged($"Installed: {modName}");
                                installed++;
                            }
                            catch (Exception ex)
                            {
                                OnStatusChanged($"Failed to install {modName}: {ex.Message}");
                            }
                        }

                        // Update progress (30% to 95%)
                        var progress = 30 + (int)((i + 1) * 65.0 / jarEntries.Count);
                        OnProgressChanged(progress);

                        await Task.Delay(50); // Small delay to prevent UI freezing
                    }

                    OnStatusChanged($"Installation complete! Installed: {installed}, Skipped: {skipped}");
                }
            }
            catch (Exception ex)
            {
                OnStatusChanged($"Failed to open or extract RAR file: {ex.Message}");
                throw new Exception($"Could not process RAR file: {ex.Message}. The file may be corrupted, password-protected, or in an unsupported format.");
            }
        }

        private bool ValidateRarFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    OnStatusChanged("RAR file does not exist.");
                    return false;
                }

                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    var buffer = new byte[8];
                    int bytesRead = fs.Read(buffer, 0, 8);

                    if (bytesRead < 7)
                    {
                        OnStatusChanged($"File too small to be a valid RAR file (only {bytesRead} bytes read).");
                        return false;
                    }

                    // Log the actual file signature for debugging
                    string signature = string.Join(" ", buffer.Take(8).Select(b => $"0x{b:X2}"));
                    OnStatusChanged($"File signature: {signature}");

                    // Check for RAR 4.x signature "Rar!\x1a\x07\x00"
                    bool isRar4 = buffer[0] == 0x52 && buffer[1] == 0x61 && buffer[2] == 0x72 &&
                                  buffer[3] == 0x21 && buffer[4] == 0x1A && buffer[5] == 0x07 &&
                                  buffer[6] == 0x00;

                    // Check for RAR 5.x signature "Rar!\x1a\x07\x01"
                    bool isRar5 = buffer[0] == 0x52 && buffer[1] == 0x61 && buffer[2] == 0x72 &&
                                  buffer[3] == 0x21 && buffer[4] == 0x1A && buffer[5] == 0x07 &&
                                  buffer[6] == 0x01;

                    if (isRar4)
                    {
                        OnStatusChanged("Detected RAR 4.x format.");
                        return true;
                    }
                    else if (isRar5)
                    {
                        OnStatusChanged("Detected RAR 5.x format.");
                        return true;
                    }
                    else
                    {
                        OnStatusChanged("File signature does not match RAR format.");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                OnStatusChanged($"Error validating RAR file: {ex.Message}");
                return false;
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