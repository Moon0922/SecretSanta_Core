using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using SecretSanta_Core.Data;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace SecretSanta_Core.Areas.Identity.Pages.Account
{
	public class ResetForgottenPasswordModel : PageModel
	{
		private readonly UserManager<ApplicationUser> _userManager;

		public ResetForgottenPasswordModel(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
		{
			_userManager = userManager;
		}

		[BindProperty] public InputModel Input { get; set; } = null!;

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

			[Required]
			public string Code { get; set; }

		}

		public IActionResult OnGet(string? code = null)
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
				ModelState.AddModelError(string.Empty, "The mail doesn't exsits");
				return null;
			}

			IdentityResult result = await _userManager.ResetPasswordAsync(user, Input.Code, Input.Password);
			if (result.Succeeded)
			{
				return RedirectToPage("./Login");
			}

			foreach (var error in result.Errors)
			{
				ModelState.AddModelError(string.Empty, error.Description);
			}

			return Page();
		}

	}

}
