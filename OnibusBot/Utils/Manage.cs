using OnibusBot.Interfaces;

namespace OnibusBot.Utils;

public class Manage
{
    public static List<ParadasFeature> GetClosestBusStop(List<double> latlon, ParadasDeOnibus paradasDeOnibus)
    {
        var res = paradasDeOnibus.Features.OrderBy(feature =>
                HaversineCalculator.HaversiniAlgorithm(latlon[0], latlon[1], feature.Geometry.Coordinates[0],
                    feature.Geometry.Coordinates[1]))
            .ToList();
        var res1 = res.Take(3).ToList();
        return res1;
    }
}