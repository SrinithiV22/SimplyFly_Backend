using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimplyFly.Api.Data;
using SimplyFly.Api.Models;
using System.Security.Claims;

namespace SimplyFly.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReviewsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ReviewsController(AppDbContext context)
        {
            _context = context;
        }

        // POST: api/reviews
        [HttpPost]
        [Authorize(Roles = "User")]
        public IActionResult AddReview([FromBody] Review review)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            // Optional: only allow reviews if user booked the flight
            var booked = _context.Bookings.Any(b => b.UserId == userId && b.FlightId == review.FlightId);
            if (!booked) return BadRequest("You can only review flights you have booked.");

            review.UserId = userId;
            review.SubmittedAt = DateTime.UtcNow;

            _context.Reviews.Add(review);
            _context.SaveChanges();

            return Ok(review);
        }

        // GET: api/reviews/flight/5
        [HttpGet("flight/{flightId}")]
        public IActionResult GetReviewsForFlight(int flightId)
        {
            var reviews = _context.Reviews
                .Where(r => r.FlightId == flightId)
                .Include(r => r.User)
                .Select(r => new
                {
                    r.Id,
                    r.Rating,
                    r.Comment,
                    r.SubmittedAt,
                    Reviewer = r.User!.Name
                })
                .ToList();

            return Ok(reviews);
        }

        // DELETE: api/reviews/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public IActionResult DeleteReview(int id)
        {
            var review = _context.Reviews.Find(id);
            if (review == null) return NotFound();

            _context.Reviews.Remove(review);
            _context.SaveChanges();

            return Ok("Review deleted.");
        }
    }
}
