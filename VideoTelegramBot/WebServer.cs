namespace VideoTelegramBot;

public class WebServer
{
    public async Task RunAsync(string[] args, CancellationToken cancellationToken)
    {
        var builder = WebApplication.CreateBuilder(args);
        var app = builder.Build();

        app.MapGet("/{video}", (string video) =>
        {
            var videoPath = Path.Combine(AppContext.BaseDirectory, "wwwroot", video);

            if (!File.Exists(videoPath))
                return Results.NotFound();

            using (var stream = new FileStream(videoPath, FileMode.Open))
            {
                return Results.Stream(stream, "video/mp4", video, enableRangeProcessing: true);
            }
        });
    
        try
        {
            while (!cancellationToken.IsCancellationRequested)
                await app.RunAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }
}
