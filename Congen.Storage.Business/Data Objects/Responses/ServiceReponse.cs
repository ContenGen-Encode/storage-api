using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Congen.Storage.Data.Data_Objects.Models;

namespace Congen.Storage.Business.Data_Objects.Responses
{
    public class ServiceResponse : ResponseBase
    {
        public ServiceResponse() 
        {
            this.ServiceData = new List<Service>();
        }
        
        [Key]
        public List<Service> ServiceData { get; set; }
    }
}


/* 
    {
        id: int
        uri: string,
        type: template_video / audio / voice

    }
*/