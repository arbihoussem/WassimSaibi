using System.Windows;

namespace LogAggregator.WPF
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Catch any unhandled exception and show a message box instead of crashing silently
            DispatcherUnhandledException += (_, args) =>
            {
                MessageBox.Show(
                    $"Unexpected error:\n\n{args.Exception.Message}",
                    "Log Aggregator — Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                args.Handled = true;
            };
        }
    }
}
