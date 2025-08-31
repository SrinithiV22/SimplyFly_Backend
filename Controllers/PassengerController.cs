using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimplyFly.Api.Data;
using SimplyFly.Api.Models;
using SimplyFly.Api.DTOs;
using System.Security.Claims;

namespace SimplyFly.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PassengerController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PassengerController(AppDbContext context)
        {
            _context = context;
        }

        // POST: api/passenger/details
        [HttpPost("details")]
        [Authorize]
        public async Task<IActionResult> SavePassengerDetails([FromBody] CreatePassengerDetailsDto request)
        {
            try
            {
                Console.WriteLine($"Received passenger details request: {System.Text.Json.JsonSerializer.Serialize(request)}");
                
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userIdClaim == null) 
                {
                    Console.WriteLine("User ID claim not found");
                    return Unauthorized("User ID not found.");
                }
                var userId = int.Parse(userIdClaim);
                Console.WriteLine($"User ID: {userId}");

                // For now, just verify the BookingId is provided - we'll validate it exists later
                if (request.BookingId <= 0)
                {
                    Console.WriteLine("Invalid BookingId provided");
                    return BadRequest("Invalid BookingId.");
                }

                // Validate passenger data
                if (request.Passengers == null || !request.Passengers.Any())
                {
                    Console.WriteLine("No passenger data provided");
                    return BadRequest("No passenger data provided.");
                }

                Console.WriteLine($"Processing {request.Passengers.Count} passengers");

                // Validate each passenger
                for (int i = 0; i < request.Passengers.Count; i++)
                {
                    var p = request.Passengers[i];
                    if (string.IsNullOrEmpty(p.FirstName))
                    {
                        return BadRequest($"Passenger {i + 1}: FirstName is required");
                    }
                    if (string.IsNullOrEmpty(p.LastName))
                    {
                        return BadRequest($"Passenger {i + 1}: LastName is required");
                    }
                    if (p.Age <= 0 || p.Age > 120)
                    {
                        return BadRequest($"Passenger {i + 1}: Invalid age");
                    }
                    if (string.IsNullOrEmpty(p.Gender))
                    {
                        return BadRequest($"Passenger {i + 1}: Gender is required");
                    }
                    if (string.IsNullOrEmpty(p.Nationality))
                    {
                        return BadRequest($"Passenger {i + 1}: Nationality is required");
                    }
                }

                // Save passenger details (with error handling for table structure issues)
                List<PassengerDetail> passengerDetails;
                
                try
                {
                    passengerDetails = request.Passengers.Select(p => new PassengerDetail
                    {
                        UserId = userId,
                        BookingId = request.BookingId > 0 ? request.BookingId : 1, // Use provided BookingId or default to 1
                        SeatNo = p.SeatNo ?? "1A", // Provide default if null
                        FirstName = p.FirstName,
                        LastName = p.LastName,
                        Age = p.Age,
                        Gender = p.Gender,
                        PassportNumber = p.PassportNumber ?? "", // Provide default if null
                        Nationality = p.Nationality,
                        CreatedAt = DateTime.Now
                    }).ToList();

                    Console.WriteLine($"Created {passengerDetails.Count} passenger detail objects");
                    
                    _context.PassengerDetails.AddRange(passengerDetails);
                    Console.WriteLine("Added passenger details to context");
                    
                    await _context.SaveChangesAsync();
                    Console.WriteLine("Saved changes to database successfully");
                }
                catch (Exception dbEx)
                {
                    Console.WriteLine($"Database save error: {dbEx.Message}");
                    
                    // If it's a column issue, try to provide a helpful error
                    if (dbEx.Message.Contains("Invalid column name"))
                    {
                        return StatusCode(500, new { 
                            message = "Database table structure mismatch", 
                            error = dbEx.Message,
                            suggestion = "The PassengerDetails table needs to be updated with missing columns"
                        });
                    }
                    
                    throw; // Re-throw for general error handling
                }

                return Ok(new
                {
                    message = "Passenger details saved successfully",
                    bookingId = request.BookingId, // Keep this for frontend compatibility
                    passengerCount = passengerDetails.Count
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving passenger details: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                    Console.WriteLine($"Inner stack trace: {ex.InnerException.StackTrace}");
                }
                
                // Return more detailed error information
                return StatusCode(500, new { 
                    message = "Error saving passenger details", 
                    error = ex.Message,
                    innerError = ex.InnerException?.Message,
                    details = "Check server logs for full stack trace"
                });
            }
        }

        // GET: api/passenger/booking/{bookingId}
        [HttpGet("booking/{bookingId}")]
        [Authorize]
        public async Task<IActionResult> GetPassengersByBooking(int bookingId)
        {
            try
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userIdClaim == null) return Unauthorized("User ID not found.");
                var userId = int.Parse(userIdClaim);

                // Verify the booking belongs to this user
                var booking = await _context.Bookings.FindAsync(bookingId);
                if (booking == null)
                {
                    return NotFound("Booking not found.");
                }

                if (booking.UserId != userId)
                {
                    return Forbid("You can only view passengers for your own bookings.");
                }

                var passengers = await _context.PassengerDetails
                    .Where(p => p.BookingId == bookingId && p.UserId == userId)
                    .OrderBy(p => p.CreatedAt)
                    .Select(p => new
                    {
                        p.PassengerId,
                        p.SeatNo,
                        p.FirstName,
                        p.LastName,
                        p.Age,
                        p.Gender,
                        p.PassportNumber,
                        p.Nationality
                    })
                    .ToListAsync();

                return Ok(passengers);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching passengers: {ex.Message}");
                return StatusCode(500, new { message = "Error fetching passengers", error = ex.Message });
            }
        }

        // DELETE: api/passenger/booking/{bookingId}
        [HttpDelete("booking/{bookingId}")]
        [Authorize]
        public async Task<IActionResult> DeletePassengersByBooking(int bookingId)
        {
            try
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userIdClaim == null) return Unauthorized("User ID not found.");
                var userId = int.Parse(userIdClaim);

                // Verify the booking belongs to this user
                var booking = await _context.Bookings.FindAsync(bookingId);
                if (booking == null)
                {
                    return NotFound("Booking not found.");
                }

                if (booking.UserId != userId)
                {
                    return Forbid("You can only delete passengers from your own bookings.");
                }

                // Find and delete all passenger details for this booking/user
                var passengers = await _context.PassengerDetails
                    .Where(p => p.BookingId == bookingId && p.UserId == userId)
                    .ToListAsync();

                if (passengers.Any())
                {
                    _context.PassengerDetails.RemoveRange(passengers);
                    await _context.SaveChangesAsync();
                }

                return Ok(new { message = "Passenger details deleted successfully", deletedCount = passengers.Count });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting passengers: {ex.Message}");
                return StatusCode(500, new { message = "Error deleting passengers", error = ex.Message });
            }
        }

        // GET: api/passenger/test
        [HttpGet("test")]
        public async Task<IActionResult> TestDatabase()
        {
            try
            {
                // Test if we can query the PassengerDetails table
                var count = await _context.PassengerDetails.CountAsync();
                return Ok(new { message = "Database connection working", passengerCount = count });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database test error: {ex.Message}");
                return StatusCode(500, new { message = "Database test failed", error = ex.Message });
            }
        }
    }
}
