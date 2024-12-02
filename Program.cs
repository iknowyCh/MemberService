using MemberService.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi.Models; // 確保已經引用這個命名空間
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MemberService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // 加入必要的服務，包括Session
            builder.Services.AddControllers();

            // 啟用Session服務
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30); // 設定Session的過期時間，這例設為30分鐘
                options.Cookie.HttpOnly = true; // 增加安全性，禁止客戶端 JavaScript 訪問
                options.Cookie.IsEssential = true; // 對於 GDPR 和 Cookie 政策要求是必要的
                options.Cookie.SameSite = SameSiteMode.None; // 設置 SameSite 為 None
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // 只允許 HTTPS 傳輸
            });

            // 加入 CORS 支援
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAllOrigins",
                    builder =>
                    {
                        builder.WithOrigins("http://localhost:4200") // 替換成你的前端 URL
                               .AllowAnyMethod()
                               .AllowAnyHeader()
                               .AllowCredentials(); // 允許跨域請求攜帶憑證（例如 Cookie）
                    });
            });

            // 載入 appsettings.json
            builder.Configuration.SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();

            // 配置 MySQL 連接
            builder.Services.AddDbContext<KeyServiceContext>(options =>
            {
                var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

                // 確保連接字串正確
                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new Exception("Database connection string is missing in appsettings.json");
                }

                // 配置 MySQL 連接
                options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 26)),
                    mySqlOptions => mySqlOptions.EnableRetryOnFailure());  // 添加重試機制
            });

            // 加入 Controller 服務
            builder.Services.AddControllers();

            // Swagger 設定
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "MemberService API", Version = "v1" });
            });

            var app = builder.Build();
            app.UseSession();

            try
            {
                // 設定 HTTP 管道
                if (app.Environment.IsDevelopment())
                {
                    app.UseSwagger();
                    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "MemberService API v1"));
                }

                // 使用 CORS 政策
                app.UseSession();  // 確保在這裡啟用 Session
                app.UseHttpsRedirection();
                app.UseCors("AllowAllOrigins");
                app.UseAuthorization();
                app.MapControllers();

                app.Run();
            }
            catch (Exception ex)
            {
                // 詳細捕捉啟動時的錯誤
                Console.WriteLine($"An error occurred while starting the application: {ex.Message}");
                throw;
            }
        }
    }
}
