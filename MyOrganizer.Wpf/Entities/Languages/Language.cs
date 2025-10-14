using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyOrganizer.Wpf.Entities.Languages
{
    // Entities/L10nKey.cs
    public class L10nKey
    {
        public int Id { get; set; }
        public string Key { get; set; } = null!;
        public string? Group { get; set; }
        public string? Description { get; set; }

        public List<L10nValue> Values { get; set; } = new();
    }

    // Entities/L10nValue.cs
    public class L10nValue
    {
        public int KeyId { get; set; }
        public string Lang { get; set; } = null!;
        public string Value { get; set; } = null!;
        public L10nKey Key { get; set; } = null!;
    }

    // Entities/Language.cs
    public class Language
    {
        [Key]
        [MaxLength(5)]
        public string Code { get; set; } = null!; // 'hy', 'ru', 'en'
        [MaxLength(50)]
        public string Name { get; set; } = null!;
    }

}
