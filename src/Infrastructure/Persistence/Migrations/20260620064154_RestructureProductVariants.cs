using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HungStore.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RestructureProductVariants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductVariants_ProductId_ColorName",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "ColorName",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "ColorName",
                table: "OrderItems");

            migrationBuilder.AddColumn<Guid>(
                name: "OptionValue1Id",
                table: "ProductVariants",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "OptionValue2Id",
                table: "ProductVariants",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VariantDescription",
                table: "OrderItems",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProductVariantOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductVariantOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductVariantOptions_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductVariantOptionValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductVariantOptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductVariantOptionValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductVariantOptionValues_ProductVariantOptions_ProductVariantOptionId",
                        column: x => x.ProductVariantOptionId,
                        principalTable: "ProductVariantOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_OptionValue1Id",
                table: "ProductVariants",
                column: "OptionValue1Id");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_OptionValue2Id",
                table: "ProductVariants",
                column: "OptionValue2Id");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_ProductId_OptionValue1Id_OptionValue2Id",
                table: "ProductVariants",
                columns: new[] { "ProductId", "OptionValue1Id", "OptionValue2Id" },
                unique: true,
                filter: "[OptionValue2Id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariantOptions_ProductId_Name",
                table: "ProductVariantOptions",
                columns: new[] { "ProductId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariantOptionValues_ProductVariantOptionId_Value",
                table: "ProductVariantOptionValues",
                columns: new[] { "ProductVariantOptionId", "Value" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductVariants_ProductVariantOptionValues_OptionValue1Id",
                table: "ProductVariants",
                column: "OptionValue1Id",
                principalTable: "ProductVariantOptionValues",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductVariants_ProductVariantOptionValues_OptionValue2Id",
                table: "ProductVariants",
                column: "OptionValue2Id",
                principalTable: "ProductVariantOptionValues",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductVariants_ProductVariantOptionValues_OptionValue1Id",
                table: "ProductVariants");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductVariants_ProductVariantOptionValues_OptionValue2Id",
                table: "ProductVariants");

            migrationBuilder.DropTable(
                name: "ProductVariantOptionValues");

            migrationBuilder.DropTable(
                name: "ProductVariantOptions");

            migrationBuilder.DropIndex(
                name: "IX_ProductVariants_OptionValue1Id",
                table: "ProductVariants");

            migrationBuilder.DropIndex(
                name: "IX_ProductVariants_OptionValue2Id",
                table: "ProductVariants");

            migrationBuilder.DropIndex(
                name: "IX_ProductVariants_ProductId_OptionValue1Id_OptionValue2Id",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "OptionValue1Id",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "OptionValue2Id",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "VariantDescription",
                table: "OrderItems");

            migrationBuilder.AddColumn<string>(
                name: "ColorName",
                table: "ProductVariants",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "ProductVariants",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ColorName",
                table: "OrderItems",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_ProductId_ColorName",
                table: "ProductVariants",
                columns: new[] { "ProductId", "ColorName" },
                unique: true);
        }
    }
}
