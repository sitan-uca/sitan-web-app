using System.Diagnostics;
using System.Threading.Tasks;
using Hyperledger.Aries.Agents;
using Hyperledger.Aries.Configuration;
using Hyperledger.Aries.Contracts;
using Hyperledger.Aries.Storage;
using Hyperledger.Indy.DidApi;
using Hyperledger.Indy.LedgerApi;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using WebAgent.Models;

namespace WebAgent.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly IWalletService _walletService;
        private readonly IProvisioningService _provisioningService;
        private readonly IAgentProvider _agentContextProvider;
        private readonly ILedgerService _ledgerService;
        private readonly AgentOptions _walletOptions;

        public HomeController(
            IWalletService walletService,
            IProvisioningService provisioningService,
            ILedgerService ledgerService,
            IAgentProvider agentContextProvider,
            IOptions<AgentOptions> walletOptions)
        {
            _walletService = walletService;
            _provisioningService = provisioningService;
            _ledgerService = ledgerService;
            _agentContextProvider = agentContextProvider;
            _walletOptions = walletOptions.Value;
        }

        public async Task<IActionResult> Index()
        {
            //var wallet = await _walletService.GetWalletAsync(
            //    _walletOptions.WalletConfiguration,
            //    _walletOptions.WalletCredentials);
            var context = await _agentContextProvider.GetContextAsync();
            
            //ViewData["LOOKUP_RESPONSE"] = await Ledger.SubmitRequestAsync(await context.Pool,
            //    await Ledger.BuildGetNymRequestAsync(_walletOptions.AgentDid, "W2SggneqnXwS2SSHmnFJk8"));
            //ViewData["LOOKUP_DIDS"] = await Did.ListMyDidsWithMetaAsync(context.Wallet);

            var provisioning = await _provisioningService.GetProvisioningAsync(context.Wallet);
            ViewData["ImagePrefix"] = provisioning.GetTag("ProfileImagePrefix");
            return View(provisioning);
        }
        
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
