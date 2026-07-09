using System.ComponentModel.DataAnnotations;

public class CreateOrderRequest
{
    [Required]
    [MaxLength(200)]
    public string ProductName { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal TotalPrice { get; set; }
}