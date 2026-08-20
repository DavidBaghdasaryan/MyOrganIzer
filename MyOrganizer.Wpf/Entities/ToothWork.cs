using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyOrganizer.Wpf.Entities
{
    public class ToothWork
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        public string ToothFdi { get; set; } = "";
        public string ProcedureName { get; set; } = "";
        public string Tier { get; set; } = "";
        public int Price { get; set; }

        /// <summary>
        /// Empty means the whole tooth (legacy rows). Otherwise a <see cref="Controls.ToothSurfaceType"/> name.
        /// </summary>
        public string Surface { get; set; } = "";

        /// <summary>
        /// Stable Lab procedure identity. Null on rows written before Stage 9.
        /// Several filling surface rows share one value.
        /// </summary>
        public Guid? ProcedureId { get; set; }

        /// <summary>
        /// Comma-separated root/canal ids. Empty for non-endo and legacy endo.
        /// </summary>
        public string RootCanalIds { get; set; } = "";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

}
