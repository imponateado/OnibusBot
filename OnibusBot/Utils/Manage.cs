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
        ParadasDeOnibus paradasDeOnibus, LinhasDeOnibus linhasDeOnibus, double distanciaMaxima = 0.1)
    {
        var resultado = new Dictionary<ParadasFeature, List<LinhasFeature>>();

        foreach (var parada in paradasDeOnibus.Features)
        {
            var linhasQuePassamNaParada = linhasDeOnibus.Features.Where(linha =>
                linha.Geometry.Coordinates.Any(coordenadaLinha =>
                    HaversineCalculator.HaversiniAlgorithm(
                        parada.Geometry.Coordinates[0],
                        parada.Geometry.Coordinates[1],
                        coordenadaLinha[0],
                        coordenadaLinha[1]
                    ) <= distanciaMaxima
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