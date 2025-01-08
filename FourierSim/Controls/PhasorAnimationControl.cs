using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Threading;
using FourierSim.Models;

namespace FourierSim.Controls;

public class PhasorAnimationControl : Control
{
    #region Properties

    public static readonly StyledProperty<ObservableCollection<Phasor>> PhasorsProperty =
        AvaloniaProperty.Register<PhasorAnimationControl, ObservableCollection<Phasor>>(nameof(Phasors), defaultBindingMode: BindingMode.TwoWay);
    public ObservableCollection<Phasor> Phasors
    {
        get => GetValue(PhasorsProperty);
        set => SetValue(PhasorsProperty, value);
    }

    //in milliseconds:
    public static readonly StyledProperty<int> SimulationStepSizeProperty = 
        AvaloniaProperty.Register<PhasorAnimationControl, int>(nameof(SimulationStepSize), defaultValue: 10);
    public int SimulationStepSize
    {
        get => GetValue(SimulationStepSizeProperty);
        set => SetValue(SimulationStepSizeProperty, value);
    }

    public static readonly StyledProperty<double> TimeFactorProperty =
        AvaloniaProperty.Register<PhasorAnimationControl, double>(nameof(TimeFactor), defaultValue: 1.0);
    public double TimeFactor
    {
        get => GetValue(TimeFactorProperty);
        set => SetValue(TimeFactorProperty, value);
    }

    public static readonly StyledProperty<int?> SelectedFrequencyProperty =
        AvaloniaProperty.Register<PhasorAnimationControl, int?>(nameof(SelectedFrequency), defaultValue: null);
    public int? SelectedFrequency
    {
        get => GetValue(SelectedFrequencyProperty);
        set => SetValue(SelectedFrequencyProperty, value);
    }
    
    public static readonly StyledProperty<bool> IsRunningProperty = 
        AvaloniaProperty.Register<PhasorAnimationControl, bool>(nameof(IsRunning), defaultValue: false);
    public bool IsRunning
    {
        get => GetValue(IsRunningProperty);
        set => SetValue(IsRunningProperty, value);
    }

    #endregion
    
    #region fields

    private readonly DispatcherTimer _animationTimer; //refreshing the visual
    private readonly Stopwatch _stopwatch = new(); //for independent simulation time
    private int _lastUpdateTime;
    private readonly List<Point> _trail = new();
    #endregion

    public PhasorAnimationControl()
    {
        _animationTimer = new DispatcherTimer()
        {
            Interval = TimeSpan.FromMilliseconds(16) // 60 hz -isch
        };
        _animationTimer.Tick += (_, __) => InvalidateVisual();
        
        IsRunningProperty.Changed.AddClassHandler<PhasorAnimationControl, bool>((_, e) =>
        {
            if(e.OldValue.Value != e.NewValue.Value)
                OnIsRunningChanged(e.NewValue.Value);
        });
        
        PhasorsProperty.Changed.AddClassHandler<PhasorAnimationControl>( (_, e) =>
        {
            if (e.OldValue is ObservableCollection<Phasor> oldPoints)
            {
                oldPoints.CollectionChanged -= OnPhasorsCollectionChanged;
            }
            if (e.NewValue is ObservableCollection<Phasor> newPoints)
            {
                newPoints.CollectionChanged += OnPhasorsCollectionChanged;
            }
            
            Restart();
            InvalidateVisual();
        });

    }

    private void OnPhasorsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        Restart();
        InvalidateVisual();
    }
    
    private void Restart()
    {
        _trail.Clear();
        _lastUpdateTime = 0;
        if (IsRunning)
            _stopwatch.Restart();
        else
            _stopwatch.Reset();
    }
    
    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var currentTime = (int)(TimeFactor * _stopwatch.ElapsedMilliseconds);
        
        //statefull part (interpolation):
            //independent simulation frequency reqiered to ensure consistency/correctness
        if (currentTime > 1000) //new revolution => reset
        {
            Restart();
            currentTime = 0;
        }
        else
        {
            Interpolation(currentTime);
        }
        
        //stateless part ("regular" rendering):
            //phasors, background, drawing the trail
        Rendering(context, currentTime);
    }

    private void Interpolation(int currentTime)
    {
        while (_lastUpdateTime + SimulationStepSize <= currentTime)
        {
            var interpolationTime = ((double)_lastUpdateTime + SimulationStepSize) / 1000; // in seconds
            var point = new Point(0, 0);
            
            foreach (var phasor in Phasors)
            {
                var r = phasor.Magnitude;
                var phi = 2.0 * Math.PI * interpolationTime * phasor.Frequency + phasor.Phase;
                  
                point += new Point(Math.Cos(phi) * r, Math.Sin(phi) * r); //correct order x/re <-> y/im ?
            }
            
            _trail.Add(point);
            _lastUpdateTime += SimulationStepSize;
        }
    }
    private void Rendering(DrawingContext context, int currentTime)
    {
        //background:
        context.DrawRectangle(new SolidColorBrush(Colors.Beige), null, new Rect(Bounds.Size));
        
        //draw trail:
        if(_trail.Count > 0)
        {
            var lastPoint = _trail.First();
            foreach (var point in _trail.Skip(1))
            {
                context.DrawLine(new Pen(new SolidColorBrush(Colors.Blue), 2), lastPoint, point);
                lastPoint = point;
            }
        }
        
        //draw phasors:
        var startPoint = new Point(0, 0);
        foreach (var phasor in Phasors)
        {
            var r = phasor.Magnitude;
            var phi = 2.0 * Math.PI * currentTime / 1000 * phasor.Frequency + phasor.Phase;
            var endPoint = startPoint + new Point(Math.Cos(phi) * r, Math.Sin(phi) * r); 
                
            var color = SelectedFrequency == phasor.Frequency ? Colors.OrangeRed : Colors.Black;
            context.DrawLine(new Pen(new SolidColorBrush(color), 2), startPoint, endPoint);
            if(phasor.Frequency != 0)
                context.DrawEllipse(null, new Pen(new SolidColorBrush(color), 1), startPoint, r,r );
            
            startPoint = endPoint;
        }
    }
    
    private void OnIsRunningChanged(bool value)
    {
        if (value) // enabling
        {
            _animationTimer.Start();
            //Restart();
            _stopwatch.Start();
        }
        else // disabling
        {
            _animationTimer.Stop();
            _stopwatch.Stop();
        }
    }
}