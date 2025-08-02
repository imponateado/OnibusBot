using System.Net.Http.Json;
using OnibusTelegram.Interfaces;

namespace OnibusTelegram.Service;

public class ApiCall
{
    private readonly HttpClient _callApi;
    private readonly HttpClientHandler _handler;

    public ApiCall
        (
        HttpClient httpClient,
        HttpClientHandler handler
        )
    {
        _callApi = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<ParadasDeOnibusResponse> GetParadasDeOnibus()
    {
        var res = await _callApi.GetAsync(
            "https://geoserver.semob.df.gov.br/geoserver/semob/ows?service=WFS&version=1.0.0&request=GetFeature&typeName=semob%3AParadas%20de%20onibus&outputFormat=application%2Fjson");
        res.EnsureSuccessStatusCode();
        var paradasDeOnibus = await res.Content.ReadFromJsonAsync<ParadasDeOnibusResponse>();
        if (paradasDeOnibus == null)
            throw new Exception("Resposta vazia ou incompatível com tipo!");

        return paradasDeOnibus;
    }

    public async Task<LinhasDeOnibusResponse> GetLinhasDeOnibus()
    {
        var res = await _callApi.GetAsync(
            "https://geoserver.semob.df.gov.br/geoserver/semob/ows?service=WFS&version=1.0.0&request=GetFeature&typeName=semob%3ALinhas%20de%20onibus&outputFormat=application%2Fjson");
        res.EnsureSuccessStatusCode();
        var linhasDeOnibus = await res.Content.ReadFromJsonAsync<LinhasDeOnibusResponse>();
        if (linhasDeOnibus == null)
            throw new Exception("Resposta vazia ou incompatível com tipo!");

        return linhasDeOnibus;
    }

    public async Task<UltimaPosicaoFrotaResponse> GetUltimaPosicaoFrota()
    {
        var res = await _callApi.GetAsync(
            "https://geoserver.semob.df.gov.br/geoserver/semob/ows?service=WFS&version=1.0.0&request=GetFeature&typeName=semob%3A%C3%9Altima%20posi%C3%A7%C3%A3o%20da%20frota&outputFormat=application%2Fjson");
        res.EnsureSuccessStatusCode();
        var ultimaPosicao = await res.Content.ReadFromJsonAsync<UltimaPosicaoFrotaResponse>();
        if (ultimaPosicao == null)
            throw new Exception("Resposta vazia ou incompatível com tipo!");

        return ultimaPosicao;
    }
}