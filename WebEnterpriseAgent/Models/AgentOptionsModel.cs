using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebAgent.Models
{
    public class AgentOptionsModel
    {
        public string AgentName { get; set; }

        public string AgentDid { get; set; }

        public string AgentImageUri { get; set; }

        public IFormFile ImageFile { get; set; }

        public bool AutoRespondCredentialRequest { get; set; }

        public bool AutoRespondCredentialOffer { get; set; }
    }
}
