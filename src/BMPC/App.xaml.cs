using BMPC.Core;
using BMPC.Core.Services;
using BMPC.Extensions;
using BMPC.Services;
using BMPC.ViewModels;
using BMPC.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IO;
using System.Windows;

namespace BMPC
{
    public partial class App : Application
    {
        public static IHost? AppHost { get; private set; }

        public App()
        {
            AppHost = Host.CreateDefaultBuilder()
                .ConfigureServices(ConfigureServices)
                .Build();
        }

        private static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
        {
            services.AddSingleton<IAppPaths, AppPaths>();
            services.AddSingleton<IMessageDialogService, MessageDialogService>();
            services.AddSingleton<IFileDialogService, FileDialogService>();
            services.AddSingleton<IProcessLauncher, ProcessLauncher>();
            services.AddSingleton<MainViewModel>();
            services.AddSingleton<Main>();
            services.AddFormFactory<CreatePackageView>();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            var exeDir = AppContext.BaseDirectory;
            if (!string.IsNullOrWhiteSpace(exeDir))
            {
                Directory.SetCurrentDirectory(exeDir);
            }

            var appPaths = AppHost!.Services.GetRequiredService<IAppPaths>();
            var messageDialogService = AppHost.Services.GetRequiredService<IMessageDialogService>();

#if !DEBUG
            if (!File.Exists(appPaths.BeeExecutableName))
            {
                messageDialogService.ShowError($"BEEmod Music Package Creator must be installed in BEEmod's folder. (Same place where {appPaths.BeeExecutableName} is located)");
                Shutdown(1);
                return;
            }
#endif

            Utils.CreateDirectoryIfMissing(appPaths.RootDirectory);
            Utils.CreateDirectoryIfMissing(appPaths.PackagesDirectory);
            Utils.CreateDirectoryIfMissing(appPaths.ResourcesDirectory);
            Utils.CreateDirectoryIfMissing(appPaths.BeePackagesDirectory);

            if (Directory.Exists(appPaths.TempDirectory))
            {
                Directory.Delete(appPaths.TempDirectory, true);
            }
            Utils.CreateDirectoryIfMissing(appPaths.TempDirectory);

            var settings = new SettingsService().Load();
            ThemeService.Apply(settings.ThemeMode);
            ThemeService.StartWatchingSystemThemeChanges(() => new SettingsService().Load().ThemeMode);

            await AppHost.StartAsync();

            var startupForm = AppHost.Services.GetRequiredService<Main>();
            startupForm.Show();

            base.OnStartup(e);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            if (AppHost != null)
            {
                ThemeService.StopWatchingSystemThemeChanges();

                await AppHost.StopAsync();
                var appPaths = AppHost.Services.GetService<IAppPaths>();
                if (appPaths != null && Directory.Exists(appPaths.TempDirectory))
                {
                    Directory.Delete(appPaths.TempDirectory, true);
                }
            }

            base.OnExit(e);
        }
    }
}
