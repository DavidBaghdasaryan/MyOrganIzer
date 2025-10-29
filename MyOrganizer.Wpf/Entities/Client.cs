// Client.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyOrganizer.Wpf.Data.Entities;

public class Client
{
    public int Id { get; set; }

    [NotMapped] // static isn’t mapped anyway; this keeps intent clear
    public static bool Edit { get; set; }

    [MaxLength(100)]
    public string? FirstName { get; set; }

    [MaxLength(100)]
    public string? LastName { get; set; }

    [MaxLength(100)]
    public string? MidlName { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? Price { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? Debet { get; set; }

    [MaxLength(50)]
    public string? PhoneNumber { get; set; }

    public DateTime DateJoin { get; set; } = DateTime.UtcNow;
    public DateTime? DateDobleJoin { get; set; } 

    [NotMapped]
    public string? DateJoinString { get; set; }

    public List<Tooth>? ClientTooths { get; set; }
}
