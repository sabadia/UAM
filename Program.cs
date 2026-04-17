using UAM.Bootstrap;

var builder = WebApplication.CreateBuilder(args);
ConfigureServices(builder);

var app = builder.Build();
ConfigurePipeline(app);

await app.RunAsync();

public partial class Program
{
    public static void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services.AddUamServices(builder.Configuration, builder.Environment);
    }

    public static void ConfigurePipeline(WebApplication app)
    {
        app.UseUAMPipeline();
    }
}