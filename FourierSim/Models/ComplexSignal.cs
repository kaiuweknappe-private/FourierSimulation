using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace FourierSim.Models;

public class ComplexSignal
{
    //Input-Points with there respective normalized offset along the path from the first point (between 0 and 1)
    private readonly Dictionary<double, Complex> _signal = new();

    public double TotalLength { get; private set; }
    public double Interval { get; private set; }
    
    public ComplexSignal(ICollection<Point> periodicSignal)
    {
        TotalLength = periodicSignal.Aggregate(
            (total: 0.0, previous: periodicSignal.First()),
            (accumulate, current) => (total: accumulate.total + GetDistance(accumulate.previous, current), previous: current),
            acc => acc.total
        );
        
        Initialize(periodicSignal);
    }

    private void Initialize(ICollection<Point> signal)
    {
        var previousPoint = signal.First();
        var currentLength = 0.0;
        foreach (var point in signal)
        {
            currentLength += GetDistance(previousPoint, point);
            _signal[currentLength / TotalLength] = new Complex(point.X, point.Y);
            
            previousPoint = point;
        }
        
        if(currentLength != TotalLength)
            throw new Exception("There must be a bug! (or only rounding inaccuracy?)");
        // kind of calculating length twice.. probably a smarter way to do this..
    }
    
    public Dictionary<double, Complex> Uniformify(double sampleDensity = 2)
    {
        var sampleAmount = (uint) (sampleDensity * TotalLength);
        Interval = 1.0D / sampleAmount;
        var result = new Dictionary<double, Complex>();
        
        result.Add(0, _signal[0]); // first Point
        for (var t = Interval; t < 1; t += Interval)
        {   
            var neighbors = GetNeighbors(t);
            var point = Interpolate(neighbors, t);
            result.Add(t, point);
        }
        // add last point (first point) again?
        return result;
    }
    
    private (KeyValuePair<double, Complex> lower, KeyValuePair<double, Complex> upper) GetNeighbors(double seed)
    {
        double lowerNeighbor = 0;
        double upperNeighbor = 0;

        foreach (var sampleTime in _signal.Keys)
        {
            if (sampleTime <= seed)
                lowerNeighbor = sampleTime;

            if (sampleTime >= seed)
            {
                upperNeighbor = sampleTime;
                break;
            }
        }

        return (new (lowerNeighbor, _signal[lowerNeighbor]),
            new (upperNeighbor, _signal[upperNeighbor]));
    }
    
    //assumes that t1 < t2 and that seed lies within [t1, t2]:
    private Complex Interpolate((KeyValuePair<double, Complex> lower, KeyValuePair<double, Complex> upper) neighbors, double seed)
    {
        var c1 = neighbors.lower.Value;
        var t1 = neighbors.lower.Key;
        var c2 = neighbors.upper.Value;
        var t2 = neighbors.upper.Key;
        var vec = c2 - c1;
        var distance = vec.Magnitude;

        if (distance == 0) return c1; 

        var normVec = vec / distance;
        var offset = (seed - t1) / (t2 - t1);
        return c1 + (normVec * offset * distance);
    }
    
    private static double GetDistance(Point p1, Point p2)
    {
        var dx = p2.X - p1.X;
        var dy = p2.Y - p1.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }
}