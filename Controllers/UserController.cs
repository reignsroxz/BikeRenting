using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System;
using BikeSharingApp.Data;
using BikeSharingApp.Models;
using BCrypt.Net;

namespace BikeSharingApp.Controllers
{
    public class UserController : Controller
    {
        private readonly BikeSharingDbContext _context;

        public UserController(BikeSharingDbContext context)
        {
            _context = context;
        }

        // GET: User
        public async Task<IActionResult> Index()
        {
            return View(await _context.Users.ToListAsync());
        }

        // GET: User/Details/5
        public async Task<IActionResult> Details(string? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            if (!int.TryParse(id, out int userId))
            {
                return BadRequest("Invalid user ID format");
            }

            var user = await _context.Users
                .Include(u => u.Bikes)
                .Include(u => u.Rentals)
                .Include(u => u.Reviews)
                .FirstOrDefaultAsync(m => m.UserID == userId);

            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // GET: User/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: User/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Username,Email,Password,FirstName,LastName,PhoneNumber")] User user)
        {
            if (ModelState.IsValid)
            {
                user.RegistrationDate = DateTime.Now;
                user.IsActive = true;
                _context.Add(user);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(user);
        }

        // GET: User/Edit/5
        public async Task<IActionResult> Edit(string? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            if (!int.TryParse(id, out int userId))
            {
                return BadRequest("Invalid user ID format");
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }

        // POST: User/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("UserID,Username,Email,Password,FirstName,LastName,PhoneNumber,IsActive")] User user)
        {
            if (id == null)
            {
                return NotFound();
            }

            if (!int.TryParse(id, out int userId))
            {
                return BadRequest("Invalid user ID format");
            }

            if (userId != user.UserID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingUser = await _context.Users.FindAsync(userId);
                    if (existingUser == null)
                    {
                        return NotFound();
                    }

                    // Preserve the registration date
                    user.RegistrationDate = existingUser.RegistrationDate;

                    _context.Entry(existingUser).CurrentValues.SetValues(user);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserExists(user.UserID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(user);
        }

        // GET: User/Delete/5
        public async Task<IActionResult> Delete(string? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            if (!int.TryParse(id, out int userId))
            {
                return BadRequest("Invalid user ID format");
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(m => m.UserID == userId);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // POST: User/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            if (!int.TryParse(id, out int userId))
            {
                return BadRequest("Invalid user ID format");
            }

            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.UserID == id);
        }

        // GET: User/Profile
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login");
            }

            if (!int.TryParse(userId, out int id))
            {
                return NotFound();
            }

            var user = await _context.Users
                .Include(u => u.Bikes)
                .Include(u => u.Rentals)
                    .ThenInclude(r => r.Bike)
                .FirstOrDefaultAsync(u => u.UserID == id);

            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // GET: User/Register
        public IActionResult Register()
        {
            return View();
        }

        // POST: User/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register([Bind("Username,FirstName,LastName,Email,Password,PhoneNumber")] User user)
        {
            if (!ModelState.IsValid)
            {
                foreach (var modelError in ModelState.Values.SelectMany(v => v.Errors))
                {
                    System.Diagnostics.Debug.WriteLine($"Model Error: {modelError.ErrorMessage}");
                }
                return View(user);
            }

            try
            {
                // Check if email or username already exists
                if (await _context.Users.AnyAsync(u => u.Email == user.Email))
                {
                    ModelState.AddModelError("Email", "Email already exists");
                    return View(user);
                }

                if (await _context.Users.AnyAsync(u => u.Username == user.Username))
                {
                    ModelState.AddModelError("Username", "Username already exists");
                    return View(user);
                }

                // Set additional user properties
                user.RegistrationDate = DateTime.Now;
                user.IsActive = true;

                // Hash the password
                user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);

                _context.Add(user);
                await _context.SaveChangesAsync();

                // Create claims for the user
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()),
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Email, user.Email)
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties();

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Registration Error: {ex.Message}");
                ModelState.AddModelError("", "An error occurred while registering. Please try again.");
                return View(user);
            }
        }

        // GET: User/Login
        public IActionResult Login()
        {
            return View();
        }

        // POST: User/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError("", "Please enter both email and password");
                return View();
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                ModelState.AddModelError("", "Invalid email or password");
                return View();
            }

            if (!BCrypt.Net.BCrypt.Verify(password, user.Password))
            {
                ModelState.AddModelError("", "Invalid email or password");
                return View();
            }

            // Create claims for the user
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()),
                new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
                new Claim(ClaimTypes.Email, user.Email)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties();

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            return RedirectToAction("Index", "Home");
        }

        // GET: User/Logout
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        // GET: User/Edit
        [Authorize]
        public async Task<IActionResult> Edit()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // POST: User/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Edit(int id, [Bind("UserID,FirstName,LastName,Email,PhoneNumber")] User user)
        {
            if (id != user.UserID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingUser = await _context.Users.FindAsync(id);
                    if (existingUser == null)
                    {
                        return NotFound();
                    }

                    // Update only allowed fields
                    existingUser.FirstName = user.FirstName;
                    existingUser.LastName = user.LastName;
                    existingUser.Email = user.Email;
                    existingUser.PhoneNumber = user.PhoneNumber;

                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Profile));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserExists(user.UserID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            return View(user);
        }
    }
}