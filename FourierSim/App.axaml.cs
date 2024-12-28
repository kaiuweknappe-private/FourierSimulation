using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using FourierSim.Services;
using FourierSim.ViewModels;
using FourierSim.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;


namespace FourierSim;

public class App : Application // why not partial needed?
{
    private IHost? _host;
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                ConfigureServices(services); 
            })
            .Build();
        
        _host.Start();
        
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindowView()
            {
                DataContext = _host.Services.GetRequiredService<MainWindowViewModel>()
            };
            
            desktop.Exit += OnExit;
        }
        
        // Line below is needed to remove Avalonia data validation.
        // Without this line you will get duplicate validations from both Avalonia and CT
        BindingPlugins.DataValidators.RemoveAt(0);

        base.OnFrameworkInitializationCompleted();
    }

    private void OnExit(object? sender, ControlledApplicationLifetimeExitEventArgs e) => _host?.Dispose();

    private static void ConfigureServices(IServiceCollection services)
    {
        //ViewModels: (maybe use transient for refresh)
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<ShapeAnalyzerViewModel>();

        //Services:
        services.AddSingleton<INavigationService, NavigationService>();
        
    }
    
    
}