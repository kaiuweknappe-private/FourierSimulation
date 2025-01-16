using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Numerics;
using Avalonia.Collections;
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
public partial class ShapeAnalyzerViewModel(IResamplingService resamplingService, IFourierService fourierService) : ViewModelBase
{
    #region Drawing Related

    public bool IsDrawing { get; private set; }

    public ObservableCollection<Point> Points { get; set; } = new();

    [ObservableProperty] // to notify on reference changes as well
    private ObservableCollection<Point> resampledPoints = new();

    [ObservableProperty] private bool isDrawingVisible = true;

    [ObservableProperty] private bool isResampleVisible = false;

    [ObservableProperty] private double sampleDensity = 1;

    [ObservableProperty] private double loopLength;
    
    [ObservableProperty] 
    [NotifyPropertyChangedFor(nameof(NyquistShannonFrequency))]
    private int sampleCount;
    public int NyquistShannonFrequency => SampleCount / 2;

    partial void OnSampleDensityChanged(double value)
    {
        Update();
    }

    [RelayCommand]
    private void StartDrawing(Point point)
    {
        Reset();

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
        if (Points.Count <= 2) return;
        
        Analyze();
    }

    #endregion

    #region plot and animation related

    private Comparer<Phasor>? _phasorSortingOrder;
    private Dictionary<int, Complex> _spectrum = new();

    [ObservableProperty] 
    private ObservableCollection<Point> magnitudePlot = new();

    [ObservableProperty] 
    private ObservableCollection<Point> phasePlot = new();

    [ObservableProperty] 
    private ObservableCollection<Phasor> animationPhasors = new();

    private void Analyze()
    {
        var uniformSignal = resamplingService.GetResample(Points, SampleDensity, out var totalLength);
        LoopLength = totalLength;
        SampleCount = uniformSignal.Count;
        
        //update resampled-points collection:
        ResampledPoints = new ObservableCollection<Point>(uniformSignal.Values);
        _spectrum = fourierService.GetCoefficientSpectrum(uniformSignal, -100, 100);
        OnPropertyChanged(nameof(SelectedCoefficient));
        
        //update coefficient Plots:
        MagnitudePlot = new ObservableCollection<Point>(_spectrum.Select(frequency =>
                new Point(frequency.Key, frequency.Value.Magnitude)));
        PhasePlot = new ObservableCollection<Point>(_spectrum.Select(frequency =>
        {
            //convert phase from [-pi;pi] -> [0;360] deg. (for convenience)
            var y = frequency.Value.Phase;
            y = y < 0 ? y + 2 * Math.PI : y;
            return new Point(frequency.Key, y * 180 / Math.PI);
        }));

        //update phasors for animation:
        UpdateFrequencies();
    }

    [ObservableProperty] 
    [NotifyPropertyChangedFor(nameof(HasSelectedFrequency))]
    [NotifyPropertyChangedFor(nameof(SelectedCoefficient))]
    private int? selectedFrequency;

    public bool HasSelectedFrequency => SelectedFrequency.HasValue;

    public string SelectedCoefficient
    {
        get
        {
            if (SelectedFrequency is not int value)
                return string.Empty;

            var c = _spectrum[value];
            var phase = c.Phase;
            phase = phase < 0 ? phase + 2 * Math.PI : phase;
            return $"{c.Magnitude:F2} ∠ {c.Phase / Math.PI:F2} π ({phase * 180/Math.PI:F2} \u00b0)";
        }
    }
    
    [ObservableProperty]
    private bool isAnimationRunning = false;

    [ObservableProperty] 
    private bool isManuallySettingTime = false;
    partial void OnIsManuallySettingTimeChanged(bool value)
    {
        if (!value)
            TimeSelection = 0;
        else
            IsAnimationRunning = false;
    }

    //offsets the animation-start within the cycle
    [ObservableProperty] 
    [NotifyPropertyChangedFor(nameof(AnimationProgress))]
    private int timeSelection;

    public string AnimationProgress => ((double)TimeSelection / 10).ToString("F1") + " %";
    
    [ObservableProperty]
    private double timeFactor = .1;

    [ObservableProperty] 
    private bool phasorVisibility = true;

    [ObservableProperty] 
    private int simulationStepSize = 5;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FrequenciesAmount))]
    private int selectedLowerFrequency = -20;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FrequenciesAmount))]
    private int selectedUpperFrequency = 20;
    
    [ObservableProperty] 
    private bool limitFrequencies = false;
    partial void OnLimitFrequenciesChanged(bool oldValue, bool newValue)
    {
        if(oldValue != newValue)
            UpdateFrequencies();
    }

    [ObservableProperty] 
    private int selectedFrequenciesAmount = 2;
    partial void OnSelectedFrequenciesAmountChanged(int value)
    {
        UpdateFrequencies();
    }
    
    public int FrequenciesAmount => Math.Abs(SelectedUpperFrequency - SelectedLowerFrequency) + 1; 
    
    [RelayCommand]
    private void UpdateFrequencies()
    {
        if(_spectrum.Count <= 0) return;

        //collect all phasors according to freq selection: (freq 0 is always added)  
        var newPhasorCollection = new List<Phasor> { new Phasor(0, _spectrum[0].Magnitude, _spectrum[0].Phase) };
        for (var frequency = SelectedLowerFrequency; frequency <= SelectedUpperFrequency; frequency++) 
        {
            if (frequency != 0) 
                newPhasorCollection.Add(new Phasor(frequency, _spectrum[frequency].Magnitude, _spectrum[frequency].Phase));
        }
        
        //limit frequencies: only select the highest {SelectedFrequenciesAmount}-frequencies (by mag.) (probably not efficient)
        if (LimitFrequencies)
        {
            //sort by ascending mag:
            newPhasorCollection.Sort((a, b) => a.Magnitude.CompareTo(b.Magnitude));
            
            //remove most insignificant to match {SelectedFrequenciesAmount}-frequencies
            newPhasorCollection.RemoveRange(0, newPhasorCollection.Count - SelectedFrequenciesAmount);
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
            //maybe adding a random option for fun?
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

    [RelayCommand]
    private void Reset()
    {
        SelectedFrequency = null;
        IsAnimationRunning = false;
        LoopLength = 0;
        SampleCount = 0;
        
        Points.Clear();
        ResampledPoints.Clear();
        AnimationPhasors.Clear();
        MagnitudePlot.Clear();
        PhasePlot.Clear();
    }
}