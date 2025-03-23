using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BikeSharingApp.Data;
using BikeSharingApp.Models;
using System;
using System.Threading.Tasks;

namespace BikeSharingApp.Controllers
{
    public class MessageController : Controller
    {
        private readonly BikeSharingDbContext _context;

        public MessageController(BikeSharingDbContext context)
        {
            _context = context;
        }

        // GET: Message
        public async Task<IActionResult> Index()
        {
            return View(await _context.Messages.ToListAsync());
        }

        // GET: Message/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Message/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("GuestName,GuestEmail,MessageText")] Message message)
        {
            if (ModelState.IsValid)
            {
                message.MessageDate = DateTime.Now;
                _context.Add(message);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(message);
        }

        public IActionResult Edit(int? id)
        {
            if (id == null) return NotFound();
            var message = _context.Messages.Find(id);
            if (message == null) return NotFound();
            return View(message);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, [Bind("MessageID,GuestName,GuestEmail,MessageText,MessageDate")] Message message)
        {
            if (id != message.MessageID) return NotFound();
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(message);
                    _context.SaveChanges();
                }
                catch (DbUpdateConcurrencyException) { if (!MessageExists(message.MessageID)) return NotFound(); else throw; }
                return RedirectToAction(nameof(Index));
            }
            return View(message);
        }

        // GET: Message/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var message = await _context.Messages
                .FirstOrDefaultAsync(m => m.MessageID == id);
            if (message == null)
            {
                return NotFound();
            }
            return View(message);
        }

        // POST: Message/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var message = await _context.Messages.FindAsync(id);
            if (message != null)
            {
                _context.Messages.Remove(message);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool MessageExists(int id) { return _context.Messages.Any(e => e.MessageID == id); }
    }
}