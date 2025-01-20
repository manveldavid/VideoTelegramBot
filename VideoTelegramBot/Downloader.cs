using YoutubeDownloader.Core.Downloading;
using YoutubeDownloader.Core.Resolving;
using YoutubeExplode.Videos.Streams;
using YoutubeDownloader.Core.Tagging;
using YoutubeExplode.Videos;
using System.Net;

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
    public async Task<string> DownloadVideoToOutputDirectory(IVideo video, VideoQualityPreference quality, Container container, string destinationDirectory, CancellationToken cancellationToken)
    {
        CookieContainer cookieContainer = new CookieContainer();
        HttpClientHandler handler = new HttpClientHandler() { CookieContainer = cookieContainer };

        using (var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromHours(1)})
            httpClient.GetAsync("https://www.youtube.com/", cancellationToken);

        var downloader = new VideoDownloader(cookieContainer.GetAllCookies().ToList().AsReadOnly());

        var download = new Download()
        {
            VideoDownloadPreference = new VideoDownloadPreference(container, quality),
            Video = video
        };

        download.VideoDownloadOption = await downloader.GetBestDownloadOptionAsync(
                download.Video!.Id,
                download.VideoDownloadPreference,
                false,
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

        return download.FilePath ?? string.Empty;
    }
}
