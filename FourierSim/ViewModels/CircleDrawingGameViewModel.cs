using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FourierSim.Services;

namespace FourierSim.ViewModels;

public partial class CircleDrawingGameViewModel(IResamplingService resamplingService, IFourierService fourierService) : ViewModelBase
{
    private readonly Dictionary<int, double> _highScores = new();
    
    [ObservableProperty] 
    private double currentScore;
    
    public double CurrentHighScore => _highScores.GetValueOrDefault(SelectedFrequency, 0);

    [ObservableProperty] 
    [NotifyPropertyChangedFor(nameof(CurrentHighScore))]
    [NotifyDataErrorInfo]
    [Required(AllowEmptyStrings = false)]
    [RegularExpression(@"^-?(?:[1-9][0-9]?|100)$", ErrorMessage = "Value must be between -100 and 100, excluding zero.")]
    private int selectedFrequency = -1;

    partial void OnSelectedFrequencyChanged(int value)
    {
        CurrentScore = 0;
        if(Points.Count > 2)
            CalculateNewScore();
    }

    public ObservableCollection<Point> Points { get; } = new();

    public bool CanDraw => !HasErrors;
    public bool IsDrawing { get; private set; }

    
    [RelayCommand(CanExecute = nameof(CanDraw))]
    private void StartDrawing(Point point)
    {
        Points.Clear();
        CurrentScore = 0;

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
        
        if (Points.Count <= 2) return;
        
        Points.Add(Points[0]);
        CalculateNewScore();
    }
    
    private void CalculateNewScore()
    {
        var resample = resamplingService.GetResample(Points, 1,out _);
        var spectrum = fourierService.GetCoefficientSpectrum(resample, -100, 100);

        var accumulatedMagnitude = spectrum.Sum(pair => pair.Key == 0 ? 0 : pair.Value.Magnitude);
        CurrentScore = spectrum[SelectedFrequency].Magnitude / accumulatedMagnitude;
        CurrentScore *= 100;

        if (CurrentScore > CurrentHighScore)
        {
            _highScores[SelectedFrequency] = CurrentScore;
            OnPropertyChanged(nameof(CurrentHighScore));
        }
    }
}