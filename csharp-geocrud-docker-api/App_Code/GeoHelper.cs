using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Models;
using Validators;
using Data;

namespace App_Code
{
    public class GeoHelper
    {
        private readonly GeoContext _context;
        private readonly GeoValidator _validator;
        private readonly ILogger<GeoHelper> _logger;
        public GeoHelper(GeoContext context, GeoValidator validator, ILogger<GeoHelper> logger)
        {
            _context = context;
            _validator = validator;
            _logger = logger;
        }

        public async Task<ActionResult<Geo>> ProcessGeo(Geo geo)
        {
            try
            {
                // Check if the record already exists
                if (await _validator.ExistingRecordAsync(geo))
                {
                    _logger.LogWarning("Record already exists with TimeStamp={TimeStamp}, Latitude= {Latitude}, Longitude={Longitude}", geo.TimeStamp, geo.Latitude, geo.Longitude);
                    return new ConflictObjectResult($"Record already exists with timestamp {geo.TimeStamp} and coordinates ({geo.Latitude}, {geo.Longitude})");
                }

                // Validate the model using DataAnnotations
                var validationResults = new List<ValidationResult>();
                var validationContext = new ValidationContext(geo);
                bool isValid = Validator.TryValidateObject(geo, validationContext, validationResults, true);

                if (!isValid)
                {
                    return new BadRequestObjectResult(validationResults);
                }

                // Add the new record to the context
                _context.Geos.Add(geo);

                // Save changes to the database
                await _context.SaveChangesAsync();

                return new OkObjectResult(geo);
            }
            catch (Exception ex)
            {
                return new ObjectResult($"Internal server error: {ex.Message}")
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }
    }
}