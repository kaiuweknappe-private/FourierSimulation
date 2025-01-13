using System.Numerics;

namespace FourierSim.Models;

/// <summary>
/// Used for FourierSeries Animation.
/// Basically associates an integer Frequency with a Complex number.
/// </summary>
public class Phasor(int frequency, double magnitude, double phase)
{
    public int Frequency { get; set; } = frequency;
    public double Magnitude { get; set; } = magnitude;
    public double Phase { get; set; } = phase;

    public double Real => Magnitude * Math.Cos(Phase);
    public double Imaginary => Magnitude * Math.Sin(Phase);

    /* Not really needed and also possibly misplaced in a model.
    (would put this into a PhasorRepo or probably even better, just exchange Mag. and Phase properties with a Complex property) 
    public static Phasor FromRectangular(int frequency, double real, double imaginary)
    {
        var magnitude = Math.Sqrt(real * real + imaginary * imaginary);
        var phase = Math.Atan2(imaginary, real); //Atan2 auto adjust the angle based on the quadrant as well
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
    */
}