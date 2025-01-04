using System.Text;
using backend.database;
using backend.Repositories;
using backend.service;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using frontend.Components;
using frontend.Extensions;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Events;

namespace frontend;

public class Program
{
    static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ICredentialRepository, CredentialRepository>();
        services.AddScoped<TokenService>();

        services.AddFido2(o =>
        {
            o.ServerDomain = configuration["fido2:ServerDomain"];
            o.ServerName = "BezpecnostSWSystemu_FIDO2_Server";
            o.Origins = configuration.GetSection("fido2:origins").Get<HashSet<string>>();
            o.TimestampDriftTolerance = configuration.GetValue<int>("fido2:timestampDriftTolerance");
        });

        services.AddSingleton<ApiSettings>(sp => configuration.GetSection("ApiSettings").Get<ApiSettings>()!);
        
        services.AddControllers();
        
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters.ValidIssuer = configuration["Jwt:Issuer"];
                options.TokenValidationParameters.ValidAudience = configuration["Jwt:Audience"];
                options.TokenValidationParameters.IssuerSigningKey =
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!));
                options.TokenValidationParameters.ClockSkew = TimeSpan.FromSeconds(30);
            });
    }
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Host.UseSerilog();
        Log.Logger = CreateLocalSerilogLogger();
        
        builder.Configuration.AddJsonFile(builder.Environment.IsDevelopment()
            ? "appsettings.Development.json"
            : "appsettings.json");

        // Add services to the container.
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();
        
        builder.Services.AddMemoryCache();
        builder.Services.AddDistributedMemoryCache();
        builder.Services.AddSession(o =>
        {
            o.IdleTimeout = TimeSpan.FromMinutes(1);
            o.Cookie.HttpOnly = false;
        });
        
        // add http client for blazor
        builder.Services.AddScoped(sp => 
            new HttpClient
            {
                BaseAddress = new Uri(builder.Configuration["ApiSettings:BaseUrl"]!)
            });
        builder.Services.AddHttpClient();
        builder.Services.AddBlazoredLocalStorage();
        
        
        // Connect DB
        if (builder.Environment.IsDevelopment())
        {
            if (Environment.GetEnvironmentVariable("USE_POSTGRES_DB") == "true")
            {
                Log.Information("Using local PostgreSQL DB");
                builder.Services.AddDbContext<AppDbContext>(o =>
                    o.UseNpgsql(builder.Configuration.GetDbConnectionString())
                );
            }
            else
            {
                Log.Information("Using local in-memory DB");
                builder.Services.AddDbContext<AppDbContext>(o =>
                    o.UseInMemoryDatabase("local_db_Constant")
                );
            }
        }
        else
        {
            Log.Information("Using prod DB");
            builder.Services.AddDbContext<AppDbContext>(o =>
                o.UseNpgsql(builder.Configuration.GetDbConnectionString())
            );
        }
        
        // Add reverse proxy middleware
        if (!builder.Environment.IsDevelopment())
        {
            builder.Services.Configure<ForwardedHeadersOptions>(o =>
            {
                o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            });
        }
        
        // Connect services
        ConfigureServices(builder.Services, builder.Configuration);
        
        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            app.UseForwardedHeaders();
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }
        else
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseHttpsRedirection();

        app.UseStaticFiles();
        app.UseRouting();
        app.UseAntiforgery();
        app.MapControllers();
        app.UseSession();
        
        app.UseAuthentication();
        app.UseAuthorization();
        
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        var context = app.Services.CreateScope().ServiceProvider.GetRequiredService<AppDbContext>();
        if (!context.Database.IsInMemory())
        {
            context.Database.Migrate();
        }
        
        app.Run();
    }
    
    
    private static Serilog.ILogger CreateLocalSerilogLogger()
    {
        var configuration = new Serilog.LoggerConfiguration()
            .Enrich.FromLogContext()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Debug)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database", LogEventLevel.Debug)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Warning)
            .WriteTo.Console();

        return configuration.CreateLogger();
    }
}