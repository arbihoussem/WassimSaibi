using System.IO;
using System.Windows;
using LogAggregator.Core.Domain;
using LogAggregator.Core.Infrastructure.Parsers;
using LogAggregator.Core.Infrastructure.Services;
using LogAggregator.WPF.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace LogAggregator.WPF
{
    public partial class App : Application
    {
        private ServiceProvider _serviceProvider = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // build the DI container
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();

            // global error handler — show MessageBox instead of silent crash
            DispatcherUnhandledException += (_, args) =>
            {
                MessageBox.Show(
                    $"Unexpected error:\n\n{args.Exception.Message}",
                    "Log Aggregator — Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                args.Handled = true;
            };

            // create and show MainWindow via DI
            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        private static void ConfigureServices(ServiceCollection services)
        {
            // Core services — one instance shared across the app
            services.AddSingleton<IAggregationService, AggregationService>();
            services.AddSingleton<IReportService,      ReportService>();
            services.AddSingleton<ILogFileSource,      FileWatcherService>();

            // ParserRegistry — registered as singleton, plugins loaded once at startup
            services.AddSingleton<ParserRegistry>(provider =>
            {
                var registry = new ParserRegistry();

                // built-in parsers
                registry.Register(new TextLogParser());
                registry.Register(new JsonLogParser());
                registry.Register(new XmlLogParser());

                // plugins from /plugins folder next to the exe
                var pluginDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins");
                var loader    = new PluginLoader();
                foreach (var plugin in loader.LoadFromDirectory(pluginDir))
                    registry.Register(plugin);

                return registry;
            });

            // ViewModel and View
            services.AddTransient<MainViewModel>();
            services.AddTransient<MainWindow>();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // dispose the container cleanly on exit
            _serviceProvider.Dispose();
            base.OnExit(e);
        }
    }
}
