using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using BikeSharingApp.Models;
using BikeSharingApp.Data;
using System;
using System.Security.Claims;
using System.Linq;
using System.Threading.Tasks;

namespace BikeSharingApp.Controllers
{
    [Authorize]
public class RentalController : Controller
{
    private readonly BikeSharingDbContext _context;

    public RentalController(BikeSharingDbContext context)
    {
        _context = context;
    }

        // GET: Rental
        public async Task<IActionResult> Index()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            return View(await _context.Rentals
                .Include(r => r.Bike)
                    .ThenInclude(b => b.Owner)
                .Include(r => r.Renter)
                .Where(r => r.RenterID == userId)
                .OrderByDescending(r => r.RentalStart)
                .ToListAsync());
        }

        // GET: Rental/MyRentals
        public async Task<IActionResult> MyRentals()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var rentals = await _context.Rentals
                .Include(r => r.Bike)
                    .ThenInclude(b => b.Owner)
                .Where(r => r.RenterID == userId)
                .OrderByDescending(r => r.RentalStart)
                .ToListAsync();

            return View(rentals);
        }

        // GET: Rental/Return/5
        public async Task<IActionResult> Return(string? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            if (!int.TryParse(id, out int rentalId))
            {
                return BadRequest("Invalid rental ID format");
            }

            var rental = await _context.Rentals
                .Include(r => r.Bike)
                .FirstOrDefaultAsync(m => m.RentalID == rentalId);

            if (rental == null)
            {
                return NotFound();
            }

            if (rental.RentalEnd.HasValue)
            {
                return BadRequest("This rental has already been returned");
            }

            rental.RentalEnd = DateTime.Now;
            rental.Bike.IsAvailable = true;

            // Calculate total cost
            var duration = rental.RentalEnd.Value - rental.RentalStart;
            var hours = Math.Ceiling(duration.TotalHours);
            rental.TotalCost = rental.Bike.HourlyRate * (decimal)hours;

            await _context.SaveChangesAsync();

            return RedirectToAction("Details", "Bike", new { id = rental.BikeID });
        }

        // GET: Rental/Details/5
        public async Task<IActionResult> Details(string? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            if (!int.TryParse(id, out int rentalId))
            {
                return BadRequest("Invalid rental ID format");
            }

            var rental = await _context.Rentals
                .Include(r => r.Bike)
                .Include(r => r.Renter)
                .FirstOrDefaultAsync(m => m.RentalID == rentalId);

            if (rental == null)
            {
                return NotFound();
            }

            // Check if the current user owns this rental or is the bike owner
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            if (rental.RenterID != userId && rental.Bike.OwnerID != userId)
            {
                return Forbid();
            }

            return View(rental);
        }

        // POST: Rental/Rent/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Rent(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var bike = await _context.Bikes
                .Include(b => b.Owner)
                .FirstOrDefaultAsync(b => b.BikeID == id);

            if (bike == null)
            {
                return NotFound();
            }

            if (bike.OwnerID == userId)
            {
                TempData["ErrorMessage"] = "You cannot rent your own bike.";
                return RedirectToAction("Details", "Bike", new { id = id });
            }

            if (!bike.IsAvailable)
            {
                TempData["ErrorMessage"] = "This bike is not available for rent.";
                return RedirectToAction("Details", "Bike", new { id = id });
            }

            var rental = new Rental
            {
                BikeID = id,
                RenterID = userId,
                RentalStart = DateTime.Now
            };

            bike.IsAvailable = false; // Set bike as not available
            _context.Rentals.Add(rental);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(MyRentals));
        }

        // GET: Rental/Create
    public IActionResult Create()
    {
        ViewBag.Bikes = _context.Bikes.ToList();
        ViewBag.Renters = _context.Users.ToList();
        return View();
    }

        // POST: Rental/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("BikeID,RenterID,RentalStart,RentalEnd,TotalCost")] Rental rental)
    {
        if (ModelState.IsValid)
        {
                _context.Add(rental);
                await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        ViewBag.Bikes = _context.Bikes.ToList();
        ViewBag.Renters = _context.Users.ToList();
        return View(rental);
    }

        // GET: Rental/Edit/5
        public async Task<IActionResult> Edit(string? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            if (!int.TryParse(id, out int rentalId))
            {
                return BadRequest("Invalid rental ID format");
            }

            var rental = await _context.Rentals.FindAsync(rentalId);
            if (rental == null)
            {
                return NotFound();
            }
        ViewBag.Bikes = _context.Bikes.ToList();
        ViewBag.Renters = _context.Users.ToList();
        return View(rental);
    }

        // POST: Rental/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("RentalID,BikeID,RenterID,RentalStart,RentalEnd,TotalCost")] Rental rental)
        {
            if (id == null)
            {
                return NotFound();
            }

            if (!int.TryParse(id, out int rentalId))
            {
                return BadRequest("Invalid rental ID format");
            }

            if (rentalId != rental.RentalID)
            {
                return NotFound();
            }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(rental);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RentalExists(rental.RentalID))
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
        ViewBag.Bikes = _context.Bikes.ToList();
        ViewBag.Renters = _context.Users.ToList();
        return View(rental);
    }

        // GET: Rental/Delete/5
        public async Task<IActionResult> Delete(string? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            if (!int.TryParse(id, out int rentalId))
            {
                return BadRequest("Invalid rental ID format");
            }

            var rental = await _context.Rentals
                .Include(r => r.Bike)
                .Include(r => r.Renter)
                .FirstOrDefaultAsync(m => m.RentalID == rentalId);
            if (rental == null)
            {
                return NotFound();
            }

        return View(rental);
    }

        // POST: Rental/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            if (!int.TryParse(id, out int rentalId))
            {
                return BadRequest("Invalid rental ID format");
            }

            var rental = await _context.Rentals.FindAsync(rentalId);
            if (rental != null)
            {
        _context.Rentals.Remove(rental);
                await _context.SaveChangesAsync();
            }

        return RedirectToAction(nameof(Index));
    }

        private bool RentalExists(int id)
        {
            return _context.Rentals.Any(e => e.RentalID == id);
        }
    }
}