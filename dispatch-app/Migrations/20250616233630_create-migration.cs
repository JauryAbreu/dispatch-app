using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dispatch_app.Migrations
{
    /// <inheritdoc />
    public partial class createmigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Bin",
                table: "Lines",
                type: "nvarchar(25)",
                maxLength: 25,
                nullable: true,
                defaultValueSql: "''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Bin",
                table: "Lines");
        }
    }
}
