using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OSBIS.Migrations
{
    /// <inheritdoc />
    public partial class AddBillImageUrlToPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BillImageUrl",
                table: "Payment",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BillImageUrl",
                table: "Payment");
        }
    }
}
