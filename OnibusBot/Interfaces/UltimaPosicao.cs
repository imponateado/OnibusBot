using System.Text.Json.Serialization;

namespace OnibusBot.Interfaces;

public class UltimaPosicao
{
    [JsonPropertyName(("type"))]
    public string Type { get; set; }
    
    [JsonPropertyName("features")]
    public UtimaFeature Features { get; set; }
    
    [JsonPropertyName("totalFeatures")]
    public int? TotalFeatures { get; set; }
    
    [JsonPropertyName("numberMatched")]
    public int? NumberMatched { get; set; }
    
    [JsonPropertyName("numberReturned")]
    public int?  NumberReturned { get; set; }
    
    [JsonPropertyName("timeStamp")]
    public string?  TimeStamp { get; set; }
    
    [JsonPropertyName("crs")]
    public UtimaCrs Crs { get; set; }
}

public class UtimaFeature
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }
    
    [JsonPropertyName("id")]
    public string? Id { get; set; }
    
    [JsonPropertyName("geometry")]
    public UtimaFeatureGeometry Geometry { get; set; }
    
    [JsonPropertyName("geometry_name")]
    public string GeometryName { get; set; }
    
    [JsonPropertyName("properties")]
    public UtimaFeatureProperty Properties { get; set; }
}

public class UtimaFeatureProperty
{
    [JsonPropertyName("id_operadora")]
    public int?  IdOperadora { get; set; }
    
    [JsonPropertyName("prefixo")]
    public string? Prefixo { get; set; }
    
    [JsonPropertyName("datalocal")]
    public DateTime? DataLocal { get; set; }
    
    [JsonPropertyName("velocidade")]
    public int? Velocidade { get; set; }
    
    [JsonPropertyName("cd_linha")]
    public double? Linha { get; set; }
    
    [JsonPropertyName("direcao")]
    public string? Direcao { get; set; }
    
    [JsonPropertyName("latitude")]
    public double? Latitude { get; set; }
    
    [JsonPropertyName("longitude")]
    public double? Longitude { get; set; }
    
    [JsonPropertyName("dataregistro")]
    public DateTime? DataRegistro { get; set; }
    
    [JsonPropertyName("imei")]
    public int? IMEI { get; set; }
    
    [JsonPropertyName("sentido")]
    public int? Sentido { get; set; }
}

public class UtimaFeatureGeometry
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }
    
    [JsonPropertyName("coordinates")]
    public List<double> Coordinates { get; set; }
}

public class UtimaCrs
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }
    
    [JsonPropertyName("properties")]
    public UtimaCrsProperty Properties { get; set; }
}

public class UtimaCrsProperty
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}