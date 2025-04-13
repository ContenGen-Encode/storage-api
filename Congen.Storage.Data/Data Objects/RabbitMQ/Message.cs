using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Congen.Storage.Data.Data_Objects.RabbitMQ
{
    public class Message
    {
        public string FileName { get; set; }

        public string Prompt { get; set; }

        public string Tone { get; set; }

        //configuration

        public string VideoName { get; set; }

        public string AudioName { get; set; }

        public string AccessToken { get; set; }

        public string UserId { get; set; }
    }
}
