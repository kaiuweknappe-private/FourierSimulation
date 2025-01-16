using System.Collections.ObjectModel;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FourierSim.Models;
using FourierSim.Services;
using FourierSim.Views;
using Microsoft.Extensions.DependencyInjection;

namespace FourierSim.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IServiceProvider _services;
    private readonly INavigationService _navigationService;
    
    public MainWindowViewModel(IServiceProvider services, INavigationService navigationService)
    {
        _services = services;
        _navigationService = navigationService;
        
        _navigationService.NavigationRequested += OnNavigationRequested;
        
        CurrentViewModel = _services.GetRequiredService<MainMenuViewModel>();
    }

    [ObservableProperty] 
    private ViewModelBase? currentViewModel;

    private void OnNavigationRequested(object? sender, NavigationMessage e)
    {
        CurrentViewModel = _services.GetRequiredService(e.ViewModelType) as ViewModelBase;
    }
    
    [RelayCommand]
    private void SwitchView(string viewName)
    {
        switch (viewName)
        {
            case "ShapeAnalyzer": 
                _navigationService.NavigateTo<ShapeAnalyzerViewModel>();
                break;
            case "MainMenu": 
                _navigationService.NavigateTo<MainMenuViewModel>();
                break;
            // ..
        }
    }

    [RelayCommand]
    private void Exit()
    {
        if (Application.Current!.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.Shutdown();
    }
}