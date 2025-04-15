using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Congen.Storage.Business.Data_Objects.Responses
{
    public class SaveFileResponse : ResponseBase
    {
        [Key]
        public string FileName { get; set; }
    }
}
