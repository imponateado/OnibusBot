using OnibusTelegram.Service;
using OnibusTelegram.Utils;

namespace OnibusTelegram
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var apiCall = new ApiCall();
            var convertStuff = new ConvertStuff();
            var latlon = new List<double> { -15.888812, -48.019994 };

            try
            {
                var linhasDeOnibus = await apiCall.GetLinhasDeOnibus();
                var paradasDeOnibus = await apiCall.GetParadasDeOnibus();
                var ultimaPosicaoFrota = await apiCall.GetUltimaPosicaoFrota();
                
                //linhas e paradas vem no padrão utm, precisam ser convertidos para wgs84
                linhasDeOnibus = convertStuff.ConverterLinhasDeOnibusCoords(linhasDeOnibus).Result;
                paradasDeOnibus = convertStuff.ConverterParadasCoords(paradasDeOnibus).Result;
                
                var teste1 = linhasDeOnibus.Features.First();
                var teste2 = teste1.Geometry.Coordinates.First();

                var teste3 = paradasDeOnibus.Features.First();
                var teste4 = teste3.Geometry.Coordinates.First();
                
                Console.WriteLine($"Teste se tá tudo ocorrendo como esperado: {teste2}");
                Console.WriteLine($"Teste se tá tudo ocorrendo como esperado: {teste4}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}