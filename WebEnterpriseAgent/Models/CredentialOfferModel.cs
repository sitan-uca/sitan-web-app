using System.Collections.Generic;

namespace WebAgent.Models
{
    public class CredentialOfferModel
    {
        public string ConnectionId { get; set; }
        public string SchemaId { get; set; }
        public string CredentialDefinitionId { get; set; }        
        public List<CredentialAttributeNameValue> CredentialAttributes { get; set; }
    }

    public class CredentialAttributeNameValue
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }
}
