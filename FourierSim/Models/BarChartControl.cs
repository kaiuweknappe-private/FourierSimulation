using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace FourierSim.Models;

public class BarChartControl : Control, IDisposable
{
    #region Bindable Properties
    public static readonly StyledProperty<ObservableCollection<Point>> PointsProperty =
        AvaloniaProperty.Register<BarChartControl, ObservableCollection<Point>>(nameof(Points));
    public ObservableCollection<Point> Points
    {
        get => GetValue(PointsProperty);
        set => SetValue(PointsProperty, value);
    }
    
    public static readonly StyledProperty<string> XAxisLabelProperty =
        AvaloniaProperty.Register<BarChartControl, string>(nameof(XAxisLabel));
    public string XAxisLabel
    {
        get => GetValue(XAxisLabelProperty);
        set => SetValue(XAxisLabelProperty, value);
    }
    
    public static readonly StyledProperty<string> YAxisLabelProperty =
        AvaloniaProperty.Register<BarChartControl, string>(nameof(YAxisLabel));
    public string YAxisLabel
    {
        get => GetValue(YAxisLabelProperty);
        set => SetValue(YAxisLabelProperty, value);
    }
    
    public static readonly StyledProperty<double> HorizontalScaleProperty =
        AvaloniaProperty.Register<BarChartControl, double>(nameof(HorizontalScale), defaultValue: 15);
    public double HorizontalScale
    {
        get => GetValue(HorizontalScaleProperty);
        set => SetValue(HorizontalScaleProperty, value);
    }
    
    public static readonly StyledProperty<double> VerticalScaleProperty =
        AvaloniaProperty.Register<BarChartControl, double>(nameof(VerticalScale), defaultValue: 4);
    public double VerticalScale
    {
        get => GetValue(VerticalScaleProperty);
        set => SetValue(VerticalScaleProperty, value);
    }
    
    public static readonly StyledProperty<int?> SelectedBarProperty =
        AvaloniaProperty.Register<BarChartControl, int?>(nameof(SelectedBar));
    public int? SelectedBar
    {
        get => GetValue(SelectedBarProperty);
        set => SetValue(SelectedBarProperty, value);
    }
    
    public static readonly StyledProperty<IBrush?> BackgroundProperty =
        AvaloniaProperty.Register<BarChartControl, IBrush?>(nameof(Background));
    public IBrush? Background
    {
        get => GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
    }
    #endregion
    
    #region pros and Fields

    public double PaddingButtom => 20;
    public double YAxisPos => Bounds.Width / 2;
    public double XAxisPos => Bounds.Height - 20;
    
    #endregion
    
    
    public BarChartControl()
    {
        PointsProperty.Changed.AddClassHandler<BarChartControl>((x, e) => x.OnPointsChanged(e));
        
        PointerEntered += OnPointerEntered;
        PointerExited += OnPointerExited;
        
    }
    
    
    #region Update Collection handling
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
    #endregion

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        
        //background:
        context.DrawRectangle(Background, null, new Rect(Bounds.Size));
        
        DrawBars(context);
        DrawGraph(context);
    }

    private void DrawGraph(DrawingContext context)
    {
        // Draw axes
        var axisPen = new Pen(Brushes.Black, 2);
        context.DrawLine(axisPen, new Point(0, XAxisPos), new Point(Bounds.Width, XAxisPos)); // X-axis
        context.DrawLine(axisPen, new Point(YAxisPos, 0), new Point(YAxisPos, Bounds.Height)); // Y-axis

        // Draw axis labels
        var textFormat = new FormattedText(XAxisLabel ?? "", CultureInfo.CurrentCulture, FlowDirection.RightToLeft, Typeface.Default, 10, Brushes.Black);
        context.DrawText(textFormat, new Point(Bounds.Width - 80, Bounds.Height - 20));
        textFormat = new FormattedText(YAxisLabel ?? "", CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface.Default, 10, Brushes.Black);
        context.DrawText(textFormat, new Point(YAxisPos + 10, 10));
    }
    private void DrawBars(DrawingContext context)
    {
        if (Points is null) return;

        var barWidth = HorizontalScale;
        var maxBarHeight = Bounds.Height - PaddingButtom;

        foreach (var point in Points)
        {
            //check if in bounds
            if (Math.Abs(point.X * barWidth + barWidth / 2) > YAxisPos) continue;

            // Calculate bar position and size
            var barX = YAxisPos + (point.X * barWidth) - (barWidth / 2);
            var barHeight = Math.Min(point.Y * VerticalScale, maxBarHeight);
            var barY = XAxisPos - barHeight;
    
            // Bar fill
            var barBrush = SelectedBar == Convert.ToInt32(point.X) ? Brushes.OrangeRed : Brushes.Blue;
            var barRect = new Rect(barX, barY, barWidth, barHeight);
            context.FillRectangle(barBrush, barRect);

            // Outline
            var barPen = new Pen(Brushes.Black, 2);
            context.DrawRectangle(barPen, barRect);
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        
        if (Points is null) return;
        
        var point = e.GetPosition(this);
        var barWidth = HorizontalScale;
        var maxBarHeight = Bounds.Height - PaddingButtom;

        foreach (var pointData in Points)
        {
            //check if in bounds
            if (Math.Abs(pointData.X * barWidth + barWidth / 2) > YAxisPos) continue;
            
            // Calculate bar position and size
            var barX = YAxisPos + (pointData.X * barWidth) - (barWidth / 2);
            var barHeight = Math.Min(pointData.Y * VerticalScale, maxBarHeight);
            var barY = XAxisPos - barHeight;

            var barRect = new Rect(barX, barY, barWidth, barHeight);
            if (barRect.Contains(point))
            {
                SelectedBar = Convert.ToInt32(pointData.X); // Update the selected bar
                InvalidateVisual();
                break;
            }
        }
    }
    
    #region handling scaling interactivity
    private void OnPointerEntered(object? sender, PointerEventArgs e)
    {
        Focus();
        PointerWheelChanged += OnPointerWheelChanged;
        //Console.Write("Entered");
    }

    private void OnPointerExited(object? sender, PointerEventArgs e)
    {
        PointerWheelChanged -= OnPointerWheelChanged;
        //Console.Write("Exited");
    }

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            VerticalScale += e.Delta.Y * 0.2;
            VerticalScale = Math.Clamp(VerticalScale, 0.01, 5); //scroling limit
        }
        else
        {
            HorizontalScale += e.Delta.Y * 1;
            HorizontalScale = Math.Clamp(HorizontalScale, 5, 50); //scroling limit
        }
        
        e.Handled = true; // needed?
        InvalidateVisual();
    }
    #endregion
    
    #region taking auto space
    protected override Size MeasureOverride(Size availableSize)
    {
        double width = availableSize.Width;
        double height = availableSize.Height;
        
        if (!double.IsNaN(Width))
            width = Width;
        
        if (!double.IsNaN(Height)) 
            height = Height;
        
        width = Math.Clamp(width, MinWidth, MaxWidth);
        height = Math.Clamp(height, MinHeight, MaxHeight);

        return new Size(width, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        return finalSize;
    }
    #endregion

    public void Dispose()
    {
        PointerEntered -= OnPointerEntered;
        PointerExited -= OnPointerExited;
        PointerWheelChanged -= OnPointerWheelChanged;
    }
}