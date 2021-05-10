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
                    ProposalJson = c.ProposalJson
                });
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
            ViewData["res"] = TempData["res"];
            return View(new ProofRequestsViewModel { ProofRequests = models });
        }

        [HttpGet]
        public async Task<IActionResult> ProofsForm()
        {           
            var context = await _agentContextProvider.GetContextAsync();            
            var model = new ProofFormModel
            {
                Connections = await _connectionService.ListAsync(context),
                CredentialDefinitions = await _schemaService.ListCredentialDefinitionsAsync(context.Wallet),
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
                        CredentialDefinitionId = m.Requirements.CredDeffinitionRestriction != "" ? m.Requirements.CredDeffinitionRestriction : null
                    });
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
                Name = "ProofReq",
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
