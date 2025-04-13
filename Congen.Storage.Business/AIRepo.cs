using Congen.Storage.Business.Data_Objects.Requests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Congen.Storage.Business
{
    public class AIRepo : IAIRepo
    {
        public void GenerateVideoFromPrompt(GenerateVideoRequest request)
        {

        }

        public void GenerateVideoFromFile(Stream file, string tone, string video, string audioId)
        {
            try
            {
                //send to AI

            }

            catch(Exception ex)
            {
                throw new Exception("Error generating video from file: " + ex.Message);
            }
        }
    }
}
