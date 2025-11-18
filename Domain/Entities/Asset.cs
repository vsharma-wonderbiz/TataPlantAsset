using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities
{
    public class Asset
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50, MinimumLength = 3)]
        [RegularExpression("^[A-Za-z0-9 _.-]+$", ErrorMessage = "Name contains invalid characters.")]
        public required string Name { get; set; }
        public List<Asset> Childrens { get; set; } = new List<Asset>();
        public bool IsDeleted { get; set; } = false;
        [Range(1, int.MaxValue, ErrorMessage = "ParentId must be greater than 0.")]
        public int? ParentId { get; set; }
    }
}
