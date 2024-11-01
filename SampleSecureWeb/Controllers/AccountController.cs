using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using SampleSecureWeb.Data;
using SampleSecureWeb.Models;
using System.ComponentModel.DataAnnotations;
using SampleSecureWeb.ViewModel;


namespace SampleSecureWeb.Controllers
{
    public class AccountController : Controller
    {
        private readonly IUser _userData;
        private const int MaxLoginAttempts = 3;
        private const int LockoutDurationMinutes = 3;
        private readonly ILogger<AccountController> _logger;
        public AccountController(IUser user, ILogger<AccountController> logger)
        {
            _userData = user;
            _logger = logger;
        }
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Register()
        {
            return View();
        }

        [HttpPost] 
        public ActionResult Register(RegistrationViewModel registrationViewModel)
        {
            try
            {   
                if(ModelState.IsValid)
                {
                    var user=new Models.User
                    {
                        Username=registrationViewModel.Username,
                        Password=registrationViewModel.Password,
                        RoleName="contributor"
                    };
                    _userData.Registration(user);
                    return RedirectToAction("Index", "Home");
                }
                else 
                {
                    foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                    {
                        Console.WriteLine(error.ErrorMessage);
                    }
                }
            }
            catch (System.Exception ex)
            {
                ViewBag.Error = ex.Message;
            }
            return View(registrationViewModel);
        } 

        public IActionResult Login(string returnUrl = null)
        {
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel loginViewModel)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(loginViewModel);
                }

                loginViewModel.ReturnUrl ??= Url.Content("~/");

                // Validasi returnUrl
                if (!string.IsNullOrEmpty(loginViewModel.ReturnUrl) && !Url.IsLocalUrl(loginViewModel.ReturnUrl))
                {
                    ModelState.AddModelError(string.Empty, "Invalid return URL");
                    return View(loginViewModel);
                }

                var existingUser = _userData.GetUser(loginViewModel.Username);
                if (existingUser == null)
                {
                    _logger.LogWarning($"Failed login attempt for non-existent user: {loginViewModel.Username}");
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return View(loginViewModel);
                }

                // Cek status lock
                if (existingUser.IsLocked)
                {
                    if (existingUser.LockoutEnd.HasValue && existingUser.LockoutEnd > DateTime.UtcNow)
                    {
                        var remainingTime = existingUser.LockoutEnd.Value - DateTime.UtcNow;
                        ModelState.AddModelError(string.Empty, 
                            $"Account is locked. Please try again after {remainingTime.Minutes:D2}:{remainingTime.Seconds:D2}");
                        _logger.LogWarning($"Attempted login on locked account: {loginViewModel.Username}");
                        return View(loginViewModel);
                    }

                    await UnlockUserAccount(existingUser);
                }

                try
                {
                    var loginUser = _userData.Login(new User
                    {
                        Username = loginViewModel.Username,
                        Password = loginViewModel.Password
                    });

                    if (loginUser != null)
                    {
                        await ResetLoginAttempts(existingUser);
                        await SignInUser(loginUser, loginViewModel.RememberLogin);
                        _logger.LogInformation($"User logged in successfully: {loginUser.Username}");
                        return LocalRedirect(loginViewModel.ReturnUrl);
                    }
                }
                catch (Exception ex)
                {
                    // Handle failed login
                    await HandleFailedLogin(existingUser);
                    ModelState.AddModelError(string.Empty, GetFailedLoginMessage(existingUser));
                    _logger.LogWarning($"Failed login attempt for user: {loginViewModel.Username}. Reason: {ex.Message}");
                    return View(loginViewModel);
                }

                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return View(loginViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Login error: {ex.Message}");
                ModelState.AddModelError(string.Empty, "An error occurred during login. Please try again.");
                return View(loginViewModel);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            try
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                _logger.LogInformation($"User logged out: {User.Identity?.Name}");
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Logout error: {ex.Message}");
                return RedirectToAction("Index", "Home");
            }
        }

        private async Task UnlockUserAccount(User user)
        {
            user.IsLocked = false;
            user.LoginAttempts = 0;
            user.LockoutEnd = null;
            _userData.UpdateUser(user);
            await Task.CompletedTask;
        }

        private async Task ResetLoginAttempts(User user)
        {
            user.LoginAttempts = 0;
            user.IsLocked = false;
            user.LockoutEnd = null;
            _userData.UpdateUser(user);
            await Task.CompletedTask;
        }

        private async Task HandleFailedLogin(User user)
        {
            user.LoginAttempts++;
            if (user.LoginAttempts >= MaxLoginAttempts)
            {
                user.IsLocked = true;
                user.LockoutEnd = DateTime.UtcNow.AddMinutes(LockoutDurationMinutes);
                _logger.LogWarning($"Account locked due to multiple failed attempts: {user.Username}");
            }
            _userData.UpdateUser(user);
            await Task.CompletedTask;
        }

        private string GetFailedLoginMessage(User user)
        {
            if (user.IsLocked)
            {
                return $"Account has been locked due to {MaxLoginAttempts} failed attempts. Please try again after {LockoutDurationMinutes} minutes.";
            }
            return $"Invalid login attempt. Remaining attempts: {MaxLoginAttempts - user.LoginAttempts}";
        }

        private async Task SignInUser(User user, bool isPersistent)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.RoleName),
                new Claim("LastLoginTime", DateTime.UtcNow.ToString())
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = isPersistent,
                    ExpiresUtc = DateTime.UtcNow.AddHours(12),
                    AllowRefresh = true
                });
        }
    }
}