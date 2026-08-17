using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyOrganizer.Wpf.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Snapshot-only: DateJoin/DateDobleJoin were already TEXT in SQLite.
            // The previous snapshot recorded them as string; the model uses DateTime.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
