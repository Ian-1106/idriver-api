using Dapper;

namespace I3S_API.Model
{
    public class PublicModel
    {
        public string where { get; set; }
        public DynamicParameters p { get; set; }
        public string ordersql { get; set; }
        public bool border { get; set; }
        public string like { get; set; }
    }
    public class OrderListModel
    {
        public int index { get; set; }
        public string orderstr { get; set; }
        public string column { get; set; }
        public string ordertype { get; set; }
    }
    public class PermissionModel2
    {
        public bool Subscribe { get; set; }
        public bool Manage { get; set; }
        public bool Update { get; set; }
        public bool Delete { get; set; }
        public bool Insert { get; set; }
        public bool Read { get; set; }
        public bool View { get; set; }

    }

    public class PermissionModel
    {
        public bool S { get; set; }
        public bool M { get; set; }
        public bool U { get; set; }
        public bool D { get; set; }
        public bool I { get; set; }
        public bool R { get; set; }
        public bool V { get; set; }

    }
    public class SqlStrModel
    {
        public string strsqlview { get; set; }
        public string strsqltotal { get; set; }
        public DynamicParameters p { get; set; }
        public bool first { get; set; }

    }
    public class UUIDAPI
    {
        public bool first { get; set; }
        public int? start{ get; set; }
        public int? counts{ get; set; }
        public string? order{ get; set; }
        public bool bTotal{ get; set; }
        public int? top{ get; set; }
        public string? like_column { get; set; }
        public string? like { get; set; }
        public int? likeMode { get; set; }
        public bool bPublicOnly { get; set; }
        public UUIDAPI()
        {
            bPublicOnly = false;
        }
    }
}
