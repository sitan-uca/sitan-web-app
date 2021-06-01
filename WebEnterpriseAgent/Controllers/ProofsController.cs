using Hyperledger.Aries;
using Hyperledger.Aries.Agents;
using Hyperledger.Aries.Configuration;
using Hyperledger.Aries.Contracts;
using Hyperledger.Aries.Extensions;
using Hyperledger.Aries.Features.DidExchange;
using Hyperledger.Aries.Features.IssueCredential;
using Hyperledger.Aries.Features.PresentProof;
using Hyperledger.Aries.Models.Records;
using Hyperledger.Aries.Storage;
using Hyperledger.Indy.DidApi;
using Hyperledger.Indy.LedgerApi;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using WebAgent.Models;

namespace WebAgent.Controllers
{
    [Authorize]
    public class ProofsController : Controller
    {
        private readonly IAgentProvider _agentContextProvider;
        private readonly IProvisioningService _provisionService;
        private readonly IWalletService _walletService;
        private readonly IWalletRecordService _walletRecordService;
        private readonly IConnectionService _connectionService;
        private readonly ICredentialService _credentialService;
        private readonly ISchemaService _schemaService;
        private readonly IMessageService _messageService;
        private readonly IProofService _proofService;
        private readonly ILedgerService _ledgerService;


        public ProofsController(
            IAgentProvider agentContextProvider,
            IProvisioningService provisionService,
            IWalletService walletService,
            IWalletRecordService walletRecordService,
            IConnectionService connectionService,
            ICredentialService credentialService,
            ISchemaService schemaService,
            IMessageService messageService,
            IProofService proofService,
            ILedgerService ledgerService)
        {
            _agentContextProvider = agentContextProvider;
            _provisionService = provisionService;
            _walletService = walletService;
            _walletRecordService = walletRecordService;
            _connectionService = connectionService;
            _credentialService = credentialService;
            _schemaService = schemaService;
            _messageService = messageService;
            _proofService = proofService;
            _ledgerService = ledgerService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var context = await _agentContextProvider.GetContextAsync();
            var proofs = await _proofService.ListAsync(context);
            var models = new List<ProofRequestViewModel>();

#if RELEASE
            foreach (ProofRecord c in proofs)
            {
                Dictionary<string, ProofAttribute> revealed = null;
                RequestedProof proof = null;
                try
                {
                    
                    //dynamic deserialized = JsonConvert.DeserializeObject(c.ProofJson);
                    //dynamic abc = deserialized["requested_proof"]["revealed_attrs"] + deserialized["requested_proof"]["self_attested_attrs"];
                    //Dictionary<string, ProofAttribute> a = deserialized["requested_proof"]["revealed_attrs"];
                    //if (a != null)
                        //revealed = a;//JsonConvert.DeserializeObject<Dictionary<string, ProofAttribute>>(a);
                    //foreach (var atr in deserialized["requested_proof"]["revealed_attrs"])
                    //{
                    //    revealed.Add(atr., atr.Value.Raw);
                    //}
                    //proof = JsonConvert.DeserializeObject<RequestedProof>(abc);

                    var connection = c.ConnectionId == null ? null : await _connectionService.GetAsync(context, c.ConnectionId);
                    models.Add(new ProofRequestViewModel
                    {
                        Connection = connection?.Alias.Name ?? "----",
                        ProofRecordId = c.Id,
                        State = c.State,
                        CreatedAtUtc = c.CreatedAtUtc,
                        UpdatedAtUtc = c.UpdatedAtUtc,
                        Verified = c.GetTag("verified"),
                        ProofJson = c.ProofJson,
                        RequestJson = c.RequestJson,
                        ProposalJson = c.ProposalJson,
                        RevealedAttrs = revealed
                    });
                }
                catch (AriesFrameworkException e)
                {
                    models.Add(new ProofRequestViewModel
                    {
                        Connection = "Deleted Connection",
                        ProofRecordId = c.Id,
                        State = c.State,
                        CreatedAtUtc = c.CreatedAtUtc,
                        UpdatedAtUtc = c.UpdatedAtUtc,
                        Verified = c.GetTag("verified"),
                        ProofJson = c.ProofJson,
                        RequestJson = c.RequestJson,
                        ProposalJson = c.ProposalJson,
                        RevealedAttrs = revealed
                    });
                }
            }

#elif DEBUG
            models.Add(new ProofRequestViewModel
            {
                Connection = "Test Connection",
                ProofRecordId = "abc123qwe",
                State = ProofState.Accepted,
                CreatedAtUtc = new DateTime(),
                UpdatedAtUtc = new DateTime(),
                Verified = null,
                ProofJson = "{Sample JSON}",
                RequestJson = "{Sample JSON}",
                ProposalJson = "{Sample JSON}"
            });
#endif            

            models.Sort((a, b) => Nullable.Compare(a.CreatedAtUtc, b.CreatedAtUtc));
            ViewData["res"] = TempData["res"];
            return View(new ProofRequestsViewModel { ProofRequests = models });
        }

        [HttpGet]
        public async Task<IActionResult> ProofsForm()
        {           
            var context = await _agentContextProvider.GetContextAsync();
            var provisioning = await _provisionService.GetProvisioningAsync(context.Wallet);

            //var validatorInfo = await Ledger.SignAndSubmitRequestAsync(await context.Pool, context.Wallet,
            //    provisioning.IssuerDid, await Ledger.BuildGetValidatorInfoRequestAsync(provisioning.Endpoint.Did));

            //object obj = JsonConvert.DeserializeObject(validatorInfo);
            List<string> credDefs = new List<string>();
            List<string> issuers = new List<string>();
            for (int i = 1; i < 60; ++i)
            {
                var getTxn = await _ledgerService.LookupTransactionAsync(context, "1", i);
                
                dynamic deserialized = JsonConvert.DeserializeObject(getTxn);
                if (deserialized["result"]["data"] == null)
                    break;

                int txnType = deserialized["result"]["data"]["txn"]["type"];
                if (txnType == 102)
                {
                    string defId = deserialized["result"]["data"]["txnMetadata"]["txnId"];
                    credDefs.Add(defId);
                }
                else if (txnType == 1)
                {
                    string role = deserialized["result"]["data"]["txn"]["data"]["role"];
                    if (role == "0" || role == "2" || role == "101")
                    {
                        string issuer = deserialized["result"]["data"]["txn"]["data"]["dest"];
                        issuers.Add(issuer);
                    }
                }
                
            }

            var model = new ProofFormModel
            {
                Connections = await _connectionService.ListAsync(context),
                IssuersFromLedger = issuers,
                //CredentialDefinitions = await _schemaService.ListCredentialDefinitionsAsync(context.Wallet),
                CredentialDefinitionsFromLedger = credDefs,
                Schemas = await _schemaService.ListSchemasAsync(context.Wallet)
            };
            return View(model);
        }

        private async Task<ProofRequest> BuildRequestProof(ProofOfferModel model) 
        {
            var context = await _agentContextProvider.GetContextAsync();            
            
            Dictionary<string, ProofAttributeInfo> reqAttrs = new Dictionary<string, ProofAttributeInfo>();
            if (model.RequestedAttributes != null)
            {
                foreach (RequestedAttributesModel m in model.RequestedAttributes)
                {
                    List<AttributeFilter> restrictions = new List<AttributeFilter>();
                    restrictions.Add(new AttributeFilter
                    {
                        SchemaName = m.Requirements.SchemaNameRestriction != "" ? m.Requirements.SchemaNameRestriction : null,
                        SchemaVersion = m.Requirements.SchemaVersionRestriction != "" ? m.Requirements.SchemaVersionRestriction : null,
                        CredentialDefinitionId = m.Requirements.CredDeffinitionRestriction != "" ? m.Requirements.CredDeffinitionRestriction : null,
                        IssuerDid = m.Requirements.IssuerRestriction != "" ? m.Requirements.IssuerRestriction : null
                    }); ;
                    reqAttrs.Add($"{m.AttributeName}_requirement", new ProofAttributeInfo
                    {
                        Name = m.AttributeName,
                        Restrictions = restrictions
                    });
                }
            }

            Dictionary<string, ProofPredicateInfo> reqPreds = new Dictionary<string, ProofPredicateInfo>();
            if (model.RequestedPredicates != null)
            {
                foreach (RequestedPredicatesModel m in model.RequestedPredicates)
                {
                    reqPreds.Add($"{m.PredicateName}_predicate", new ProofPredicateInfo
                    {
                        Name = m.PredicateName,
                        PredicateType = m.PredicateType,
                        PredicateValue = m.PredicateValue,
                        Restrictions = new List<AttributeFilter>()
                    });
                }
            }

            var proofRequestObject = new ProofRequest
            {
                Name = "Proof Request",
                Version = "3.0",
                Nonce = new BigInteger(Guid.NewGuid().ToByteArray()).ToString(),
                RequestedAttributes = reqAttrs,
                RequestedPredicates = reqPreds
            };            

            return proofRequestObject;
        }

        [HttpPost]
        public async Task<IActionResult> RequestProof(ProofOfferModel model)
        {
            var context = await _agentContextProvider.GetContextAsync();            
            var connection = await _connectionService.GetAsync(context, model.ConnectionId);
            var proofRequest = await BuildRequestProof(model);
            var (requestMsg, _) = await _proofService.CreateRequestAsync(context, proofRequest, connection.Id);
            await _messageService.SendAsync(context, requestMsg, connection);

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> BuildRequestProofQR(ProofOfferModel model)
        {
            //TODO implement logic here 
            var context = await _agentContextProvider.GetContextAsync();
            var proofRequest = await BuildRequestProof(model);
            var(requestMsg, _) = await _proofService.CreateRequestAsync(context, proofRequest);
            var requestURI = $"{(await _provisionService.GetProvisioningAsync(context.Wallet)).Endpoint.Uri}?c_i={EncodeProofRequest(requestMsg)}";
            
            return Json(requestURI);
        }

        [HttpPost]
        public async Task<IActionResult> VerifyPresentation(string proofRecordId)
        {                        
            var context = await _agentContextProvider.GetContextAsync();
            var proof = await _proofService.GetAsync(context, proofRecordId);
            var verified = await _proofService.VerifyProofAsync(context, proofRecordId);
            proof.SetTag("verified", verified.ToString());
            await _walletRecordService.UpdateAsync(context.Wallet, proof);                      
                        

            return RedirectToAction("Index");
        }

        public string EncodeProofRequest(RequestPresentationMessage proofRequest)
        {
            return proofRequest.ToJson().ToBase64();
        }

        public RequestPresentationMessage DecodeProofRequest(string encodedProofRequest)
        {
            return JsonConvert.DeserializeObject<RequestPresentationMessage>(Encoding.UTF8.GetString(Convert.FromBase64String(encodedProofRequest)));
        }
    }
}
