using CommunityToolkit.Mvvm.Input;
using FourierSim.Services;

namespace FourierSim.ViewModels;

public partial class MainMenuViewModel(INavigationService navigationService) : ViewModelBase
{
    [RelayCommand]
    private void SwitchView(string viewName)
    {
        switch (viewName)
        {
            case "ShapeAnalyzer": 
                navigationService.NavigateTo<ShapeAnalyzerViewModel>();
                break;
            // ..
        }
    }
}