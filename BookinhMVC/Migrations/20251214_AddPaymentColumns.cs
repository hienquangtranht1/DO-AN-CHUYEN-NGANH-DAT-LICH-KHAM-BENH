using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookinhMVC.Migrations
{
    public partial class AddPaymentColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GhiChu",
                table: "GiaoDichThanhToan",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MoTa",
                table: "GiaoDichThanhToan",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NoiDung",
                table: "GiaoDichThanhToan",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhuongThucThanhToan",
                table: "GiaoDichThanhToan",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NguoiXuLy",
                table: "GiaoDichThanhToan",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            // NgayCapNhat non-nullable -> thêm default SQL ?? không phá h?ng d? li?u hi?n có
            migrationBuilder.AddColumn<DateTime>(
                name: "NgayCapNhat",
                table: "GiaoDichThanhToan",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETDATE()");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "GhiChu", table: "GiaoDichThanhToan");
            migrationBuilder.DropColumn(name: "MoTa", table: "GiaoDichThanhToan");
            migrationBuilder.DropColumn(name: "NoiDung", table: "GiaoDichThanhToan");
            migrationBuilder.DropColumn(name: "PhuongThucThanhToan", table: "GiaoDichThanhToan");
            migrationBuilder.DropColumn(name: "NguoiXuLy", table: "GiaoDichThanhToan");
            migrationBuilder.DropColumn(name: "NgayCapNhat", table: "GiaoDichThanhToan");
        }
    }
}