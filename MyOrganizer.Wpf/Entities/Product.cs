// Product.cs
using System.ComponentModel.DataAnnotations;

namespace MyOrganizer.Wpf.Data.Entities;

public class Product
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string ProductName { get; set; } = "";

    public int Count { get; set; }

    [Required, MaxLength(50)]
    public string Value { get; set; } = ""; // unit or type
}
