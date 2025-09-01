using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimplyFly.Api.Data;
using SimplyFly.Api.Models;
using SimplyFly.Api.DTOs;
using System.Security.Claims;
using System.Text.Json;

namespace SimplyFly.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BookingsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public BookingsController(AppDbContext context)
        {
            _context = context;
        }

        // POST: api/bookings
        [HttpPost]
        public async Task<IActionResult> CreateBooking([FromBody] CreateBookingDto request)
        {
            try
            {
                // Add detailed logging
                Console.WriteLine($"Received booking request: {JsonSerializer.Serialize(request)}");
                
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                int userId = 1; // Default user ID for testing
                if (userIdClaim != null)
                {
                    userId = int.Parse(userIdClaim);
                }

                Console.WriteLine($"User ID: {userId}, Flight ID: {request.FlightId}");

                var flight = await _context.Flights.FindAsync(request.FlightId);
                if (flight == null) return NotFound("Flight not found.");

                // Check if any of the requested seats are already booked
                if (!string.IsNullOrEmpty(request.SelectedSeats))
                {
                    var requestedSeats = request.SelectedSeats.Split(',').Select(s => s.Trim()).ToList();
                    var bookedSeats = await GetBookedSeatsForFlight(request.FlightId);
                    var conflictingSeats = requestedSeats.Where(seat => bookedSeats.Contains(seat)).ToList();
                    
                    if (conflictingSeats.Any())
                    {
                        return BadRequest($"The following seats are already booked: {string.Join(", ", conflictingSeats)}");
                    }
                }

                var booking = new Booking
                {
                    UserId = userId,
                    FlightId = request.FlightId,
                    Flight = request.Flight ?? "SimplyFly Airlines",
                    Route = request.Route,
                    SelectedSeats = request.SelectedSeats,
                    Passengers = request.Passengers,
                    TotalAmount = request.TotalAmount,
                    TicketType = request.TicketType,
                    TicketBookingDate = DateTime.Today,
                    TicketBookingTime = DateTime.Now.TimeOfDay,
                    DepartureTime = request.DepartureTime,
                    ArrivalTime = request.ArrivalTime,
                    CreatedAt = DateTime.Now
                };

                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    bookingId = booking.BookingId,
                    message = "Booking created successfully",
                    seats = request.SelectedSeats,
                    totalAmount = request.TotalAmount
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error creating booking: {ex.Message}");
            }
        }

        // GET: api/bookings
        [HttpGet]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> GetMyBookings()
        {
            try
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userIdClaim == null) return Unauthorized("User ID not found.");
                var userId = int.Parse(userIdClaim);

                var bookings = await _context.Bookings
                    .Where(b => b.UserId == userId)
                    .Include(b => b.FlightDetails)
                    .Select(b => new
                    {
                        b.BookingId,
                        b.TicketBookingDate,
                        b.TicketBookingTime,
                        b.SelectedSeats,
                        b.Passengers,
                        b.TotalAmount,
                        b.TicketType,
                        b.Route,
                        b.DepartureTime,
                        b.ArrivalTime,
                        Flight = new
                        {
                            b.FlightDetails!.Id,
                            b.FlightDetails.Origin,
                            b.FlightDetails.Destination,
                            b.FlightDetails.Price
                        }
                    })
                    .ToListAsync();

                return Ok(bookings);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error fetching bookings: {ex.Message}");
            }
        }

        // GET: api/bookings/flight/{flightId}/seats
        [HttpGet("flight/{flightId}/seats")]
        public async Task<IActionResult> GetBookedSeats(int flightId)
        {
            try
            {
                var bookedSeats = await GetBookedSeatsForFlight(flightId);
                return Ok(bookedSeats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error fetching booked seats: {ex.Message}");
            }
        }

        // GET: api/bookings/all (Admin and FlightOwner only)
        [HttpGet("all")]
        [Authorize(Roles = "Admin,Flightowner")]
        public async Task<IActionResult> GetAllBookings()
        {
            try
            {
                var bookings = await _context.Bookings
                    .Include(b => b.FlightDetails)
                    .Include(b => b.User)
                    .Select(b => new
                    {
                        b.BookingId,
                        b.FlightId,
                        b.Flight, // Include the flight name from the booking
                        b.TicketBookingDate,
                        b.TicketBookingTime,
                        b.SelectedSeats,
                        b.Passengers,
                        b.TotalAmount,
                        b.TicketType,
                        b.Route,
                        b.DepartureTime,
                        b.ArrivalTime,
                        User = new
                        {
                            b.User!.Id,
                            b.User.Email,
                            b.User.Name
                        },
                        FlightDetails = new
                        {
                            b.FlightDetails!.Id,
                            b.FlightDetails.Origin,
                            b.FlightDetails.Destination,
                            b.FlightDetails.Price
                        }
                    })
                    .ToListAsync();

                return Ok(bookings);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error fetching all bookings: {ex.Message}");
            }
        }

        // PUT: api/bookings/{id}/request-cancel (Request cancellation)
        [HttpPut("{id}/request-cancel")]
        [Authorize] // User can request cancellation
        public async Task<IActionResult> RequestCancelBooking(int id)
        {
            try
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var userRole = User.FindFirstValue(ClaimTypes.Role);
                
                if (userIdClaim == null) return Unauthorized("User ID not found.");
                var userId = int.Parse(userIdClaim);

                var booking = await _context.Bookings.FindAsync(id);
                if (booking == null) return NotFound("Booking not found.");

                // Only allow user to cancel their own booking, or admin to cancel any booking
                if (booking.UserId != userId && userRole != "Admin")
                {
                    return Forbid("You can only cancel your own bookings.");
                }

                // Check if booking is already cancelled or requested for cancellation
                if (booking.Status == "Cancelled" || booking.Status == "RequestedToCancel")
                {
                    return BadRequest($"Booking is already {booking.Status.ToLower()}");
                }

                Console.WriteLine($"Requesting cancellation for booking {id}");

                // Update booking status to "RequestedToCancel" instead of deleting
                booking.Status = "RequestedToCancel";
                booking.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                return Ok(new { 
                    message = "Cancellation request submitted successfully", 
                    status = "RequestedToCancel",
                    bookingId = id,
                    note = "Flight owner will review your cancellation request and process the refund if approved."
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error cancelling booking: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, $"Error cancelling booking: {ex.Message}");
            }
        }

        // DELETE: api/bookings/{id} (Hard delete - Admin only)
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> HardDeleteBooking(int id)
        {
            Console.WriteLine($"=== CANCEL ENDPOINT HIT: BookingId={id} ===");
            
            try
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                Console.WriteLine($"UserIdClaim: {userIdClaim}");
                
                if (userIdClaim == null) return Unauthorized("User ID not found.");
                var userId = int.Parse(userIdClaim);

                Console.WriteLine($"Cancel request: BookingId={id}, UserId={userId}");

                // Use a transaction to ensure both deletions succeed or both fail
                using var transaction = await _context.Database.BeginTransactionAsync();
                
                try
                {
                    // Step 1: Delete passenger details for this booking (regardless of user)
                    var passengerDeleteResult = await _context.Database
                        .ExecuteSqlRawAsync("DELETE FROM PassengerDetails WHERE BookingId = {0}", id);
                    Console.WriteLine($"Deleted {passengerDeleteResult} passenger records for booking {id}");

                    // Step 2: Delete the booking (check user ownership here)
                    var bookingDeleteResult = await _context.Database
                        .ExecuteSqlRawAsync("DELETE FROM Bookings WHERE BookingId = {0} AND UserId = {1}", id, userId);
                    Console.WriteLine($"Deleted {bookingDeleteResult} booking records for booking {id} and user {userId}");

                    // Commit the transaction
                    await transaction.CommitAsync();

                    if (bookingDeleteResult > 0)
                    {
                        Console.WriteLine($"SUCCESS: Booking {id} cancelled successfully");
                        return Ok(new { 
                            message = "Booking cancelled successfully", 
                            refundInfo = "Refund will be processed within 2-3 working days",
                            bookingId = id
                        });
                    }
                    else
                    {
                        Console.WriteLine($"FAILED: No booking deleted for ID {id} and user {userId}");
                        return NotFound("Booking not found or you don't have permission to cancel it");
                    }
                }
                catch (Exception innerEx)
                {
                    await transaction.RollbackAsync();
                    Console.WriteLine($"Transaction failed: {innerEx.Message}");
                    return StatusCode(500, $"Database error: {innerEx.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in alternative cancel: {ex.Message}");
                return StatusCode(500, $"Error cancelling booking: {ex.Message}");
            }
        }

        // Simple test endpoint to verify routing
        [HttpGet("{id}/test")]
        public IActionResult TestEndpoint(int id)
        {
            Console.WriteLine($"=== TEST ENDPOINT HIT: ID={id} ===");
            return Ok(new { message = $"Test endpoint working for ID {id}" });
        }

        // GET: api/bookings/details/{id}
        [HttpGet("details/{id}")]
        [Authorize]
        public async Task<IActionResult> GetBookingDetails(int id)
        {
            Console.WriteLine($"=== DETAILS ENDPOINT HIT: BookingId={id} ===");
            
            try
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                Console.WriteLine($"UserIdClaim: {userIdClaim}");
                
                if (userIdClaim == null) 
                {
                    Console.WriteLine("UserIdClaim is null - returning Unauthorized");
                    return Unauthorized("User ID not found.");
                }
                
                var userId = int.Parse(userIdClaim);
                Console.WriteLine($"Getting details for booking {id}, user {userId}");

                // Get booking details with flight information
                var booking = await _context.Bookings
                    .Include(b => b.FlightDetails)
                    .Where(b => b.BookingId == id && b.UserId == userId)
                    .Select(b => new {
                        b.BookingId,
                        b.TicketBookingDate,
                        b.TicketBookingTime,
                        b.SelectedSeats,
                        b.Passengers,
                        b.TotalAmount,
                        b.TicketType,
                        b.Route,
                        b.DepartureTime,
                        b.ArrivalTime,
                        b.UserId,
                        b.FlightId,
                        b.Flight, // This is the airline name string
                        FlightDetails = b.FlightDetails != null ? new {
                            b.FlightDetails.Id,
                            b.FlightDetails.Origin,
                            b.FlightDetails.Destination,
                            b.FlightDetails.Price
                        } : null
                    })
                    .FirstOrDefaultAsync();

                Console.WriteLine($"Booking query result: {(booking == null ? "NULL" : "FOUND")}");

                if (booking == null)
                {
                    Console.WriteLine($"No booking found with ID {id} for user {userId}");
                    return NotFound("Booking not found or you don't have permission to view it.");
                }

                Console.WriteLine($"Found booking {booking.BookingId}, now getting passenger details");

                // Get passenger details for this booking
                var passengerDetails = await _context.PassengerDetails
                    .Where(p => p.BookingId == id)
                    .Select(p => new {
                        p.PassengerId,
                        p.FirstName,
                        p.LastName,
                        p.Age,
                        p.Gender,
                        p.SeatNo,
                        p.PassportNumber,
                        p.Nationality,
                        p.BookingId
                    })
                    .ToListAsync();

                Console.WriteLine($"Found {passengerDetails.Count} passenger details");

                var result = new {
                    Booking = booking,
                    Passengers = passengerDetails
                };

                Console.WriteLine($"Result object created: Booking={booking?.BookingId}, Passengers count={passengerDetails?.Count}");
                Console.WriteLine($"Final result JSON preview: BookingId={result.Booking?.BookingId}, PassengerCount={result.Passengers?.Count}");
                
                Console.WriteLine("Returning booking details successfully");
                return Ok(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting booking details: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, $"Error retrieving booking details: {ex.Message}");
            }
        }

        private async Task<List<string>> GetBookedSeatsForFlight(int flightId)
        {
            var bookings = await _context.Bookings
                .Where(b => b.FlightId == flightId)
                .Select(b => b.SelectedSeats)
                .ToListAsync();

            var allSeats = new List<string>();
            foreach (var seatString in bookings)
            {
                if (!string.IsNullOrEmpty(seatString))
                {
                    var seats = seatString.Split(',').Select(s => s.Trim()).ToList();
                    allSeats.AddRange(seats);
                }
            }

            return allSeats;
        }
    }
}
