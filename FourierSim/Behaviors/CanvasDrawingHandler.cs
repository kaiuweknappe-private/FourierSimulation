using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Xaml.Interactivity;

namespace FourierSim.Behaviors;

public class CanvasDrawingHandler : Behavior<Canvas>
{
    public static readonly StyledProperty<ICommand?> StartCommandProperty =
        AvaloniaProperty.Register<CanvasDrawingHandler, ICommand?>(nameof(StartCommand));

    public static readonly StyledProperty<ICommand?> MoveCommandProperty =
        AvaloniaProperty.Register<CanvasDrawingHandler, ICommand?>(nameof(MoveCommand));

    public static readonly StyledProperty<ICommand?> StopCommandProperty =
        AvaloniaProperty.Register<CanvasDrawingHandler, ICommand?>(nameof(StopCommand));

    public ICommand? StartCommand
    {
        get => GetValue(StartCommandProperty);
        set => SetValue(StartCommandProperty, value);
    }

    public ICommand? MoveCommand
    {
        get => GetValue(MoveCommandProperty);
        set => SetValue(MoveCommandProperty, value);
    }

    public ICommand? StopCommand
    {
        get => GetValue(StopCommandProperty);
        set => SetValue(StopCommandProperty, value);
    }
        
    protected override void OnAttached()
    {
        base.OnAttached();
        if (AssociatedObject == null) return;
        AssociatedObject.PointerPressed += OnPointerPressed;
        AssociatedObject.PointerMoved += OnPointerMoved;
        AssociatedObject.PointerReleased += OnPointerReleased;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        if (AssociatedObject == null) return;
        AssociatedObject.PointerPressed -= OnPointerPressed;
        AssociatedObject.PointerMoved -= OnPointerMoved;
        AssociatedObject.PointerReleased -= OnPointerReleased;
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if(StartCommand?.CanExecute(e.GetPosition(AssociatedObject)) ?? false)
            StartCommand?.Execute(e.GetPosition(AssociatedObject));
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!(MoveCommand?.CanExecute(e.GetPosition(AssociatedObject)) ?? false)) return;
        
        var position = e.GetPosition(AssociatedObject);
        if (AssociatedObject is not Canvas canvas ||
            position.X < 0 || position.Y < 0 || position.X > canvas.Bounds.Width || position.Y > canvas.Bounds.Height)
        {
            StopCommand?.Execute(null);
        } 
        else
            MoveCommand.Execute(position);
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        StopCommand?.Execute(null);
    }
        
}