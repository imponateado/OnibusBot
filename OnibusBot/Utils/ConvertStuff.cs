using OnibusBot.Interfaces;

namespace OnibusBot.Utils;

public class ConvertStuff : CoordConverter
{
    public Task<ParadasDeOnibusResponseParadas> ConverterParadasCoords(ParadasDeOnibusResponseParadas paradasDeOnibus)
    {
        foreach (var feature in paradasDeOnibus.Features)
        {
            var utmCoords2 = feature.GeometryParadas.Coordinates;

            var utmCoords = utmCoords2[0];

            if (utmCoords == null || utmCoords.Count < 2)
                continue;

            var easting = (double)utmCoords[0];
            var northing = (double)utmCoords[1];

            var coords = UTMToLatLon(easting, northing, 23, true);
            var lat = coords[0];
            var lon = coords[1];

            var resultCoords2 = new List<double>() { lon, lat };
            var resultCoords = new List<List<double>>() { resultCoords2 };

            feature.GeometryParadas.Coordinates = resultCoords;
        }

        return Task.FromResult<ParadasDeOnibusResponseParadas>(paradasDeOnibus);
    }

    public Task<LinhasDeOnibusResponseParadas> ConverterLinhasDeOnibusCoords(LinhasDeOnibusResponseParadas linhasDeOnibus)
    {
        foreach (var feature in linhasDeOnibus.Features)
        {
            var utmCoords = feature.GeometryParadas.Coordinates;
            var newUtmCoords = new List<List<double>>();

            foreach (var coord in utmCoords)
            {
                var newCoords = new List<double>();

                for (var i = 0; i < coord.Count; i += 2)
                {
                    var easting = coord[i];
                    var northing = coord[i + 1];

                    var coords = UTMToLatLon(easting, northing, 23, true);

                    var lat = coords[0];
                    var lon = coords[1];

                    newCoords.Add(lon);
                    newCoords.Add(lat);
                }
                newUtmCoords.Add(newCoords);
            }

            feature.GeometryParadas.Coordinates = newUtmCoords;
        }

        return Task.FromResult(linhasDeOnibus);
    }
}