using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace VideoTelegramBot;

public class TelegramBot
{
    public async Task RunAsync(string baseUrl, string apiKey, TimeSpan pollPeriod, CancellationToken cancellationToken)
    {
        var offset = 0;
        var outputDirectory = "wwwroot";
        var outputPath = Path.Combine(AppContext.BaseDirectory, outputDirectory);
        var telegramBot = new TelegramBotClient(apiKey);
        var downloader = new Downloader();

        if (!Directory.Exists(outputPath))
            Directory.CreateDirectory(outputPath);

        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(pollPeriod, cancellationToken);

            try
            {
                var updates = await telegramBot.GetUpdates(offset);

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
                        {
                            var resolveTask = downloader.ResolveUrl(uri);

                            while (!resolveTask.IsCompleted)
                            {
                                await telegramBot.SendChatAction(update.Message.Chat, ChatAction.Typing);
                                await Task.Delay(3000);
                            }

                            if (!resolveTask.Result.Any())
                            {
                                await telegramBot.SendMessage(update.Message.Chat, "Nothing found!");
                                continue;
                            }

                            var downloadTask = downloader.DownloadVideosToOutputDirectory(resolveTask.Result, 10, outputPath);

                            while (!downloadTask.IsCompleted)
                            {
                                await telegramBot.SendChatAction(update.Message.Chat, ChatAction.UploadVideo);
                                await Task.Delay(3000);
                            }

                            var filePaths = downloadTask.Result;

                            foreach (var path in filePaths)
                                await telegramBot.SendMessage(
                                    update.Message.Chat, 
                                    Path.Combine(baseUrl, path.Replace(outputPath, ".")).Replace('\\', '/'), 
                                    replyParameters: new Telegram.Bot.Types.ReplyParameters() { MessageId=update.Message.MessageId});
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}

