using System.Collections.ObjectModel;
using System.Diagnostics;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FourierSim.Models;
using FourierSim.Services;
using FourierSim.Views;
using Microsoft.Extensions.DependencyInjection;

namespace FourierSim.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IServiceProvider _service;
    private readonly INavigationService _navigationService;
    
    public MainWindowViewModel(IServiceProvider service, INavigationService navigationService)
    {
        _service = service;
        _navigationService = navigationService;
        
        _navigationService.NavigationRequested += OnNavigationRequested;
        
        //default view.. event. homeView machen ?
        CurrentViewModel = _service.GetRequiredService<ShapeAnalyzerViewModel>();
    }

    [ObservableProperty] 
    private ViewModelBase? currentViewModel;

    [RelayCommand]
    private void SwitchView(string viewName)
    {
        switch (viewName)
        {
            case "ShapeAnalyzer": 
                _navigationService.NavigateTo<ShapeAnalyzerViewModel>();
                break;
            
        }
    }
    
    private void OnNavigationRequested(object? sender, NavigationMessage e)
    {
        CurrentViewModel = _service.GetRequiredService(e.ViewModelType) as ViewModelBase;
    }
    
}