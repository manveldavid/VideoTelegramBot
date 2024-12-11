using YoutubeDownloader.Core.Downloading;
using YoutubeExplode.Videos;

namespace VideoTelegramBot;

public class Download
{
    public string? FilePath { get; set; }
    public string? FileName => Path.GetFileName(FilePath);
    public IVideo Video { get; set; }
    public VideoDownloadPreference VideoDownloadPreference { get; set; }
    public VideoDownloadOption VideoDownloadOption { get; set; }
}
