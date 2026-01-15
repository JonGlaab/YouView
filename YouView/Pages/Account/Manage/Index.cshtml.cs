using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YouView.Models;
using YouView.Services;

namespace YouView.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly BlobService _blobService;

        public IndexModel(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            BlobService blobService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _blobService = blobService;
        }

        public string Username { get; set; }

        public string CurrentProfilePicUrl { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Display(Name = "Username")]
            public string Username { get; set; }

            [Display(Name = "First Name")]
            public string FirstName { get; set; }

            [Display(Name = "Last Name")]
            public string LastName { get; set; }

            [Display(Name = "Bio")]
            public string Bio { get; set; }

            [Display(Name = "Profile Picture")]
            public IFormFile ProfilePictureUpload { get; set; }
        }

        private async Task LoadAsync(User user)
        {
            var userName = await _userManager.GetUserNameAsync(user);

            Username = userName;
            CurrentProfilePicUrl = user.ProfilePicUrl;

            Input = new InputModel
            {
                Username = userName,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Bio = user.Bio
            };
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
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

            // HANDLE PROFILE PICTURE UPLOAD 
            if (Input.ProfilePictureUpload != null)
            {
                // SAFETY CHECK: Only try to delete if a URL actually exists
                if (!string.IsNullOrEmpty(user.ProfilePicUrl))
                {
                    await _blobService.DeleteFileAsync(user.ProfilePicUrl);
                }

                // Upload the new one
                string newUrl = await _blobService.UploadFileAsync(Input.ProfilePictureUpload);

                // Save URL to Database
                user.ProfilePicUrl = newUrl;

                // Force Update immediately
                await _userManager.UpdateAsync(user);
            }

            // HANDLE USERNAME CHANGE
            var currentUsername = await _userManager.GetUserNameAsync(user);
            if (Input.Username != currentUsername)
            {
                var setUserNameResult = await _userManager.SetUserNameAsync(user, Input.Username);
                if (!setUserNameResult.Succeeded)
                {
                    StatusMessage = "Unexpected error when trying to set user name.";
                    return RedirectToPage();
                }
            }

            // HANDLE CUSTOM FIELDS 
            if (user.FirstName != Input.FirstName ||
                user.LastName != Input.LastName ||
                user.Bio != Input.Bio)
            {
                user.FirstName = Input.FirstName;
                user.LastName = Input.LastName;
                user.Bio = Input.Bio;

                await _userManager.UpdateAsync(user);
            }

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Your profile has been updated";
            return RedirectToPage();
        }
    }
}