using Azure.Core;
using Congen.Storage.Business.Data_Objects.Requests;
using Congen.Storage.Data.Data_Objects.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Congen.Storage.Business
{
    public interface IServiceRepo
    {
        public List<Service> GetServiceFiles(int type = 0);
    }
}
