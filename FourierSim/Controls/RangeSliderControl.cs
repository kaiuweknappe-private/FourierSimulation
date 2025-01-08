using System.Globalization;
using System.Linq;
using System.Net;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;

namespace FourierSim.Controls;


public class RangeSliderControl : Control
{
    #region Binding Properties
    
    public static readonly StyledProperty<double> MinimumProperty = 
        AvaloniaProperty.Register<RangeSliderControl, double>(nameof(Minimum), defaultValue: 0);
    public double Minimum
    {
        get => GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public static readonly StyledProperty<double> MaximumProperty = 
        AvaloniaProperty.Register<RangeSliderControl, double>(nameof(Maximum), defaultValue: 100);
    public double Maximum
    {
        get => GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public static readonly StyledProperty<double> LowerValueProperty = 
        AvaloniaProperty.Register<RangeSliderControl, double>(nameof(LowerValue), defaultValue: 25, defaultBindingMode: BindingMode.TwoWay);
    public double LowerValue
    {
        get => GetValue(LowerValueProperty);
        set => SetValue(LowerValueProperty, value);
    }

    public static readonly StyledProperty<double> UpperValueProperty = 
        AvaloniaProperty.Register<RangeSliderControl, double>(nameof(UpperValue), defaultValue: 75, defaultBindingMode: BindingMode.TwoWay);
    public double UpperValue
    {
        get => GetValue(UpperValueProperty);
        set => SetValue(UpperValueProperty, value);
    }

    public static readonly StyledProperty<double?> TickIntervalProperty = 
        AvaloniaProperty.Register<RangeSliderControl, double?>(nameof(TickInterval), defaultValue: null);
    public double? TickInterval
    {
        get => GetValue(TickIntervalProperty);
        set => SetValue(TickIntervalProperty, value);
    }

    public static readonly StyledProperty<ICommand?> OnSelectionChangedCommandProperty = 
        AvaloniaProperty.Register<RangeSliderControl, ICommand?>(nameof(OnSelectionChangedCommand));
    public ICommand? OnSelectionChangedCommand
    {
        get => GetValue(OnSelectionChangedCommandProperty);
        set => SetValue(OnSelectionChangedCommandProperty, value);
    }
    
    #endregion

    #region Fields

    private Rect _lowerHandle;
    private Rect _upperHandle;

    private enum Selection { None, Lower, Upper, Both }
    private Selection _selection = Selection.None;
    
    private Point? _lastPointerPosition;
    private bool _isSelectionDirty = false;
    #endregion
    
    public RangeSliderControl()
    {
        //invalidate on prop changes?
        //LowerValueProperty.Changed.AddClassHandler<RangeSliderControl>((_, _) => { InvalidateVisual(); });
        //UpperValueProperty.Changed.AddClassHandler<RangeSliderControl>((_, _) => { InvalidateVisual(); });
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        
        //base bar:
        context.DrawRectangle(Brushes.DarkGray, null, new Rect(0, 10, Bounds.Width, 10));
        
        var maxRange = Math.Abs(Maximum - Minimum);
        var width = Bounds.Width;
        //handels
        var lowerPos = new Point((LowerValue - Minimum)/maxRange * width - 5, 10);
        _lowerHandle = new Rect(lowerPos, new Size(10, 20));
        context.DrawRectangle(Brushes.RoyalBlue, null, _lowerHandle);
        
        var upperPos = new Point((UpperValue - Minimum)/maxRange * width - 5, 0);
        _upperHandle = new Rect(upperPos, new Size(10, 20));
        context.DrawRectangle(Brushes.RoyalBlue, null, _upperHandle);
        
        //selected part of bar:
        var start = (LowerValue - Minimum) / maxRange * width;
        var selectedWidth = (UpperValue - Minimum) / maxRange * width - start;
        context.DrawRectangle(Brushes.RoyalBlue, null, new Rect(start+5, 10, selectedWidth-10, 10));
        
        //labels:
        var text = new FormattedText(LowerValue.ToString(), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface.Default, 15, Brushes.White);
        context.DrawText(text, new (lowerPos.X, lowerPos.Y - 20));
        text = new FormattedText(UpperValue.ToString(), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface.Default, 15, Brushes.White);
        context.DrawText(text, new (upperPos.X - 5, lowerPos.Y + 15));
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        var click = e.GetPosition(this);
        var middle = new Rect(new Point(_lowerHandle.X + 10, 10), new Point(_upperHandle.X, 20));
        
        if (_lowerHandle.Contains(click))
        {
            _selection = Selection.Lower;
        } 
        else if (_upperHandle.Contains(click))
        {
            _selection = Selection.Upper;
        }
        else if (middle.Contains(click))
        {
            _selection = Selection.Both;
        }
            
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _selection = Selection.None;
        _lastPointerPosition = null;

        if (_isSelectionDirty)
            OnSelectionChangedCommand?.Execute(null);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_selection == Selection.None) return;

        var pos = e.GetPosition(this);
        var maxRange = Math.Abs(Maximum - Minimum);
        var width = Bounds.Width;
        var newValue = Minimum + (pos.X / width * maxRange);
        
        if (_selection == Selection.Lower)
        {
            LowerValue = Math.Max(Minimum, Math.Min(newValue, UpperValue));
            _isSelectionDirty = true;
        }
        else if (_selection == Selection.Upper)
        {
            UpperValue = Math.Min(Maximum, Math.Max(newValue, LowerValue));
            _isSelectionDirty = true;
        }
        else if (_selection == Selection.Both) //move entire thing 
        {
            //Note: works bit clunky if u only want to move small amounts, room for improvement.. 
            // ah i got it.. its due to the rounding if u only move small amount and set the tickinterval to 1 it always rounds down.. maybe clamp the valueDelta to Min.: TickInterval
            if (_lastPointerPosition != null)
            {
                var posDelta = pos.X - _lastPointerPosition.Value.X;
                var valueDelta = posDelta / width * maxRange;
                //valueDelta = Math.Max(valueDelta, TickInterval ?? double.NegativeInfinity);
                LowerValue = Math.Max(Minimum, Math.Min(LowerValue + valueDelta, UpperValue));
                UpperValue = Math.Min(Maximum, Math.Max(UpperValue + valueDelta, LowerValue));
                _isSelectionDirty = true;
            }
            _lastPointerPosition = pos;
        }
        
        //ensure TickInterval:
        if (TickInterval != null && TickInterval != 0)
        {
            LowerValue = Math.Round(LowerValue / TickInterval.Value) * TickInterval.Value;
            var b = Math.Round(UpperValue / TickInterval.Value) * TickInterval.Value;
            UpperValue = b;
        }
        
        InvalidateVisual();
    }
}