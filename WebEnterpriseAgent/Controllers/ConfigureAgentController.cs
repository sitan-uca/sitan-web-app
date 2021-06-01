using Hyperledger.Aries.Agents;
using Hyperledger.Aries.Configuration;
using Hyperledger.Aries.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Processing;
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
        private readonly AgentOptions _agentOptions;
        private readonly IAgentProvider _agentContextProvider;
        private readonly IProvisioningService _provisionService;
        private readonly IWalletRecordService _walletRecordService;

        private IWebHostEnvironment WebHostEnvironment;

        public ConfigureAgentController(
            IAgentProvider agentContextProvider,
            IProvisioningService provisionService,
            IOptions<AgentOptions> agentOptions,
            IWalletRecordService walletRecordService,
            IWebHostEnvironment environment)
        {
            _agentContextProvider = agentContextProvider;
            _provisionService = provisionService;
            _agentOptions = agentOptions.Value;
            _walletRecordService = walletRecordService;
            WebHostEnvironment = environment;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var context = await _agentContextProvider.GetContextAsync();
            var provisioningRecord = await _provisionService.GetProvisioningAsync(context.Wallet);
            ViewData["ImagePrefix"] = provisioningRecord.GetTag("ProfileImagePrefix");
            return View(provisioningRecord);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateAgentOptions(AgentOptions model, IFormFile ImageFile)
        {
            var context = await _agentContextProvider.GetContextAsync();
            var provisioningRecord = await _provisionService.GetProvisioningAsync(context.Wallet);

            if (model.AgentName != null)
            {
                provisioningRecord.Owner.Name = model.AgentName;
                Environment.SetEnvironmentVariable("AGENT_NAME", model.AgentName);
            }

            //_agentOptions.AutoRespondCredentialOffer = model.AutoRespondCredentialOffer;
            //_agentOptions.AutoRespondCredentialRequest = model.AutoRespondCredentialRequest;

            string wwwPath = WebHostEnvironment.WebRootPath;            

            string path = Path.Combine(wwwPath, "images");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            if (ImageFile != null && ImageFile.Length > 0)
            {
                using (var ms = new MemoryStream())
                {
                    int maxWidth = 300;
                    IImageFormat format;
                    ImageFile.CopyTo(ms);
                    ms.Position = 0;
                    Image image = Image.Load(ms, out format);
                    image.Mutate(x => x.Resize(maxWidth, image.Height * (maxWidth / image.Width)));
                    

                    string imageBase64 = image.ToBase64String(format);
                    string prefix = imageBase64.Substring(0, imageBase64.IndexOf(",")+1);
                    string data = imageBase64.Substring(imageBase64.IndexOf(",")+1);
                    provisioningRecord.Owner.ImageUrl = data;
                    provisioningRecord.SetTag("ProfileImagePrefix", prefix);
                    
                    Environment.SetEnvironmentVariable("AGENT_IMAGE", provisioningRecord.Owner.ImageUrl);
                }

                //using (FileStream stream = new FileStream(Path.Combine(path,
                //Path.GetFileName(ImageFile.FileName)), FileMode.Create))
                //{
                //    ImageFile.CopyTo(stream);                    
                //    provisioningRecord.Owner.ImageUrl = "/images/" + Path.GetFileName(ImageFile.FileName);
                //}
            }

            await _walletRecordService.UpdateAsync(context.Wallet, provisioningRecord);

            return RedirectToAction("Index");
        }
    }
}
