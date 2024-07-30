// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using SecretSanta_Core.Data;

namespace SecretSanta_Core.Areas.Identity.Pages.Account
{
    public class ResetPasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public ResetPasswordModel(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

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
            [Required]
            [EmailAddress]
            public string Email { get; set; }

            [Display(Name = "Confirm Email")]
            [Compare("Email", ErrorMessage = "The email addresses do not match.")]
            public string ConfirmEmail { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
			[RegularExpression(@"(?=.*\d)(?=.*[a-z])(?=.*[A-Z])(?=.*\W).{6,}", ErrorMessage = "Passwords must be at least 6 characters and contain at least one uppercase letter, one lowercase letter, one number, and one special character.")]
			[DataType(DataType.Password)]
            public string Password { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            public string Code { get; set; }

            [Required(ErrorMessage = "First Name is required")]
            [Display(Name = "First Name")]
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

        public IActionResult OnGet(string code = null)
        {
            if (code == null)
            {
                return BadRequest("A code must be supplied for password reset.");
            }

            Input = new InputModel
            {
                Code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code))
            };
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.FindByEmailAsync(Input.Email);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return RedirectToPage("./ResetPasswordConfirmation");
            }

            IdentityResult result = null;

            using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
            try
            {
                result = await _userManager.ResetPasswordAsync(user, Input.Code, Input.Password);

                if (/*result.Succeeded*/true)
                {
                    user.IsConfirmed = true;
                    await _userManager.UpdateAsync(user);
                    var agencyContact = new TblAgencyContact
                    {
                        Id = user.Id,
                        FirstName = Input.FirstName,
                        LastName = Input.LastName,
                        Phone = Input.Phone,
                        AltPhone = Input.AltPhone,
                        Fax = Input.Fax,
                        EstimateWishes = Input.EstimateWishes
                    };
                    _context.Add(agencyContact);
                    _context.SaveChanges();
                   
                }

                //await _userManager.AddToRoleAsync(user, "CANEDIT");

                scope.Complete();
                return RedirectToPage("./Login");
            }
            catch
            {
                scope.Dispose();
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return Page();
        }
    }
}
