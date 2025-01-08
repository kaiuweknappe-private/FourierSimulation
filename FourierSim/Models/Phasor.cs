using System.Numerics;

namespace FourierSim.Models;

/// <summary>
/// Used for FourierSeries Animation
/// </summary>
public class Phasor
{
    public int Frequency { get; set; }
    public double Magnitude { get; set; }
    public double Phase { get; set; }
    
    public double Real => Magnitude * Math.Cos(Phase);
    public double Imaginary => Magnitude * Math.Sin(Phase);

    public Phasor(int frequency, double magnitude, double phase)
    {
        Frequency = frequency;
        Magnitude = magnitude;
        Phase = phase;
    }

    public static Phasor FromRectangular(int frequency, double real, double imaginary)
    {
        var magnitude = Math.Sqrt(real * real + imaginary * imaginary);
        var phase = Math.Atan2(imaginary, real); 
        return new Phasor(frequency, magnitude, phase);
    }
    
    public Complex ToComplex() => new Complex(Real, Imaginary);
    
    public static Phasor Add(Phasor p1, Phasor p2)
    {
        if (p1.Frequency != p2.Frequency) return new Phasor(0, 0, 0);
        
        var real = p1.Real + p2.Real;
        var imaginary = p1.Imaginary + p2.Imaginary;
        return FromRectangular(p1.Frequency, real, imaginary);
    }
}