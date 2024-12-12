namespace VideoTelegramBot
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var videoLifeTimeInSeconds = TimeSpan.FromSeconds(double.TryParse(Environment.GetEnvironmentVariable("VIDEO_LIFETIME_IN_SECONDS"), out var _videoLifeTimeInSeconds) ? _videoLifeTimeInSeconds : 14400d);
            var videoCollectorInpectPeriodInSeconds = TimeSpan.FromSeconds(double.TryParse(Environment.GetEnvironmentVariable("CHECK_VIDEO_LIFETIME_PERIOD_IN_SECONDS"), out var _videoCollectorInpectPeriodInSeconds) ? _videoCollectorInpectPeriodInSeconds : 3600d);
            var tgBotPollPeriodInSeconds = TimeSpan.FromSeconds(double.TryParse(Environment.GetEnvironmentVariable("TG_BOT_POLL_PERIOD_SECONDS"), out var _tgBotPollPeriodInSeconds) ? _tgBotPollPeriodInSeconds : 5d);
            var baseUrl = Environment.GetEnvironmentVariable("PUBLIC_URL")!;
            var apiKey = Environment.GetEnvironmentVariable("API_KEY")!;

            var tasks = new Task[] {
                new WebServer().RunAsync(
                    args, 
                    CancellationToken.None
                    ),

                new TelegramBot().RunAsync(
                    baseUrl,
                    apiKey,
                    tgBotPollPeriodInSeconds, 
                    CancellationToken.None
                    ),

                new FileCollector().RunAsync(
                    videoCollectorInpectPeriodInSeconds,
                    videoCollectorInpectPeriodInSeconds, 
                    CancellationToken.None
                    ),
            };

            Console.WriteLine("bot run!");

            await Task.WhenAll(tasks);
        }
    }
}
