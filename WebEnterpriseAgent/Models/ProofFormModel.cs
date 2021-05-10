using Hyperledger.Aries.Features.DidExchange;
using Hyperledger.Aries.Models.Records;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WebAgent.Models
{
    public class ProofFormModel
    {
        public List<ConnectionRecord> Connections { get; set; }
        public List<DefinitionRecord> CredentialDefinitions { get; set; }
        public List<SchemaRecord> Schemas { get; set; }
        //public List<Record> Issuers { get; set; }
        
    }
}
