namespace VideoTelegramBot;

public class FileCollector
{
    public async Task RunAsync(string observeDirectory, TimeSpan fileLifeTime, TimeSpan pollPeriod, CancellationToken cancellationToken)
    {
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

            foreach(var file in expiredFiles)
            {
                try
                {
                    File.Delete(file.FullName);
                }
                catch
                {
                    //some files can be used
                }
            }
        }
    }
}
