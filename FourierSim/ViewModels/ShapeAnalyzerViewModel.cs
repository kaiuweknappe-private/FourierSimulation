using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Numerics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FourierSim.Models;
using FourierSim.Services;

namespace FourierSim.ViewModels;

/// <summary>
/// This ViewModel and corresponding View got a bit long due to the various interaction-options.
/// Note: I thought about splitting this into even smaller peaces.
/// Although more complex logic is already encapsulated in a Service or custom Control.
/// One Idea is to take the drawing related part out into a separate usercontrol with vm.
/// In that case i would have to make e.g. the ResamplingService stateful and somehow manage that this vm knows when to update.">
/// lets the user draw a loop and analyze the frequencies it consists of.
/// </summary>
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
    [NotifyPropertyChangedFor(nameof(HasSelectedFrequency))]
    private int? selectedFrequency;

    public bool HasSelectedFrequency => SelectedFrequency.HasValue;
    
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
    private int simulationStepSize = 5;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FrequenciesAmount))]
    private double selectedLowerFrequency = -20;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FrequenciesAmount))]
    private double selectedUpperFrequency = 20;

    [ObservableProperty] 
    private bool limitFrequencies = false;
    partial void OnLimitFrequenciesChanged(bool oldValue, bool newValue)
    {
        if(oldValue != newValue)
            UpdateFrequencies();
    }

    [ObservableProperty] 
    private double selectedFrequenciesAmount;
    partial void OnSelectedFrequenciesAmountChanged(double value)
    {
        UpdateFrequencies();
    }
    
    public double FrequenciesAmount => Math.Abs(SelectedUpperFrequency - SelectedLowerFrequency) + 1; 
    
    [RelayCommand]
    private void UpdateFrequencies()
    {
        if(_spectrum.Count <= 0) return;

        //collect all phasors according to freq selection: (freq 0 is always added)  
        var newPhasorCollection = new List<Phasor>();
        newPhasorCollection.Add(new Phasor(0, _spectrum[0].Magnitude, _spectrum[0].Phase));
        for (var frequency = (int)SelectedLowerFrequency; frequency <= (int)SelectedUpperFrequency; frequency++) 
        {
            if (frequency != 0) 
                newPhasorCollection.Add(new Phasor(frequency, _spectrum[frequency].Magnitude, _spectrum[frequency].Phase));
        }
        
        //limit frequencies: only select the highest {SelectedFrequenciesAmount}-frequencies (by mag.) 
        if (LimitFrequencies)
        {
            //sort by ascending mag:
            newPhasorCollection.Sort((a, b) => a.Magnitude.CompareTo(b.Magnitude));
            
            //remove most insignificant to match {SelectedFrequenciesAmount}-frequencies
            newPhasorCollection.RemoveRange(0, newPhasorCollection.Count - (int)SelectedFrequenciesAmount);
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
            //maybe a random option for fun?
            _ => null
        };
        
        //refresh:
        UpdateFrequencies();
    }
    
    [RelayCommand]
    private void ChangeSimulationStepSize(string change)
    {
        if (!int.TryParse(change, out var value)) return;
        SimulationStepSize = Math.Clamp(SimulationStepSize + value, 1, 16);
    }
    
    [RelayCommand]
    private void ChangeSelectedFrequency(string change)
    {
        if (SelectedFrequency == null || !int.TryParse(change, out var value)) return;
        SelectedFrequency = Math.Clamp((int)SelectedFrequency + value, -100, 100);
    }
    
    #endregion
}