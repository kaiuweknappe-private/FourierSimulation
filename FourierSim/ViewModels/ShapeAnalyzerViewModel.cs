using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Numerics;
using Avalonia.Input.TextInput;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FourierSim.Models;
using FourierSim.Services;
using Tmds.DBus.Protocol;

namespace FourierSim.ViewModels;

public partial class ShapeAnalyzerViewModel : ViewModelBase
{
    #region Drawing Related

    public bool IsDrawing { get; private set; }

    public ObservableCollection<Point> Points { get; set; } = new();

    [ObservableProperty] // to notify on reference changes as well
    private ObservableCollection<Point> resampledPoints = new();

    [ObservableProperty] private bool isDrawingVisible = true;

    [ObservableProperty] private bool isResampleVisible = false;

    [ObservableProperty] private double sampleDensity = 2.0;

    partial void OnSampleDensityChanged(double oldValue, double newValue)
    {
        if (Math.Abs(oldValue - newValue) > 0.01)
            Update();
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
        IsAnimationRunning = false;
        AnimationPhasors.Clear();

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
        Points.Add(Points[0]); //close loop

        Update();
    }

    private void Update()
    {
        if (Points.Count <= 1) return;

        Analyze();
    }

    #endregion

    #region plot and animation related

    private readonly IResamplingService _resamplingService = new ResamplingService();
    private readonly IFourierService _fourierService = new FourierSeries();

    private Comparer<Phasor>? _phasorSortingOrder;

    private Dictionary<int, Complex> _spectrum = new();
    public ObservableCollection<Point> MagnitudePlot { get; set; } = new();
    public ObservableCollection<Point> PhasePlot { get; set; } = new();

    [ObservableProperty]
    private ObservableCollection<Phasor> animationPhasors = new();
    
    private void Analyze()
    {
        var uniformSignal = _resamplingService.GetResample(Points, SampleDensity);
        
        //update resampled-points collection:
        ResampledPoints = new ObservableCollection<Point>(uniformSignal.Values);
        _spectrum = _fourierService.GetCoefficientSpectrum(uniformSignal, -100, 100);
        
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
        
        //update phasors for animation:
        UpdateFrequencies();    
    }

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
    private string selectedCoefficient = string.Empty;

    [ObservableProperty]
    private bool isAnimationRunning = false;
    
    [ObservableProperty]
    private double timeFactor = .1;

    [ObservableProperty] 
    private int simulationStepSize = 10;

    [ObservableProperty]
    private double selectedLowerFrequency = -20;
    [ObservableProperty] 
    private double selectedUpperFrequency = 20;

    [RelayCommand]
    private void UpdateFrequencies()
    {
        if(_spectrum.Count <= 0) return;

        //collect all phasors according to freq selection: (freq 0 is always added)  
        var newPhasorCollection = new List<Phasor>();
        newPhasorCollection.Add(new Phasor(0, _spectrum[0].Magnitude, _spectrum[0].Phase));
        for (var f = SelectedLowerFrequency; f <= SelectedUpperFrequency; f++) 
        {
            var frequency = Convert.ToInt32(f);
            if (frequency == 0) continue;
            newPhasorCollection.Add(new Phasor(frequency, _spectrum[frequency].Magnitude, _spectrum[frequency].Phase));
        }
        
        //sort and update PhasorCollection 
        if(_phasorSortingOrder != null)
            newPhasorCollection.Sort(_phasorSortingOrder);
        AnimationPhasors = new ObservableCollection<Phasor>(newPhasorCollection);
    }

    [RelayCommand]
    private void SwitchPhasorSorting(int selectedIndex)
    {
        //sets the IComparer<Phasor> that is used to sort the Phasor-Collection for the Animation:
        _phasorSortingOrder = selectedIndex switch
        {
            //by descending magnitude:
            0 => Comparer<Phasor>.Create((a, b) => a.Magnitude.CompareTo(b.Magnitude) * -1),
            //by ascending angular velocity:
            1 => Comparer<Phasor>.Create((a, b) => Math.Abs(a.Frequency).CompareTo(Math.Abs(b.Frequency))),
            _ => null
        };
        
        //refresh:
        UpdateFrequencies();
    }
    
    [RelayCommand]
    private void ChangeSimulationStepSize(string change)
    {
        if (!int.TryParse(change, out var value)) return;
        SimulationStepSize += value;
        SimulationStepSize = Math.Clamp(SimulationStepSize, 1, 16);
    }
    
    #endregion
}