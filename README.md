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

Để xem chi tiết về kiến trúc Backend, cài đặt và cấu hình, vui lòng tham khảo [README Backend](EventiqBE/README.md).

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
