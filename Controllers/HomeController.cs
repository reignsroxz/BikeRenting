using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BikeSharingApp.Data;
using BikeSharingApp.Models;

namespace BikeSharingApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly BikeSharingDbContext _context;

        public HomeController(BikeSharingDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var viewModel = new HomeViewModel
            {
                AvailableBikesCount = await _context.Bikes.CountAsync(b => b.IsAvailable),
                TotalUsersCount = await _context.Users.CountAsync(),
                RecentReviews = await _context.Reviews
                    .Include(r => r.Bike)
                    .Include(r => r.Renter)
                    .OrderByDescending(r => r.ReviewDate)
                    .Take(5)
                    .ToListAsync()
            };

            return View(viewModel);
        }

        public IActionResult Contact()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage(Message message)
        {
            if (ModelState.IsValid)
            {
                message.MessageDate = DateTime.Now;
                _context.Messages.Add(message);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Your message has been sent successfully!";
                return RedirectToAction(nameof(Contact));
            }

            return View("Contact", message);
        }
    }
}