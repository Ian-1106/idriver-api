using Dapper;

namespace I3S_API.Model
{

    public class TxModel
    {
        public string? wke { get; set; }
        public TxModel()
        {
            wke = "做中學";
        }

    }

    public class TxInOut
    {
        public DynamicParameters p { get; set; }
        public List<dynamic> outputs { get; set; }

        public dynamic columnjson { get; set; }

    }

    public class TxView
    {
        public DynamicParameters p { get; set; }
        public string sqlstring { get; set; }

    }

}
