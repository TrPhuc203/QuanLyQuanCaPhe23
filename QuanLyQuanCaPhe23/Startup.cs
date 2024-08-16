﻿using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
                                   //services.AddScoped<IOtpService, OtpService>();
            services.AddScoped<ESMSHelper>();
            services.Configure<ESMSOptions>(Configuration.GetSection("ESMS"));
            // Add framework services.
            services.AddMvc();

            // Add HttpClientFactory
            services.AddHttpClient();


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
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseSession();
            app.UseRouting();

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
