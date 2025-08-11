using System.Collections.Concurrent;
using System.IO.Enumeration;
using OnibusBot.Interfaces;

namespace OnibusBot.Utils;

public static class Manage
{
    public static List<LinhasFeature> GetLinhas(LinhasDeOnibus linhasDeOnibus, string linha)
    {
        var res = linhasDeOnibus.Features.Where(x => x.Properties.Linha == linha).ToList();

        if (!res.Any())
            throw new Exception("Nenhuma linha com esse número foi encontrado.");
        
        return res;
    }

    public static bool LineExistsAtLastPosition(UltimaPosicao ultimaPosicao, string linha)
    {
        var res = ultimaPosicao.Features.Any(x => x.Properties.Linha == linha);
        return res;
    }

    public static List<List<double>> GetClosestPoint(List<List<double>> coords, List<double> pointLatLon)
    {
        var res = coords.OrderBy(coord =>
            HaversineCalculator.HaversiniAlgorithm(pointLatLon[0], pointLatLon[1], coord[0], coord[1])).ToList();
        return res;
    }
    public static List<UltimaFeature> GetLinha(UltimaPosicao ultimaPosicao, string sentido, string linha)
    {
        if (ultimaPosicao?.Features == null)
            return new List<UltimaFeature>();
    
        var res = ultimaPosicao.Features
            .Where(x => string.Equals(x.Properties.Linha, linha, StringComparison.OrdinalIgnoreCase) && 
                        string.Equals(x.Properties.Sentido, sentido, StringComparison.OrdinalIgnoreCase))
            .ToList();
    
        return res;
    }
}