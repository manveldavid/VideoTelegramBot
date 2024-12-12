namespace VideoTelegramBot;

public class WebServer
{
    public async Task RunAsync(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var app = builder.Build();

        app.MapGet("/{video}", (string video) =>
        {
            var videoPath = Path.Combine(AppContext.BaseDirectory, "wwwroot", video);

            try
            {
                if (!File.Exists(videoPath))
                    return Results.NotFound();

                using(var stream = new FileStream(videoPath, FileMode.Open))
                {
                    return Results.Stream(stream, "video/mp4", video, enableRangeProcessing: true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return Results.BadRequest();
            }
        });

        await app.RunAsync();
    }
}
