namespace OnibusBot.Interfaces;

public class ViagensProgramadasPorLinhaProperties
{
    string SgOperadora { get; set; }
    string NmOperadora { get; set; }
    string CdLinha { get; set; }
    string TxDenominacaoLinha { get; set; }
    string CsSentido { get; set; }
    bool? StDomingo { get; set; }
    bool? StSegunda { get; set; }
    bool? StTerca { get; set; }
    bool? StQuarta { get; set; }
    bool? StQuinta { get; set; }
    bool? StSexta { get; set; }
    bool? StSabado { get; set; }
    TimeSpan? HoraPrevista { get; set; }
}
