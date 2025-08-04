using OnibusBot.Interfaces;

namespace OnibusBot.Utils;

public class CleanObjects
{
    public ParadasDeOnibus CleanParadasDeOnibusObject(ParadasDeOnibus paradasDeOnibus)
    {
        paradasDeOnibus.Features.RemoveAll(feature => feature.Properties.Situacao == "DESATIVADA");
        return paradasDeOnibus;
    }

    public UltimaPosicao CleanUltimaPosicaoObject(UltimaPosicao ultimaPosicao)
    {
        ultimaPosicao.Features.RemoveAll(feature => feature.Properties.Linha == "");
        ultimaPosicao.Features.RemoveAll(feature => feature.Properties.Velocidade == "0");
        return ultimaPosicao;
    }
}