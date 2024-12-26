using AngleSharp.Dom;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using YoutubeDownloader.Core.Downloading;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

namespace VideoTelegramBot;

public class TelegramBot
{
    private const char _argsSpliter = ' ';
    private CancellationTokenSource DownloadingCancellationTokenSource { get; set; } = new();
    private Container[] Containers { get; } = 
        [
            Container.Mp3,
            Container.Mp4,
            Container.Tgpp,
            Container.WebM
        ];
    private VideoQualityPreference[] Qualities { get; } =
        [
            VideoQualityPreference.Lowest,
            VideoQualityPreference.Highest,
            VideoQualityPreference.UpTo360p,
            VideoQualityPreference.UpTo480p,
            VideoQualityPreference.UpTo720p, 
            VideoQualityPreference.UpTo1080p
        ];

    public async Task RunAsync(string baseUrl, string baseSecureUrl, string apiKey, TimeSpan pollPeriod, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(baseUrl) || 
            string.IsNullOrEmpty(baseSecureUrl) || 
            string.IsNullOrEmpty(apiKey))
            return;

        var offset = 0;
        var telegramBot = new TelegramBotClient(apiKey);

        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(pollPeriod, cancellationToken);

            Update[] updates = Array.Empty<Update>();
            try
            {
                updates = await telegramBot.GetUpdates(offset, timeout:(int)pollPeriod.TotalSeconds, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            if (!cancellationToken.IsCancellationRequested)
            {
                foreach (var update in updates)
                {
                    offset = update.Id + 1;

                    if (update is null || update.Message is null)
                        continue;

                    if(update.Message.Text == "/stop")
                    {
                        DownloadingCancellationTokenSource.Cancel();
                        DownloadingCancellationTokenSource.Dispose();
                        DownloadingCancellationTokenSource = new CancellationTokenSource();
                        Console.WriteLine("all downloading stopped");
                        continue;
                    }

                    if (update.Message.Type != MessageType.Text ||
                        string.IsNullOrEmpty(update.Message.Text) ||
                        update.Message.Text == "/start")
                        continue;

                    var args = update.Message.Text.Split(_argsSpliter);

                    foreach (var arg in args)
                        if (Uri.TryCreate(arg, UriKind.Absolute, out var uri))
                            DownloadVideoAndSendLink(
                                telegramBot,
                                update.Message.Chat.Id,
                                update.Message.MessageId,
                                uri,
                                baseUrl,
                                baseSecureUrl,
                                args.Where(a => a != arg),
                                DownloadingCancellationTokenSource.Token);
                }
            }
        }
    }

    private async Task DownloadVideoAndSendLink(TelegramBotClient telegramBot, long chatId, int messageId, Uri videoUrl, string baseUrlPrefix, string baseSecureUrlPrefix, IEnumerable<string> args, CancellationToken cancellationToken)
    {
        try
        {
            var outputDirectory = "wwwroot";
            var outputPath = Path.Combine(AppContext.BaseDirectory, outputDirectory);
            var downloader = new Downloader();

            var quality = VideoQualityPreference.UpTo720p;
            var container = Container.Mp4;
            var startPlaylistIndex = 0;
            var endPlaylistIndex = int.MaxValue;

            foreach (var arg in args) {
                foreach(var option in Qualities)
                    quality = option.ToString().ToLower().Contains(arg.ToLower()) ? option : quality;
                foreach(var option in Containers)
                    container = option.ToString().ToLower().Contains(arg.ToLower()) ? option : container;

                if(arg.Contains('[') && arg.Contains(']'))
                {
                    var range = arg.Replace("[", string.Empty).Replace("]", string.Empty).Split('-');
                    startPlaylistIndex = range.FirstOrDefault() is not null ? int.TryParse(range.First(), out var parsedStartIndex) ? parsedStartIndex : startPlaylistIndex : startPlaylistIndex;
                    endPlaylistIndex = range.LastOrDefault() is not null ? int.TryParse(range.Last(), out var parsedEndIndex) ? parsedEndIndex : endPlaylistIndex : endPlaylistIndex;
                }
            }

            var replyMessage = await telegramBot.SendMessage(
                    chatId,
                    $"Resolving url ...",
                    replyParameters: new ReplyParameters() { MessageId = messageId });

            var videos = (await downloader.ResolveUrl(videoUrl, cancellationToken)).ToList();

            if (!videos.Any())
            {
                await telegramBot.EditMessageText(chatId, replyMessage.Id, "Nothing found :(");
                return;
            }

            var videoCount = (startPlaylistIndex == 0 && endPlaylistIndex == int.MaxValue ? videos.Count : endPlaylistIndex - startPlaylistIndex + 1);

            Console.WriteLine($"downloading {videoUrl} ({container},{quality}) {videoCount} video");
            replyMessage = await telegramBot.EditMessageText(
                    chatId,
                    replyMessage.Id,
                    $"Downloading {videoCount} video ...");

            var replyMessageText = string.Empty;
            var index = startPlaylistIndex;

            foreach (var video in videos.Where(v => videos.IndexOf(v) >= startPlaylistIndex && videos.IndexOf(v) <= endPlaylistIndex))
            {
                var videoIndex = (startPlaylistIndex == endPlaylistIndex || videos.Count == 1 ? string.Empty : $"[{index++}] ");
                replyMessage = await telegramBot.EditMessageText(
                    chatId,
                    replyMessage.Id,
                    $"{replyMessageText}\n\n{videoIndex}{video.Title}\nDownloading...",
                    ParseMode.Html);

                try
                {
                    var filePath = await downloader.DownloadVideoToOutputDirectory(video, quality, container, outputPath, cancellationToken);

                    replyMessageText = $"{replyMessageText}\n\n{videoIndex}{video.Title}\n<a href='{Path.Combine(baseUrlPrefix, filePath.Replace(outputPath, ".")).Replace('\\', '/')}'>http</a> or <a href='{Path.Combine(baseSecureUrlPrefix, filePath.Replace(outputPath, ".")).Replace('\\', '/')}'>https</a>";
                    replyMessage = await telegramBot.EditMessageText(
                        chatId,
                        replyMessage.Id,
                        replyMessageText, 
                        ParseMode.Html);
                }
                catch (Exception ex) 
                {
                    replyMessage = await telegramBot.EditMessageText(
                    chatId,
                    replyMessage.Id,
                    $"{replyMessageText}\n\n{video.Title}\n\nStopped", ParseMode.Html);

                    throw ex;
                }
            }
        }
        catch (AggregateException) { }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }
}

