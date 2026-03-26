# Backend - Hệ Thống Quản Lý Nhà Trọ

Backend API cho hệ thống quản lý nhà trọ, xây dựng bằng **ASP.NET Core .NET 8**, cung cấp nghiệp vụ quản lý phòng, hợp đồng, hóa đơn, thanh toán và tích hợp dịch vụ ngoài.

## Công nghệ sử dụng

- **Framework:** ASP.NET Core Web API (.NET 8)
- **ORM / Database:** Entity Framework Core, SQL Server
- **Authentication & Authorization:** JWT Bearer, Role-based Access Control (RBAC)
- **Tài liệu API:** Swagger / OpenAPI (Swashbuckle)
- **Cloud & AI:** Cloudinary, OpenAI OCR, Tesseract
- **Payment:** MoMo
- **Bot / Messaging:** Telegram Bot, Zalo OA, eSMS, Email (MailKit)
- **IoT:** MQTT (MQTTnet)
- **Khác:** DotNetEnv, BCrypt, DinkToPdf

## Kiến trúc chính

- `Controllers/` - Định nghĩa API endpoint
- `Services/` - Xử lý nghiệp vụ & tích hợp bên thứ ba
- `Data/` - `ApplicationDbContext` và truy cập dữ liệu
- `Models/` - Entity/domain model
- `Migrations/` - EF Core migrations
- `.env` - Biến môi trường runtime (không commit file thật)

## Yêu cầu hệ thống

- .NET SDK 8.0+
- SQL Server (LocalDB/SQL Express/SQL Server)
- (Tùy chọn) Tài khoản MoMo Sandbox, Cloudinary, OpenAI API Key, Telegram Bot Token, MQTT Broker

## Cài đặt nhanh

### 1) Clone project

```bash
git clone <your-repo-url>
cd <your-repo-folder>/backend
