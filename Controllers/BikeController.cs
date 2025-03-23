using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using BikeSharingApp.Data;
using BikeSharingApp.Models;
using Microsoft.Extensions.Logging;

namespace BikeSharingApp.Controllers
{
    public class BikeController : Controller
    {
        private readonly BikeSharingDbContext _context;
        private readonly ILogger<BikeController> _logger;

        public BikeController(BikeSharingDbContext context, ILogger<BikeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string searchString, string bikeType, string location)
        {
            var query = _context.Bikes
                .Include(b => b.Owner)
                .Where(b => b.IsAvailable);

            // Apply search filters
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(b => 
                    b.BikeType.Contains(searchString) || 
                    b.Location.Contains(searchString) ||
                    b.Owner.FullName.Contains(searchString));
            }

            if (!string.IsNullOrEmpty(bikeType))
            {
                query = query.Where(b => b.BikeType == bikeType);
            }

            if (!string.IsNullOrEmpty(location))
            {
                query = query.Where(b => b.Location == location);
            }

            // Get unique bike types and locations for filter dropdowns
            var bikeTypes = await _context.Bikes.Select(b => b.BikeType).Distinct().ToListAsync();
            var locations = await _context.Bikes.Select(b => b.Location).Distinct().ToListAsync();

            var viewModel = new BikeIndexViewModel
            {
                Bikes = await query.ToListAsync(),
                BikeTypes = bikeTypes,
                Locations = locations,
                SearchString = searchString,
                SelectedBikeType = bikeType,
                SelectedLocation = location
            };

            return View(viewModel);
        }

        // GET: Bike/Details/5
        public async Task<IActionResult> Details(string? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            if (!int.TryParse(id, out int bikeId))
            {
                return BadRequest("Invalid bike ID format");
            }

            var bike = await _context.Bikes
                .Include(b => b.Owner)
                .Include(b => b.Reviews)
                .ThenInclude(r => r.Renter)
                .Include(b => b.Rentals)
                .FirstOrDefaultAsync(m => m.BikeID == bikeId);

            if (bike == null)
            {
                return NotFound();
            }

            return View(bike);
        }

        // GET: Bike/Create
        [Authorize]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Bike/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Create([Bind("BikeType,Location,Description,HourlyRate")] Bike bike)
        {
            System.Diagnostics.Debug.WriteLine("Create Bike POST - Starting");
            System.Diagnostics.Debug.WriteLine($"Bike Data - Type: {bike.BikeType}, Location: {bike.Location}, Rate: {bike.HourlyRate}");

            // Log all model state errors
            foreach (var modelStateEntry in ModelState.Values)
            {
                foreach (var error in modelStateEntry.Errors)
                {
                    System.Diagnostics.Debug.WriteLine($"Model State Error: {error.ErrorMessage}");
                    TempData["ErrorMessage"] = error.ErrorMessage;
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    System.Diagnostics.Debug.WriteLine($"User ID Claim: {userIdClaim}");
                    
                    if (string.IsNullOrEmpty(userIdClaim))
                    {
                        System.Diagnostics.Debug.WriteLine("No user ID claim found");
                        TempData["ErrorMessage"] = "User ID not found. Please try logging in again.";
                        return View(bike);
                    }

                    if (!int.TryParse(userIdClaim, out int userId))
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to parse user ID: {userIdClaim}");
                        TempData["ErrorMessage"] = "Invalid user ID. Please try logging in again.";
                        return View(bike);
                    }

                    var user = await _context.Users
                        .Include(u => u.Bikes)
                        .FirstOrDefaultAsync(u => u.UserID == userId);

                    if (user == null)
                    {
                        System.Diagnostics.Debug.WriteLine("User not found");
                        TempData["ErrorMessage"] = "User not found. Please try logging in again.";
                        return View(bike);
                    }

                    System.Diagnostics.Debug.WriteLine($"Setting OwnerID to: {userId}");
                    bike.OwnerID = userId;
                    bike.IsAvailable = true;

                    System.Diagnostics.Debug.WriteLine($"Final Bike Data - OwnerID: {bike.OwnerID}, Type: {bike.BikeType}, Location: {bike.Location}, Rate: {bike.HourlyRate}");

                    try
                    {
                        _context.Add(bike);
                        await _context.SaveChangesAsync();
                        System.Diagnostics.Debug.WriteLine("Bike saved successfully");
                        
                        TempData["SuccessMessage"] = "Your bike has been listed successfully!";
                        return RedirectToAction(nameof(Index));
                    }
                    catch (DbUpdateException ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Database update error: {ex.Message}");
                        System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException?.Message}");
                        TempData["ErrorMessage"] = $"Database error: {ex.InnerException?.Message ?? ex.Message}";
                        return View(bike);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error saving bike: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                    TempData["ErrorMessage"] = $"Error: {ex.Message}";
                    return View(bike);
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Model validation failed:");
                foreach (var modelError in ModelState.Values.SelectMany(v => v.Errors))
                {
                    System.Diagnostics.Debug.WriteLine($"Validation error: {modelError.ErrorMessage}");
                    TempData["ErrorMessage"] = modelError.ErrorMessage;
                }
            }
            return View(bike);
        }

        // GET: Bike/Edit/5
        [Authorize]
        public async Task<IActionResult> Edit(string? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            if (!int.TryParse(id, out int bikeId))
            {
                return BadRequest("Invalid bike ID format");
            }

            var bike = await _context.Bikes.FindAsync(bikeId);
            if (bike == null)
            {
                return NotFound();
            }

            // Check if the current user owns this bike
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            if (bike.OwnerID != userId)
            {
                return Forbid();
            }

            return View(bike);
        }

        // POST: Bike/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Edit(string id, [Bind("BikeID,BikeType,Location,Description,HourlyRate,IsAvailable")] Bike bike)
        {
            if (id == null)
            {
                return NotFound();
            }

            if (!int.TryParse(id, out int bikeId))
            {
                return BadRequest("Invalid bike ID format");
            }

            if (bikeId != bike.BikeID)
            {
                return NotFound();
            }

            // Check if the current user owns this bike
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            if (bike.OwnerID != userId)
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingBike = await _context.Bikes.FindAsync(bikeId);
                    if (existingBike == null)
                    {
                        return NotFound();
                    }

                    // Preserve the owner ID
                    bike.OwnerID = existingBike.OwnerID;

                    _context.Entry(existingBike).CurrentValues.SetValues(bike);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BikeExists(bike.BikeID))
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
            return View(bike);
        }

        // GET: Bike/Delete/5
        [Authorize]
        public async Task<IActionResult> Delete(string? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            if (!int.TryParse(id, out int bikeId))
            {
                return BadRequest("Invalid bike ID format");
            }

            var bike = await _context.Bikes
                .Include(b => b.Owner)
                .FirstOrDefaultAsync(m => m.BikeID == bikeId);
            if (bike == null)
            {
                return NotFound();
            }

            // Check if the current user owns this bike
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            if (bike.OwnerID != userId)
            {
                return Forbid();
            }

            return View(bike);
        }

        // POST: Bike/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            if (!int.TryParse(id, out int bikeId))
            {
                return BadRequest("Invalid bike ID format");
            }

            var bike = await _context.Bikes.FindAsync(bikeId);
            if (bike != null)
            {
                // Check if the current user owns this bike
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                if (bike.OwnerID != userId)
                {
                    return Forbid();
                }

                _context.Bikes.Remove(bike);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Bike/Rent/5
        [Authorize]
        public async Task<IActionResult> Rent(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var bike = await _context.Bikes
                .Include(b => b.Owner)
                .FirstOrDefaultAsync(m => m.BikeID == id);

            if (bike == null)
            {
                return NotFound();
            }

            if (!bike.IsAvailable)
            {
                TempData["ErrorMessage"] = "This bike is not available for rent.";
                return RedirectToAction(nameof(Details), new { id = bike.BikeID });
            }

            return View(bike);
        }

        // POST: Bike/Rent/5
        [HttpPost, ActionName("Rent")]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> RentConfirmed(int id)
        {
            var bike = await _context.Bikes.FindAsync(id);
            if (bike == null)
            {
                return NotFound();
            }

            if (bike.IsAvailable != true)
            {
                TempData["ErrorMessage"] = "This bike is not available for rent.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Check if user is trying to rent their own bike
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            if (bike.OwnerID == userId)
            {
                TempData["ErrorMessage"] = "You cannot rent your own bike.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var rental = new Rental
            {
                BikeID = id,
                RenterID = userId,
                RentalStart = DateTime.Now
            };

            bike.IsAvailable = false; // Not Available
            _context.Rentals.Add(rental);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id });
        }

        private bool BikeExists(int id)
        {
            return _context.Bikes.Any(e => e.BikeID == id);
        }
    }
}