# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy csproj file và restore dependencies (tối ưu cache layer)
COPY *.csproj ./
RUN dotnet restore

# Copy tất cả source code
COPY . .

# Build project và publish vào thư mục /app/out
RUN dotnet publish -c Release -o out

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Cài đặt các dependencies cần thiết cho Tesseract OCR và System.Drawing
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
    libgdiplus \
    libc6-dev \
    tesseract-ocr \
    libtesseract-dev \
    && rm -rf /var/lib/apt/lists/*

# Copy output từ stage build
COPY --from=build /app/out .

# Expose port 80 và 443
EXPOSE 80
EXPOSE 443

# Set environment variable
ENV ASPNETCORE_URLS=http://+:80

# Chạy ứng dụng
ENTRYPOINT ["dotnet", "DoAnCoSo.dll"]

