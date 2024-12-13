using YoutubeDownloader.Core.Downloading;
using YoutubeDownloader.Core.Resolving;
using YoutubeExplode.Videos.Streams;
using YoutubeDownloader.Core.Tagging;
using YoutubeExplode.Videos;

namespace VideoTelegramBot;

public class Downloader
{
    public async Task<IEnumerable<IVideo>> ResolveUrl(Uri url, CancellationToken cancellationToken)
    {
        try
        {
            return (await new QueryResolver().ResolveAsync(
                url.ToString().Split(
                    "\n",
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
                ), cancellationToken:cancellationToken)).Videos;
        }
        catch
        {
            return Enumerable.Empty<IVideo>();
        }
    }
    public async Task<string[]> DownloadVideosToOutputDirectory(IEnumerable<IVideo> videos, int maxVideoCount, string destinationDirectory, CancellationToken cancellationToken)
    {
        var downloader = new VideoDownloader();
        var downloads = new List<string>();
        foreach (var video in videos)
        {
            var download = new Download()
            {
                VideoDownloadPreference = new VideoDownloadPreference(Container.Mp4, VideoQualityPreference.Highest),
                Video = video
            };

            download.VideoDownloadOption = await downloader.GetBestDownloadOptionAsync(
                    download.Video!.Id,
                    download.VideoDownloadPreference,
                    cancellationToken
                );

            download.FilePath = Path.Combine(destinationDirectory, Guid.NewGuid().ToString() + '.' + download.VideoDownloadOption.Container.Name);
            File.Create(download.FilePath).Close();

            await downloader.DownloadVideoAsync(
                download.FilePath!,
                download.Video!,
                download.VideoDownloadOption,
                true,
                cancellationToken: cancellationToken
            );

            try
            {
                await new MediaTagInjector().InjectTagsAsync(
                    download.FilePath!,
                    download.Video!,
                    cancellationToken
                );
            }
            catch
            {
                // Media tagging is not critical
            }

            downloads.Add(download.FilePath);
        }

        return downloads.ToArray();
    }
}
