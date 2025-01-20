namespace VideoTelegramBot
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var videoLifeTime = TimeSpan.FromSeconds(double.TryParse(Environment.GetEnvironmentVariable("VIDEO_LIFETIME_IN_SECONDS"), out var _videoLifeTimeInSeconds) ? _videoLifeTimeInSeconds : 14400d);
            var videoCollectorInpectPeriod = TimeSpan.FromSeconds(double.TryParse(Environment.GetEnvironmentVariable("CHECK_VIDEO_LIFETIME_PERIOD_IN_SECONDS"), out var _videoCollectorInpectPeriodInSeconds) ? _videoCollectorInpectPeriodInSeconds : 3600d);
            var tgBotPollPeriod = TimeSpan.FromSeconds(double.TryParse(Environment.GetEnvironmentVariable("TG_BOT_POLL_PERIOD_SECONDS"), out var _tgBotPollPeriodInSeconds) ? _tgBotPollPeriodInSeconds : 5d);
            var baseUrl = Environment.GetEnvironmentVariable("PUBLIC_URL")!;
            var baseSecureUrl = Environment.GetEnvironmentVariable("PUBLIC_URL_SECURE")!;
            var apiKey = Environment.GetEnvironmentVariable("API_KEY")!;

            Console.WriteLine("bot run!");

            await Task.WhenAll([
                new WebServer().RunAsync(
                    args,
                    videoLifeTime,
                    CancellationToken.None
                    ),

                new TelegramBot().RunAsync(
                    baseUrl,
                    baseSecureUrl,
                    apiKey,
                    tgBotPollPeriod,
                    CancellationToken.None
                    ),

                new FileCollector().RunAsync(
                    videoLifeTime,
                    videoCollectorInpectPeriod,
                    CancellationToken.None
                    )
                ]);
        }
    }
}
