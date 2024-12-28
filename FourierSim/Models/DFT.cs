using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace FourierSim.Models;

public class FourierSeries
{
    private readonly Dictionary<double,Complex> _signal;
    private readonly double _dt;
    
    public FourierSeries(Dictionary<double,Complex> signal, double dt)
    {
        //_signal checking for uniform, between 0-1 etc., samples according to dt ?
        _signal = signal;
        _dt = dt;
    }

    public Complex GetCoefficient(int frequency)
    {
        var coefficient = new Complex(0,0);
        foreach (var sample in _signal)
        {
            var angle = -2 * Math.PI * sample.Key * frequency; 
            var eTerm = new Complex(Math.Cos(angle), Math.Sin(angle));
            
            coefficient += eTerm * sample.Value * _dt;
        }

        return coefficient;
    }

    public Dictionary<int, Complex> GetCoefficientSpectrum(int startFrequency, int endFrequency)
    {
        var result = new Dictionary<int, Complex>();
        for (var i = startFrequency; i <= endFrequency; i++)
        {
            result.Add(i, GetCoefficient(i));
        }

        return result;
    }
}