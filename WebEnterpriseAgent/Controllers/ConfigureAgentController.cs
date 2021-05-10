using Hyperledger.Aries.Agents;
using Hyperledger.Aries.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WebAgent.Models;

namespace WebAgent.Controllers
{
    [Authorize]
    public class ConfigureAgentController : Controller
    {
        private readonly AgentOptions _walletOptions;
        private readonly IAgentProvider _agentContextProvider;
        private readonly IProvisioningService _provisionService;

        private IWebHostEnvironment Environment;

        public ConfigureAgentController(
            IAgentProvider agentContextProvider,
            IProvisioningService provisionService, 
            IOptions<AgentOptions> walletOptions, 
            IWebHostEnvironment environment)
        {
            _agentContextProvider = agentContextProvider;
            _provisionService = provisionService;
            _walletOptions = walletOptions.Value;
            Environment = environment;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var context = await _agentContextProvider.GetContextAsync();
            var provisioningRecord = await _provisionService.GetProvisioningAsync(context.Wallet);

            return View(provisioningRecord);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateAgentOptions(AgentOptions model, IFormFile ImageFile)
        {
            var context = await _agentContextProvider.GetContextAsync();
            var provisioningRecord = await _provisionService.GetProvisioningAsync(context.Wallet);

            _walletOptions.AgentName = model.AgentName;
            _walletOptions.AutoRespondCredentialOffer = model.AutoRespondCredentialOffer;
            _walletOptions.AutoRespondCredentialRequest = model.AutoRespondCredentialRequest;

            string wwwPath = Environment.WebRootPath;            

            string path = Path.Combine(wwwPath, "images");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }          
            if (ImageFile != null)
            {
                using (FileStream stream = new FileStream(Path.Combine(path,
                Path.GetFileName(ImageFile.FileName)), FileMode.Create))
                {
                    ImageFile.CopyTo(stream);
                    _walletOptions.AgentImageUri = "/images/" + Path.GetFileName(ImageFile.FileName);
                }
            }            

            return RedirectToAction("Index");
        }
    }
}
