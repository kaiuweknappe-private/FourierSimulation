using System.Collections.Generic;
using System.Linq;

namespace FourierSim.Services;

/// <summary>
/// decouples and encapsulates logic specifically needed in ShapeAnalyzerViewModel (thus manually instantiated there).
/// Resamples Points along a given loop/path at an arbitrary sample-density uniformly and maps them between 0 and 1.
/// 
/// Note: not really optimal implementation, probably should avoid using floating point and scale to integers in the first place..
/// </summary>
public interface IResamplingService
{
    Dictionary<double, Point> GetResample(IEnumerable<Point> points, double sampleDensity = 2);
}

public class ResamplingService : IResamplingService
{
    
    public Dictionary<double, Point> GetResample(IEnumerable<Point> points, double sampleDesity = 2)
    {
        if (points.Count() <= 1) return new();
        
        var totalLength = CalculateTotalLength(points);
 
        //Input-Points with there respective normalized offset along the path from the first point (between 0 and 1):
        var inputSignal = Initialize(points, totalLength);
        
        return Resample(inputSignal, sampleDesity, totalLength);

    }

    private double CalculateTotalLength(IEnumerable<Point> points)
    {
        return points.Aggregate(
            (total: 0.0, previous: points.First()),
            (accumulate, current) => (total: accumulate.total + GetDistance(accumulate.previous, current), previous: current),
            acc => acc.total
        );
    }
    
    private Dictionary<double, Point> Initialize(IEnumerable<Point> points, double totalLength)
    {
        var signal = new Dictionary<double, Point>();
        
        var previousPoint = points.First();
        var currentLength = 0.0;
        
        foreach (var point in points)
        {
            currentLength += GetDistance(previousPoint, point);
            signal[currentLength / totalLength] = point;
            
            previousPoint = point;
        }
        
        if(currentLength != totalLength)
            throw new Exception("There must be a bug! (or only rounding inaccuracy?)");
        
        return signal;
    }
    
    private Dictionary<double, Point> Resample(Dictionary<double, Point> inputSignal, double sampleDensity, double totalLength)
    {
        var sampleAmount = (uint) (sampleDensity * totalLength);
        var interval = 1.0D / sampleAmount;
        var resampledSignal = new Dictionary<double, Point>();
        
        resampledSignal.Add(0, inputSignal[0]); // first Point
        for (var t = interval; t <= 1 - (interval / 2)/*no overlap due to rounding*/; t += interval)
        {   
            var point = Interpolate(inputSignal, t);
            resampledSignal.Add(t, point);
        }
        
        return resampledSignal;
    }
    
    private Point Interpolate(Dictionary<double, Point> inputSignal, double seed)
    {
        double lowerNeighbor = 0;
        double upperNeighbor = 0;

        //find neighbours:
        foreach (var sampleTime in inputSignal.Keys)
        {
            if (sampleTime <= seed)
                lowerNeighbor = sampleTime;

            if (sampleTime >= seed)
            {
                upperNeighbor = sampleTime;
                break;
            }
        }

        //get point in between:
        return Interpolate(new (lowerNeighbor, inputSignal[lowerNeighbor]),
            new (upperNeighbor, inputSignal[upperNeighbor]), seed);
    }
    
    //assumes that t1 < t2 and that seed lies within [t1, t2]:
    private Point Interpolate(KeyValuePair<double, Point> lower, KeyValuePair<double, Point> upper, double seed)
    {
        var c1 = lower.Value;
        var t1 = lower.Key;
        var c2 = upper.Value;
        var t2 = upper.Key;
        var vec = c2 - c1;
        var distance = Math.Sqrt(vec.X * vec.X + vec.Y * vec.Y);

        if (distance == 0) return c1; 

        var normVec = vec / distance;
        var offset = (seed - t1) / (t2 - t1);
        return c1 + normVec * (offset * distance);
    }
    
    private static double GetDistance(Point p1, Point p2)
    {
        var dx = p2.X - p1.X;
        var dy = p2.Y - p1.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }
    
}