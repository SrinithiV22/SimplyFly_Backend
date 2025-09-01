using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimplyFly.Api.Data;
using SimplyFly.Api.Models;

namespace SimplyFly.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FlightOwnerController : ControllerBase
    {
        private readonly AppDbContext _context;

        public FlightOwnerController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/FlightOwner/flight-details/{userId}
        [HttpGet("flight-details/{userId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetFlightDetailsByOwner(int userId)
        {
            try
            {
                // First, get the FlightOwner record for this user
                var flightOwner = await _context.FlightOwners
                    .FirstOrDefaultAsync(fo => fo.UserId == userId);

                if (flightOwner == null)
                {
                    return Ok(new List<object>()); // Return empty list if user is not a flight owner yet
                }

                // Get flight details owned by this flight owner
                var flightDetails = await _context.FlightDetails
                    .Include(fd => fd.Flight)
                    .Include(fd => fd.FlightOwner)
                    .Where(fd => fd.FlightOwnerId == flightOwner.FlightOwnerId)
                    .Select(fd => new
                    {
                        fd.FlightDetailId,
                        fd.FlightId,
                        fd.FlightOwnerId,
                        fd.FlightName,
                        fd.BaggageInfo,
                        fd.NumberOfSeats,
                        fd.DepartureTime,
                        fd.ArrivalTime,
                        fd.Fare,
                        fd.CreatedAt,
                        FlightRoute = $"{fd.Flight.Origin} → {fd.Flight.Destination}",
                        FlightPrice = fd.Flight.Price
                    })
                    .ToListAsync();

                return Ok(flightDetails);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving flight details", error = ex.Message });
            }
        }

        // POST: api/FlightOwner/flight-details
        [HttpPost("flight-details")]
        public async Task<ActionResult<FlightDetail>> CreateFlightDetail([FromBody] CreateFlightDetailDto dto)
        {
            try
            {
                // Validate the flight exists
                var flight = await _context.Flights.FindAsync(dto.FlightId);
                if (flight == null)
                {
                    return BadRequest(new { message = "Flight not found" });
                }

                // Get or create FlightOwner record
                var flightOwner = await _context.FlightOwners
                    .FirstOrDefaultAsync(fo => fo.UserId == dto.FlightOwnerId);

                if (flightOwner == null)
                {
                    // Create FlightOwner record if it doesn't exist
                    var user = await _context.Users.FindAsync(dto.FlightOwnerId);
                    if (user == null)
                    {
                        return BadRequest(new { message = "User not found" });
                    }

                    flightOwner = new FlightOwner
                    {
                        UserId = dto.FlightOwnerId,
                        AirlineName = user.Name + " Airlines", // Default airline name
                        CreatedAt = DateTime.Now
                    };

                    _context.FlightOwners.Add(flightOwner);
                    await _context.SaveChangesAsync();
                }

                // Create the flight detail
                var flightDetail = new FlightDetail
                {
                    FlightId = dto.FlightId,
                    FlightOwnerId = flightOwner.FlightOwnerId,
                    FlightName = dto.FlightName,
                    BaggageInfo = dto.BaggageInfo,
                    NumberOfSeats = dto.NumberOfSeats,
                    DepartureTime = dto.DepartureTime,
                    ArrivalTime = dto.ArrivalTime,
                    Fare = dto.Fare,
                    CreatedAt = DateTime.Now
                };

                _context.FlightDetails.Add(flightDetail);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetFlightDetailsByOwner), 
                    new { userId = dto.FlightOwnerId }, flightDetail);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error creating flight detail", error = ex.Message });
            }
        }

        // PUT: api/FlightOwner/flight-details/{id}
        [HttpPut("flight-details/{id}")]
        public async Task<IActionResult> UpdateFlightDetail(int id, [FromBody] UpdateFlightDetailDto dto)
        {
            try
            {
                var flightDetail = await _context.FlightDetails.FindAsync(id);
                if (flightDetail == null)
                {
                    return NotFound(new { message = "Flight detail not found" });
                }

                // Validate the flight exists if being updated
                if (dto.FlightId != flightDetail.FlightId)
                {
                    var flight = await _context.Flights.FindAsync(dto.FlightId);
                    if (flight == null)
                    {
                        return BadRequest(new { message = "Flight not found" });
                    }
                }

                // Update properties
                flightDetail.FlightId = dto.FlightId;
                flightDetail.FlightName = dto.FlightName;
                flightDetail.BaggageInfo = dto.BaggageInfo;
                flightDetail.NumberOfSeats = dto.NumberOfSeats;
                flightDetail.DepartureTime = dto.DepartureTime;
                flightDetail.ArrivalTime = dto.ArrivalTime;
                flightDetail.Fare = dto.Fare;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Flight detail updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error updating flight detail", error = ex.Message });
            }
        }

        // DELETE: api/FlightOwner/flight-details/{id}
        [HttpDelete("flight-details/{id}")]
        public async Task<IActionResult> DeleteFlightDetail(int id)
        {
            try
            {
                var flightDetail = await _context.FlightDetails.FindAsync(id);
                if (flightDetail == null)
                {
                    return NotFound(new { message = "Flight detail not found" });
                }

                _context.FlightDetails.Remove(flightDetail);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Flight detail deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error deleting flight detail", error = ex.Message });
            }
        }

        // PUT: api/FlightOwner/bookings/{bookingId}/approve-refund
        [HttpPut("bookings/{bookingId}/approve-refund")]
        [Authorize(Roles = "Flightowner")]
        public async Task<IActionResult> ApproveRefund(int bookingId)
        {
            try
            {
                var booking = await _context.Bookings.FindAsync(bookingId);
                if (booking == null)
                {
                    return NotFound(new { message = "Booking not found" });
                }

                // Check if booking is in RequestedToCancel status
                if (booking.Status != "RequestedToCancel")
                {
                    return BadRequest(new { message = "Booking is not in cancellation request status" });
                }

                // Update status to Refunded
                booking.Status = "Refunded";
                booking.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                return Ok(new { 
                    message = "Refund approved successfully", 
                    bookingId = bookingId,
                    status = "Refunded",
                    refundAmount = booking.TotalAmount
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error approving refund", error = ex.Message });
            }
        }

        // PUT: api/FlightOwner/bookings/{bookingId}/reject-refund
        [HttpPut("bookings/{bookingId}/reject-refund")]
        [Authorize(Roles = "Flightowner")]
        public async Task<IActionResult> RejectRefund(int bookingId)
        {
            try
            {
                var booking = await _context.Bookings.FindAsync(bookingId);
                if (booking == null)
                {
                    return NotFound(new { message = "Booking not found" });
                }

                // Check if booking is in RequestedToCancel status
                if (booking.Status != "RequestedToCancel")
                {
                    return BadRequest(new { message = "Booking is not in cancellation request status" });
                }

                // Update status back to Confirmed
                booking.Status = "Confirmed";
                booking.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                return Ok(new { 
                    message = "Refund request rejected", 
                    bookingId = bookingId,
                    status = "Confirmed"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error rejecting refund", error = ex.Message });
            }
        }

        // GET: api/FlightOwner/bookings/{userId}
        [HttpGet("bookings/{userId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetBookingsByFlightOwner(int userId)
        {
            try
            {
                // Get the FlightOwner record for this user
                var flightOwner = await _context.FlightOwners
                    .FirstOrDefaultAsync(fo => fo.UserId == userId);

                if (flightOwner == null)
                {
                    return Ok(new List<object>()); // Return empty list if user is not a flight owner
                }

                // Get bookings for flights owned by this flight owner
                var bookings = await _context.Bookings
                    .Include(b => b.User)
                    .Where(b => _context.FlightDetails
                        .Any(fd => fd.FlightOwnerId == flightOwner.FlightOwnerId && fd.FlightId == b.FlightId))
                    .Select(b => new
                    {
                        b.BookingId,
                        b.UserId,
                        UserName = b.User != null ? b.User.Name : "Unknown",
                        UserEmail = b.User != null ? b.User.Email : "Unknown",
                        b.FlightId,
                        FlightName = _context.FlightDetails
                            .Where(fd => fd.FlightId == b.FlightId && fd.FlightOwnerId == flightOwner.FlightOwnerId)
                            .Select(fd => fd.FlightName)
                            .FirstOrDefault(),
                        BookingDate = b.TicketBookingDate,
                        PassengerCount = b.Passengers,
                        b.TotalAmount,
                        b.Route,
                        b.SelectedSeats,
                        b.TicketType,
                        Status = "Confirmed" // Default status since we don't have status column yet
                    })
                    .ToListAsync();

                return Ok(bookings);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving bookings", error = ex.Message });
            }
        }
    }

    // DTOs for the API
    public class CreateFlightDetailDto
    {
        public int FlightId { get; set; }
        public int FlightOwnerId { get; set; }
        public string FlightName { get; set; } = string.Empty;
        public string? BaggageInfo { get; set; }
        public int NumberOfSeats { get; set; }
        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }
        public decimal Fare { get; set; }
    }

    public class UpdateFlightDetailDto
    {
        public int FlightId { get; set; }
        public string FlightName { get; set; } = string.Empty;
        public string? BaggageInfo { get; set; }
        public int NumberOfSeats { get; set; }
        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }
        public decimal Fare { get; set; }
    }
}
