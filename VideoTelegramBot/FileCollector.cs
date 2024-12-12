namespace VideoTelegramBot;

public class FileCollector
{
    public async Task RunAsync(TimeSpan fileLifeTime, TimeSpan pollPeriod, CancellationToken cancellationToken)
    {
        var observeDirectory = Path.Combine(AppContext.BaseDirectory, "wwwroot");

        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(pollPeriod, cancellationToken);

            if (!Directory.Exists(observeDirectory))
                continue;

            var expiredFiles =
                Directory.GetFiles(observeDirectory)
                    .Select(p => new FileInfo(p))
                    .Where(f => DateTime.UtcNow - f.CreationTimeUtc > fileLifeTime)
                    .ToList();

            foreach (var file in expiredFiles)
            {
                try
                {
                    File.Delete(file.FullName);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
        }
    }
}
