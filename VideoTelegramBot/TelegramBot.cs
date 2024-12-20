using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using YoutubeDownloader.Core.Downloading;
using YoutubeExplode.Videos.Streams;

namespace VideoTelegramBot;

public class TelegramBot
{
    private const char _argsSpliter = ' ';
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
            VideoQualityPreference.UpTo1080p,
        ];

    public async Task RunAsync(string baseUrl, string apiKey, TimeSpan pollPeriod, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(baseUrl) || string.IsNullOrEmpty(apiKey))
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
                                args.Where(a => a != arg),
                                cancellationToken);
                }
            }
        }
    }

    private async Task DownloadVideoAndSendLink(TelegramBotClient telegramBot, long chatId, int messageId, Uri videoUrl, string baseUrlPrefix, IEnumerable<string> args, CancellationToken cancellationToken)
    {
        try
        {
            var outputDirectory = "wwwroot";
            var outputPath = Path.Combine(AppContext.BaseDirectory, outputDirectory);
            var downloader = new Downloader();

            var quality = VideoQualityPreference.UpTo720p;
            var container = Container.Mp4;

            foreach (var arg in args) {
                foreach(var option in Qualities)
                    quality = option.ToString().ToLower().Contains(arg.ToLower()) ? option : quality;
                foreach(var option in Containers)
                    container = option.ToString().ToLower().Contains(arg.ToLower()) ? option : container;
            }

            var resolveTask = downloader.ResolveUrl(videoUrl, cancellationToken);

            while (!resolveTask.IsCompleted)
            {
                await telegramBot.SendChatAction(chatId, ChatAction.Typing);
                await Task.Delay(3000);
            }

            if (!resolveTask.Result.Any())
            {
                await telegramBot.SendMessage(chatId, "Nothing found!");
                return;
            }


            foreach (var video in resolveTask.Result)
                {
                    var downloadTask = downloader.DownloadVideoToOutputDirectory(video, quality, container, outputPath, cancellationToken);

                    while (!downloadTask.IsCompleted)
                    {
                        await telegramBot.SendChatAction(chatId, ChatAction.UploadVideo);
                        await Task.Delay(3000);
                    }

                var filePath = downloadTask.Result;

                    await telegramBot.SendMessage(
                        chatId,
                        $"{video.Title}\n {Path.Combine(baseUrlPrefix, filePath.Replace(outputPath, ".")).Replace('\\', '/')}",
                        replyParameters: new ReplyParameters() { MessageId = messageId });
                }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }
}

