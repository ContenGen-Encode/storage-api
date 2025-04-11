using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Congen.Storage.Data.Data_Objects.Account
{
    public class Verification
    {
        public string Status { get; set; }
        public string Strategy { get; set; }
        public string ExternalVerificationRedirectURL { get; set; }
        public int? Attempts { get; set; }
        public long? ExpireAt { get; set; }
        public string Nonce { get; set; }
        public string Message { get; set; }
    }
}
