using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dispatch_app.Migrations
{
    /// <inheritdoc />
    public partial class secondmigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Store_StoreId",
                table: "Stores",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_Header_ReceiptId",
                table: "Headers",
                column: "ReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_Fiscal_ReceiptId",
                table: "Fiscal",
                column: "ReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_Customer_CustomerId",
                table: "Customers",
                column: "CustomerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Store_StoreId",
                table: "Stores");

            migrationBuilder.DropIndex(
                name: "IX_Header_ReceiptId",
                table: "Headers");

            migrationBuilder.DropIndex(
                name: "IX_Fiscal_ReceiptId",
                table: "Fiscal");

            migrationBuilder.DropIndex(
                name: "IX_Customer_CustomerId",
                table: "Customers");
        }
    }
}
