using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace FourierSim.Services;

/// <summary>
/// made to work with an input signal that is mapped between 0 and 1 and sampled uniformly.
/// calculates the complex coefficient (magnitude and phase information) for one or a range of arbitrary integer frequency's
/// </summary>
public interface IFourierService
{
    Dictionary<int, Complex> GetCoefficientSpectrum(Dictionary<double, Point> signal, int startFrequency, int endFrequency);
    Complex GetCoefficient(Dictionary<double, Point> signal, int frequency);
}

public class FourierSeries : IFourierService
{
    public Complex GetCoefficient(Dictionary<double, Point> signal, int frequency)
    {
        var dt = signal.Skip(1).First().Key; // have to check if rounding issues occur
        var coefficient = new Complex(0,0);
        
        //Numerical integration (riemann):
        foreach (var sample in signal)
        {
            var angle = -2 * Math.PI * sample.Key * frequency; 
            var eTerm = new Complex(Math.Cos(angle), Math.Sin(angle));
            var signalValue = new Complex(sample.Value.X, sample.Value.Y);
            
            coefficient += eTerm * signalValue * dt;
        }

        return coefficient;
    }

    public Dictionary<int, Complex> GetCoefficientSpectrum(Dictionary<double, Point> signal, int startFrequency, int endFrequency)
    {
        var result = new Dictionary<int, Complex>();
        for (var i = startFrequency; i <= endFrequency; i++)
        {
            result.Add(i, GetCoefficient(signal, i));
        }

        return result;
    }
}