using OnibusBot.Interfaces;

namespace OnibusBot.Utils;

public class ConvertStuff : CoordConverter
{
    public Task<ParadasDeOnibus> ConverterParadasCoords(ParadasDeOnibus paradasDeOnibus)
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

            var resultCoords = new List<double>() { lon, lat };

            feature.Geometry.Coordinates = resultCoords;
        }

        return Task.FromResult<ParadasDeOnibus>(paradasDeOnibus);
    }

    public Task<LinhasDeOnibus> ConverterLinhasDeOnibusCoords(LinhasDeOnibus linhasDeOnibus)
    {
        foreach (var feature in linhasDeOnibus.Features)
        {
            var utmCoords = feature.Geometry.Coordinates;
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

            feature.Geometry.Coordinates = newUtmCoords;
        }

        return Task.FromResult(linhasDeOnibus);
    }
}