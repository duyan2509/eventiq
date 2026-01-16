# Eventiq Backend - Documentation

Phần này chứa tài liệu chi tiết về kiến trúc và cài đặt Backend của hệ thống Eventiq.

## Kiến trúc Backend

Hệ thống được xây dựng theo kiến trúc Clean Architecture với các tầng độc lập:

### Eventiq.Api (Presentation Layer)
Tầng giao tiếp với client, chịu trách nhiệm:
- Nhận và xử lý HTTP requests
- Authentication và Authorization
- Validation input
- Mapping DTOs
- SignalR Hubs cho real-time communication
- Health checks

**Các Controller chính:**
- `AuthController` - Authentication và user management
- `EventController` - Quản lý sự kiện
- `OrganizationController` - Quản lý tổ chức
- `CheckoutController` - Xử lý đặt vé
- `PaymentController` - Xử lý thanh toán
- `CheckinController` - Check-in vé tại sự kiện
- `StaffController` - Quản lý nhân viên
- `PayoutController` - Quản lý thanh toán cho tổ chức
- `RevenueController` - Báo cáo doanh thu
- `TicketsController` - Quản lý vé
- `CustomerEventController` - Xem sự kiện cho khách hàng
- `AdminController` - Quản lý admin (duyệt sự kiện)

### Eventiq.Application (Application Layer)
Tầng nghiệp vụ, chứa business logic:
- **Services**: Xử lý các nghiệp vụ chính
  - `EventService` - Logic quản lý sự kiện
  - `OrganizationService` - Logic quản lý tổ chức
  - `CheckoutService` - Logic đặt vé
  - `PaymentService` - Logic thanh toán
  - `CheckinService` - Logic check-in
  - `StaffService` - Logic quản lý nhân viên
  - `RevenueService` - Logic báo cáo doanh thu
  - `TransferService` - Logic chuyển vé
  - `UserService` - Logic quản lý user

- **DTOs**: Data Transfer Objects cho việc truyền dữ liệu giữa các tầng
- **Interfaces**: Định nghĩa contracts cho repositories và services
- **Mappings**: AutoMapper profiles

### Eventiq.Domain (Domain Layer)
Tầng domain, chứa business entities và rules:
- **Entities**: Domain models
  - `Event`, `EventItem`, `EventAddress` - Sự kiện
  - `Organization` - Tổ chức
  - `Ticket`, `TicketClass` - Vé
  - `Chart`, `EventSeat`, `EventSeatState` - Sơ đồ ghế ngồi
  - `Checkout`, `Payment`, `Payout` - Thanh toán
  - `Staff`, `StaffInvitation`, `EventTask` - Nhân viên
  - `Checkin` - Check-in
  - `TransferRequest`, `VerifyRequest` - Chuyển và xác thực vé
  - `EventApprovalHistory` - Lịch sử duyệt sự kiện

- **Enums**: Domain enums (EventStatus, PaymentStatus, SeatStatus, etc.)
- **BaseEntity**: Base class với soft delete và audit fields

### Eventiq.Infrastructure (Infrastructure Layer)
Tầng hạ tầng, xử lý các vấn đề kỹ thuật:
- **Persistence**: 
  - `ApplicationDbContext` - EF Core DbContext
  - Repositories - Implementation của repository pattern
  - `UnitOfWork` - Transaction management

- **Identity**: 
  - `IdentityService` - User authentication và authorization
  - `ApplicationUser` - Custom user entity

- **Services**:
  - `SeatsIoService` - Integration với Seats.io API
  - `VnPayService` - Integration với VNPay
  - `CloudinaryStorageService` - Upload và quản lý hình ảnh
  - `RedisService` - Redis operations (seat locking, session)
  - `SmtpEmailService` - Email service
  - `EventProcessingWorker` - Background worker xử lý event approval

- **DependencyInjection**: Cấu hình dependency injection

### Eventiq.Migrations
Chứa các database migrations được tạo bởi EF Core.

## Server Backend Setup

### Yêu cầu hệ thống
- Docker và Docker Compose
- PostgreSQL 16
- Redis 7.2
- Nginx (cho production)

### Cài đặt Development với Docker Compose

1. **Clone repository**
```bash
git clone <repository-url>
cd eventiq/EventiqBE
```

2. **Cấu hình appsettings.json**
Tạo file `Eventiq.Api/appsettings.json` với nội dung:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=postgres;Port=5432;Database=evtDb;Username=postgres;Password=123456"
  },
  "Jwt": {
    "Key": "your-super-secret-key-with-at-least-32-characters",
    "Issuer": "Eventiq",
    "Audience": "Eventiq",
    "ExpiryInMinutes": 60
  },
  "SeedAdmin": {
    "Email": "admin@eventiq.com",
    "Password": "Admin@123"
  },
  "CloudinarySettings": {
    "CloudName": "your-cloud-name",
    "ApiKey": "your-api-key",
    "ApiSecret": "your-api-secret"
  },
  "SeatsIo": {
    "SecretKey": "your-seats-io-secret-key"
  },
  "VnPay": {
    "TmnCode": "YOUR_TMN_CODE",
    "HashSecret": "YOUR_HASH_SECRET",
    "PaymentUrl": "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html",
    "Version": "2.1.0",
    "Command": "pay",
    "CurrCode": "VND",
    "Locale": "vn",
    "HashType": "HmacSHA512"
  },
  "Redis": {
    "ConnectionString": "redis:6379"
  }
}
```

3. **Build và chạy với Docker Compose**
```bash
docker-compose up -d --build
```

Docker Compose sẽ tự động:
- Build và chạy API service trên port 5001
- Khởi động PostgreSQL trên port 5432
- Khởi động Redis trên port 6379
- Khởi động pgAdmin trên port 8080

4. **Chạy migrations**
```bash
# Vào container của API
docker exec -it <container-id> bash

# Chạy migrations
dotnet ef database update --project Eventiq.Migrations
```

5. **Kiểm tra health check**
```bash
curl http://localhost:5001/health
```

**Development Endpoints:**
- Swagger UI: `http://localhost:5001`
- Health check: `http://localhost:5001/health`
- SignalR Hub: `http://localhost:5001/hubs/notifications`
- pgAdmin: `http://localhost:8080`

### Cài đặt Production với Nginx và VPS Compose

1. **Chuẩn bị VPS**
```bash
# Cài đặt Docker và Docker Compose
sudo apt-get update
sudo apt-get install docker.io docker-compose -y
sudo systemctl start docker
sudo systemctl enable docker
```

2. **Clone repository trên VPS**
```bash
git clone <repository-url>
cd eventiq/EventiqBE
```

3. **Cấu hình SSL Certificate**
```bash
# Tạo thư mục cho SSL certificates
sudo mkdir -p /etc/ssl/certs/eventiq

# Copy SSL certificates vào thư mục
sudo cp eventiq.crt /etc/ssl/certs/eventiq/
sudo cp eventiq.key /etc/ssl/certs/eventiq/
sudo chmod 600 /etc/ssl/certs/eventiq/eventiq.key
```

4. **Cấu hình vps_compose.yaml**
Chỉnh sửa file `vps_compose.yaml` và cập nhật các environment variables:
```yaml
environment:
  # Database
  ConnectionStrings__DefaultConnection: Host=postgres;Port=5432;Database=evtDb;Username=postgres;Password=YOUR_SECURE_PASSWORD
  
  # Redis
  Redis__ConnectionString: redis:6379
  
  # JWT
  Jwt__Key: YOUR_SECURE_JWT_KEY_AT_LEAST_32_CHARACTERS
  Jwt__Issuer: Eventiq
  Jwt__Audience: EventiqUsers
  Jwt__ExpiryInMinutes: 60
  
  # Seed admin
  SeedAdmin__Email: admin@yourdomain.com
  SeedAdmin__Password: YOUR_SECURE_PASSWORD
  
  # External services
  CloudinarySettings__CloudName: your-cloud-name
  CloudinarySettings__ApiKey: your-api-key
  CloudinarySettings__ApiSecret: your-api-secret
  
  SeatsIo__SecretKey: your-seats-io-secret-key
  
  VnPay__TmnCode: YOUR_TMN_CODE
  VnPay__HashSecret: YOUR_HASH_SECRET
  
  Cors__VercelUrl: https://your-frontend-domain.com
```

5. **Cấu hình Nginx**
File `nginx/nginx.conf` đã được cấu hình sẵn với:
- HTTP to HTTPS redirect
- SSL/TLS configuration
- Reverse proxy cho API
- WebSocket support cho SignalR

Đảm bảo SSL certificates được đặt đúng đường dẫn: `/etc/ssl/certs/eventiq/`

6. **Build và chạy với vps_compose.yaml**
```bash
docker-compose -f vps_compose.yaml up -d --build
```

7. **Kiểm tra services**
```bash
# Kiểm tra containers đang chạy
docker ps

# Kiểm tra logs
docker-compose -f vps_compose.yaml logs -f api
docker-compose -f vps_compose.yaml logs -f nginx
```

8. **Chạy migrations**
```bash
# Vào container của API
docker exec -it eventiq-api bash

# Chạy migrations
dotnet ef database update --project Eventiq.Migrations
```

9. **Kiểm tra health check**
```bash
# Kiểm tra qua HTTPS
curl https://yourdomain.com/health
```

**Production Endpoints:**
- API: `https://yourdomain.com`
- Health check: `https://yourdomain.com/health`
- SignalR Hub: `https://yourdomain.com/hubs/notifications`

### Cấu hình Firewall

Đảm bảo mở các ports cần thiết:
```bash
# HTTP và HTTPS
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp

# Kiểm tra firewall status
sudo ufw status
```

### Backup và Restore Database

**Backup:**
```bash
docker exec eventiq-postgres pg_dump -U postgres evtDb > backup_$(date +%Y%m%d_%H%M%S).sql
```

**Restore:**
```bash
docker exec -i eventiq-postgres psql -U postgres evtDb < backup_file.sql
```



### Seed Data

Trong môi trường Development và Production, hệ thống tự động seed:
- Admin user (email và password từ `SeedAdmin` config)
- Roles (Admin, Organization, Customer)
