using MemberService.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi.Models; // �T�O�w�g�ޥγo�өR�W�Ŷ�
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MemberService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // �[�J���n���A�ȡA�]�ASession
            builder.Services.AddControllers();

            // �ҥ�Session�A��
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30); // �]�wSession���L���ɶ��A�o�ҳ]��30����
                options.Cookie.HttpOnly = true; // �W�[�w���ʡA�T��Ȥ�� JavaScript �X��
                options.Cookie.IsEssential = true; // ��� GDPR �M Cookie �F���n�D�O���n��
                options.Cookie.SameSite = SameSiteMode.None; // �]�m SameSite �� None
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // �u���\ HTTPS �ǿ�
            });

            // �[�J CORS �䴩
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAllOrigins",
                    builder =>
                    {
                        builder.WithOrigins("http://localhost:4200") // �������A���e�� URL
                               .AllowAnyMethod()
                               .AllowAnyHeader()
                               .AllowCredentials(); // ���\���ШD��a���ҡ]�Ҧp Cookie�^
                    });
            });

            // ���J appsettings.json
            builder.Configuration.SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();

            // �t�m MySQL �s��
            builder.Services.AddDbContext<KeyServiceContext>(options =>
            {
                var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

                // �T�O�s���r�꥿�T
                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new Exception("Database connection string is missing in appsettings.json");
                }

                // �t�m MySQL �s��
                options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 26)),
                    mySqlOptions => mySqlOptions.EnableRetryOnFailure());  // �K�[���վ���
            });

            // �[�J Controller �A��
            builder.Services.AddControllers();

            // Swagger �]�w
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "MemberService API", Version = "v1" });
            });

            var app = builder.Build();
            app.UseSession();

            try
            {
                // �]�w HTTP �޹D
                if (app.Environment.IsDevelopment())
                {
                    app.UseSwagger();
                    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "MemberService API v1"));
                }

                // �ϥ� CORS �F��
                app.UseSession();  // �T�O�b�o�̱ҥ� Session
                app.UseHttpsRedirection();
                app.UseCors("AllowAllOrigins");
                app.UseAuthorization();
                app.MapControllers();

                app.Run();
            }
            catch (Exception ex)
            {
                // �ԲӮ����Ұʮɪ����~
                Console.WriteLine($"An error occurred while starting the application: {ex.Message}");
                throw;
            }
        }
    }
}
