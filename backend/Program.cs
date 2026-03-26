using Microsoft.EntityFrameworkCore;
using DoAnCoSo.Data;
using DoAnCoSo.Configurations;
using CloudinaryDotNet;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Linq;
using DoAnCoSo.Services;
using DoAnCoSo.Models;
using DotNetEnv;

// Load .env file from the project directory
// Try multiple locations to find .env file
var envPath = "";
var possiblePaths = new List<string>
{
    Path.Combine(Directory.GetCurrentDirectory(), ".env"),  // Current directory (project root when running from VS)
    Path.Combine(AppContext.BaseDirectory, ".env"),  // bin/Debug/net8.0/.env
};

// Try going up from bin/Debug/net8.0 to find project root (where .csproj file is)
var baseDir = AppContext.BaseDirectory;
for (int i = 0; i < 6; i++)
{
    var testPath = Path.Combine(baseDir, ".env");
    var normalizedPath = Path.GetFullPath(testPath);
    if (!possiblePaths.Any(p => Path.GetFullPath(p).Equals(normalizedPath, StringComparison.OrdinalIgnoreCase)))
    {
        possiblePaths.Add(normalizedPath);
    }
    
    var parent = Directory.GetParent(baseDir);
    if (parent == null) break;
    baseDir = parent.FullName;
}

// Also try to find from assembly location
var assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
if (!string.IsNullOrEmpty(assemblyLocation))
{
    var assemblyDir = Path.GetDirectoryName(assemblyLocation);
    if (!string.IsNullOrEmpty(assemblyDir))
    {
        var assemblyEnvPath = Path.Combine(assemblyDir, ".env");
        if (!possiblePaths.Any(p => Path.GetFullPath(p).Equals(Path.GetFullPath(assemblyEnvPath), StringComparison.OrdinalIgnoreCase)))
        {
            possiblePaths.Add(assemblyEnvPath);
        }
        
        // Go up from assembly location
        var assemblyBaseDir = assemblyDir;
        for (int i = 0; i < 6; i++)
        {
            var testPath = Path.Combine(assemblyBaseDir, ".env");
            var normalizedPath = Path.GetFullPath(testPath);
            if (!possiblePaths.Any(p => Path.GetFullPath(p).Equals(normalizedPath, StringComparison.OrdinalIgnoreCase)))
            {
                possiblePaths.Add(normalizedPath);
            }
            
            var parent = Directory.GetParent(assemblyBaseDir);
            if (parent == null) break;
            assemblyBaseDir = parent.FullName;
        }
    }
}

foreach (var path in possiblePaths)
{
    if (!string.IsNullOrEmpty(path) && File.Exists(path))
    {
        envPath = Path.GetFullPath(path);
        break;
    }
}

if (!string.IsNullOrEmpty(envPath))
{
    Env.Load(envPath);
    Console.WriteLine($"[ENV] Loaded .env from: {envPath}");
}
else
{
    Console.WriteLine("[ENV] Warning: .env file not found. Using appsettings.json or environment variables.");
    Console.WriteLine($"[ENV] Searched in {possiblePaths.Count} locations");
}

var builder = WebApplication.CreateBuilder(args);

// Add environment variables to configuration (override appsettings.json)
builder.Configuration.AddEnvironmentVariables();

// Map environment variables to configuration sections
// Debug: Check if CONNECTION_STRING is loaded from .env
var envConnectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING");
Console.WriteLine($"[DEBUG] CONNECTION_STRING from env: {(string.IsNullOrEmpty(envConnectionString) ? "NOT FOUND" : "FOUND")}");

var connectionString = envConnectionString
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? builder.Configuration["ConnectionStrings:DefaultConnection"];

if (string.IsNullOrEmpty(connectionString))
{
    Console.WriteLine("[ERROR] ConnectionString is empty!");
    Console.WriteLine($"[DEBUG] Environment variable CONNECTION_STRING: {(envConnectionString ?? "NULL")}");
    Console.WriteLine($"[DEBUG] appsettings ConnectionStrings:DefaultConnection: {(builder.Configuration["ConnectionStrings:DefaultConnection"] ?? "NULL")}");
    throw new InvalidOperationException(
        "ConnectionString is not configured. Please set CONNECTION_STRING in .env file or ConnectionStrings:DefaultConnection in appsettings.json");
}

builder.Configuration["ConnectionStrings:DefaultConnection"] = connectionString;
Console.WriteLine($"[CONFIG] ConnectionString configured: Yes");
Console.WriteLine($"[CONFIG] ConnectionString value: {connectionString.Substring(0, Math.Min(50, connectionString.Length))}...");

// Verify other critical configurations
var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY") ?? builder.Configuration["Jwt:Key"];
if (string.IsNullOrEmpty(jwtKey))
{
    Console.WriteLine("[CONFIG] Warning: JWT_KEY is not configured!");
}

if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("JWT_KEY")))
{
    builder.Configuration["Jwt:Key"] = Environment.GetEnvironmentVariable("JWT_KEY");
    builder.Configuration["Jwt:Issuer"] = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? builder.Configuration["Jwt:Issuer"];
    builder.Configuration["Jwt:Audience"] = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? builder.Configuration["Jwt:Audience"];
}

if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CLOUDINARY_CLOUD_NAME")))
{
    builder.Configuration["CloudinarySettings:CloudName"] = Environment.GetEnvironmentVariable("CLOUDINARY_CLOUD_NAME");
    builder.Configuration["CloudinarySettings:ApiKey"] = Environment.GetEnvironmentVariable("CLOUDINARY_API_KEY");
    builder.Configuration["CloudinarySettings:ApiSecret"] = Environment.GetEnvironmentVariable("CLOUDINARY_API_SECRET");
}

if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MOMO_SECRET_KEY")))
{
    builder.Configuration["Momo:SecretKey"] = Environment.GetEnvironmentVariable("MOMO_SECRET_KEY");
    builder.Configuration["Momo:AccessKey"] = Environment.GetEnvironmentVariable("MOMO_ACCESS_KEY");
    builder.Configuration["Momo:PartnerCode"] = Environment.GetEnvironmentVariable("MOMO_PARTNER_CODE") ?? builder.Configuration["Momo:PartnerCode"];
    builder.Configuration["Momo:ReturnUrl"] = Environment.GetEnvironmentVariable("MOMO_RETURN_URL") ?? builder.Configuration["Momo:ReturnUrl"];
    builder.Configuration["Momo:NotifyUrl"] = Environment.GetEnvironmentVariable("MOMO_NOTIFY_URL") ?? builder.Configuration["Momo:NotifyUrl"];
}

if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("EMAIL_FROM")))
{
    builder.Configuration["EmailSettings:From"] = Environment.GetEnvironmentVariable("EMAIL_FROM");
    builder.Configuration["EmailSettings:Password"] = Environment.GetEnvironmentVariable("EMAIL_PASSWORD");
    builder.Configuration["EmailSettings:SmtpServer"] = Environment.GetEnvironmentVariable("EMAIL_SMTP_SERVER") ?? builder.Configuration["EmailSettings:SmtpServer"];
    builder.Configuration["EmailSettings:Port"] = Environment.GetEnvironmentVariable("EMAIL_PORT") ?? builder.Configuration["EmailSettings:Port"];
}

if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("OCR_API_KEY")))
{
    builder.Configuration["OCRSettings:ApiKey"] = Environment.GetEnvironmentVariable("OCR_API_KEY");
}

if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ZALO_ACCESS_TOKEN")))
{
    builder.Configuration["Zalo:AccessToken"] = Environment.GetEnvironmentVariable("ZALO_ACCESS_TOKEN");
}

// ESms Configuration
if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ESMS_API_KEY")))
{
    builder.Configuration["ESms:ApiKey"] = Environment.GetEnvironmentVariable("ESMS_API_KEY")?.Trim();
    builder.Configuration["ESms:SecretKey"] = Environment.GetEnvironmentVariable("ESMS_SECRET_KEY")?.Trim();
    builder.Configuration["ESms:BrandName"] = Environment.GetEnvironmentVariable("ESMS_BRAND_NAME")?.Trim() ?? "Baotrixemay";
    var apiUrl = Environment.GetEnvironmentVariable("ESMS_API_URL")?.Trim() ?? "https://rest.esms.vn/MainService.svc/json/SendMultipleMessage_V4_post_json/";
    // Đảm bảo URL có dấu / ở cuối
    if (!apiUrl.EndsWith("/"))
    {
        apiUrl += "/";
    }
    builder.Configuration["ESms:ApiUrl"] = apiUrl;
}

if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("FRONTEND_BASE_URL")))
{
    builder.Configuration["Frontend:BaseUrl"] = Environment.GetEnvironmentVariable("FRONTEND_BASE_URL");
}


// ========================= DB Context =========================
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(30), null);
        });
});

// ========================= Logging =========================
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// ========================= JWT Auth =========================
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])
            )
        };
    });

// ========================= Register Services =========================
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddSingleton(typeof(DinkToPdf.Contracts.IConverter),
    new DinkToPdf.SynchronizedConverter(new DinkToPdf.PdfTools()));

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "DoAnCoSo API",
        Version = "v1"
    });
    
    // Thêm JWT Bearer authentication cho Swagger
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ========================= Cloudinary =========================
builder.Services.Configure<CloudinarySettings>(
    builder.Configuration.GetSection("CloudinarySettings"));

builder.Services.AddSingleton<Cloudinary>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<CloudinarySettings>>().Value;
    return new Cloudinary(new Account(settings.CloudName, settings.ApiKey, settings.ApiSecret));
});
// ========================= MQTT SETTINGS =========================
builder.Services.Configure<MqttSettings>(
    builder.Configuration.GetSection("Mqtt"));
// ========================= CORS (CHUẨN CHỈNH) =========================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ========================= Other Services =========================
builder.Services.AddHttpClient<DoAnCoSo.Services.ILocalAIChatService, DoAnCoSo.Services.LocalAIChatService>();
builder.Services.AddScoped<ILocalAIChatService, LocalAIChatService>();
builder.Services.AddHostedService<TelegramBotService>();
builder.Services.AddHttpClient<MomoPaymentService>();
builder.Services.AddScoped<MomoPaymentService>();
builder.Services.AddHttpClient<DoAnCoSo.Services.IOCRAPIService, DoAnCoSo.Services.OCRAPIService>();
builder.Services.AddScoped<IOCRAPIService, OCRAPIService>();
builder.Services.AddScoped<HoaDonService>();
builder.Services.AddHostedService<InvoiceBackgroundService>();
builder.Services.AddScoped<DoAnCoSo.Services.EmailService>();
builder.Services.AddHostedService<ContractStatusService>();
       builder.Services.AddHttpClient<IZaloService, ZaloService>();
       builder.Services.AddScoped<IZaloService, ZaloService>();
       builder.Services.AddHttpClient<IESmsService, ESmsService>();
       builder.Services.AddScoped<IESmsService, ESmsService>();
builder.Services.AddSingleton<IMqttDoorService, MqttDoorService>();


var app = builder.Build();

// ========================= AUTO CREATE ADMIN =========================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        
        bool TableExists(string tableName)
        {
            using var command = context.Database.GetDbConnection().CreateCommand();
            command.CommandText = $"SELECT OBJECT_ID(N'[{tableName}]')";
            context.Database.OpenConnection();
            var result = command.ExecuteScalar();
            context.Database.CloseConnection();
            return result != null && result != DBNull.Value;
        }

        // Kiểm tra xem database có tồn tại không
        if (context.Database.CanConnect())
        {
            // Thử chạy migration, nhưng không crash nếu có lỗi
            try
            {
                var pendingMigrations = context.Database.GetPendingMigrations().ToList();
                if (pendingMigrations.Any())
                {
                    // Nếu bảng cốt lõi đã tồn tại (ví dụ DichVu) nhưng thiếu history, bỏ qua migrate để tránh lỗi "object exists"
                    if (TableExists("DichVu") && pendingMigrations.Count > 0)
                    {
                        logger.LogWarning("Phát hiện bảng DichVu đã tồn tại nhưng còn migration pending. Bỏ qua chạy migration để tránh lỗi trùng bảng. Vui lòng đồng bộ lại migration thủ công nếu cần.");
                    }
                    else
                    {
                        logger.LogInformation($"Đang apply {pendingMigrations.Count} migration(s) pending...");
                        context.Database.Migrate();
                        logger.LogInformation("Migration hoàn tất");
                    }
                }
                else
                {
                    logger.LogInformation("Database đã được cập nhật, không có migration pending");
                }
            }
            catch (Microsoft.Data.SqlClient.SqlException sqlEx) when (sqlEx.Number == 2714) // Object already exists
            {
                // Bảng đã tồn tại, có thể do migration đã được apply một phần
                logger.LogWarning($"Một số bảng đã tồn tại trong database. Bỏ qua migration: {sqlEx.Message}");
            }
            catch (Exception migrateEx)
            {
                // Log lỗi migration nhưng không crash app
                logger.LogWarning(migrateEx, "Có lỗi khi chạy migration, nhưng tiếp tục khởi động ứng dụng");
            }
        }
        else
        {
            // Database chưa tồn tại, tạo mới
            logger.LogInformation("Database chưa tồn tại, đang tạo mới...");
            context.Database.Migrate();
        }

        // Tạo admin mặc định nếu chưa có
        if (!context.Users.Any(u => u.TenDangNhap == "Admin"))
        {
            var passwordHash = BCrypt.Net.BCrypt.HashPassword("Admin123");

            context.Users.Add(new User
            {
                TenDangNhap = "Admin",
                VaiTro = "Admin",
                MatKhau = passwordHash
            });

            context.SaveChanges();
            logger.LogInformation("Đã tạo tài khoản Admin mặc định");
        }
        else
        {
            logger.LogInformation("Tài khoản Admin đã tồn tại");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Lỗi khi khởi tạo database và tài khoản admin");
        // Không throw để app vẫn có thể chạy
    }
}

// ========================= Middlewares =========================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// ⭐ CHỈ 1 LẦN CORS – ĐÚNG VỊ TRÍ ⭐
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
