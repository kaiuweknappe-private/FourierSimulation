using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;

namespace FourierSim.Controls;

/// <summary>
/// wasnt happy with LivePlot or OxyChart. So did my own custom Bar Chart here.
/// not sure if that was the best idea.
/// i would call it semi-generic (which makes also only semi sense).
/// if u want to make something like this totally generic, u need to put way more work into this.
/// but since it is for educational purposes only, its fine i guess. 
/// </summary>
public class BarChartControl : Control, IDisposable
{
    #region Bindable Properties
    public static readonly StyledProperty<ObservableCollection<Point>?> PointsProperty =
        AvaloniaProperty.Register<BarChartControl, ObservableCollection<Point>?>(nameof(Points), defaultValue: null);
    public ObservableCollection<Point>? Points
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
        AvaloniaProperty.Register<BarChartControl, int?>(nameof(SelectedBar), defaultValue: null, defaultBindingMode: BindingMode.TwoWay); 
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

    //requires to click into the control first, to then be able to scroll/scale the chart
    //so it doesnt interfere with a scrollview or something
    //loses this "inline" focus on pointerExit
    private bool _isFocused;

    //private ToolTip _toolTip;
    #endregion
    
    public BarChartControl()
    {
        PointsProperty.Changed.AddClassHandler<BarChartControl>((x, e) => x.OnPointsChanged(e));
        SelectedBarProperty.Changed.AddClassHandler<BarChartControl>((_, _) => InvalidateVisual());
        
        //init tooltip
        /*_toolTip = new ToolTip()
        {
            IsVisible = false,
            Background = new SolidColorBrush(Colors.OrangeRed)
        };
        ToolTip.SetTip(this, _toolTip);*/

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

        InvalidateVisual(); //Redraw because the instance of the collection changed
    }

    private void OnPointsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        InvalidateVisual(); //Redraw because the content of the collection changed  
    }
    #endregion

    #region Rendering itself
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

        //arrows at end of axis
        //x-axis 
        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            var middlePoint = new Point(Bounds.Width - 10, Bounds.Height - PaddingButtom);
            ctx.BeginFigure(new Point(middlePoint.X, middlePoint.Y -5), true);
            ctx.LineTo(new Point(middlePoint.X + 10, middlePoint.Y));
            ctx.LineTo(new Point(middlePoint.X, middlePoint.Y +5));
            ctx.EndFigure(true);
        }
        context.DrawGeometry(Brushes.Black, null, geometry);
        //y-axis
        using (var ctx = geometry.Open())
        {
            var middlePoint = new Point(YAxisPos, 10);
            ctx.BeginFigure(new Point(middlePoint.X - 5, middlePoint.Y), true);
            ctx.LineTo(new Point(middlePoint.X, 0));
            ctx.LineTo(new Point(middlePoint.X + 5, middlePoint.Y));
            ctx.EndFigure(true);
        }
        context.DrawGeometry(Brushes.Black, null, geometry);
        
        // Draw axis labels
        var textFormat = new FormattedText(XAxisLabel, CultureInfo.CurrentCulture, FlowDirection.RightToLeft, new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Bold), 13, Brushes.Black);
        context.DrawText(textFormat, new Point(Bounds.Width - 110, Bounds.Height - PaddingButtom - 25));
        textFormat = new FormattedText(YAxisLabel, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Bold), 13, Brushes.Black);
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
            var barPen = new Pen(Brushes.Gray, 1);
            context.DrawRectangle(barPen, barRect);

            //x-axies label
            if (barWidth >= 20 || barWidth >= 5 && (int)point.X % 5 == 0)
            {
                var xTagPos = new Point(YAxisPos + point.X * barWidth, Bounds.Height - PaddingButtom + 4);
                var text = new FormattedText(((int)point.X).ToString(), CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight, Typeface.Default, 10, Brushes.Black);
                context.DrawText(text, xTagPos);
                var rect = new Rect(new Point(xTagPos.X, xTagPos.Y - 4), new Size(1, 4));
                context.DrawRectangle(new Pen(Brushes.Black, 1), rect);
            }
        }
    }
    
    #endregion 

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        
        if (Points == null || !Points.Any()) return;
        //set focus so its scrollable now
        _isFocused = true;
        
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
                //Toggle the selection of that bar
                var bar = Convert.ToInt32(pointData.X);
                SelectedBar = bar == SelectedBar ? null : bar;
                
                InvalidateVisual();
                break;
            }
        }
    }
    
    #region handling scaling interactivity
    
    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
        
        PointerWheelChanged += OnPointerWheelChanged;
        //Console.Write("Entered");
    }
    
    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        
        _isFocused = false;
        PointerWheelChanged -= OnPointerWheelChanged;
        //Console.Write("Exited");
    }

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if(!_isFocused) return;
        
        if (e.KeyModifiers.HasFlag(KeyModifiers.Shift)) //vertical
        {
            VerticalScale += e.Delta.Y * 0.1; //scrolling sensitivity
            VerticalScale = Math.Clamp(VerticalScale, 0.01, 10); //scrolling limit
        }
        else //horizontal
        {
            HorizontalScale += e.Delta.Y * 1; //scrolling sensitivity
            HorizontalScale = Math.Clamp(HorizontalScale, 5, 50); //scrolling limit
        }
        
        //so nothing else reacts to the scroll routed event
        e.Handled = true; 
        
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
        PointerWheelChanged -= OnPointerWheelChanged;
        
        if(Points != null)
            Points.CollectionChanged -= OnPointsCollectionChanged;
    }
    
}