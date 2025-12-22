using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookinhMVC.Migrations
{
    /// <inheritdoc />
    public partial class suaLaIGD : Migration
    {
        /// <inheritdoc /> 
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GhiChu",
                table: "TaiKhoanBenhNhan",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayTao",
                table: "TaiKhoanBenhNhan",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<decimal>(
                name: "TongTienChi",
                table: "TaiKhoanBenhNhan",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TongTienNap",
                table: "TaiKhoanBenhNhan",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "TrangThai",
                table: "TaiKhoanBenhNhan",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "TrangThai",
                table: "GiaoDichThanhToan",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NoiDung",
                table: "GiaoDichThanhToan",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "MaThamChieu",
                table: "GiaoDichThanhToan",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "LoaiGiaoDich",
                table: "GiaoDichThanhToan",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GhiChu",
                table: "GiaoDichThanhToan",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MoTa",
                table: "GiaoDichThanhToan",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayCapNhat",
                table: "GiaoDichThanhToan",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "NguoiXuLy",
                table: "GiaoDichThanhToan",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PhuongThucThanhToan",
                table: "GiaoDichThanhToan",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GhiChu",
                table: "TaiKhoanBenhNhan");

            migrationBuilder.DropColumn(
                name: "NgayTao",
                table: "TaiKhoanBenhNhan");

            migrationBuilder.DropColumn(
                name: "TongTienChi",
                table: "TaiKhoanBenhNhan");

            migrationBuilder.DropColumn(
                name: "TongTienNap",
                table: "TaiKhoanBenhNhan");

            migrationBuilder.DropColumn(
                name: "TrangThai",
                table: "TaiKhoanBenhNhan");

            migrationBuilder.DropColumn(
                name: "GhiChu",
                table: "GiaoDichThanhToan");

            migrationBuilder.DropColumn(
                name: "MoTa",
                table: "GiaoDichThanhToan");

            migrationBuilder.DropColumn(
                name: "NgayCapNhat",
                table: "GiaoDichThanhToan");

            migrationBuilder.DropColumn(
                name: "NguoiXuLy",
                table: "GiaoDichThanhToan");

            migrationBuilder.DropColumn(
                name: "PhuongThucThanhToan",
                table: "GiaoDichThanhToan");

            migrationBuilder.AlterColumn<string>(
                name: "TrangThai",
                table: "GiaoDichThanhToan",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "NoiDung",
                table: "GiaoDichThanhToan",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "MaThamChieu",
                table: "GiaoDichThanhToan",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "LoaiGiaoDich",
                table: "GiaoDichThanhToan",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);
        }
    }
}
