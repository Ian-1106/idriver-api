using Newtonsoft.Json.Linq;

public class WeatherResponse
{
    public string City { get; set; }
    public string Town { get; set; }
    public JObject StationJson { get; set; }
}

public class POI
{
    public int OID { get; set; }
    public int Type { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public string CName { get; set; }
    public string CDes { get; set; }
    public string EName { get; set; }
    public string EDes { get; set; }
    public DateTime Date { get; set; }
    public DateTime LastModifiedDT { get; set; }
    public DateTime OtherDT { get; set; }
    public int DataByte { get; set; }
    public int OwenerMID { get; set; }
    public int nClick { get; set; }
    public int nOutlinks { get; set; }
    public int nInlinks { get; set; }
    public bool bHided { get; set; }
    public bool bDel { get; set; }
}

public class CombinedResponse
{
    public WeatherResponse Weather { get; set; }
    public List<POI> POIs { get; set; }
}
