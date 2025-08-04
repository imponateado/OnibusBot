using OnibusBot.Service;
using OnibusBot.Utils;

namespace OnibusBot
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var httpClient = new HttpClient();
            var httpHandle = new HttpClientHandler();
            var apiCall = new ApiCall(httpClient, httpHandle);
            var convertStuff = new ConvertStuff();
            var cleanObjects = new CleanObjects();
            var latlon = new List<double> { -15.888812, -48.019994 };

            try
            {
                var linhasDeOnibus = await apiCall.GetLinhasDeOnibus();
                var paradasDeOnibus = await apiCall.GetParadasDeOnibus();
                var ultimaPosicaoFrota = await apiCall.GetUltimaPosicaoFrota();
                
                //linhas e paradas vem no padrão utm, precisam ser convertidos para wgs84
                linhasDeOnibus = convertStuff.ConverterLinhasDeOnibusCoords(linhasDeOnibus).Result;
                paradasDeOnibus = convertStuff.ConverterParadasCoords(paradasDeOnibus).Result;

                //remove features não necessárias
                paradasDeOnibus = cleanObjects.CleanParadasDeOnibusObject(paradasDeOnibus);
                ultimaPosicaoFrota = cleanObjects.CleanUltimaPosicaoObject(ultimaPosicaoFrota);
                
                //encontra as 3 paradas mais próximas do usuário
                //var res = manage.GetClosestBusStops(latlon, paradasDeOnibus);
                var res = Manage.GetClosestBusStop(latlon, paradasDeOnibus);
                foreach (var feature in res)
                {
                    Console.WriteLine($"Parada encontrada: {feature.Properties.Parada}");
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
