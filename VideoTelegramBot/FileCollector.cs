namespace VideoTelegramBot;

public class FileCollector
{
    public async Task RunAsync(string observeDirectory, TimeSpan fileLifeTime, TimeSpan pollPeriod, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(pollPeriod, cancellationToken);

            if (Directory.Exists(observeDirectory))
                Directory.GetFiles(observeDirectory)
                    .Select(p => new FileInfo(p))
                    .Where(f => DateTime.UtcNow - f.CreationTimeUtc > fileLifeTime)
                    .ToList().ForEach(f => File.Delete(f.FullName));
        }
    }
}
