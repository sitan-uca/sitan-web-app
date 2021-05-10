using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Hyperledger.Aries.Agents;
using Hyperledger.Aries.Configuration;
using Hyperledger.Aries.Extensions;
using Hyperledger.Aries.Features.DidExchange;
using Hyperledger.Aries.Features.IssueCredential;
using Hyperledger.Aries.Models.Records;
using Hyperledger.Aries.Storage;
using Hyperledger.Indy.DidApi;
using Hyperledger.Indy.LedgerApi;
using Newtonsoft.Json;
using WebAgent.Models;
using Hyperledger.Indy.AnonCredsApi;
using Hyperledger.Aries;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Authorization;

namespace WebAgent.Controllers
{
    [Authorize]
    public class CredentialsController : Controller
    {
        private readonly IAgentProvider _agentContextProvider;
        private readonly IProvisioningService _provisionService;
        private readonly IWalletService _walletService;
        private readonly IWalletRecordService _walletRecordService;
        private readonly IConnectionService _connectionService;
        private readonly ICredentialService _credentialService;
        private readonly ISchemaService _schemaService;
        private readonly IMessageService _messageService;

        public CredentialsController(
            IAgentProvider agentContextProvider,
            IProvisioningService provisionService,
            IWalletService walletService,
            IWalletRecordService walletRecordService,
            IConnectionService connectionService,
            ICredentialService credentialService,
            ISchemaService schemaService,
            IMessageService messageService)
        {
            _agentContextProvider = agentContextProvider;
            _provisionService = provisionService;
            _walletService = walletService;
            _walletRecordService = walletRecordService;
            _connectionService = connectionService;
            _credentialService = credentialService;
            _schemaService = schemaService;
            _messageService = messageService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var context = await _agentContextProvider.GetContextAsync();
            var credentials = await _credentialService.ListAsync(context);
            var models = new List<CredentialViewModel>();
            foreach ( var c in credentials) {
                models.Add(new CredentialViewModel{
                    SchemaId = c.SchemaId,
                    CreatedAt = c.CreatedAtUtc ?? DateTime.MinValue,
                    State = c.State,
                    CredentialAttributesValues = c.CredentialAttributesValues
                });
            }
            return View(new CredentialsViewModel{ Credentials = models });
        }

        [HttpGet]
        public async Task<IActionResult> BootstrapSchemaAndCredDef()
        {
            var context = await _agentContextProvider.GetContextAsync();
            var issuer = await _provisionService.GetProvisioningAsync(context.Wallet);
            var Trustee = await Did.CreateAndStoreMyDidAsync(context.Wallet,
                new { seed = "111222333444555666BAKSAKSteward4" }.ToJson());
            await Ledger.SignAndSubmitRequestAsync(await context.Pool, context.Wallet, Trustee.Did,
                await Ledger.BuildNymRequestAsync(Trustee.Did, issuer.IssuerDid, issuer.IssuerVerkey, null, "ENDORSER"));


            var schemaId = await _schemaService.CreateSchemaAsync(
                context: context,
                issuerDid: issuer.IssuerDid,
                name: "degree-schema",
                version: "1.0",
                attributeNames: new[] { "name", "date", "degree", "age", "timestamp" });
            await _schemaService.CreateCredentialDefinitionAsync(context, new CredentialDefinitionConfiguration{
                SchemaId = schemaId,
                Tag = "default",
                EnableRevocation = false,
                RevocationRegistrySize = 0,
                RevocationRegistryBaseUri = "",
                RevocationRegistryAutoScale = false,
                IssuerDid = issuer.IssuerDid});

            return RedirectToAction("CredentialsForm");
        }

        [HttpPost]
        public async Task<IActionResult> RegisterSchema(string schemaName, string schemaVer, string schemaAttrs)
        {
            var context = await _agentContextProvider.GetContextAsync();
            var issuer = await _provisionService.GetProvisioningAsync(context.Wallet);

            // TODO Build and Submit the NYM transaction during provisioning
            //var Trustee = await Did.CreateAndStoreMyDidAsync(context.Wallet,
            //    new { seed = "111222333444555666BAKSAKSteward4" }.ToJson());
            //await Ledger.SignAndSubmitRequestAsync(await context.Pool, context.Wallet, Trustee.Did,
            //    await Ledger.BuildNymRequestAsync(Trustee.Did, issuer.IssuerDid, issuer.IssuerVerkey, null, "ENDORSER"));

            var schema = await AnonCreds.IssuerCreateSchemaAsync(issuer.IssuerDid, schemaName,
                schemaVer, schemaAttrs.Split(",").ToJson());

            var response = await Ledger.SignAndSubmitRequestAsync(await context.Pool, context.Wallet, 
                issuer.IssuerDid, await Ledger.BuildSchemaRequestAsync(issuer.IssuerDid, schema.SchemaJson));

            EnsureSuccessResponse(response);

            var schemaRecord = new SchemaRecord
            {
                Id = schema.SchemaId,
                Name = schemaName,
                Version = schemaVer,
                AttributeNames = schemaAttrs.Split(",")
            };

            await _walletRecordService.AddAsync(context.Wallet, schemaRecord);

            //var schemaId = await _schemaService.CreateSchemaAsync(
            //    context: context,
            //    issuerDid: issuer.IssuerDid,
            //    name: schemaName,
            //    version: schemaVer,
            //    attributeNames: schemaAttrs.Split(","));

            return RedirectToAction("CredentialsForm");
        }

        [HttpPost]
        public async Task<IActionResult> RegisterCredDefinition(string SchemaId, string credDefTag)
        {
            var context = await _agentContextProvider.GetContextAsync();
            var issuer = await _provisionService.GetProvisioningAsync(context.Wallet);

            await _schemaService.CreateCredentialDefinitionAsync(context, new CredentialDefinitionConfiguration
            {
                SchemaId = SchemaId,
                Tag = credDefTag,
                EnableRevocation = false,
                RevocationRegistrySize = 0,
                RevocationRegistryBaseUri = "",
                RevocationRegistryAutoScale = false,
                IssuerDid = issuer.IssuerDid
            });

            return RedirectToAction("CredentialsForm");
        }

        [HttpGet]
        public async Task<IActionResult> CredentialsForm()
        {
            var context = await _agentContextProvider.GetContextAsync();
            var model = new CredentialFormModel
            {
                Connections = await _connectionService.ListAsync(context),
                CredentialDefinitions = await _schemaService.ListCredentialDefinitionsAsync(context.Wallet),
                Schemas = await _schemaService.ListSchemasAsync(context.Wallet)
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> IssueCredentials(CredentialOfferModel model)
        {
            var context = await _agentContextProvider.GetContextAsync();
            var issuer = await _provisionService.GetProvisioningAsync(context.Wallet);
            var connection = await _connectionService.GetAsync(context, model.ConnectionId);
            //var schema = _walletRecordService.GetAsync<SchemaRecord>(context.Wallet, model.SchemaId);

            //var values = JsonConvert.DeserializeObject<List<CredentialPreviewAttribute>>(model.CredentialAttributes);

            List<CredentialPreviewAttribute> attrs = new List<CredentialPreviewAttribute>();
            foreach (CredentialAttributeNameValue attr in model.CredentialAttributes)
            {
                CredentialPreviewAttribute cpa = new CredentialPreviewAttribute
                {
                    MimeType = CredentialMimeTypes.ApplicationJsonMimeType,
                    Name = attr.Name,
                    Value = attr.Value
                };
                attrs.Add(cpa);
            }

            var (offer, _) = await _credentialService.CreateOfferAsync(context, 
                new OfferConfiguration
                {
                    CredentialDefinitionId = model.CredentialDefinitionId,
                    IssuerDid = issuer.IssuerDid,
                    CredentialAttributeValues = attrs
                });
            
            await _messageService.SendAsync(context, offer, connection);

            return RedirectToAction("Index");
        }

        public async Task<ActionResult> OnCredDefSelected(string credDefId)
        {
            var context = await _agentContextProvider.GetContextAsync();
                        
            var credDef = await _schemaService.GetCredentialDefinitionAsync(context.Wallet, credDefId);
            var schema = await _walletRecordService.GetAsync<SchemaRecord>(context.Wallet, credDef?.SchemaId);
            var attrNames = schema?.AttributeNames;
            //Dictionary<string, string> credAtributes = new Dictionary<string, string>();
            //foreach(string attr in attrNames) credAtributes.Add(attr, string.Empty);
            //ViewBag["AttrsDict"] = credAtributes;
            return Json(attrNames);
            //return Json(credAtributes);
        }

        void EnsureSuccessResponse(string res)
        {
            var response = JObject.Parse(res);

            if (!response["op"].ToObject<string>().Equals("reply", StringComparison.OrdinalIgnoreCase))
                throw new AriesFrameworkException(ErrorCode.LedgerOperationRejected, "Ledger operation rejected");
        }
    }
}