using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Data;
using Models;
using Validators;
using App_Code;

namespace csharp_geocrud_docker_api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GeoController : ControllerBase
{
    private readonly GeoContext _context;
    private readonly GeoValidator _validator;
    private readonly GeoHelper _helper;
    private readonly ILogger<GeoController> _logger;
    public GeoController(GeoContext context, GeoValidator validator, ILogger<GeoController> logger, ILogger<GeoHelper> helperLogger)
    {
        _context = context;
        _validator = validator;
        _helper = new GeoHelper(context, validator, helperLogger);
        _logger = logger;
    }

    /***********************************************
     ***********************************************
     ****************** GET ************************
     ***********************************************
     ***********************************************/

    // GET: api/geo
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Geo>>> GetGeos()
    {
        /**
         * GET request. Returns all the records from the table
        */
        try
        {
            var result = await _context.Geos.ToListAsync();

            if (result == null || !result.Any())
            {
                return NotFound("No data found.");
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, $"Internal server error: {ex.Message}");
        }
    }

    // GET: api/geo/colour
    [HttpGet("colour")]
    public async Task<IActionResult> GetLatLon([FromQuery] string? colour)
    {
        /**
         * GET request. Returns the latitude and longitude for all the records filtered by colour.
         * Returns all records if both filters are missing.
         *
         * Example:
         *   1) GET /api/geo/colour?colour=#FF0000
         *   2) GET /api/geo/colour
        */
        try{
            _logger.LogInformation("*** Get Colour {Color} ***", colour);
            var query = _context.Geos.AsQueryable();

            if (!string.IsNullOrEmpty(colour))
            {
                query = query.Where(n => n.Colour == colour);
            }

            var result = await query
                        .Select(n => new { n.Latitude, n.Longitude, n.Colour })
                        .ToListAsync();
            if (result == null || !result.Any())
            {
                return NotFound("No data found.");
            }
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, $"Internal server error: {ex.Message}");
        }
    }

    // GET: api/geo/id
    [HttpGet("id")]
    public async Task<IActionResult> GetIdFromCoordinates([FromQuery] string? timestamp, [FromQuery] decimal? latitude, [FromQuery] decimal? longitude)
    {
        try{
            var query = _context.Geos.AsQueryable();
            if (!string.IsNullOrEmpty(timestamp))
            {
                query = query.Where(n => n.TimeStamp == timestamp);
            }

            if (latitude.HasValue)
            {
                query = query.Where(n => n.Latitude == latitude);
            }

            if (longitude.HasValue)
            {
                query = query.Where(n => n.Longitude == longitude);
            }
            var result = await query
                         .Select(n => new { n.Id })
                         .ToListAsync();
            if (result == null || !result.Any())
            {
                return NotFound("No data found.");
            }
            return Ok(result);
        }
        catch (Exception ex){
            return StatusCode(StatusCodes.Status500InternalServerError, $"Internal server error: {ex.Message}");
        }
    }

    // GET: api/geo/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<Geo>> GetGeoById(int id)
    {
        var geo = await _context.Geos.FindAsync(id);

        if (geo == null)
        {
            return NotFound();
        }

        return Ok(geo);
    }

    // dummy endpoint to test the database connection
    [HttpGet("test")]
    public string Test()
    {
        return "Hello World!";
    }


    /***********************************************
     ***********************************************
     ***************** POST ************************
     ***********************************************
     ***********************************************/
    // POST: api/geo
    [HttpPost]
    public async Task<IActionResult> PostGeo([FromBody] JsonElement data)
    {
        _logger.LogInformation("*** Received request at {Time} ***", DateTime.UtcNow);
        try
        {
            if (data.ValueKind == JsonValueKind.Array)
            {
                var geoList = JsonSerializer.Deserialize<List<Geo>>(data.GetRawText());
                if (geoList == null || !geoList.Any())
                {
                    _logger.LogWarning("Empty list or invalid format");
                    return BadRequest("Empty list or invalid format");
                }
                var totalRecords = geoList.Count;
                var successfullyAdded = 0;
                foreach (var geo in geoList)
                {
                    _validator.ValidateColour(geo); // Validate the color
                    var result = await _helper.ProcessGeo(geo); // Use the helper here
                    if (result.Result is BadRequestObjectResult || result.Result is ConflictObjectResult)
                    {
                        continue;
                    }
                    if (result.Result is OkObjectResult okResult && okResult.Value is Geo newGeo)
                    {
                        successfullyAdded++;
                        _logger.LogInformation("Successfully added record: TimeStamp={TimeStamp}, Latitude={Latitude}, Longitude={Longitude}", newGeo.TimeStamp, newGeo.Latitude, newGeo.Longitude);
                    }
                }
                return Ok(new
                {
                    TotalRecords = totalRecords,
                    SuccessfullyAdded = successfullyAdded,
                    Failed = totalRecords - successfullyAdded
                });
            }
            else if (data.ValueKind == JsonValueKind.Object)
            {
                // Handle the case where the request contains a single object
                var geo = JsonSerializer.Deserialize<Geo>(data.GetRawText());

                if (geo == null)
                {
                    return BadRequest("Invalid object format");
                }

                _validator.ValidateColour(geo); // Validate the color
                var result = await _helper.ProcessGeo(geo); // Use the helper here
                if (result.Result is OkObjectResult okResult && okResult.Value is Geo newGeo)
                {
                    return CreatedAtAction(nameof(GetGeoById), new { id = newGeo.Id }, newGeo);
                }
                return result.Result ?? StatusCode(StatusCodes.Status500InternalServerError, "Unexpected error: null result.");
            }
            else
            {
                return BadRequest("Invalid JSON data. Expected a JSON object or an array of objects.");
            }
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, $"Internal server error: {ex.Message}");
        }
    }


    /***********************************************
     ***********************************************
     ****************** PUT ************************
     ***********************************************
     ***********************************************/
     // PUT: api/geo/{id}
     [HttpPut("{id}")]
    public async Task<IActionResult> PutId(int id, [FromBody] JsonElement data)
    {
        try{
            if (data.ValueKind != JsonValueKind.Object){
                return BadRequest("Not a JSON object");
            }
            var geo = JsonSerializer.Deserialize<Geo>(data.GetRawText());
            if (geo == null)
            {
                return BadRequest("Invalid object format");
            }
            if (id != geo.Id)
            {
                return BadRequest("IDs don't match");
            }
            var existingEntity = await _context.Geos.FindAsync(id);
            if (existingEntity == null)
            {
                return NotFound($"Entity with ID {id} not found");
            }
            _context.Entry(existingEntity).CurrentValues.SetValues(geo);
            if (_context.Entry(existingEntity).State == EntityState.Modified)
            {
                // Save changes to the database
                await _context.SaveChangesAsync();
                return NoContent(); // Success
            }
            else
            {
                return BadRequest("No changes detected");
            }
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, $"Internal server error: {ex.Message}");
        }
    }


    /***********************************************
     ***********************************************
     ***************** DELETE **********************
     ***********************************************
     ***********************************************/
     // DELETE: api/geo/{id}
     [HttpDelete("{id}")]
     public async Task<IActionResult> DeleteGeo(int id)
     {
        try{
            var existingEntity = await _context.Geos.FindAsync(id);
            if (existingEntity == null)
            {
                return NotFound($"Entity with ID {id} not found");
            }
            _context.Geos.Remove(existingEntity);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, $"Internal server error: {ex.Message}");
        }
     }
}