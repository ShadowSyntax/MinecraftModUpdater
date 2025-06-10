namespace MinecraftModUpdater
{
    // GitHub API response classes
    public class GitHubRelease
    {
        public string tag_name { get; set; }
        public string name { get; set; }
        public GitHubAsset[] assets { get; set; }
    }

    public class GitHubAsset
    {
        public string name { get; set; }
        public string browser_download_url { get; set; }
        public long size { get; set; }
    }
}