using System.Runtime.InteropServices;

namespace OpenPronounceSharp.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            LoadLibraries();

            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
            }
            app.UseRouting();

            app.UseAuthorization();

            app.MapStaticAssets();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
                .WithStaticAssets();

            app.Run();
        }

        static void LoadLibraries()
        {
            try
            {
                string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "runtimes", "win-x64", "native");

                if (Directory.Exists(folder))
                {
                    var files = new DirectoryInfo(folder).GetFiles("*.dll");

                    foreach (var file in files)
                    {
                        NativeLibrary.Load(file.FullName);
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }
    }
}
