namespace VideoTelegramBot
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var tasks = new Task[] {
                new WebServer().RunAsync(args),
                new TelegramBot().RunAsync(TimeSpan.FromSeconds(5), CancellationToken.None),
                new FileCollector().RunAsync(Path.Combine(AppContext.BaseDirectory, "wwwroot"), TimeSpan.FromHours(2), TimeSpan.FromHours(1), CancellationToken.None)
            };

            Console.WriteLine("bot run!");

            await Task.WhenAll(tasks);
        }
    }
}
