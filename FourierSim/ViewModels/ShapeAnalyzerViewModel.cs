using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FourierSim.Models;

namespace FourierSim.ViewModels;

public partial class ShapeAnalyzerViewModel : ViewModelBase
{
    public bool IsDrawing { get; private set; }

    public ObservableCollection<Point> Points { get; set; } = new();
    
    public ObservableCollection<Point> UniPoints { get; set; } = new();
    
    [ObservableProperty]
    private bool isDrawingVisible = true;

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
        UniPoints.Clear();
        Points.Clear();
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
        Points.Add(Points[0]);
        Resample();
    }

    [RelayCommand]
    private void Update()
    {
        if(Points.Count == 0) return;
        
        UniPoints.Clear();
        Resample();  
    }
    
    private void Resample()
    {
        var signal = new ComplexSignal(Points);
        var uniformSignal = signal.Uniformify(SampleDensity);

        foreach (var item in uniformSignal)
        {
            UniPoints.Add(new(item.Value.Real, item.Value.Imaginary));
        }

        FourierSeries fourierSeries = new(uniformSignal, signal.Interval);
        var spectrum = fourierSeries.GetCoefficientSpectrum(-100,100);
        //update magnitude Plot:
        MagnitudePlot.Clear();
        foreach (var sample in spectrum)
        {
            double x = sample.Key;
            double y = sample.Value.Magnitude;
            MagnitudePlot.Add(new Point(x,y));
        }
    }

    public ObservableCollection<Point> MagnitudePlot { get; set; } = new();
    
    [RelayCommand]
    private void Analyse()
    {
        
    }
}