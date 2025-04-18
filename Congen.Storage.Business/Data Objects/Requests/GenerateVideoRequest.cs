using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Congen.Storage.Business.Data_Objects.Requests
{
    public class GenerateVideoRequest
    {
        public string Prompt { get; set; }

        public int Tone { get; set; }

        public string Video { get; set; }

        public string Audio { get; set; }
    }
}
