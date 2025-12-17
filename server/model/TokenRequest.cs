using Paradigm.Data.Model;

namespace Paradigm.Server.Model
{
    public class TokenRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public bool IsSuperAdmin { get; set; }
        //public AuditTrails AuditTrails { get; set; }
    }
}
