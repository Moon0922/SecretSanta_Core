// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SecretSanta_Core.Data;

namespace SecretSanta_Core.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;

        public IndexModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string Username { get; set; }

        public int? AgencyId { get; set; }
        [TempData]
        public string StatusMessage { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            ///
            
            public string FirstName { get; set; }

            [Required(ErrorMessage = "Last Name is required")]
            [Display(Name = "Last Name")]
            public string LastName { get; set; }

            [Required(ErrorMessage = "Phone is required")]
            [Display(Name = "Phone")]
            public string Phone { get; set; }

            [Display(Name = "Alt Phone")]
            public string AltPhone { get; set; }

            public string Fax { get; set; }

            [Display(Name = "Estimate Wishes")]
            public int? EstimateWishes { get; set; }
        }

        private async Task LoadAsync(ApplicationUser user)
        {
            var userName = await _userManager.GetUserNameAsync(user);
           

            Username = userName;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            AgencyId = user.AgencyId;
            ViewData["HasAgencyId"] = false;
            if (user.AgencyId != null)
            {
                ViewData["HasAgencyId"] = true;
                var agencyUser = _context.TblAgencyContacts.Find(user.Id);
                Input = new InputModel
                {
                    FirstName = agencyUser.FirstName,
                    LastName = agencyUser.LastName,
                    Phone = agencyUser.Phone,
                    AltPhone = agencyUser.AltPhone,
                    Fax = agencyUser.Fax,
                    EstimateWishes = agencyUser.EstimateWishes
                };
            }

            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            if (user.AgencyId != null)
            {

                var agencyUser = _context.TblAgencyContacts.Find(user.Id);
                agencyUser.FirstName = Input.FirstName;
                agencyUser.LastName = Input.LastName;
                agencyUser.Phone = Input.Phone;
                agencyUser.AltPhone = Input.AltPhone;
                agencyUser.Fax = Input.Fax;
                agencyUser.EstimateWishes = Input.EstimateWishes;
                _context.Update(agencyUser);
                try
                {
                    _context.SaveChanges();
                }
                catch
                {
                    StatusMessage = "Unexpected error when trying to update.";
                    return RedirectToPage();
                }
            }

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Your profile has been updated";
            return RedirectToPage();
        }
    }
}
