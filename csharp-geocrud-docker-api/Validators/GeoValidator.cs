using System.Drawing;
using Microsoft.EntityFrameworkCore;
using Models;
using Data;
using Microsoft.Extensions.Logging;


namespace Validators
{
    public class GeoValidator
    {
        private readonly GeoContext _context;
        private readonly ILogger<GeoValidator> _logger;

        public GeoValidator(GeoContext context, ILogger<GeoValidator> logger)
        {
            _context = context;
            _logger = logger;
        }

        public void ValidateColour(Geo geo)
        {
            string colorInput = geo.Colour;
            _logger.LogInformation("Validating color: {Color}", colorInput);
            try
            {
                // Check if it's a valid named color
                Color color = Color.FromName(colorInput);

                if (!color.IsKnownColor && !color.IsNamedColor && !color.IsSystemColor)
                {
                    _logger.LogWarning("Color '{Color}' is not a known color", colorInput);
                    if (colorInput.StartsWith("#") && (colorInput.Length == 7 || colorInput.Length == 9))
                    {
                        color = ColorTranslator.FromHtml(colorInput);
                    }
                    else
                    {
                        _logger.LogError("Invalid color format for: {Color}", colorInput);
                        throw new ArgumentException("Invalid color format.");
                    }
                }

                // Convert to hex code and assign it back to geo.Colour
                geo.Colour = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
                 _logger.LogInformation("Color validated and converted to hex: {HexColor}", geo.Colour);
            }
            catch (Exception)
            {
                // If color is invalid, set geo.Colour to black
                geo.Colour = "#000000";
            }
        }

        public async Task<bool> ExistingRecordAsync(Geo geo)
        {
            return await _context.Geos
                .AnyAsync(n => n.TimeStamp == geo.TimeStamp &&
                               n.Latitude == geo.Latitude &&
                               n.Longitude == geo.Longitude);
        }
    }
}