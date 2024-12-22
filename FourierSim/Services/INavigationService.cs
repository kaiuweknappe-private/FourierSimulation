using FourierSim.ViewModels;

namespace FourierSim.Services;

public interface INavigationService
{
    event EventHandler<NavigationMessage> NavigationRequested;

    void NavigateTo<TViewModel>() where TViewModel : ViewModelBase;
}

public class NavigationMessage(Type viewModelType) : EventArgs
{
    public Type ViewModelType { get; } = viewModelType;
}

public class NavigationService : INavigationService
{
    public event EventHandler<NavigationMessage>? NavigationRequested;

    public void NavigateTo<TViewModel>() where TViewModel : ViewModelBase
    {
        NavigationRequested?.Invoke(this, new NavigationMessage(typeof(TViewModel)));
    }

}