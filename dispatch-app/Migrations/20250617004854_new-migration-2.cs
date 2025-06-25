using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dispatch_app.Migrations
{
    /// <inheritdoc />
    public partial class newmigration2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "UserCode",
                table: "Lines",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                defaultValueSql: "''",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true,
                oldDefaultValueSql: "1");

            migrationBuilder.AddColumn<string>(
                name: "UserCode",
                table: "Headers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                defaultValueSql: "''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserCode",
                table: "Headers");

            migrationBuilder.AlterColumn<string>(
                name: "UserCode",
                table: "Lines",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                defaultValueSql: "1",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true,
                oldDefaultValueSql: "''");
        }
    }
}
