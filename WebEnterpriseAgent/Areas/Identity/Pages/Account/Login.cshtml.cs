using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Hyperledger.Aries.Configuration;
using Hyperledger.Indy.WalletApi;

namespace WebEnterpriseAgent.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ILogger<LoginModel> _logger;
        private readonly AgentOptions _agentOptions;
        private readonly IProvisioningService _provisioningService;

        public LoginModel(
            SignInManager<IdentityUser> signInManager, 
            IOptions<AgentOptions> agentOptions,
            IProvisioningService provisioningService,
            ILogger<LoginModel> logger,
            UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _agentOptions = agentOptions.Value;
            _provisioningService = provisioningService;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public string ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; }

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [Display(Name = "Remember me?")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl = returnUrl ?? Url.Content("~/");

            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
            
            _userManager.Options.Password.RequireNonAlphanumeric = false;
            _userManager.Options.SignIn.RequireConfirmedAccount = false;

            var user = _signInManager.UserManager.FindByEmailAsync("uca-reg@test.com");
            if (user.Result == null)
            {
                var newuser = new IdentityUser { UserName = "WebAgentWallet03", Email = "uca-reg@test.com" };
                
                await _userManager.CreateAsync(newuser, "MyWalletKey123");
            }

            var user2 = _signInManager.UserManager.FindByEmailAsync("uca-international@test.com");
            if (user2.Result == null)
            {
                var newuser = new IdentityUser { UserName = "WebAgentWalletINT02", Email = "uca-international@test.com" };

                await _userManager.CreateAsync(newuser, "MyWalletKey123INT");
            }


            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl = returnUrl ?? Url.Content("~/");

            if (ModelState.IsValid)
            {
                var user = _signInManager.UserManager.FindByEmailAsync(Input.Email);                
                // This doesn't count login failures towards account lockout
                // To enable password failures to trigger account lockout, set lockoutOnFailure: true
                var result = await _signInManager.PasswordSignInAsync(user.Result?.UserName ?? string.Empty, Input.Password, Input.RememberMe, lockoutOnFailure: false);                
                if (result.Succeeded)
                {   
                    _agentOptions.WalletCredentials.Key = Input.Password;
                    _agentOptions.WalletConfiguration.Id = user.Result.UserName;                    

                    try
                    {
                        await _provisioningService.ProvisionAgentAsync(_agentOptions);
                    } catch (WalletExistsException)
                    {
                        // OK
                    }                   
                    
                    _logger.LogInformation("User logged in.");
                    return LocalRedirect(returnUrl);
                }
                if (result.RequiresTwoFactor)
                {
                    return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
                }
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User account locked out.");
                    return RedirectToPage("./Lockout");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return Page();
                }
            }

            // If we got this far, something failed, redisplay form
            return Page();
        }
    }
}
