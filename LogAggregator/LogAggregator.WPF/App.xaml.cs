using System.IO;
using System.Windows;
using Ninject;
using LogAggregator.Core.Domain;
using LogAggregator.Core.Infrastructure.Parsers;
using LogAggregator.Core.Infrastructure.Services;
using LogAggregator.Plugins;
using LogAggregator.WPF.ViewModels;

namespace LogAggregator.WPF
{
    public partial class App : Application
    {
        public static IKernel Kernel = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            Kernel = new StandardKernel();
            ConfigureServices(Kernel);

            // Global exception handler (same behavior as before)
            DispatcherUnhandledException += (_, args) =>
            {
                MessageBox.Show(
                    $"Unexpected error:\n\n{args.Exception.Message}",
                    "Log Aggregator — Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                args.Handled = true;
            };

            var mainWindow = Kernel.Get<MainWindow>();
            mainWindow.Show();
        }

        private static void ConfigureServices(IKernel kernel)
        {
        
            // Core services — SINGLETON
      

            kernel.Bind<IAggregationService>()
                  .To<AggregationService>()
                  .InSingletonScope();

            kernel.Bind<IReportService>()
                  .To<ReportService>()
                  .InSingletonScope();

            kernel.Bind<ILogFileSource>()
                  .To<FileWatcherService>()
                  .InSingletonScope();

     
            // ParserRegistry — SINGLETON with plugins
      

            kernel.Bind<ParserRegistry>()
                  .ToMethod(_ =>
                  {
                      var registry = new ParserRegistry();

                      // Built-in parsers
                      registry.Register(new TextLogParser());
                      registry.Register(new JsonLogParser());
                      registry.Register(new XmlLogParser());

                      // Plugins from /plugins directory
                      var pluginDir = Path.Combine(
                          AppDomain.CurrentDomain.BaseDirectory,
                          "plugins");

                      var loader = new PluginLoader();
                      foreach (var plugin in loader.LoadFromDirectory(pluginDir))
                          registry.Register(plugin);

                      return registry;
                  })
                  .InSingletonScope();

    
            // ViewModels & Views — TRANSIENT
  

            kernel.Bind<MainViewModel>().ToSelf();
            kernel.Bind<MainWindow>().ToSelf();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Kernel.Dispose();
            base.OnExit(e);
        }
    }
}
