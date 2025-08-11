using System.Text.Json.Serialization;

namespace OnibusBot.Interfaces;

public class UltimaPosicao
{
    [JsonPropertyName(("type"))]
    public string Type { get; set; }
    
    [JsonPropertyName("features")]
    public List<UltimaFeature> Features { get; set; }
    
    [JsonPropertyName("totalFeatures")]
    public int? TotalFeatures { get; set; }
    
    [JsonPropertyName("numberMatched")]
    public int? NumberMatched { get; set; }
    
    [JsonPropertyName("numberReturned")]
    public int?  NumberReturned { get; set; }
    
    [JsonPropertyName("timeStamp")]
    public string?  TimeStamp { get; set; }
    
    [JsonPropertyName("crs")]
    public UltimaCrs Crs { get; set; }
}

public class UltimaFeature
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }
    
    [JsonPropertyName("id")]
    public string? Id { get; set; }
    
    [JsonPropertyName("geometry")]
    public UltimaFeatureGeometry Geometry { get; set; }
    
    [JsonPropertyName("geometry_name")]
    public string? GeometryName { get; set; }
    
    [JsonPropertyName("properties")]
    public UltimaFeatureProperty Properties { get; set; }
}

public class UltimaFeatureProperty
{
    [JsonPropertyName("id_operadora")]
    public int?  IdOperadora { get; set; }
    
    [JsonPropertyName("prefixo")]
    public string? Prefixo { get; set; }
    
    [JsonPropertyName("datalocal")]
    public string? DataLocal { get; set; }
    
    [JsonPropertyName("velocidade")]
    public string? Velocidade { get; set; }
    
    [JsonPropertyName("cd_linha")]
    public string? Linha { get; set; }
    
    [JsonPropertyName("direcao")]
    public string? Direcao { get; set; }
    
    [JsonPropertyName("latitude")]
    public double? Latitude { get; set; }
    
    [JsonPropertyName("longitude")]
    public double? Longitude { get; set; }
    
    [JsonPropertyName("dataregistro")]
    public string? DataRegistro { get; set; }
    
    [JsonPropertyName("imei")]
    public string? IMEI { get; set; }
    
    [JsonPropertyName("sentido")]
    public string? Sentido { get; set; }
}

public class UltimaFeatureGeometry
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }
    
    [JsonPropertyName("coordinates")]
    public List<double> Coordinates { get; set; }
}

public class UltimaCrs
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }
    
    [JsonPropertyName("properties")]
    public UltimaCrsProperty Properties { get; set; }
}

public class UltimaCrsProperty
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}