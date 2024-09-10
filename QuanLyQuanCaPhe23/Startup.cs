using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using QuanLyQuanCaPhe23.Models;
using QuanLyQuanCaPhe23.OTP;
using QuanLyQuanCaPhe23.PAYPAL;
using QuanLyQuanCaPhe23.VNPAY;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuanLyQuanCaPhe23
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDistributedMemoryCache();
            services.AddDbContext<QUANLYCAPHEContext>(options =>
                 options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));

            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30); // Thời gian chờ của session
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });
            services.AddHttpContextAccessor();
            services.AddControllersWithViews();
            services.AddSingleton(Configuration);
            services.AddSingleton<IVnPayService, VnPayService>();
            services.AddSingleton<IPayPalService, PayPalService>();
            services.AddLogging(); // Thêm dịch vụ logging
            services.AddScoped<ESMSHelper>();
            services.Configure<ESMSOptions>(Configuration.GetSection("ESMS"));

            // Add framework services.
            services.AddMvc();

            // Add HttpClientFactory
            services.AddHttpClient();
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
            })
            .AddCookie()
            .AddGoogle(options =>
            {
                IConfigurationSection googleAuthNSection =
                    Configuration.GetSection("Authentication:Google");

                options.ClientId = googleAuthNSection["ClientId"];
                options.ClientSecret = googleAuthNSection["ClientSecret"];
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseSession();
            app.UseRouting();

            app.UseAuthentication(); // Thêm dòng này để bật xác thực
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=TrangChu}/{id?}");

                // Cấu hình thêm cho NguoiDungController nếu cần
                endpoints.MapControllerRoute(
                    name: "payment",
                    pattern: "GioHang/{action=PaymentCallback}/{id?}");
            });

            logger.LogInformation("Application started.");
        }
    }
}
