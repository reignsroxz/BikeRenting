using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using BikeSharingApp.Models;
using BikeSharingApp.Data;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;

namespace BikeSharingApp.Controllers
{
    [Authorize]
    public class ReviewController : Controller
    {
        private readonly BikeSharingDbContext _context;

        public ReviewController(BikeSharingDbContext context)
        {
            _context = context;
        }

        // GET: Review
        public async Task<IActionResult> Index()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            System.Diagnostics.Debug.WriteLine($"Current user ID claim: {userIdClaim}");
            
            if (string.IsNullOrEmpty(userIdClaim))
            {
                System.Diagnostics.Debug.WriteLine("No user ID claim found");
                return RedirectToAction("Login", "User");
            }

            if (!int.TryParse(userIdClaim, out int userId))
            {
                System.Diagnostics.Debug.WriteLine($"Failed to parse user ID: {userIdClaim}");
                return RedirectToAction("Login", "User");
            }

            System.Diagnostics.Debug.WriteLine($"Filtering reviews for user ID: {userId}");
            var reviews = await _context.Reviews
                .Include(r => r.Bike)
                .Where(r => r.RenterID == userId)
                .OrderByDescending(r => r.ReviewDate)
                .ToListAsync();

            System.Diagnostics.Debug.WriteLine($"Found {reviews.Count} reviews for user {userId}");
            foreach (var review in reviews)
            {
                System.Diagnostics.Debug.WriteLine($"Review {review.ReviewID} - RenterID: {review.RenterID}, BikeID: {review.BikeID}");
            }

            return View(reviews);
        }

        // GET: Review/Create/5
        public async Task<IActionResult> Create(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var bike = await _context.Bikes
                .Include(b => b.Owner)
                .FirstOrDefaultAsync(b => b.BikeID == id);

            if (bike == null)
            {
                return NotFound();
            }

            // Check if user has rented this bike before
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var hasRented = await _context.Rentals
                .AnyAsync(r => r.BikeID == id && r.RenterID == userId);

            if (!hasRented)
            {
                TempData["ErrorMessage"] = "You can only review bikes you have rented.";
                return RedirectToAction("Details", "Bike", new { id });
            }

            // Check if user has already reviewed this bike
            var existingReview = await _context.Reviews
                .FirstOrDefaultAsync(r => r.BikeID == id && r.RenterID == userId);

            if (existingReview != null)
            {
                TempData["ErrorMessage"] = "You have already reviewed this bike.";
                return RedirectToAction("Details", "Bike", new { id });
            }

            var review = new Review
            {
                BikeID = (int)id,
                RenterID = userId,
                ReviewDate = DateTime.Now
            };

            return View(review);
        }

        // POST: Review/Create/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int id, [Bind("Rating,ReviewText")] Review review)
        {
            System.Diagnostics.Debug.WriteLine($"Create Review POST - BikeID: {id}");
            System.Diagnostics.Debug.WriteLine($"Review Data - Rating: {review.Rating}, Text: {review.ReviewText}");

            if (id == 0)
            {
                System.Diagnostics.Debug.WriteLine("Invalid bike ID");
                return NotFound();
            }

            review.BikeID = id;

            if (ModelState.IsValid)
            {
                try
                {
                    var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                    System.Diagnostics.Debug.WriteLine($"User ID: {userId}");
                    
                    review.RenterID = userId;
                    review.ReviewDate = DateTime.Now;

                    // Load the Bike and Renter entities
                    review.Bike = await _context.Bikes.FindAsync(id);
                    review.Renter = await _context.Users.FindAsync(userId);

                    if (review.Bike == null || review.Renter == null)
                    {
                        ModelState.AddModelError("", "Could not find the bike or user information.");
                        return View(review);
                    }

                    _context.Reviews.Add(review);
                    await _context.SaveChangesAsync();
                    System.Diagnostics.Debug.WriteLine("Review saved successfully");
                    
                    TempData["SuccessMessage"] = "Your review has been submitted successfully!";
                    return RedirectToAction("Details", "Bike", new { id });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error saving review: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                    ModelState.AddModelError("", "An error occurred while saving your review. Please try again.");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Model validation failed:");
                foreach (var modelError in ModelState.Values.SelectMany(v => v.Errors))
                {
                    System.Diagnostics.Debug.WriteLine($"Validation error: {modelError.ErrorMessage}");
                }
            }

            return View(review);
        }

        // GET: Review/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var review = await _context.Reviews.FindAsync(id);
            if (review == null)
            {
                return NotFound();
            }

            // Check if the current user owns this review
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            if (review.RenterID != userId)
            {
                return Forbid();
            }

            return View(review);
        }

        // POST: Review/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ReviewID,BikeID,RenterID,Rating,ReviewText,ReviewDate")] Review review)
        {
            if (id != review.ReviewID)
            {
                return NotFound();
            }

            // Check if the current user owns this review
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            if (review.RenterID != userId)
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(review);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ReviewExists(review.ReviewID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction("Details", "Bike", new { id = review.BikeID });
            }
            return View(review);
        }

        // GET: Review/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var review = await _context.Reviews
                .Include(r => r.Bike)
                .FirstOrDefaultAsync(r => r.ReviewID == id);

            if (review == null)
            {
                return NotFound();
            }

            // Check if the current user owns this review
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            if (review.RenterID != userId)
            {
                return Forbid();
            }

            return View(review);
        }

        // POST: Review/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review != null)
            {
                // Check if the current user owns this review
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                if (review.RenterID != userId)
                {
                    return Forbid();
                }

                _context.Reviews.Remove(review);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Details", "Bike", new { id = review.BikeID });
        }

        private bool ReviewExists(int id)
        {
            return _context.Reviews.Any(e => e.ReviewID == id);
        }
    }
}