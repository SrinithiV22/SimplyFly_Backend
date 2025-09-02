using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimplyFly.Api.Data;
using SimplyFly.Api.Models;

namespace SimplyFly.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        // ============ FLIGHT MANAGEMENT ============

        // GET: api/admin/flights
        [HttpGet("flights")]
        public async Task<IActionResult> GetAllFlights()
        {
            try
            {
                var flights = await _context.Flights.ToListAsync();
                return Ok(flights);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Get flights error: {ex.Message}");
                return StatusCode(500, new { message = "Error retrieving flights", error = ex.Message });
            }
        }

        // POST: api/admin/flight
        [HttpPost("flight")]
        [Authorize(Roles = "Admin,Flightowner")]
        public async Task<IActionResult> CreateFlight([FromBody] Flight flight)
        {
            try
            {
                if (flight == null)
                {
                    return BadRequest(new { message = "Flight data is required" });
                }

                if (string.IsNullOrEmpty(flight.Origin) || string.IsNullOrEmpty(flight.Destination))
                {
                    return BadRequest(new { message = "Origin and Destination are required" });
                }

                if (flight.Price <= 0)
                {
                    return BadRequest(new { message = "Price must be greater than 0" });
                }

                _context.Flights.Add(flight);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetFlightById), new { id = flight.Id }, flight);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Create flight error: {ex.Message}");
                return StatusCode(500, new { message = "Error creating flight", error = ex.Message });
            }
        }

        // GET: api/admin/flight/{id}
        [HttpGet("flight/{id}")]
        public async Task<IActionResult> GetFlightById(int id)
        {
            try
            {
                var flight = await _context.Flights.FindAsync(id);
                if (flight == null)
                {
                    return NotFound(new { message = "Flight not found" });
                }

                return Ok(flight);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Get flight error: {ex.Message}");
                return StatusCode(500, new { message = "Error retrieving flight", error = ex.Message });
            }
        }

        // PUT: api/admin/flight/{id}
        [HttpPut("flight/{id}")]
        [Authorize(Roles = "Admin,Flightowner")]
        public async Task<IActionResult> UpdateFlight(int id, [FromBody] Flight flight)
        {
            try
            {
                if (id != flight.Id)
                {
                    return BadRequest(new { message = "Flight ID mismatch" });
                }

                var existingFlight = await _context.Flights.FindAsync(id);
                if (existingFlight == null)
                {
                    return NotFound(new { message = "Flight not found" });
                }

                if (string.IsNullOrEmpty(flight.Origin) || string.IsNullOrEmpty(flight.Destination))
                {
                    return BadRequest(new { message = "Origin and Destination are required" });
                }

                if (flight.Price <= 0)
                {
                    return BadRequest(new { message = "Price must be greater than 0" });
                }

                existingFlight.Origin = flight.Origin;
                existingFlight.Destination = flight.Destination;
                existingFlight.Price = flight.Price;

                await _context.SaveChangesAsync();

                return Ok(existingFlight);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Update flight error: {ex.Message}");
                return StatusCode(500, new { message = "Error updating flight", error = ex.Message });
            }
        }

        // DELETE: api/admin/flight/{id}
        [HttpDelete("flight/{id}")]
        [Authorize(Roles = "Admin,Flightowner")]
        public async Task<IActionResult> DeleteFlight(int id)
        {
            try
            {
                var flight = await _context.Flights.FindAsync(id);
                if (flight == null)
                {
                    return NotFound(new { message = "Flight not found" });
                }

                _context.Bookings.RemoveRange(_context.Bookings.Where(b => b.FlightId == id));
                _context.Flights.Remove(flight);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Flight deleted successfully" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Delete flight error: {ex.Message}");
                return StatusCode(500, new { message = "Error deleting flight", error = ex.Message });
            }
        }

        // ============ USER MANAGEMENT ============

        // GET: api/admin/users
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            try
            {
                var users = await _context.Users
                    .Select(u => new { u.Id, u.Name, u.Email, u.Role })
                    .ToListAsync();
                return Ok(users);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Get users error: {ex.Message}");
                return StatusCode(500, new { message = "Error retrieving users", error = ex.Message });
            }
        }

        // PUT: api/admin/user/{id}/role
        [HttpPut("user/{id}/role")]
        public async Task<IActionResult> UpdateUserRole(int id, [FromBody] UpdateRoleRequest request)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                if (string.IsNullOrEmpty(request.Role) || 
                    !new[] { "User", "Admin", "Flightowner" }.Contains(request.Role))
                {
                    return BadRequest(new { message = "Invalid role. Valid roles are: User, Admin, Flightowner" });
                }

                user.Role = request.Role;
                await _context.SaveChangesAsync();

                return Ok(new { message = "User role updated successfully", user = new { user.Id, user.Name, user.Email, user.Role } });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Update user role error: {ex.Message}");
                return StatusCode(500, new { message = "Error updating user role", error = ex.Message });
            }
        }

        // ============ BOOKING MANAGEMENT ============

        // GET: api/admin/bookings
        [HttpGet("bookings")]
        [AllowAnonymous] // Temporarily allow without auth for debugging
        public async Task<IActionResult> GetAllBookings()
        {
            try
            {
                Console.WriteLine("🔵 Admin GetAllBookings endpoint hit");
                
                // Get all bookings first
                var bookings = await _context.Bookings.ToListAsync();
                Console.WriteLine($"� Found {bookings.Count} bookings in database");

                if (bookings.Count == 0)
                {
                    Console.WriteLine("⚠️ No bookings found in database");
                    return Ok(new List<object>());
                }

                // Get all users and flights for reference
                var users = await _context.Users.ToListAsync();
                var flights = await _context.Flights.ToListAsync();
                
                Console.WriteLine($"� Found {users.Count} users");
                Console.WriteLine($"✈️ Found {flights.Count} flights");

                var result = bookings.Select(b => {
                    var user = users.FirstOrDefault(u => u.Id == b.UserId);
                    var flight = flights.FirstOrDefault(f => f.Id == b.FlightId);
                    
                    return new
                    {
                        bookingId = b.BookingId,
                        userId = b.UserId,
                        userName = user?.Name ?? "Unknown",
                        userEmail = user?.Email ?? "Unknown",
                        flightId = b.FlightId,
                        flightOrigin = flight?.Origin ?? "Unknown",
                        flightDestination = flight?.Destination ?? "Unknown",
                        flightPrice = flight?.Price ?? 0,
                        route = b.Route,
                        flight = b.Flight,
                        seatNumbers = b.SelectedSeats,
                        selectedSeats = b.SelectedSeats,
                        passengerCount = b.Passengers,
                        passengers = b.Passengers,
                        totalAmount = b.TotalAmount,
                        ticketType = b.TicketType,
                        departureTime = b.DepartureTime,
                        arrivalTime = b.ArrivalTime,
                        bookingDate = b.TicketBookingDate,
                        ticketBookingDate = b.TicketBookingDate,
                        ticketBookingTime = b.TicketBookingTime.ToString(@"hh\:mm"),
                        status = b.Status// Default status
                    };
                }).ToList();

                Console.WriteLine($"✅ Returning {result.Count} booking records");
                Console.WriteLine($"🔍 First booking: {System.Text.Json.JsonSerializer.Serialize(result.FirstOrDefault())}");
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Get bookings error: {ex.Message}");
                Console.WriteLine($"🔍 Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { message = "Error retrieving bookings", error = ex.Message });
            }
        }

        // PUT: api/admin/booking/{id}/cancel - Soft delete (update status)
        [HttpPut("booking/{id}/cancel")]
        public async Task<IActionResult> CancelBooking(int id)
        {
            try
            {
                var booking = await _context.Bookings.FindAsync(id);
                if (booking == null)
                {
                    return NotFound(new { message = "Booking not found" });
                }

                booking.Status = "Cancelled";
                booking.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Booking cancelled successfully", bookingId = id, status = "Cancelled" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Cancel booking error: {ex.Message}");
                return StatusCode(500, new { message = "Error cancelling booking", error = ex.Message });
            }
        }

        // DELETE: api/admin/booking/{id} - Hard delete (admin only)
        [HttpDelete("booking/{id}")]
        public async Task<IActionResult> DeleteBooking(int id)
        {
            try
            {
                var booking = await _context.Bookings.FindAsync(id);
                if (booking == null)
                {
                    return NotFound(new { message = "Booking not found" });
                }

                _context.Bookings.Remove(booking);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Booking permanently deleted" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Delete booking error: {ex.Message}");
                return StatusCode(500, new { message = "Error deleting booking", error = ex.Message });
            }
        }

        // PUT: api/admin/bookings/{id}/status
        [HttpPut("bookings/{id}/status")]
        [AllowAnonymous]
        public async Task<IActionResult> UpdateBookingStatus(int id, [FromBody] UpdateBookingStatusRequest request)
        {
            try
            {
                Console.WriteLine($"🔵 UpdateBookingStatus called for booking {id} with status {request.Status}");
                
                var booking = await _context.Bookings.FindAsync(id);
                if (booking == null)
                {
                    return NotFound(new { message = "Booking not found" });
                }

                // Validate status
                var validStatuses = new[] { "Confirmed", "Cancelled", "Pending", "Refunded" };
                if (!validStatuses.Contains(request.Status))
                {
                    return BadRequest(new { message = "Invalid status. Valid statuses are: " + string.Join(", ", validStatuses) });
                }

                booking.Status = request.Status;
                booking.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
                
                Console.WriteLine($"✅ Booking {id} status updated to {request.Status}");
                return Ok(new { message = "Booking status updated successfully", bookingId = id, status = request.Status });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error updating booking status: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }


        // GET: api/admin/bookings/{id}
        [HttpGet("bookings/{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetBookingDetails(int id)
        {
            try
            {
                Console.WriteLine($"🔵 GetBookingDetails called for booking {id}");
                
                var booking = await _context.Bookings.FindAsync(id);
                if (booking == null)
                {
                    return NotFound(new { message = "Booking not found" });
                }

                var user = await _context.Users.FindAsync(booking.UserId);
                var flight = await _context.Flights.FindAsync(booking.FlightId);
                var passengerDetails = await _context.PassengerDetails
                    .Where(p => p.BookingId == booking.BookingId)
                    .ToListAsync();

                var bookingDetail = new
                {
                    bookingId = booking.BookingId,
                    userId = booking.UserId,
                    flightId = booking.FlightId,
                    route = booking.Route,
                    flight = booking.Flight,
                    seatNumbers = booking.SelectedSeats,
                    totalAmount = booking.TotalAmount,
                    passengerCount = booking.Passengers,
                    ticketType = booking.TicketType,
                    departureTime = booking.DepartureTime,
                    arrivalTime = booking.ArrivalTime,
                    bookingDate = booking.TicketBookingDate,
                    ticketBookingTime = booking.TicketBookingTime.ToString(@"hh\:mm"),
                    status = booking.Status ?? "Confirmed",
                    
                    // User information
                    userName = user?.Name ?? "Unknown User",
                    userEmail = user?.Email ?? "N/A",
                    
                    // Flight information
                    flightOrigin = flight?.Origin ?? "N/A",
                    flightDestination = flight?.Destination ?? "N/A",
                    flightPrice = flight?.Price ?? 0,
                    
                    // Passenger details
                    passengers = passengerDetails.Select(p => new
                    {
                        firstName = p.FirstName,
                        lastName = p.LastName,
                        age = p.Age,
                        gender = p.Gender,
                        nationality = p.Nationality,
                        passportNumber = p.PassportNumber,
                        seatNo = p.SeatNo
                    }).ToList()
                };

                Console.WriteLine($"✅ Returning booking details for booking {id}");
                return Ok(bookingDetail);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error getting booking details: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }
    }

    public class UpdateRoleRequest
    {
        public string Role { get; set; } = string.Empty;
    }

    public class UpdateBookingStatusRequest
    {
        public string Status { get; set; } = string.Empty;
    }
}