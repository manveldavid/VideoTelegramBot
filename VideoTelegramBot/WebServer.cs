using System.IO;

namespace VideoTelegramBot;

public class WebServer
{
    public async Task RunAsync(string[] args, TimeSpan fileLifeTime, CancellationToken cancellationToken)
    {
        var builder = WebApplication.CreateBuilder(args);
        var app = builder.Build();

        app.MapGet("/{video}", async (string video) =>
        {
            var videoPath = Path.Combine(AppContext.BaseDirectory, "wwwroot", video);

            if (!File.Exists(videoPath))
                return Results.NotFound();

            var stream = new FileStream(videoPath, FileMode.Open);
            Task.Delay(fileLifeTime, cancellationToken).ContinueWith(res => stream.Dispose());

            return Results.Stream(
                stream: stream, 
                contentType: "video/mp4", 
                fileDownloadName: null, 
                enableRangeProcessing: true);
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
