using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models
{
    [Table("geo")]
    public class Geo
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("timestamp")]
        public required string TimeStamp { get; set; }

        [Required]
        [Column("latitude")]
        public decimal Latitude { get; set; }

        [Required]
        [Column("longitude")]
        public decimal Longitude { get; set; }

        [Column("colour")]
        public required string Colour { get; set; }
    }
}