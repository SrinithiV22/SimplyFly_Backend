using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimplyFly.Api.Data;
using SimplyFly.Api.Models;

namespace SimplyFly.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FlightsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public FlightsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/flights
        [HttpGet]
        [AllowAnonymous] // Allow anonymous access for testing
        public async Task<IActionResult> GetFlights()
        {
            try
            {
                Console.WriteLine("📍 GetFlights endpoint called");
                var flights = await _context.Flights
                    .OrderBy(f => f.Id)
                    .ToListAsync();

                Console.WriteLine($"📍 Found {flights.Count} flights");
                return Ok(flights);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Get flights error: {ex.Message}");
                Console.WriteLine($"❌ Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { message = "Error retrieving flights", error = ex.Message });
            }
        }

        // GET: api/flights/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetFlightById(int id)
        {
            try
            {
                var flight = await _context.Flights
                    .FirstOrDefaultAsync(f => f.Id == id);

                if (flight == null)
                {
                    return NotFound(new { message = "Flight not found" });
                }

                return Ok(flight);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Get flight by ID error: {ex.Message}");
                return StatusCode(500, new { message = "Error retrieving flight", error = ex.Message });
            }
        }

        // GET: api/flights/search
        [HttpGet("search")]
        [AllowAnonymous]
        public async Task<IActionResult> SearchFlights(string? origin, string? destination, string? sortBy = "price")
        {
            try
            {
                var query = _context.Flights.AsQueryable();

                if (!string.IsNullOrWhiteSpace(origin))
                    query = query.Where(f => f.Origin.ToLower().Contains(origin.ToLower()));

                if (!string.IsNullOrWhiteSpace(destination))
                    query = query.Where(f => f.Destination.ToLower().Contains(destination.ToLower()));

                query = sortBy?.ToLower() switch
                {
                    "destination" => query.OrderBy(f => f.Destination),
                    "origin" => query.OrderBy(f => f.Origin),
                    "id" => query.OrderBy(f => f.Id),
                    _ => query.OrderBy(f => f.Price),
                };

                var flights = await query.ToListAsync();
                return Ok(flights);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Search flights error: {ex.Message}");
                return StatusCode(500, new { message = "Error searching flights", error = ex.Message });
            }
        }

        // POST: api/flights
        [HttpPost]
        [Authorize] // Require authentication for adding flights
        public async Task<IActionResult> CreateFlight([FromBody] Flight newFlight)
        {
            try
            {
                Console.WriteLine($"✈️ CreateFlight called");
                Console.WriteLine($"📝 Flight data: {System.Text.Json.JsonSerializer.Serialize(newFlight)}");

                // Validate required fields
                if (string.IsNullOrWhiteSpace(newFlight.Origin) || 
                    string.IsNullOrWhiteSpace(newFlight.Destination) || 
                    newFlight.Price <= 0)
                {
                    Console.WriteLine($"❌ Validation failed - missing required fields");
                    return BadRequest(new { message = "Origin, Destination, and Price are required fields" });
                }

                // Trim whitespace
                newFlight.Origin = newFlight.Origin.Trim();
                newFlight.Destination = newFlight.Destination.Trim();

                // Add the flight to the database
                _context.Flights.Add(newFlight);
                await _context.SaveChangesAsync();
                Console.WriteLine($"✅ Flight created successfully with ID: {newFlight.Id}");

                return CreatedAtAction(nameof(GetFlightById), new { id = newFlight.Id }, newFlight);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Create flight error: {ex.Message}");
                Console.WriteLine($"❌ Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { message = "Error creating flight", error = ex.Message });
            }
        }

        // PUT: api/flights/{id}
        [HttpPut("{id}")]
        [Authorize] // Require authentication for editing
        public async Task<IActionResult> UpdateFlight(int id, [FromBody] Flight updatedFlight)
        {
            try
            {
                Console.WriteLine($"🔄 UpdateFlight called for ID: {id}");
                Console.WriteLine($"📝 Update data: {System.Text.Json.JsonSerializer.Serialize(updatedFlight)}");

                var existingFlight = await _context.Flights.FindAsync(id);
                if (existingFlight == null)
                {
                    Console.WriteLine($"❌ Flight with ID {id} not found");
                    return NotFound(new { message = "Flight not found" });
                }

                // Update only the fields that should be editable
                existingFlight.Origin = updatedFlight.Origin?.Trim() ?? existingFlight.Origin;
                existingFlight.Destination = updatedFlight.Destination?.Trim() ?? existingFlight.Destination;
                existingFlight.Price = updatedFlight.Price;

                // Validate required fields
                if (string.IsNullOrWhiteSpace(existingFlight.Origin) || 
                    string.IsNullOrWhiteSpace(existingFlight.Destination) || 
                    existingFlight.Price <= 0)
                {
                    return BadRequest(new { message = "Origin, Destination, and Price are required fields" });
                }

                await _context.SaveChangesAsync();
                Console.WriteLine($"✅ Flight {id} updated successfully");

                return Ok(new { message = "Flight updated successfully", flight = existingFlight });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Update flight error: {ex.Message}");
                Console.WriteLine($"❌ Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { message = "Error updating flight", error = ex.Message });
            }
        }

        // DELETE: api/flights/{id}
        [HttpDelete("{id}")]
        [Authorize] // Require authentication for deleting
        public async Task<IActionResult> DeleteFlight(int id)
        {
            try
            {
                Console.WriteLine($"🗑️ DeleteFlight called for ID: {id}");

                var flight = await _context.Flights.FindAsync(id);
                if (flight == null)
                {
                    Console.WriteLine($"❌ Flight with ID {id} not found");
                    return NotFound(new { message = "Flight not found" });
                }

                // Check if there are any bookings for this flight
                var hasBookings = await _context.Bookings.AnyAsync(b => b.FlightId == id);
                if (hasBookings)
                {
                    Console.WriteLine($"❌ Cannot delete flight {id} - has existing bookings");
                    return BadRequest(new { message = "Cannot delete flight with existing bookings" });
                }

                _context.Flights.Remove(flight);
                await _context.SaveChangesAsync();
                Console.WriteLine($"✅ Flight {id} deleted successfully");

                return Ok(new { message = "Flight deleted successfully" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Delete flight error: {ex.Message}");
                Console.WriteLine($"❌ Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { message = "Error deleting flight", error = ex.Message });
            }
        }

        // GET: api/flights/names
        [HttpGet("names")]
        [Authorize] // Allow any authenticated user for now
        public async Task<IActionResult> GetFlightNames()
        {
            try
            {
                Console.WriteLine("📍 GetFlightNames endpoint called");
                
                // Get all flights first
                var allFlights = await _context.Flights.Select(f => f.Id).ToListAsync();
                
                // Get flight names from bookings
                var flightNamesFromBookings = await _context.Bookings
                    .Where(b => !string.IsNullOrEmpty(b.Flight))
                    .GroupBy(b => b.FlightId)
                    .Select(g => new 
                    {
                        FlightId = g.Key,
                        FlightName = g.First().Flight
                    })
                    .ToListAsync();

                // Create a complete list with default names for flights without bookings
                var flightNames = allFlights.Select(flightId => 
                {
                    var existingName = flightNamesFromBookings.FirstOrDefault(fn => fn.FlightId == flightId);
                    return new 
                    {
                        FlightId = flightId,
                        FlightName = existingName?.FlightName ?? "SimplyFly Airlines" // Default airline name
                    };
                }).ToList();

                Console.WriteLine($"📍 Found {flightNames.Count} flight names (including defaults)");
                return Ok(flightNames);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Get flight names error: {ex.Message}");
                Console.WriteLine($"❌ Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { message = "Error retrieving flight names", error = ex.Message });
            }
        }
    }
}
