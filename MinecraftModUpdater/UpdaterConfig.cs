using System;
using System.IO;

namespace MinecraftModUpdater
{
    public class UpdaterConfig
    {
        public string GitHubRepoUrl { get; set; } = "https://github.com/ShadowSyntax/MinecraftModUpdater.git";

        public string MinecraftModsPath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft", "mods");

        public string TempDownloadPath { get; set; } = Path.Combine(Path.GetTempPath(), "minecraft_mod_update");

        public string ModpackRarUrl { get; set; } = "https://github.com/ShadowSyntax/MinecraftModUpdater/raw/master/Modpack.rar";

        public string LocalModpackRarPath => Path.Combine(TempDownloadPath, "Modpack.rar");

        public void EnsureTempFolderExists()
        {
            if (!Directory.Exists(TempDownloadPath))
            {
                Directory.CreateDirectory(TempDownloadPath);
            }
        }
    }
}
