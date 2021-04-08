using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Sigo.WebApp.ExternalServices;
using Sigo.WebApp.Models;

namespace Sigo.WebApp.Controllers
{
    [Authorize]
    public class StandardsController : Controller
    {
        private readonly IStandardApiService _standardApiService;

        public StandardsController(IStandardApiService standardApiService)
        {
            _standardApiService = standardApiService ?? throw new ArgumentNullException(nameof(standardApiService));
        }

        public async Task<IActionResult> Index()
        {
            await LogTokenAndClaims();
            return View(await _standardApiService.GetStandardsAsync());
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateStandard standard)
        {
            if (!ModelState.IsValid) return View();
            
            await _standardApiService.CreateStandardAsync(standard);
            return RedirectToAction(nameof(Index));
        }
       
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return UnprocessableEntity();
            }

            var standard = await _standardApiService.GetStandardByIdAsync(id);
            
            if (standard == null)
            {
                return NotFound();
            }
            
            return View(standard);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, UpdateStandard standard)
        {
            if (ModelState.IsValid)
            {
                var response = await _standardApiService.UpdateStandardAsync(standard);
                if (response == null) return NotFound();
            }
            
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(string id)
        {
            await _standardApiService.DeleteStandardAsync(id);
            return Ok();
        }

        public async Task LogTokenAndClaims()
        {
            var identityToken = await HttpContext.GetTokenAsync(OpenIdConnectParameterNames.IdToken);

            Debug.WriteLine($"Identity token: {identityToken}");

            foreach (var claim in User.Claims)
            {
                Debug.WriteLine($"Claim type: {claim.Type} - Claim value: {claim.Value}");
            }
        }

        [Authorize(Roles = "admin")]
        public async Task<IActionResult> OnlyAdmin()
        {
            var userInfo = await _standardApiService.GetUserInfoAsync();
            return View(userInfo);
        }
    }
}