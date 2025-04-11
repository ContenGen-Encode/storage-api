using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Congen.Storage.Data.Data_Objects.Account
{
    public class EmailAddress
    {
        public string Id { get; set; }

        public string EmailAddressValue { get; set; }

        public Verification Verification { get; set; }

        public List<object> LinkedTo { get; set; }
    }
}
