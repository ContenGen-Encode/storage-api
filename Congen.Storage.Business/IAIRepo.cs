using Azure.Core;
using Congen.Storage.Business.Data_Objects.Requests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Congen.Storage.Business
{
    public interface IAIRepo
    {
        public void GenerateVideoFromPrompt(GenerateVideoRequest request);

        public void GenerateVideoFromFile(Stream file, string tone, string videoId, string audioId);
    }
}
