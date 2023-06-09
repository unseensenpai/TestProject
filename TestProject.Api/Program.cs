using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Serialization;
using System.Net.Http.Headers;
using TestProject.Core.Middlewares;
using TestProject.DAL;
using TestProject.Dto.Auth;
using TestProject.HttpApi.Core;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
RegisterConfiguations(builder);
RegisterServices(builder);
WebApplication app = builder.Build();
ConfigurePipelineSettings(app);
app.Run();
app.Logger.LogInformation("Application started!");

static void RegisterInjectionServices(WebApplicationBuilder builder)
{
    builder.Services.AddServiceModules();
    builder.Services.AddHttpClient(builder.Configuration.GetSection("Endpoints:Local:Client").Value, options =>
    {
        options.BaseAddress = new Uri(builder.Configuration.GetValue<string>("Endpoints:Local:Address"));
        options.DefaultRequestHeaders.Accept.Clear();
        options.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        options.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", builder.Configuration.GetSection("").Value);
        options.Timeout = TimeSpan.FromSeconds(30);
    });
}


static void RegisterServices(WebApplicationBuilder builder)
{    

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    builder.Services.AddDbContext<MySqlContext>(options =>
    {
        options.UseSqlServer(builder.Configuration.GetConnectionString("MssqlTest"));
    });

    builder.Services.AddControllers(options =>
    {
        options.RespectBrowserAcceptHeader = true;
    }).AddApplicationPart(typeof(CoreController).Assembly)
        .AddXmlSerializerFormatters()
        .AddJsonOptions(jsonOptions =>
        {
            jsonOptions.JsonSerializerOptions.PropertyNamingPolicy = null;
        })
        .AddNewtonsoftJson(options =>
        {
            options.SerializerSettings.ContractResolver = new DefaultContractResolver();
        });

    RegisterInjectionServices(builder);
}

static void ConfigurePipelineSettings(WebApplication app)
{
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

    app.UseHttpsRedirection();
    app.UseRouting();

    app.UseAuthorization();

    app.UseMiddleware<ExceptionMiddleware>();

    app.MapControllers();

}

static void RegisterConfiguations(WebApplicationBuilder builder)
{
    builder.Configuration.AddJsonFile("appsettings.json", true, true);
    builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", true, true);
    builder.Services.Configure<ServiceConfigs>(builder.Configuration.GetSection("ServiceConfiguration"));
}


AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);


void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
{
    app.Logger.LogCritical("an error occured.");
}
