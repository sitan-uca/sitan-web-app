using Hyperledger.Aries.Features.PresentProof;
using System;
using System.Collections.Generic;

namespace WebAgent.Models
{
    public class ProofRequestViewModel
    {
        public string Connection { get; set; }

        public string ProofRecordId { get; set; }

        public string Comment { get; set; }

        public ProofState State { get; set; }        

        public string ProofJson { get; set; }

        public string RequestJson { get; set; }

        public string ProposalJson { get; set; }

        public DateTime? CreatedAtUtc { get; set; }

        public DateTime? UpdatedAtUtc { get; set; }

        public string Verified { get; set; }
    }
}
