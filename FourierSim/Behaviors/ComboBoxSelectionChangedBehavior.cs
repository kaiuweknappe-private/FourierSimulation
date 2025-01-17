using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Xaml.Interactivity;

namespace FourierSim.Behaviors;

/// <summary>
/// lets u bind a command to the OnSelectionChanged event of a ComboBox
/// ignores the canExecute
/// (passes the SelectedIndex as parameter)
/// </summary>
public class ComboBoxSelectionChangedBehavior : Behavior<ComboBox>
{
    public static readonly StyledProperty<ICommand?> OnSelectionChangedCommandProperty = 
        AvaloniaProperty.Register<ComboBoxSelectionChangedBehavior, ICommand?>(nameof(OnSelectionChangedCommand));
    public ICommand? OnSelectionChangedCommand
    {
        get => GetValue(OnSelectionChangedCommandProperty);
        set => SetValue(OnSelectionChangedCommandProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        if (AssociatedObject == null) return;
         
        AssociatedObject.SelectionChanged += OnSelectionChanged;
    }

    public ComboBoxSelectionChangedBehavior()
    {
        //initial trigger when the command is set:
        OnSelectionChangedCommandProperty.Changed.AddClassHandler<ComboBoxSelectionChangedBehavior>((_, _) =>
        {
            //might exclude SelectedIndex: -1 (None)
            OnSelectionChanged(this, null);
        });
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        
        if (AssociatedObject == null) return;
        AssociatedObject.SelectionChanged -= OnSelectionChanged;
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs? e)
    {
        if (AssociatedObject == null) return;
        
        //not checking any CanExecute
        //doesnt work if the T of the relaycommand is a value type and i invoke the canExecute with null-> returns false even if the private canexecute predicate is null (in which case true would be expected)
        // -> in which case i cant differentiate between whether there is no can execute or it just evaluates to false .. not sure how to handle this
        //would need to get the underlaying type if its a RelayCommand<> and call .CanExecute() with the default of that type
        //this seems not like a good solution to this though, in my costum case i dont need it anyways
        //if(OnSelectionChangedCommand?.CanExecute(null) ?? false)
        
        OnSelectionChangedCommand?.Execute(AssociatedObject.SelectedIndex);
    }
    
}
