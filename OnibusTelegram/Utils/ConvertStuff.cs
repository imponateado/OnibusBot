using OnibusTelegram.Interfaces;

namespace OnibusTelegram.Utils;

public class ConvertStuff : CoordConverter
{
    public Task<ParadasDeOnibusResponse> ConverterParadasCoords(ParadasDeOnibusResponse paradasDeOnibus)
    {
        foreach (var feature in paradasDeOnibus.Features)
        {
            var utmCoords = feature.Geometry.Coordinates;

            if (utmCoords == null || utmCoords.Count < 2)
                continue;
            
            var easting = utmCoords[0];
            var northing = utmCoords[1];

            var coords = UTMToLatLon(easting, northing, 23, true);
            var lat = coords[0];
            var lon = coords[1];

            feature.Geometry.Coordinates = new List<double>() { lon, lat };
        }

        return Task.FromResult<ParadasDeOnibusResponse>(paradasDeOnibus);
    }

    public Task<LinhasDeOnibusResponse> ConverterLinhasDeOnibusCoords(LinhasDeOnibusResponse linhasDeOnibus)
    {
        foreach (var feature in linhasDeOnibus.Features)
        {
            var utmCoords = feature.Geometry.Coordinates;

            var newCoords = new List<double>();

            for (var i = 0; i < utmCoords.Count; i += 2)
            {
                var easting = utmCoords[i];
                var northing = utmCoords[i + 1];

                var coords = UTMToLatLon(easting, northing, 23, true);

                var lat = coords[0];
                var lon = coords[1];
                
                newCoords.Add(lon);
                newCoords.Add(lat);
            }

            feature.Geometry.Coordinates = newCoords;
        }

        return Task.FromResult(linhasDeOnibus);
    }
}