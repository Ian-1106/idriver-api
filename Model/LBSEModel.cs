using Newtonsoft.Json.Linq;
using System.Collections.Generic;

public class LBSEModel
{
    public string OCName { get; set; }
    public double Lat { get; set; }
    public double Lon { get; set; }
    public JObject jData { get; set; }
    public int ID { get; set; }
}

public class Root
{
    public List<LBSEModel> Data { get; set; }
}