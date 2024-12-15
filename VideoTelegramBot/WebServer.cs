using System.IO;

namespace VideoTelegramBot;

public class WebServer
{

    public async Task<Stream> GetStream(string filePath)
    {
        using (var fs = new FileStream(filePath, FileMode.Open))
        {
            var ms = new MemoryStream();
            await fs.CopyToAsync(ms);
            return ms;
        }
    }

    public async Task RunAsync(string[] args, TimeSpan fileLifeTime, CancellationToken cancellationToken)
    {
        var builder = WebApplication.CreateBuilder(args);
        var app = builder.Build();

        app.MapGet("/{video}", async (string video) =>
        {
            var videoPath = Path.Combine(AppContext.BaseDirectory, "wwwroot", video);

            if (!File.Exists(videoPath))
                return Results.NotFound();

            var stream = await GetStream(videoPath);
            Task.Delay(fileLifeTime, cancellationToken).ContinueWith(res => stream.Dispose());

            return Results.Stream(stream, "video/mp4", video, enableRangeProcessing: true);
        });

        app.MapGet("/isAlive", () => Results.Ok(true));

        try
        {
            while (!cancellationToken.IsCancellationRequested)
                await app.RunAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }
}
