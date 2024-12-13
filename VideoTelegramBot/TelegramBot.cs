using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace VideoTelegramBot;

public class TelegramBot
{
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

                    if (update.Message.Type == MessageType.Text &&
                        !string.IsNullOrEmpty(update.Message.Text) &&
                        update.Message.Text != "/start" &&
                        Uri.TryCreate(update.Message.Text, UriKind.Absolute, out var uri))
                            DownloadVideoAndReturnLink(
                                telegramBot,
                                update.Message.Chat.Id,
                                update.Message.MessageId,
                                uri,
                                baseUrl,
                                cancellationToken);
                }
            }
        }
    }

    private async Task DownloadVideoAndReturnLink(TelegramBotClient telegramBot, long chatId, int messageId, Uri videoUrl, string baseUrlPrefix, CancellationToken cancellationToken)
    {
        try
        {
            var outputDirectory = "wwwroot";
            var outputPath = Path.Combine(AppContext.BaseDirectory, outputDirectory);
            var downloader = new Downloader();
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

            var downloadTask = downloader.DownloadVideosToOutputDirectory(resolveTask.Result, 10, outputPath, cancellationToken);

            while (!downloadTask.IsCompleted)
            {
                await telegramBot.SendChatAction(chatId, ChatAction.UploadVideo);
                await Task.Delay(3000);
            }

            var filePaths = downloadTask.Result;

            foreach (var path in filePaths)
                await telegramBot.SendMessage(
                    chatId,
                    Path.Combine(baseUrlPrefix, path.Replace(outputPath, ".")).Replace('\\', '/'),
                    replyParameters: new Telegram.Bot.Types.ReplyParameters() { MessageId = messageId });
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }
}

