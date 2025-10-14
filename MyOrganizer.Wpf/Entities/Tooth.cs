// Tooth.cs
using System.ComponentModel.DataAnnotations;

namespace MyOrganizer.Wpf.Data.Entities;

public class Tooth
{
    public int Id { get; set; }

    // FK → Client
    public int ClientId { get; set; }

    [MaxLength(10)]
    public string? ToothNumber { get; set; } // keeping your name; can rename via Fluent API if needed

    // numeric flags/codes as provided
    public int Byugel { get; set; }
    public int Protez { get; set; }
    public int Implantzr { get; set; }
    public int Implmk { get; set; }
    public int Zrkemax { get; set; }
    public int Mk30 { get; set; }
    public int Rest { get; set; }
    public int Plomb { get; set; }
    public int Shift { get; set; }
    public int Endo { get; set; }

    public bool Up { get; set; }

    // nav
    public Client? Client { get; set; }
}
