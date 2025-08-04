using System.Text.Json.Serialization;

namespace OnibusBot.Interfaces;

public class ParadasDeOnibus
{
    [JsonPropertyName(("type"))]
    public string Type { get; set; }
    
    [JsonPropertyName("features")]
    public List<ParadasFeature> Features { get; set; }
    
    [JsonPropertyName("totalFeatures")]
    public int? TotalFeatures { get; set; }
    
    [JsonPropertyName("numberMatched")]
    public int? NumberMatched { get; set; }
    
    [JsonPropertyName("numberReturned")]
    public int?  NumberReturned { get; set; }
    
    [JsonPropertyName("timeStamp")]
    public string?  TimeStamp { get; set; }
    
    [JsonPropertyName("crs")]
    public ParadasCrs Crs { get; set; }
}

public class ParadasFeature
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }
    
    [JsonPropertyName("id")]
    public string? Id { get; set; }
    
    [JsonPropertyName("geometry")]
    public ParadaFeatureGeometry Geometry { get; set; }
    
    [JsonPropertyName("geometry_name")]
    public string GeometryName { get; set; }
    
    [JsonPropertyName("properties")]
    public ParadasFeatureProperty Properties { get; set; }
}

public class ParadasFeatureProperty
{
    [JsonPropertyName("parada")]
    public string? Parada { get; set; }
    
    [JsonPropertyName("descricao")]
    public string? Descricao { get; set; }
    
    [JsonPropertyName("situacao")]
    public string? Situacao { get; set; }
    
    [JsonPropertyName("estrutura_de_paragem")]
    public string? EstruturaDeParagem { get; set; }
    
    [JsonPropertyName("tipo")]
    public string? Tipo { get; set; }
}

public class ParadaFeatureGeometry
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }
    
    [JsonPropertyName("coordinates")]
    public List<double> Coordinates { get; set; }
}

public class ParadasCrs
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }
    
    [JsonPropertyName("properties")]
    public ParadasCrsProperty Properties { get; set; }
}

public class ParadasCrsProperty
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}