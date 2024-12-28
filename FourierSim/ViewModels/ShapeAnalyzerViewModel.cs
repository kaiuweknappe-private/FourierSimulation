using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Numerics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FourierSim.Models;
using Tmds.DBus.Protocol;

namespace FourierSim.ViewModels;

public partial class ShapeAnalyzerViewModel : ViewModelBase
{
    #region Drawing Related
    public bool IsDrawing { get; private set; }

    public ObservableCollection<Point> Points { get; set; } = new();
    
    public ObservableCollection<Point> ResampledPoints { get; set; } = new();
    
    [ObservableProperty]
    private bool isDrawingVisible = true;

    [ObservableProperty] 
    private bool isResampleVisible = false;

    [ObservableProperty]
    private double sampleDensity = 1.0;
    
    partial void OnSampleDensityChanged(double oldValue, double newValue)
    {
        if (Math.Abs(oldValue - newValue) > 0.01)
            UpdateCommand.Execute(null);
    }

    [RelayCommand]
    private void StartDrawing(Point point)
    {
        //reset:
        ResampledPoints.Clear();
        Points.Clear();
        MagnitudePlot.Clear();
        PhasePlot.Clear();
        SelectedFrequency = null;
        
        Points.Add(point);
        IsDrawing = true;
    }

    [RelayCommand(CanExecute = nameof(IsDrawing))]
    private void UpdateDrawing(Point point)
    {
        Points.Add(point);
    }

    [RelayCommand]
    private void FinishDrawing()
    {
        IsDrawing = false;
        if (Points.Count <= 1) return;
        
        Points.Add(Points[0]);
        Resample();
    }

    [RelayCommand]
    private void Update()
    {
        if(Points.Count == 0) return;
        
        ResampledPoints.Clear();
        Resample();  
    }
    #endregion
    
    private void Resample()
    {
        var signal = new ComplexSignal(Points);
        var uniformSignal = signal.Uniformify(SampleDensity);

        foreach (var item in uniformSignal) 
            ResampledPoints.Add(new(item.Value.Real, item.Value.Imaginary));

        FourierSeries fourierSeries = new(uniformSignal, signal.Interval);
        _spectrum = fourierSeries.GetCoefficientSpectrum(-100, 100);
        
        //update coefficient Plots:
        MagnitudePlot.Clear();
        PhasePlot.Clear();
        foreach (var sample in _spectrum)
        {
            double x = sample.Key;
            double y = sample.Value.Magnitude;
            MagnitudePlot.Add(new Point(x,y));
            y = sample.Value.Phase;
            PhasePlot.Add(new Point(x,y));
        }
        
        //update animation phasors:
        AnimationPhasors.Clear();
        AnimationPhasors.Add(new Phasor(0, _spectrum[0].Magnitude, _spectrum[0].Phase));
        for (int f = 1; f <= 50; f++)
        {
            AnimationPhasors.Add(new Phasor(f, _spectrum[f].Magnitude, _spectrum[f].Phase));
            AnimationPhasors.Add(new Phasor(-f, _spectrum[-f].Magnitude, _spectrum[-f].Phase));
        }
        OnPropertyChanged(nameof(AnimationPhasors));
    }

    private Dictionary<int, Complex> _spectrum = new();
    public ObservableCollection<Point> MagnitudePlot { get; private set; } = new();
    public ObservableCollection<Point> PhasePlot { get; private set; } = new();
    public ObservableCollection<Phasor> AnimationPhasors { get; private set; } = new();

    [ObservableProperty]
    private int? selectedFrequency;

    partial void OnSelectedFrequencyChanged(int? value)
    {
        if (value is null)
        {
            SelectedCoefficient = string.Empty;
            return;
        }

        var c = _spectrum[(int)value];
        SelectedCoefficient = $"{c.Magnitude:F2} ∠ {c.Phase/Math.PI:F2} π";

    }

    [ObservableProperty]
    private string selectedCoefficient;

    [ObservableProperty]
    private bool isAnimationRunning = false;
    
    [RelayCommand]
    private void ToggleAnimation()
    {
        IsAnimationRunning = !IsAnimationRunning;
    }
}