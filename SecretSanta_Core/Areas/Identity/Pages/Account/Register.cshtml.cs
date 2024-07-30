// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using SecretSanta_Core.Data;
using SecretSanta_Core.Models;
using SecretSanta_Core.Services;

namespace SecretSanta_Core.Areas.Identity.Pages.Account
{
	public class RegisterModel : PageModel
	{
		private readonly SignInManager<ApplicationUser> _signInManager;
		private readonly UserManager<ApplicationUser> _userManager;
		private readonly IUserStore<ApplicationUser> _userStore;
		private readonly IUserEmailStore<ApplicationUser> _emailStore;
		private readonly ILogger<RegisterModel> _logger;
		private readonly EmailService _emailSender;
		private readonly ApplicationDbContext _context;

		public RegisterModel(
			UserManager<ApplicationUser> userManager,
			IUserStore<ApplicationUser> userStore,
			SignInManager<ApplicationUser> signInManager,
			ILogger<RegisterModel> logger,
			EmailService emailSender,
			ApplicationDbContext context)
		{
			_userManager = userManager;
			_userStore = userStore;
			_emailStore = GetEmailStore();
			_signInManager = signInManager;
			_logger = logger;
			_emailSender = emailSender;
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
		public string ReturnUrl { get; set; }

		public List<string> Roles { get; set; }

		/// <summary>
		///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
		///     directly from your code. This API may change or be removed in future releases.
		/// </summary>

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
			[Display(Name = "Email")]
			public string Email { get; set; }

			/// <summary>
			///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
			///     directly from your code. This API may change or be removed in future releases.
			/// </summary>
			[Required]
			[StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
			[DataType(DataType.Password)]
			[Display(Name = "Password")]
			public string Password { get; set; }

			/// <summary>
			///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
			///     directly from your code. This API may change or be removed in future releases.
			/// </summary>
			[DataType(DataType.Password)]
			[Display(Name = "Confirm password")]
			[Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
			public string ConfirmPassword { get; set; }

			public int AgencyId { get; set; }

			public string Role { get; set; }

		}


		public async Task<IActionResult> OnGetAsync(int id, string returnUrl = null)
		{
			var user = await _userManager.GetUserAsync(User);
			if (user == null)
			{
				return LocalRedirect(Url.Page("/Account/Login"));
			}
			ReturnUrl = returnUrl;
			ViewData["Roles"] = GetRolesForRegister();
			ViewData["AgencyId"] = id;
			ViewData["Password"] = "SecretSanta!01";
			return Page();
		}

		public async Task<IActionResult> OnPostAsync(string returnUrl = null)
		{
			returnUrl ??= Url.Content("~/");
			if (ModelState.IsValid)
			{
				IdentityResult result = null;

				using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
				try
				{
					var user = new ApplicationUser {
						UserName = Input.Email,
						Email = Input.Email,
						IsConfirmed = false,
						AgencyId = Input.AgencyId,
						Archive = false,
						IsActive = true
					};

					await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
					await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);
					result = await _userManager.CreateAsync(user, Input.Password);
					
					if (result.Succeeded)
					{
						_logger.LogInformation("User created a new account with password.");
						await _userManager.AddToRoleAsync(user, Input.Role);

						var code = await _userManager.GeneratePasswordResetTokenAsync(user);
						code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
						
						await SendLoginEmail(Input.Email, code, Input.Role);
						scope.Complete();
						return LocalRedirect(returnUrl);

					}
					else
					{
						scope.Complete();
						return (IActionResult)result;
					}

				}
				catch
				{
					scope.Dispose();
					foreach (var error in result.Errors)
					{
						ModelState.AddModelError(string.Empty, error.Description);
					}
					ViewData["Roles"] = GetRolesForRegister();
					ViewData["AgencyId"] = Input.AgencyId;
					ReturnUrl = returnUrl;
					ViewData["Password"] = "SecretSanta!01";
				}
			}

			// If we got this far, something failed, redisplay form
			return Page();
		}

		private ApplicationUser CreateUser()
		{
			try
			{
				return Activator.CreateInstance<ApplicationUser>();
			}
			catch
			{
				throw new InvalidOperationException($"Can't create an instance of '{nameof(ApplicationUser)}'. " +
					$"Ensure that '{nameof(IdentityUser)}' is not an abstract class and has a parameterless constructor, or alternatively " +
					$"override the register page in /Areas/Identity/Pages/Account/Register.cshtml");
			}
		}

		private IUserEmailStore<ApplicationUser> GetEmailStore()
		{
			if (!_userManager.SupportsUserEmail)
			{
				throw new NotSupportedException("The default UI requires a user store with email support.");
			}
			return (IUserEmailStore<ApplicationUser>)_userStore;
		}

		private SelectList GetRolesForRegister()
		{
			List<string> roles = new List<string>();
			if (User.IsInRole("Admin"))
			{
				roles.Add("PRIMARY");
				roles.Add("STANDARD");
				roles.Add("PICKUP");
			}
			else
			{
				roles.Add("PRIMARY");
				roles.Add("STANDARD");
				roles.Add("PICKUP");
			}

			return new SelectList(
				(roles.ToDictionary(d => d, d => d)).Select(x => new { value = x.Key, text = x.Value }),
				"value", "text");
		}

		private async Task SendLoginEmail(string email, string code, string role)
		{

			var callbackUrl = Url.Page(
				"/Account/ResetPassword",
				pageHandler: null,
				values: new { area = "Identity", code },
				protocol: Request.Scheme);
			var adminEmail = _context.TblAdminEmails.FirstOrDefault(l => l.EmailName == "Login Email");
			var content = String.Format(adminEmail.EmailContent, new object[]
			{
				email, callbackUrl
			});


			var emailServiceModel = new EmailServiceModel
			{
				To = email,
				Subject = "Secret Santa Login Info",
				Message = content,
				HasAttachment = false,
				CC = String.Empty
			};

			await _emailSender.SendEmail(emailServiceModel);

		}
	}
}
