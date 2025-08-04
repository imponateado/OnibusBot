using System.Text.Json.Serialization;

namespace OnibusBot.Interfaces;

public class LinhasDeOnibus
{
    [JsonPropertyName(("type"))]
    public string Type { get; set; }
    
    [JsonPropertyName("features")]
    public List<LinhasFeature> Features { get; set; }
    
    [JsonPropertyName("totalFeatures")]
    public int? TotalFeatures { get; set; }
    
    [JsonPropertyName("numberMatched")]
    public int? NumberMatched { get; set; }
    
    [JsonPropertyName("numberReturned")]
    public int?  NumberReturned { get; set; }
    
    [JsonPropertyName("timeStamp")]
    public string?  TimeStamp { get; set; }
    
    [JsonPropertyName("crs")]
    public LinhasCrs Crs { get; set; }
}

public class LinhasFeature
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }
    
    [JsonPropertyName("id")]
    public string? Id { get; set; }
    
    [JsonPropertyName("geometry")]
    public LinhasFeatureGeometry Geometry { get; set; }
    
    [JsonPropertyName("geometry_name")]
    public string GeometryName { get; set; }
    
    [JsonPropertyName("properties")]
    public LinhasFeatureProperty Properties { get; set; }
}

public class LinhasFeatureProperty
{
    [JsonPropertyName("id")]
    public int? Id { get; set; }
    
    [JsonPropertyName("linha")]
    public string? Linha { get; set; }
    
    [JsonPropertyName("nome")]
    public string? Nome { get; set; }
    
    [JsonPropertyName("sentido")]
    public string? Sentido { get; set; }
    
    [JsonPropertyName("faixa_tarifaria")]
    public string? FaixaTarifaria { get; set; }
    
    [JsonPropertyName("tarifa")]
    public double? Tarifa { get; set; }
    
    [JsonPropertyName("situacao")]
    public string? Situacao { get; set; }
    
    [JsonPropertyName("bacia")]
    public int? Bacia { get; set; }
    
    [JsonPropertyName("tipo_da_linha")]
    public string? TipoDaLinha { get; set; }
    
    [JsonPropertyName("situacao_da_linha")]
    public bool? SituacaoDaLinha { get; set; }
}

public class LinhasFeatureGeometry
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }
    
    [JsonPropertyName("coordinates")]
    public List<List<double>> Coordinates { get; set; }
}

public class LinhasCrs
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }
    
    [JsonPropertyName("properties")]
    public LinhasCrsProperty Properties { get; set; }
}

public class LinhasCrsProperty
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}