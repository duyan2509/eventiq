# Eventiq - Hệ thống quản lý sự kiện và bán vé trực tuyến

Eventiq là một hệ thống quản lý sự kiện và bán vé trực tuyến, cho phép các tổ chức tạo và quản lý sự kiện, bán vé, quản lý nhân viên, và xử lý thanh toán. Hệ thống tích hợp với Seats.io để quản lý sơ đồ ghế ngồi và VNPay để xử lý thanh toán.

## Hosting Information

- **VPS Hosting**: 07/01/2026 - 13/01/2026
- **Render Hosting**: từ 14/01/2026 - now (Note: chờ backend start 2-3p)

## Tech Stack

### Backend Framework
- **.NET 9.0** - Framework chính cho backend API
- **ASP.NET Core Web API** - RESTful API framework
- **Entity Framework Core 9.0** - ORM cho database operations
- **PostgreSQL 16** - Database chính
- **Redis 7.2** - Caching và session management

### Authentication & Authorization
- **ASP.NET Core Identity** - User management
- **JWT Bearer Authentication** - Token-based authentication
- **Policy-based Authorization** - Role và permission management

### External Services Integration
- **Seats.io** - Quản lý sơ đồ ghế ngồi và đặt chỗ
- **VNPay** - Cổng thanh toán trực tuyến
- **Cloudinary** - Lưu trữ và quản lý hình ảnh
- **Mailtrap** - Email service (development)

### Real-time Communication
- **SignalR** - Real-time notifications và updates

### Logging & Monitoring
- **Serilog** - Structured logging với file và console sinks

### Other Libraries
- **AutoMapper** - Object mapping
- **Swagger/OpenAPI** - API documentation
- **Docker** - Containerization

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

## Use Cases

### Quản lý Tổ chức
- **Đăng ký tổ chức**: User đăng ký tài khoản và tạo tổ chức
- **Quản lý thông tin tổ chức**: Cập nhật tên, avatar, thông tin ngân hàng
- **Xem danh sách sự kiện**: Xem tất cả sự kiện của tổ chức

### Quản lý Sự kiện
- **Tạo sự kiện**: Tạo sự kiện mới với thông tin cơ bản (tên, mô tả, banner, thời gian, địa chỉ)
- **Tạo phiên sự kiện**: Tạo các phiên/buổi cho sự kiện
- **Tạo sơ đồ ghế ngồi**: Tạo và cấu hình sơ đồ ghế ngồi từ Seats.io
- **Tạo loại vé**: Định nghĩa các loại vé với giá và số lượng
- **Gửi duyệt sự kiện**: Submit sự kiện để admin duyệt
- **Xem chi tiết sự kiện**: Xem thông tin đầy đủ của sự kiện
- **Chỉnh sửa sự kiện**: Cập nhật thông tin sự kiện (chỉ khi ở trạng thái Draft)
- **Xóa sự kiện**: Soft delete sự kiện
- **Đồng bộ ghế từ Seats.io**: Đồng bộ dữ liệu ghế từ Seats.io vào database

### Duyệt Sự kiện (Admin)
- **Xem danh sách sự kiện chờ duyệt**: Xem các sự kiện có status Pending
- **Duyệt sự kiện**: Admin duyệt sự kiện, hệ thống tự động:
  - Lấy dữ liệu ghế từ Seats.io
  - Đưa vào queue để xử lý bất đồng bộ
  - Background worker bulk insert ghế vào database
  - Cập nhật status từ InProgress sang Published khi hoàn thành
- **Từ chối sự kiện**: Admin từ chối sự kiện với lý do
- **Xem lịch sử duyệt**: Xem lịch sử các lần duyệt/từ chối

### Đặt vé và Thanh toán (Customer)
- **Xem danh sách sự kiện công khai**: Xem các sự kiện đã được publish
- **Xem chi tiết sự kiện**: Xem thông tin sự kiện, sơ đồ ghế, giá vé
- **Chọn ghế**: Chọn ghế từ sơ đồ ghế ngồi
- **Tạo checkout session**: Tạo phiên đặt vé, lock ghế trên Redis và Seats.io
- **Thanh toán**: Tạo payment URL từ VNPay và redirect đến cổng thanh toán
- **Xử lý callback thanh toán**: Xử lý IPN callback từ VNPay:
  - Xác thực payment
  - Tạo tickets
  - Cập nhật EventSeatState từ Free sang Paid
  - Book ghế trên Seats.io
  - Tạo/update Payout record
- **Xem vé của mình**: Xem danh sách vé đã mua
- **Hủy checkout**: Hủy phiên đặt vé nếu không thanh toán

### Quản lý Nhân viên
- **Mời nhân viên**: Tổ chức mời user làm nhân viên cho sự kiện
- **Chấp nhận/từ chối lời mời**: User phản hồi lời mời
- **Tạo nhiệm vụ**: Tạo các nhiệm vụ cho sự kiện (ví dụ: Gate A, Gate B)
- **Phân công nhiệm vụ**: Gán nhân viên vào các nhiệm vụ cụ thể
- **Xem danh sách nhân viên**: Xem tất cả nhân viên của sự kiện

### Check-in
- **Check-in vé**: Nhân viên quét mã vé để check-in
- **Xác thực vé**: Xác thực vé bằng OTP (nếu cần)
- **Xem lịch sử check-in**: Xem lịch sử check-in của vé

### Chuyển vé
- **Yêu cầu chuyển vé**: Người sở hữu vé yêu cầu chuyển cho người khác
- **Chấp nhận/từ chối chuyển vé**: Người nhận phản hồi yêu cầu chuyển vé
- **Xem lịch sử chuyển vé**: Xem lịch sử các lần chuyển vé

### Thanh toán cho Tổ chức
- **Xem danh sách payout**: Tổ chức xem các khoản thanh toán
- **Thanh toán thủ công**: Admin thực hiện thanh toán cho tổ chức
- **Upload bằng chứng**: Admin upload ảnh bằng chứng chuyển khoản
- **Cập nhật trạng thái**: Đánh dấu payout đã thanh toán

### Báo cáo Doanh thu
- **Xem báo cáo doanh thu**: Tổ chức xem báo cáo doanh thu theo thời gian
- **Xem chi tiết thanh toán**: Xem chi tiết các giao dịch thanh toán
- **Xuất báo cáo**: Xuất báo cáo dưới dạng file (nếu có)

### Quản lý User
- **Đăng ký**: User đăng ký tài khoản mới
- **Đăng nhập**: Xác thực và nhận JWT token
- **Đăng xuất**: Invalidate token
- **Quản lý profile**: Cập nhật thông tin cá nhân
- **Ban/Unban user**: Admin ban hoặc unban user

### Real-time Notifications
- **Notification khi sự kiện được duyệt**: Tổ chức nhận notification khi sự kiện được duyệt
- **Notification khi có thanh toán**: User nhận notification khi thanh toán thành công
- **Notification khi có lời mời nhân viên**: User nhận notification khi được mời làm nhân viên
