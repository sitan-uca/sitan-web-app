using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebAgent.Models
{
    public class ProofOfferModel
    {
        public string ConnectionId { get; set; }
        public string Comment { get; set; }
        public List<RequestedAttributesModel> RequestedAttributes { get; set; }
        public List<RequestedPredicatesModel> RequestedPredicates { get; set; }
    }

    public class RequestedAttributesModel
    {
        public string AttributeName { get; set; }
        public Requirements Requirements { get; set; }
    }
    public class RequestedPredicatesModel
    { 
        public string PredicateName { get; set; }
        public string PredicateType { get; set; }
        public int PredicateValue { get; set; }
        public Requirements Requirements { get; set; }
    }

    public class Requirements
    {
        public string SchemaNameRestriction { get; set; }
        public string SchemaVersionRestriction { get; set; }
        public string IssuerRestriction { get; set; }
        public string CredDeffinitionRestriction { get; set; }
    }
}
