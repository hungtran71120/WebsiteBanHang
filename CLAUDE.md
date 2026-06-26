# CLAUDE.md

Tài liệu định hướng cho dự án **Hưng Store** (single-vendor e-commerce). Đọc file này trước khi bắt đầu bất kỳ session làm việc nào — nó chứa các quyết định đã chốt để không phải hỏi lại.

## 1. Tổng quan dự án

Website thương mại điện tử single-vendor lấy cảm hứng từ Shopee: xem sản phẩm, tìm kiếm, xem chi tiết, giỏ hàng, đặt hàng, đánh giá sản phẩm, có trang quản trị (Admin Dashboard). Mục tiêu chính là **portfolio để xin việc** — ưu tiên kiến trúc rõ ràng, code chuẩn, có test, dễ giải thích khi phỏng vấn.

## 2. Tech stack

| Layer | Công nghệ |
|---|---|
| Backend | .NET (bản LTS mới nhất khả dụng tại thời điểm setup — kiểm tra bằng `dotnet --list-sdks`) |
| ORM | Entity Framework Core (Code-First, Migrations) |
| Database | SQL Server (LocalDB khi dev, có thể đổi sang full SQL Server khi deploy) |
| Auth | ASP.NET Core Identity + JWT (access token + refresh token) |
| Validation | FluentValidation |
| Mapping | AutoMapper (hoặc mapping tay nếu đơn giản — quyết định khi code) |
| Test | xUnit, Moq, FluentAssertions, `WebApplicationFactory` (integration test) |
| API Docs | Swagger / OpenAPI |
| Frontend | Vue 3 + TypeScript + Vite |
| State management (frontend) | Pinia |
| HTTP client (frontend) | Axios |
| Lưu ảnh sản phẩm | Local filesystem (`wwwroot/uploads`) |
| Container | Không dùng Docker — chạy trực tiếp local |

## 3. Kiến trúc & quy ước code

### 3.1 Backend — Clean Architecture

```
/src
  /Domain          - Entities, Value Objects, Domain Exceptions, Repository interfaces (không phụ thuộc layer nào khác)
  /Application     - DTOs, Use-case services, Interfaces, FluentValidation validators
  /Infrastructure  - EF Core DbContext, Repository implementations, JWT service, File storage
  /API             - Controllers, Program.cs, Middleware, Swagger config
/tests
  /Domain.UnitTests
  /Application.UnitTests
  /API.IntegrationTests   (dùng WebApplicationFactory + DB test riêng)
/client                   - Vue 3 + TypeScript + Vite (customer-facing + route /admin)
```

**Dependency rule**: `Domain` không reference layer nào khác. `Application` chỉ reference `Domain`. `Infrastructure` implement interface định nghĩa ở `Application`/`Domain`. `API` chỉ biết `Application` (không gọi trực tiếp `Infrastructure` hay `Domain` entity ra ngoài — luôn qua DTO).

**Naming convention**:
- Entity: PascalCase, danh từ số ít (`Product`, `Category`, `Order`).
- Service interface: `I{Name}Service`, implementation: `{Name}Service`.
- Repository interface: `I{Entity}Repository`.
- DTO: `{Entity}Dto`, `Create{Entity}Request`, `Update{Entity}Request`.

**Error handling**: middleware tập trung (`ExceptionHandlingMiddleware`) bắt exception và trả về response lỗi chuẩn hóa (status code + message + traceId).

### 3.2 Frontend — Vue 3 SPA

- 1 project Vue duy nhất, không tách project Admin riêng.
- Route khách hàng: `/`, `/products`, `/products/:id`, `/cart`, `/checkout`, `/orders`...
- Route admin: nhóm dưới `/admin/*`, có route guard kiểm tra role `Admin` (redirect nếu không phải admin).
- Gọi API qua Axios instance dùng chung, có interceptor đính kèm JWT và xử lý refresh token.

## 4. Các quyết định đã chốt (không hỏi lại)

| Hạng mục | Quyết định |
|---|---|
| Mô hình kinh doanh | Single-vendor — 1 Admin quản lý toàn bộ sản phẩm, không có Seller/marketplace |
| Thanh toán | Giả lập (mock checkout — COD hoặc đánh dấu "đã thanh toán" giả). Không tích hợp cổng thanh toán thật ở giai đoạn này |
| Lưu ảnh | Local `wwwroot/uploads` khi dev; Azure Blob Storage khi deploy production (chọn tự động qua `Storage:AzureBlob:ConnectionString` — rỗng thì dùng Local, có giá trị thì dùng Blob) |
| Docker | Không dùng, chạy trực tiếp local |
| Testing | Bắt buộc viết unit test + integration test cho mọi phase, không được bỏ qua để "làm nhanh" |
| Admin Dashboard | Cùng 1 Vue app với route `/admin`, không tách project riêng |
| Roles | Chỉ có `Admin` và `Customer` |

## 5. Lộ trình theo Phase

Mỗi phase coi là **Done** khi: chức năng chạy được end-to-end (test bằng Swagger/UI) **và** có unit/integration test đi kèm **và** `PROGRESS.md` được cập nhật.

### Phase 0 — Khởi tạo & Scaffold kiến trúc
- Tạo solution .NET theo cấu trúc Clean Architecture ở trên.
- Cấu hình EF Core + connection string SQL Server, tạo `AppDbContext` rỗng.
- Cấu hình ASP.NET Core Identity + JWT scaffold (chưa cần đủ logic, chỉ setup).
- Khởi tạo project Vue 3 + TS bằng Vite trong `/client`, cấu hình router, Pinia, Axios instance.
- Khởi tạo 3 test project (`Domain.UnitTests`, `Application.UnitTests`, `API.IntegrationTests`).
- Cấu hình Swagger.
- `git init`, `.gitignore` phù hợp cho .NET + Node.

### Phase 1 — Authentication & User Management
- Đăng ký, đăng nhập, JWT access token + refresh token.
- Roles `Admin` / `Customer`, phân quyền bằng `[Authorize(Roles=...)]`.
- Hồ sơ người dùng: tên, sđt, địa chỉ giao hàng.
- Unit test cho Application service, integration test cho API auth endpoints.

### Phase 2 — Product Catalog
- Entity `Category` (hỗ trợ phân cấp cha-con), `Product` (tên, giá, mô tả, ảnh, tồn kho, danh mục).
- API: danh sách sản phẩm có phân trang + filter theo category/khoảng giá, tìm kiếm theo từ khóa, chi tiết sản phẩm.
- Upload ảnh sản phẩm lưu local (chỉ Admin).
- Frontend: trang danh sách sản phẩm (filter, search, phân trang), trang chi tiết sản phẩm.
- Test cho service tìm kiếm/filter và các endpoint.

### Phase 3 — Cart & Checkout/Orders
- Entity `Cart`/`CartItem` lưu theo user; API thêm/sửa số lượng/xóa item.
- Flow checkout: chọn địa chỉ giao hàng → tạo `Order` từ cart → mock trạng thái thanh toán.
- Trạng thái đơn hàng: `Pending → Confirmed → Shipped → Delivered`, hoặc `Cancelled`.
- Frontend: trang giỏ hàng, trang checkout, trang lịch sử đơn hàng + chi tiết đơn.
- Test cho luồng tạo order (trừ tồn kho, validate giỏ hàng rỗng, v.v.).

### Phase 4 — Review & Rating
- Entity `Review` (rating 1-5 + comment) gắn với `Product` + `User`.
- API: tạo review, lấy danh sách review theo sản phẩm, tính rating trung bình.
- Frontend: hiển thị sao trung bình + danh sách review ở trang chi tiết sản phẩm, form gửi review.
- Test cho tính rating trung bình và validate (ví dụ không cho review trùng).

### Phase 5 — Admin Dashboard
- Route `/admin/*` với route guard role Admin.
- CRUD Category, CRUD Product (kèm upload ảnh) qua UI thay vì Swagger.
- Quản lý Order: xem danh sách, đổi trạng thái.
- Quản lý User: danh sách, khóa/mở tài khoản.
- Thống kê cơ bản: tổng doanh thu, số đơn hàng theo trạng thái (chart đơn giản).
- Test cho các endpoint quản trị (đảm bảo chỉ Admin gọi được).

### Phase 6 — Backlog / Future (ghi nhận, chưa làm trong các phase trên)
Các chức năng Shopee thật có nhưng được hoãn lại có chủ đích, không phải bị quên:
- Voucher / mã giảm giá
- Wishlist / sản phẩm yêu thích
- Flash sale / khung giờ vàng
- Tích hợp cổng thanh toán thật (VNPay/Momo/ZaloPay)
- Notification (email/push)
- Multi-vendor marketplace (nhiều Seller)
- Chat giữa khách và shop
- Recommendation engine / sản phẩm gợi ý

## 6. Cách chạy dự án local

Yêu cầu máy đã có: .NET 10 SDK, Node.js, và một SQL Server instance (full SQL Server hoặc LocalDB — sửa `ConnectionStrings:DefaultConnection` trong `src/API/appsettings.json` cho đúng instance của bạn).

```
# Backend (chạy từ thư mục root)
dotnet ef database update --project src/Infrastructure/HungStore.Infrastructure.csproj --startup-project src/API/HungStore.API.csproj
dotnet run --project src/API/HungStore.API.csproj
# Swagger UI: https://localhost:<port>/swagger
# Health check: https://localhost:<port>/health

# Frontend
cd client
npm install
npm run dev
```

Chạy test:
```
dotnet test
```

Tạo migration mới khi thêm/sửa entity (chạy từ thư mục root):
```
dotnet ef migrations add <TenMigration> --project src/Infrastructure/HungStore.Infrastructure.csproj --startup-project src/API/HungStore.API.csproj --output-dir Persistence/Migrations
```

## 7. Theo dõi tiến độ

Xem `PROGRESS.md` để biết phase nào đang làm, task nào đã xong.
