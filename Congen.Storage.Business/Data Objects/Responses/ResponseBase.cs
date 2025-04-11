using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Congen.Storage.Business.Data_Objects.Responses
{
    public class ResponseBase
    {
        public ResponseBase()
        {
            this.ErrorCode = 200;
        }

        public bool IsSuccessful { get; set; }

        public string ErrorMessage { get; set; }

        public int ErrorCode { get; set; }
    }
}
