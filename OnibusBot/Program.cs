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
                var closestBusStops = Manage.GetClosestBusStop(latlon, paradasDeOnibus);
                var linesByBusStopCoord = Manage.GetLinesByBusStopCoord(paradasDeOnibus, linhasDeOnibus);

                if (linesByBusStopCoord.Any())
                {
                    var primeiraParada = linesByBusStopCoord.First();
                    var parada = primeiraParada.Key;
                    var primeiraLinha = primeiraParada.Value.First();
                    
                    Console.WriteLine($"A primeira parada encontrada foi {parada.Properties.Descricao} e a primeira linha encontrada dessa parada foi {primeiraLinha.Properties.Nome}!");
                }
                else
                {
                    Console.WriteLine($"Nada foi encontrado!");
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
