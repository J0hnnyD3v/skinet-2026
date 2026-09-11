using System.ComponentModel.DataAnnotations;

namespace API.Dtos.Products;

// Hereda Name/Description/Price/PictureUrl/Type/Brand/QuantityInStock con su validación.
// Solo agrega el Id, que el controller contrasta contra el de la ruta.
public class UpdateProductDto : CreateProductDto
{
    [Range(1, int.MaxValue, ErrorMessage = "El Id debe ser mayor que 0.")]
    public int Id { get; set; }
}
