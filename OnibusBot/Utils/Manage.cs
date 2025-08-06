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

    public static Dictionary<ParadasFeature, List<LinhasFeature>> GetLinesByBusStopCoord(
        ParadasDeOnibus paradasDeOnibus, LinhasDeOnibus linhasDeOnibus)
    {
        var resultado = new Dictionary<ParadasFeature, List<LinhasFeature>>();

        foreach (var parada in paradasDeOnibus.Features)
        {
            var linhasQuePassamNaParada = linhasDeOnibus.Features.Where(linha =>
                linha.Geometry.Coordinates.Any(coordenadaLinha =>
                    Math.Abs(coordenadaLinha[0] - parada.Geometry.Coordinates[0]) < 0.0001 &&
                    Math.Abs(coordenadaLinha[1] - parada.Geometry.Coordinates[1]) < 0.0001
                )
            ).ToList();

            if (linhasQuePassamNaParada.Any())
            {
                resultado[parada] = linhasQuePassamNaParada;
            }
        }

        return resultado;
    }
}