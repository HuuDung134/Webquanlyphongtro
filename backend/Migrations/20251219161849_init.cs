using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoAnCoSo.Migrations
{
    /// <inheritdoc />
    public partial class init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DichVu",
                columns: table => new
                {
                    MaDichVu = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenDichVu = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Tiendichvu = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DichVu", x => x.MaDichVu);
                });

            migrationBuilder.CreateTable(
                name: "GiaDien",
                columns: table => new
                {
                    MaGiaDien = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BacDien = table.Column<int>(type: "int", nullable: false),
                    GiaTienDien = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TuSoDien = table.Column<int>(type: "int", nullable: false),
                    DenSoDien = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GiaDien", x => x.MaGiaDien);
                });

            migrationBuilder.CreateTable(
                name: "GiaNuoc",
                columns: table => new
                {
                    MaGiaNuoc = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BacNuoc = table.Column<int>(type: "int", nullable: false),
                    GiaTienNuoc = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TuSoNuoc = table.Column<int>(type: "int", nullable: false),
                    DenSoNuoc = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GiaNuoc", x => x.MaGiaNuoc);
                });

            migrationBuilder.CreateTable(
                name: "LoaiPhong",
                columns: table => new
                {
                    MaLoaiPhong = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenLoaiPhong = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoaiPhong", x => x.MaLoaiPhong);
                });

            migrationBuilder.CreateTable(
                name: "NhaTro",
                columns: table => new
                {
                    MaNhaTro = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenNhaTro = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DiaChi = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NhaTro", x => x.MaNhaTro);
                });

            migrationBuilder.CreateTable(
                name: "ThongBao",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpireAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThongBao", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TinNhan",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaNguoiGui = table.Column<int>(type: "int", nullable: false),
                    MaNguoiNhan = table.Column<int>(type: "int", nullable: false),
                    LoaiNguoiGui = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    LoaiNguoiNhan = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NoiDung = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ThoiGianGui = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DaDocAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DaThuHoi = table.Column<bool>(type: "bit", nullable: false),
                    DaSua = table.Column<bool>(type: "bit", nullable: false),
                    NoiDungGoc = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TinNhan", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    MaNguoiDung = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenDangNhap = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MatKhau = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    VaiTro = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TrangThai = table.Column<bool>(type: "bit", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TelegramChatId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.MaNguoiDung);
                });

            migrationBuilder.CreateTable(
                name: "LichSuGiaDichVu",
                columns: table => new
                {
                    MaLichSu = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaDichVu = table.Column<int>(type: "int", nullable: false),
                    GiaDichVu = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NgayHieuLuc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LichSuGiaDichVu", x => x.MaLichSu);
                    table.ForeignKey(
                        name: "FK_LichSuGiaDichVu_DichVu_MaDichVu",
                        column: x => x.MaDichVu,
                        principalTable: "DichVu",
                        principalColumn: "MaDichVu",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Phong",
                columns: table => new
                {
                    MaPhong = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaNhaTro = table.Column<int>(type: "int", nullable: false),
                    MaLoaiPhong = table.Column<int>(type: "int", nullable: false),
                    TenPhong = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DienTich = table.Column<float>(type: "real", nullable: true),
                    GiaPhong = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SucChua = table.Column<int>(type: "int", nullable: false),
                    TrangThai = table.Column<int>(type: "int", nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    HinhAnh = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Phong", x => x.MaPhong);
                    table.ForeignKey(
                        name: "FK_Phong_LoaiPhong_MaLoaiPhong",
                        column: x => x.MaLoaiPhong,
                        principalTable: "LoaiPhong",
                        principalColumn: "MaLoaiPhong");
                    table.ForeignKey(
                        name: "FK_Phong_NhaTro_MaNhaTro",
                        column: x => x.MaNhaTro,
                        principalTable: "NhaTro",
                        principalColumn: "MaNhaTro");
                });

            migrationBuilder.CreateTable(
                name: "ChatHistory",
                columns: table => new
                {
                    MaChatHistory = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TelegramChatId = table.Column<long>(type: "bigint", nullable: false),
                    MaNguoiDung = table.Column<int>(type: "int", nullable: false),
                    UserMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    BotResponse = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: true),
                    Intent = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    VaiTro = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ThoiGian = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MessageType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ContextData = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatHistory", x => x.MaChatHistory);
                    table.ForeignKey(
                        name: "FK_ChatHistory_Users_MaNguoiDung",
                        column: x => x.MaNguoiDung,
                        principalTable: "Users",
                        principalColumn: "MaNguoiDung",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NguoiThue",
                columns: table => new
                {
                    MaNguoiThue = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HoTen = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CCCD = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    SDT = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    NgaySinh = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DiaChi = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    GioiTinh = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    QuocTich = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NoiCongTac = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    MaNguoiDung = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NguoiThue", x => x.MaNguoiThue);
                    table.ForeignKey(
                        name: "FK_NguoiThue_Users_MaNguoiDung",
                        column: x => x.MaNguoiDung,
                        principalTable: "Users",
                        principalColumn: "MaNguoiDung",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ChiSoDien",
                columns: table => new
                {
                    MaDien = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaPhong = table.Column<int>(type: "int", nullable: false),
                    MaGiaDien = table.Column<int>(type: "int", nullable: false),
                    SoDienCu = table.Column<int>(type: "int", nullable: false),
                    SoDienMoi = table.Column<int>(type: "int", nullable: false),
                    SoDienTieuThu = table.Column<int>(type: "int", nullable: false),
                    TienDien = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    HinhAnhDien = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NgayThangDien = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChiSoDien", x => x.MaDien);
                    table.ForeignKey(
                        name: "FK_ChiSoDien_GiaDien_MaGiaDien",
                        column: x => x.MaGiaDien,
                        principalTable: "GiaDien",
                        principalColumn: "MaGiaDien",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChiSoDien_Phong_MaPhong",
                        column: x => x.MaPhong,
                        principalTable: "Phong",
                        principalColumn: "MaPhong");
                });

            migrationBuilder.CreateTable(
                name: "ChiSoNuoc",
                columns: table => new
                {
                    MaNuoc = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaPhong = table.Column<int>(type: "int", nullable: false),
                    MaGiaNuoc = table.Column<int>(type: "int", nullable: false),
                    SoNuocCu = table.Column<int>(type: "int", nullable: false),
                    SoNuocMoi = table.Column<int>(type: "int", nullable: false),
                    SoNuocTieuThu = table.Column<int>(type: "int", nullable: false),
                    TienNuoc = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    HinhAnhNuoc = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NgayThangNuoc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChiSoNuoc", x => x.MaNuoc);
                    table.ForeignKey(
                        name: "FK_ChiSoNuoc_GiaNuoc_MaGiaNuoc",
                        column: x => x.MaGiaNuoc,
                        principalTable: "GiaNuoc",
                        principalColumn: "MaGiaNuoc",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChiSoNuoc_Phong_MaPhong",
                        column: x => x.MaPhong,
                        principalTable: "Phong",
                        principalColumn: "MaPhong");
                });

            migrationBuilder.CreateTable(
                name: "LichSuDongMo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaPhong = table.Column<int>(type: "int", nullable: false),
                    HanhDong = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ThoiGian = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiThucHien = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LichSuDongMo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LichSuDongMo_Phong_MaPhong",
                        column: x => x.MaPhong,
                        principalTable: "Phong",
                        principalColumn: "MaPhong",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HopDong",
                columns: table => new
                {
                    MaHopDong = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaNguoiThue = table.Column<int>(type: "int", nullable: false),
                    MaPhong = table.Column<int>(type: "int", nullable: false),
                    NgayBatDau = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NgayKetThuc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TienCoc = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NoiDung = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HopDong", x => x.MaHopDong);
                    table.ForeignKey(
                        name: "FK_HopDong_NguoiThue_MaNguoiThue",
                        column: x => x.MaNguoiThue,
                        principalTable: "NguoiThue",
                        principalColumn: "MaNguoiThue");
                    table.ForeignKey(
                        name: "FK_HopDong_Phong_MaPhong",
                        column: x => x.MaPhong,
                        principalTable: "Phong",
                        principalColumn: "MaPhong");
                });

            migrationBuilder.CreateTable(
                name: "SuCo",
                columns: table => new
                {
                    MaSuCo = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaNguoiThue = table.Column<int>(type: "int", nullable: false),
                    MaPhong = table.Column<int>(type: "int", nullable: false),
                    TieuDe = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NgayBaoCao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NgayXuLy = table.Column<DateTime>(type: "datetime2", nullable: true),
                    HinhAnh = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SuCo", x => x.MaSuCo);
                    table.ForeignKey(
                        name: "FK_SuCo_NguoiThue_MaNguoiThue",
                        column: x => x.MaNguoiThue,
                        principalTable: "NguoiThue",
                        principalColumn: "MaNguoiThue",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SuCo_Phong_MaPhong",
                        column: x => x.MaPhong,
                        principalTable: "Phong",
                        principalColumn: "MaPhong",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HoaDon",
                columns: table => new
                {
                    MaHoaDon = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaNguoiThue = table.Column<int>(type: "int", nullable: false),
                    MaPhong = table.Column<int>(type: "int", nullable: false),
                    MaDien = table.Column<int>(type: "int", nullable: false),
                    MaNuoc = table.Column<int>(type: "int", nullable: false),
                    TienDichVu = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TongTien = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NgayLap = table.Column<DateTime>(type: "datetime2", nullable: false),
                    KyHoaDon = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HoaDon", x => x.MaHoaDon);
                    table.ForeignKey(
                        name: "FK_HoaDon_ChiSoDien_MaDien",
                        column: x => x.MaDien,
                        principalTable: "ChiSoDien",
                        principalColumn: "MaDien");
                    table.ForeignKey(
                        name: "FK_HoaDon_ChiSoNuoc_MaNuoc",
                        column: x => x.MaNuoc,
                        principalTable: "ChiSoNuoc",
                        principalColumn: "MaNuoc");
                    table.ForeignKey(
                        name: "FK_HoaDon_NguoiThue_MaNguoiThue",
                        column: x => x.MaNguoiThue,
                        principalTable: "NguoiThue",
                        principalColumn: "MaNguoiThue");
                    table.ForeignKey(
                        name: "FK_HoaDon_Phong_MaPhong",
                        column: x => x.MaPhong,
                        principalTable: "Phong",
                        principalColumn: "MaPhong");
                });

            migrationBuilder.CreateTable(
                name: "ChiTietHopDong",
                columns: table => new
                {
                    MaChiTietHopDong = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaHopDong = table.Column<int>(type: "int", nullable: false),
                    MaNguoiThue = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChiTietHopDong", x => x.MaChiTietHopDong);
                    table.ForeignKey(
                        name: "FK_ChiTietHopDong_HopDong_MaHopDong",
                        column: x => x.MaHopDong,
                        principalTable: "HopDong",
                        principalColumn: "MaHopDong",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChiTietHopDong_NguoiThue_MaNguoiThue",
                        column: x => x.MaNguoiThue,
                        principalTable: "NguoiThue",
                        principalColumn: "MaNguoiThue",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChiTietHoaDon",
                columns: table => new
                {
                    MaChiTiet = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaHoaDon = table.Column<int>(type: "int", nullable: false),
                    LoaiKhoan = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SoTien = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MaDichVu = table.Column<int>(type: "int", nullable: true),
                    SoLuong = table.Column<int>(type: "int", nullable: true),
                    DonGia = table.Column<decimal>(type: "decimal(18,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChiTietHoaDon", x => x.MaChiTiet);
                    table.ForeignKey(
                        name: "FK_ChiTietHoaDon_DichVu_MaDichVu",
                        column: x => x.MaDichVu,
                        principalTable: "DichVu",
                        principalColumn: "MaDichVu");
                    table.ForeignKey(
                        name: "FK_ChiTietHoaDon_HoaDon_MaHoaDon",
                        column: x => x.MaHoaDon,
                        principalTable: "HoaDon",
                        principalColumn: "MaHoaDon",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ThanhToan",
                columns: table => new
                {
                    MaThanhToan = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaHoaDon = table.Column<int>(type: "int", nullable: false),
                    MaNguoiThue = table.Column<int>(type: "int", nullable: false),
                    NgayThanhToan = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TongTien = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    HinhThucThanhToan = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    TrangThai = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThanhToan", x => x.MaThanhToan);
                    table.ForeignKey(
                        name: "FK_ThanhToan_HoaDon_MaHoaDon",
                        column: x => x.MaHoaDon,
                        principalTable: "HoaDon",
                        principalColumn: "MaHoaDon");
                    table.ForeignKey(
                        name: "FK_ThanhToan_NguoiThue_MaNguoiThue",
                        column: x => x.MaNguoiThue,
                        principalTable: "NguoiThue",
                        principalColumn: "MaNguoiThue");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatHistory_MaNguoiDung",
                table: "ChatHistory",
                column: "MaNguoiDung");

            migrationBuilder.CreateIndex(
                name: "IX_ChiSoDien_MaGiaDien",
                table: "ChiSoDien",
                column: "MaGiaDien");

            migrationBuilder.CreateIndex(
                name: "IX_ChiSoDien_MaPhong",
                table: "ChiSoDien",
                column: "MaPhong");

            migrationBuilder.CreateIndex(
                name: "IX_ChiSoNuoc_MaGiaNuoc",
                table: "ChiSoNuoc",
                column: "MaGiaNuoc");

            migrationBuilder.CreateIndex(
                name: "IX_ChiSoNuoc_MaPhong",
                table: "ChiSoNuoc",
                column: "MaPhong");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietHoaDon_MaDichVu",
                table: "ChiTietHoaDon",
                column: "MaDichVu");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietHoaDon_MaHoaDon",
                table: "ChiTietHoaDon",
                column: "MaHoaDon");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietHopDong_MaHopDong",
                table: "ChiTietHopDong",
                column: "MaHopDong");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietHopDong_MaNguoiThue",
                table: "ChiTietHopDong",
                column: "MaNguoiThue");

            migrationBuilder.CreateIndex(
                name: "IX_GiaDien_BacDien",
                table: "GiaDien",
                column: "BacDien",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GiaNuoc_BacNuoc",
                table: "GiaNuoc",
                column: "BacNuoc",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HoaDon_MaDien",
                table: "HoaDon",
                column: "MaDien");

            migrationBuilder.CreateIndex(
                name: "IX_HoaDon_MaNguoiThue",
                table: "HoaDon",
                column: "MaNguoiThue");

            migrationBuilder.CreateIndex(
                name: "IX_HoaDon_MaNuoc",
                table: "HoaDon",
                column: "MaNuoc");

            migrationBuilder.CreateIndex(
                name: "IX_HoaDon_MaPhong",
                table: "HoaDon",
                column: "MaPhong");

            migrationBuilder.CreateIndex(
                name: "IX_HopDong_MaNguoiThue",
                table: "HopDong",
                column: "MaNguoiThue");

            migrationBuilder.CreateIndex(
                name: "IX_HopDong_MaPhong",
                table: "HopDong",
                column: "MaPhong");

            migrationBuilder.CreateIndex(
                name: "IX_LichSuDongMo_MaPhong",
                table: "LichSuDongMo",
                column: "MaPhong");

            migrationBuilder.CreateIndex(
                name: "IX_LichSuGiaDichVu_MaDichVu",
                table: "LichSuGiaDichVu",
                column: "MaDichVu");

            migrationBuilder.CreateIndex(
                name: "IX_NguoiThue_MaNguoiDung",
                table: "NguoiThue",
                column: "MaNguoiDung",
                unique: true,
                filter: "[MaNguoiDung] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Phong_MaLoaiPhong",
                table: "Phong",
                column: "MaLoaiPhong");

            migrationBuilder.CreateIndex(
                name: "IX_Phong_MaNhaTro",
                table: "Phong",
                column: "MaNhaTro");

            migrationBuilder.CreateIndex(
                name: "IX_SuCo_MaNguoiThue",
                table: "SuCo",
                column: "MaNguoiThue");

            migrationBuilder.CreateIndex(
                name: "IX_SuCo_MaPhong",
                table: "SuCo",
                column: "MaPhong");

            migrationBuilder.CreateIndex(
                name: "IX_ThanhToan_MaHoaDon",
                table: "ThanhToan",
                column: "MaHoaDon");

            migrationBuilder.CreateIndex(
                name: "IX_ThanhToan_MaNguoiThue",
                table: "ThanhToan",
                column: "MaNguoiThue");

            migrationBuilder.CreateIndex(
                name: "IX_Users_TenDangNhap",
                table: "Users",
                column: "TenDangNhap",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatHistory");

            migrationBuilder.DropTable(
                name: "ChiTietHoaDon");

            migrationBuilder.DropTable(
                name: "ChiTietHopDong");

            migrationBuilder.DropTable(
                name: "LichSuDongMo");

            migrationBuilder.DropTable(
                name: "LichSuGiaDichVu");

            migrationBuilder.DropTable(
                name: "SuCo");

            migrationBuilder.DropTable(
                name: "ThanhToan");

            migrationBuilder.DropTable(
                name: "ThongBao");

            migrationBuilder.DropTable(
                name: "TinNhan");

            migrationBuilder.DropTable(
                name: "HopDong");

            migrationBuilder.DropTable(
                name: "DichVu");

            migrationBuilder.DropTable(
                name: "HoaDon");

            migrationBuilder.DropTable(
                name: "ChiSoDien");

            migrationBuilder.DropTable(
                name: "ChiSoNuoc");

            migrationBuilder.DropTable(
                name: "NguoiThue");

            migrationBuilder.DropTable(
                name: "GiaDien");

            migrationBuilder.DropTable(
                name: "GiaNuoc");

            migrationBuilder.DropTable(
                name: "Phong");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "LoaiPhong");

            migrationBuilder.DropTable(
                name: "NhaTro");
        }
    }
}
