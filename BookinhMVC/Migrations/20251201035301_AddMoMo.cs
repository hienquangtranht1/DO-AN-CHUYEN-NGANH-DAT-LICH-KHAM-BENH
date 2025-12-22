using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookinhMVC.Migrations
{
    /// <inheritdoc />
    public partial class AddMoMo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GiaoDichThanhToan",
                columns: table => new
                {
                    MaGiaoDich = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaBenhNhan = table.Column<int>(type: "int", nullable: false),
                    MaLich = table.Column<int>(type: "int", nullable: true),
                    LoaiGiaoDich = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SoTien = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NgayGiaoDich = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MaThamChieu = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GiaoDichThanhToan", x => x.MaGiaoDich);
                    table.ForeignKey(
                        name: "FK_GiaoDichThanhToan_BenhNhans_MaBenhNhan",
                        column: x => x.MaBenhNhan,
                        principalTable: "BenhNhans",
                        principalColumn: "MaBenhNhan");
                    table.ForeignKey(
                        name: "FK_GiaoDichThanhToan_LichHens_MaLich",
                        column: x => x.MaLich,
                        principalTable: "LichHens",
                        principalColumn: "MaLich");
                });

            migrationBuilder.CreateTable(
                name: "TaiKhoanBenhNhan",
                columns: table => new
                {
                    MaBenhNhan = table.Column<int>(type: "int", nullable: false),
                    SoDuHienTai = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NgayCapNhatCuoi = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaiKhoanBenhNhan", x => x.MaBenhNhan);
                    table.ForeignKey(
                        name: "FK_TaiKhoanBenhNhan_BenhNhans_MaBenhNhan",
                        column: x => x.MaBenhNhan,
                        principalTable: "BenhNhans",
                        principalColumn: "MaBenhNhan",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GiaoDichThanhToan_MaBenhNhan",
                table: "GiaoDichThanhToan",
                column: "MaBenhNhan");

            migrationBuilder.CreateIndex(
                name: "IX_GiaoDichThanhToan_MaLich",
                table: "GiaoDichThanhToan",
                column: "MaLich");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GiaoDichThanhToan");

            migrationBuilder.DropTable(
                name: "TaiKhoanBenhNhan");
        }
    }
}
