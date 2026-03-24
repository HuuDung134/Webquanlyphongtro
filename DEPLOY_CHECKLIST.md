# Checklist Deploy - Hệ Thống Quản Lý Nhà Trọ

## ✅ Đã Hoàn Thành

- [x] File `.env` đã được tạo và chứa tất cả API keys
- [x] File `.env` đã được thêm vào `.gitignore` (không commit)
- [x] Code đã được cập nhật để đọc từ biến môi trường
- [x] Ứng dụng đã test thành công trên local

## 📋 Trước Khi Deploy

### 1. Kiểm tra Git
```bash
# Đảm bảo file .env KHÔNG được commit
git status
# Nếu thấy .env trong danh sách, đừng commit nó!
```

### 2. Tạo file .env.example (nếu chưa có)
File này sẽ được commit để hướng dẫn cấu hình

### 3. Commit các thay đổi
```bash
git add .
git commit -m "Refactor: Move API keys to .env file"
```

## 🚀 Deploy Options

### Option 1: Deploy trên Server (IIS, Linux Server)

1. **Copy code lên server**
   ```bash
   git clone <repository-url>
   cd backend
   ```

2. **Tạo file `.env` trên server**
   ```bash
   # Copy từ .env.example và điền thông tin thật
   cp .env.example .env
   nano .env  # hoặc vi .env
   ```

3. **Build và chạy**
   ```bash
   dotnet publish -c Release -o ./publish
   cd publish
   dotnet DoAnCoSo.dll
   ```

### Option 2: Deploy với Docker

1. **Cập nhật Dockerfile để sử dụng biến môi trường**
   - Option A: Copy file .env vào container (không khuyến nghị cho production)
   - Option B: Sử dụng environment variables từ Docker compose hoặc Kubernetes

2. **Docker Compose example:**
   ```yaml
   services:
     backend:
       build: ./backend
       environment:
         - CONNECTION_STRING=${CONNECTION_STRING}
         - JWT_KEY=${JWT_KEY}
         - CLOUDINARY_CLOUD_NAME=${CLOUDINARY_CLOUD_NAME}
         # ... các biến khác
   ```

### Option 3: Deploy trên Cloud Platforms

#### Azure App Service:
1. Vào Azure Portal → App Service → Configuration
2. Thêm Application Settings (Environment Variables):
   - `CONNECTION_STRING`
   - `JWT_KEY`
   - `CLOUDINARY_CLOUD_NAME`
   - ... (tất cả các biến trong .env)

#### AWS Elastic Beanstalk:
1. Vào EB Console → Configuration → Software
2. Thêm Environment Properties tương tự như trên

#### Heroku:
```bash
heroku config:set CONNECTION_STRING="your_connection_string"
heroku config:set JWT_KEY="your_jwt_key"
# ... các biến khác
```

#### Railway / Render / Fly.io:
- Thêm Environment Variables trong dashboard của platform

## 🔐 Security Checklist

- [ ] File `.env` KHÔNG được commit vào Git
- [ ] File `.env.example` được commit (không chứa giá trị thật)
- [ ] Các biến môi trường được cấu hình trên server/hosting
- [ ] Database connection string an toàn
- [ ] API keys được giữ bí mật

## 📝 Các Biến Môi Trường Cần Cấu Hình

Khi deploy, đảm bảo cấu hình các biến sau:

```
CONNECTION_STRING          # Database connection
JWT_KEY                    # JWT secret key
JWT_ISSUER                 # JWT issuer
JWT_AUDIENCE               # JWT audience
CLOUDINARY_CLOUD_NAME      # Cloudinary cloud name
CLOUDINARY_API_KEY         # Cloudinary API key
CLOUDINARY_API_SECRET      # Cloudinary API secret
MOMO_SECRET_KEY            # Momo payment secret
MOMO_ACCESS_KEY            # Momo payment access key
EMAIL_FROM                 # Email sender
EMAIL_PASSWORD             # Email app password
EMAIL_SMTP_SERVER          # SMTP server
EMAIL_PORT                 # SMTP port
OCR_API_KEY                # OpenAI API key
ZALO_ACCESS_TOKEN          # Zalo OA token
FRONTEND_BASE_URL          # Frontend URL
```

## ⚠️ Lưu Ý

1. **File .env.local không được commit** - đã có trong .gitignore
2. **Kiểm tra kỹ .gitignore** trước khi push code
3. **Sử dụng environment variables** thay vì file .env trên production (nếu có thể)
4. **Backup database** trước khi deploy
5. **Test trên staging** trước khi deploy production

## ✅ Sau Khi Deploy

- [ ] Kiểm tra logs để đảm bảo .env được load
- [ ] Test database connection
- [ ] Test các API endpoints
- [ ] Kiểm tra background services hoạt động
- [ ] Verify CORS settings phù hợp với production

