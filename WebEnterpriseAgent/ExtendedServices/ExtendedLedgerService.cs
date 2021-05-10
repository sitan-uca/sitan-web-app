using Hyperledger.Aries.Agents;
using Hyperledger.Aries.Contracts;
using Hyperledger.Aries.Ledger;
using Hyperledger.Indy.LedgerApi;
using Hyperledger.Aries.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebAgent.Utils;
using Hyperledger.Indy;
using Newtonsoft.Json.Linq;
using Hyperledger.Aries;

namespace WebAgent.ExtendedServices
{
    public class ExtendedLedgerService : DefaultLedgerService
    {
        
        public ExtendedLedgerService(ILedgerSigningService signingService) : base(signingService)
        {           
        }

        public override async Task<ParseResponseResult> LookupSchemaAsync(IAgentContext agentContext, string schemaId)
        {
            async Task<ParseResponseResult> LookupSchema()
            {
                
                var req = await Ledger.BuildGetSchemaRequestAsync(null, schemaId);
                var res = await Ledger.SubmitRequestAsync(await agentContext.Pool, req);                

                EnsureSuccessResponse(res);

                return await Ledger.ParseGetSchemaResponseAsync(res);
            };

            return await ResilienceUtils.RetryPolicyAsync(
                action: LookupSchema,
                exceptionPredicate: (IndyException e) => e.SdkErrorCode == 309);
            //return base.LookupSchemaAsync(agentContext, schemaId);
        }

        void EnsureSuccessResponse(string res)
        {
            var response = JObject.Parse(res);

            if (!response["op"].ToObject<string>().Equals("reply", StringComparison.OrdinalIgnoreCase))
                throw new AriesFrameworkException(Hyperledger.Aries.ErrorCode.LedgerOperationRejected, "Ledger operation rejected");
        }
    }
}
