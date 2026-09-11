using System.ComponentModel.DataAnnotations;

namespace API.Dtos.Products;

public class CreateProductDto
{
    [Required]
    [MaxLength(100)]
    public required string Name { get; set; }

    [Required]
    [MaxLength(1000)]
    public required string Description { get; set; }

    [Range(0.01, 999999.99, ErrorMessage = "El precio debe estar entre {1} y {2}.")]
    public decimal Price { get; set; }

    [Required]
    [Url]
    [MaxLength(2048)]
    public required string PictureUrl { get; set; }

    [Required]
    [MaxLength(100)]
    public required string Type { get; set; }

    [Required]
    [MaxLength(100)]
    public required string Brand { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "El stock no puede ser negativo.")]
    public int QuantityInStock { get; set; }
}
