using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Avalonia.Controls;
using Avalonia.Media;

namespace FourierSim.Controls;

/// <summary>
/// simply displays points. (like a polyline without the lines)
/// </summary>
public class PointCollectionControl : Control
{
    #region Bindable Properties
    
    public static readonly StyledProperty<ObservableCollection<Point>> PointsProperty =
        AvaloniaProperty.Register<PointCollectionControl, ObservableCollection<Point>>(nameof(Points));
    public ObservableCollection<Point> Points
    {
        get => GetValue(PointsProperty);
        set => SetValue(PointsProperty, value);
    }                   

    public static readonly StyledProperty<IBrush> PointBrushProperty =
        AvaloniaProperty.Register<PointCollectionControl, IBrush>(nameof(PointBrush), Brushes.Black);
    public IBrush PointBrush
    {
        get => GetValue(PointBrushProperty);
        set => SetValue(PointBrushProperty, value);
    }
    
    public static readonly StyledProperty<IPen> PointPenProperty =
        AvaloniaProperty.Register<PointCollectionControl, IPen>(nameof(PointPen));
    public IPen PointPen
    {
        get => GetValue(PointPenProperty);
        set => SetValue(PointPenProperty, value);
    }
    public static readonly StyledProperty<double> PointSizeProperty =
        AvaloniaProperty.Register<PointCollectionControl, double>(nameof(PointSize), 5.0);
    public double PointSize
    {
        get => GetValue(PointSizeProperty);
        set => SetValue(PointSizeProperty, value);
    }
    
    #endregion
    
    public PointCollectionControl()
    {
        PointsProperty.Changed.AddClassHandler<PointCollectionControl>((x, e) => x.OnPointsChanged(e));
    }

    private void OnPointsChanged(AvaloniaPropertyChangedEventArgs e)
    {
        if (e.OldValue is ObservableCollection<Point> oldPoints)
        {
            oldPoints.CollectionChanged -= OnPointsCollectionChanged;
        }

        if (e.NewValue is ObservableCollection<Point> newPoints)
        {
            newPoints.CollectionChanged += OnPointsCollectionChanged;
        }

        InvalidateVisual(); //Redraw because the Property of the collection changed
    }

    private void OnPointsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        InvalidateVisual(); //Redraw because the content of the collection changed  
    }
    
    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (Points is null || Points.Count == 0) return;

        foreach (var point in Points)
        {
            context.DrawEllipse(PointBrush, PointPen, point, PointSize / 2, PointSize / 2);
        }
    }
    
}