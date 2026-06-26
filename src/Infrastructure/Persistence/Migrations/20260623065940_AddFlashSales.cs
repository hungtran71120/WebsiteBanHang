using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HungStore.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFlashSales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FlashSales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StartsAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndsAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlashSales", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FlashSaleItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FlashSaleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SalePrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    QuantityLimit = table.Column<int>(type: "int", nullable: false),
                    QuantitySold = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlashSaleItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FlashSaleItems_FlashSales_FlashSaleId",
                        column: x => x.FlashSaleId,
                        principalTable: "FlashSales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FlashSaleItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FlashSaleItems_FlashSaleId_ProductId",
                table: "FlashSaleItems",
                columns: new[] { "FlashSaleId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FlashSaleItems_ProductId",
                table: "FlashSaleItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_FlashSales_StartsAt_EndsAt",
                table: "FlashSales",
                columns: new[] { "StartsAt", "EndsAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FlashSaleItems");

            migrationBuilder.DropTable(
                name: "FlashSales");
        }
    }
}
