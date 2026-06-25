# PROGRESS.md

Theo dõi tiến độ dự án **Shopee Clone**. Cập nhật trạng thái `[ ]` / `[~]` / `[x]` sau mỗi buổi làm việc. Xem `CLAUDE.md` để biết chi tiết quyết định kiến trúc và phạm vi từng phase.

## Trạng thái hiện tại

- **Phase đang làm**: Phase 6 (Backlog) — đã làm xong "Chat giữa khách và shop", "Notification (email/push)", "Wishlist / sản phẩm yêu thích", "Recommendation engine", "Voucher / mã giảm giá", "Flash sale / khung giờ vàng" (6/8 mục). Còn lại: "Tích hợp cổng thanh toán thật" (chưa làm) và "Multi-vendor marketplace" (mâu thuẫn quyết định single-vendor ở `CLAUDE.md` §4, cần xác nhận lại kiến trúc trước khi làm, không tự ý đổi)
- **Cập nhật gần nhất**: 2026-06-23

---

## Phase 0 — Khởi tạo & Scaffold kiến trúc

- Bắt đầu: 2026-06-19
- Hoàn thành: 2026-06-19

- [x] Tạo solution .NET theo cấu trúc Clean Architecture (`Domain`, `Application`, `Infrastructure`, `API`)
- [x] Cấu hình EF Core + connection string SQL Server, tạo `AppDbContext` rỗng (IdentityDbContext)
- [x] Cấu hình ASP.NET Core Identity + JWT scaffold (AddIdentity + AddAuthentication/AddJwtBearer trong `Infrastructure/DependencyInjection.cs`)
- [x] Khởi tạo project Vue 3 + TS (Vite) trong `/client`, cấu hình router + Pinia + Axios
- [x] Khởi tạo 3 test project: `Domain.UnitTests`, `Application.UnitTests`, `API.IntegrationTests`
- [x] Cấu hình Swagger (Swashbuckle, hỗ trợ nhập Bearer token)
- [x] `git init` + `.gitignore` cho .NET + Node

**Ghi chú / vấn đề phát sinh**:
- .NET 10 (SDK 10.0.300) là bản LTS mới nhất hiện có trên máy, dùng làm TargetFramework `net10.0`.
- Microsoft.OpenApi v2 (kéo theo bởi Swashbuckle.AspNetCore 10.x) đổi namespace/API: dùng `Microsoft.OpenApi` (không còn `.Models`) và `OpenApiSecuritySchemeReference` thay cho `OpenApiSecurityScheme.Reference` khi cấu hình Bearer security requirement.
- Đã thêm route `/health` (health checks) và 1 integration test smoke test (`HealthCheckTests`) để xác nhận `WebApplicationFactory` + DI graph khởi động được — `dotnet build` và `dotnet test` đều pass.
- `dotnet new sln` ở .NET 10 sinh ra file `ShopeeClone.slnx` (format XML mới) thay vì `.sln` — vẫn hoạt động bình thường với `dotnet build/test`.
- Domain.UnitTests và Application.UnitTests chưa có test case nào (chưa có code nghiệp vụ để test) — sẽ bổ sung ngay khi viết logic ở Phase 1.
- `dotnet ef` cần package `Microsoft.EntityFrameworkCore.Design` ở cả startup project (`API`) lẫn project chứa DbContext (`Infrastructure`) mới chạy được lệnh migration.
- Máy dev không có LocalDB, chỉ có SQL Server instance đầy đủ (`MSSQLSERVER`) → đã đổi connection string sang `Server=.` thay vì `(localdb)\MSSQLLocalDB`. Nếu chuyển máy khác, kiểm tra lại connection string cho phù hợp.
- Đã tạo migration `InitialIdentitySchema`, chạy `dotnet ef database update` thành công, tạo database `ShopeeCloneDb` với các bảng AspNetUsers/AspNetRoles... Đã `dotnet run` thử và gọi `GET /health` trả về `Healthy` — xác nhận backend chạy được end-to-end.
- `dotnet-ef` global tool đang là bản 8.0.8, cũ hơn runtime EF Core 10.0.9 (chỉ cảnh báo, không lỗi). Có thể cập nhật bằng `dotnet tool update -g dotnet-ef` nếu muốn hết warning.
- Frontend: `npm run build` (gồm `vue-tsc -b` type-check + `vite build`) chạy thành công, không lỗi TypeScript.

---

## Phase 1 — Authentication & User Management

- Bắt đầu: 2026-06-19
- Hoàn thành: 2026-06-19

- [x] Đăng ký tài khoản (`POST /api/auth/register`, tự gán role `Customer`)
- [x] Đăng nhập, phát JWT access token + refresh token (`POST /api/auth/login`, `POST /api/auth/refresh-token` với rotation + revoke-on-reuse, `POST /api/auth/logout`)
- [x] Phân quyền Admin / Customer (role claim trong JWT, `[Authorize]`/`[Authorize(Roles=...)]` sẵn sàng dùng từ Phase 2)
- [x] Hồ sơ người dùng (tên, sđt, địa chỉ giao hàng) — `GET /api/users/me`, `PUT /api/users/me`
- [x] Unit test cho Application service (`AuthServiceTests`, `UserServiceTests` — 8 test case, dùng Moq)
- [x] Integration test cho API auth endpoints (`AuthControllerTests` — register→login→get profile, sai mật khẩu, refresh-token rotation, truy cập không token)

**Ghi chú / vấn đề phát sinh**:
- Kiến trúc theo đúng dependency rule: `IIdentityService`/`ITokenService` định nghĩa ở Application, implement ở Infrastructure (`Infrastructure/Identity/IdentityService.cs`, `TokenService.cs`) để Application không phụ thuộc trực tiếp `UserManager`/ASP.NET Core Identity.
- `RefreshToken` đặt ở Domain (`Domain/Entities/RefreshToken.cs`) chỉ chứa `UserId` (string) thay vì navigation tới `ApplicationUser`, giữ Domain không phụ thuộc Infrastructure.Identity. Áp dụng rotation: mỗi lần refresh sẽ revoke token cũ và phát token mới (`ReplacedByToken`), refresh token cũ dùng lại sẽ bị từ chối — đã verify qua test và thử tay (401).
- Thêm `FluentValidation` + `FluentValidation.DependencyInjectionExtensions` (v12.1.1) cho `RegisterRequestValidator`, `LoginRequestValidator`, `UpdateProfileRequestValidator`; validate thủ công trong controller qua `IValidator<T>` (không dùng `FluentValidation.AspNetCore` vì package đó đã deprecated).
- Seed roles `Admin`/`Customer` + 1 tài khoản Admin mặc định (dev-only, email/password đọc từ `appsettings.json` section `SeedAdmin`) khi app khởi động (`Infrastructure/Persistence/SeedData.cs`, gọi từ `Program.cs` sau `builder.Build()`). Đã xác nhận đăng nhập bằng tài khoản admin seed trả về JWT có role `Admin`.
- `API.IntegrationTests` dùng `CustomWebApplicationFactory` riêng (không phải `WebApplicationFactory<Program>` trực tiếp như `HealthCheckTests`) để trỏ sang DB test riêng (`ShopeeCloneDb_Test`, đúng như định hướng "DB test riêng" trong `CLAUDE.md`). Phải tạo/migrate DB test **trước khi** gọi `base.CreateHost()` (override `CreateHost`, tạo `AppDbContext` tạm bằng `DbContextOptionsBuilder` riêng) — nếu làm sau thì `Program.cs` đã tự gọi `SeedData.SeedAsync` lúc host start và fail vì DB chưa tồn tại.
- Xóa `WeatherForecastController.cs`/`WeatherForecast.cs` (template mặc định của `dotnet new webapi`, không còn cần từ Phase 1).
- Đã tạo migration `AddRefreshTokenAndSeedRoles` (chỉ thêm bảng `RefreshTokens`; seed role/user chạy ở code lúc startup, không qua migration) và áp thành công vào `ShopeeCloneDb`.
- Verify thủ công qua curl: register → login → `GET /api/users/me` (200, đúng thông tin) → không token (401) → refresh-token (200, token cũ dùng lại bị 401) → logout (204) → dùng lại refresh token đã logout (401) → đăng nhập tài khoản admin seed (role `Admin` trong JWT). Toàn bộ `dotnet build`, `dotnet test` (13/13 pass), `npm run build` đều pass.
- Theo xác nhận của user: Phase 1 chỉ làm backend (test qua Swagger/curl), chưa làm trang Login/Register/Profile ở frontend Vue — sẽ làm khi cần ở phase sau.

---

## Phase 2 — Product Catalog

- Bắt đầu: 2026-06-19
- Hoàn thành: 2026-06-19

- [x] Entity `Category` (phân cấp cha-con qua `ParentCategoryId` tự tham chiếu)
- [x] Entity `Product` (giá, mô tả, ảnh, tồn kho, danh mục)
- [x] API danh sách sản phẩm: phân trang + filter category/giá (`GET /api/products?categoryId=&minPrice=&maxPrice=&page=&pageSize=`)
- [x] API tìm kiếm theo từ khóa (`keyword` filter trên `Name`)
- [x] API chi tiết sản phẩm (`GET /api/products/{id}`)
- [x] Upload ảnh sản phẩm (local `wwwroot/uploads/products`, chỉ Admin — `POST /api/products/{id}/image`)
- [x] Frontend: trang danh sách sản phẩm (filter, search, phân trang) — `ProductListView.vue`
- [x] Frontend: trang chi tiết sản phẩm — `ProductDetailView.vue`
- [x] Test cho service tìm kiếm/filter + endpoint (`ProductServiceTests`, `CategoryServiceTests`, `ProductsControllerTests`, `CategoriesControllerTests` — 18 test mới)

**Ghi chú / vấn đề phát sinh**:
- Giữ đúng dependency rule như Phase 1: `ICategoryRepository`/`IProductRepository` định nghĩa ở Domain, implement ở Infrastructure; `ICategoryService`/`IProductService` ở Application chỉ gọi qua repository interface, không đụng `AppDbContext` trực tiếp.
- Upload ảnh: `IFileStorageService` ở Application nhận `Stream`/`contentType` (không dùng `IFormFile`) để Application không phụ thuộc ASP.NET Core MVC; `LocalFileStorageService` (Infrastructure) nhận `webRootPath` qua tham số DI thay vì `IWebHostEnvironment` trực tiếp — tránh phải thêm `FrameworkReference` vào project Infrastructure (vốn dùng `Microsoft.NET.Sdk` thường, không phải `Sdk.Web`). `AddInfrastructureServices` đổi signature thêm tham số `webRootPath`, gọi từ `Program.cs` bằng `builder.Environment.WebRootPath`.
- Đây là lần đầu frontend (Vite dev, port 5173) gọi thật API (port 5039) — phải thêm CORS (`Cors:AllowedOrigins` trong `appsettings.json`, mặc định `http://localhost:5173`) và `app.UseStaticFiles()` để serve ảnh qua `/uploads/...`. Đã verify bằng `curl` với header `Origin: http://localhost:5173` thấy `Access-Control-Allow-Origin` trả đúng, cả cho preflight (`OPTIONS`) và request thật.
- `client/.env` (`VITE_API_BASE_URL`) trước đó trỏ sai cổng (`https://localhost:5001`) từ lúc scaffold Phase 0 — sửa thành `http://localhost:5039/api` khớp với `applicationUrl` thật của launch profile `http` trong `launchSettings.json`.
- Ảnh sản phẩm trả về dạng URL tương đối (`/uploads/products/xxx.jpg`) từ API gốc (khác origin với Vite dev) — frontend cần ghép với origin của API (`resolveImageUrl` trong `client/src/utils/url.ts`, tách từ `VITE_API_BASE_URL` bỏ hậu tố `/api`), không thể dùng trực tiếp làm `src` ảnh.
- Test tích hợp: 3 lớp test cùng dùng `CustomWebApplicationFactory` (cùng tên DB `ShopeeCloneDb_Test`) ban đầu bị lỗi "Database already exists" do xUnit chạy các class test song song, mỗi class tự `EnsureDeleted()`/`Migrate()` độc lập gây race condition. Fix: gom tất cả vào 1 `[Collection("IntegrationTests")]` (`IntegrationTestCollection.cs`) để dùng chung 1 instance factory và chạy tuần tự.
- Xóa Category bị chặn (400) nếu còn category con hoặc còn sản phẩm thuộc category đó — không cascade ngầm, đúng quyết định trong kế hoạch.
- Frontend Phase 2 chỉ có trang khách hàng (danh sách + chi tiết sản phẩm) bằng CSS thuần tự viết, không thêm UI library — theo xác nhận của user. Không có trang quản trị Category/Product (CRUD qua UI là việc của Phase 5); Admin tạo dữ liệu test qua Swagger/curl.
- Verify thủ công qua curl: tạo category/product bằng tài khoản admin seed (kể cả tên tiếng Việt có dấu — xác nhận lưu/trả về UTF-8 đúng, lỗi hiển thị `?` chỉ là do encoding của terminal Git Bash trên Windows, không phải lỗi server), filter theo keyword + category + khoảng giá đều đúng, upload ảnh trả về `imageUrl` và ảnh truy cập được qua `/uploads/...` (200), customer thường gọi `POST /api/products` bị 403. `dotnet build`, `dotnet test` (31/31 pass), `npm run build` đều pass. Không thể click-test trực tiếp trên trình duyệt thật trong môi trường này — đã verify gián tiếp qua CORS thật (preflight + request có header `Origin`) và type-check/build sạch của `ProductListView.vue`/`ProductDetailView.vue`.

**Cập nhật bổ sung (cùng ngày 2026-06-19) — Redesign UI theo mẫu Shopee**:
- Theo yêu cầu của user, redesign toàn bộ giao diện khách hàng cho giống trang category/listing thật của Shopee (tham khảo ảnh `shopeevn.png` user cung cấp): thêm `AppHeader.vue` (top bar + logo + search + cart) và `AppFooter.vue` (cột liên kết), wire vào `App.vue`; redesign `ProductListView.vue` theo layout sidebar (cây danh mục + filter khoảng giá kiểu Shopee) + tabs sắp xếp + grid sản phẩm + pagination; redesign `ProductDetailView.vue` theo layout breadcrumb + price box + mô tả. Thay toàn bộ `style.css` (vốn là CSS mặc định của `create vite`) bằng theme màu Shopee (`--shopee-orange: #ee4d2d`...).
- Có chủ đích **bỏ qua** một số phần của mẫu Shopee không phù hợp với phạm vi dự án: dải logo "Shopee Mall"/brand (dự án là single-vendor, không có Seller/marketplace theo `CLAUDE.md`), badge giảm giá/% sale và số lượt "đã bán" trên thẻ sản phẩm (không có dữ liệu discount/sold thật, tránh tạo UI giả), link "Đăng Nhập/Đăng Ký" ở header chỉ là text tĩnh chứ không phải link thật (chưa làm trang Login/Register ở frontend theo quyết định Phase 1).
- Thêm tính năng sắp xếp sản phẩm (`ProductSortBy`: Default/Newest/PriceAsc/PriceDesc) vào `IProductRepository.GetPagedAsync`, `ProductFilterRequest`, `ProductRepository` để tab "Sắp xếp theo" trên UI có chức năng thật (không phải nút giả) — đã cập nhật mock trong `ProductServiceTests` theo signature mới, verify thứ tự bằng curl với nhiều mức giá khác nhau.
- Phát hiện và sửa: `client/src/layouts/AppHeader.vue` search box ban đầu không đồng bộ với `route.query.keyword` khi vào trang qua URL/link — thêm `watch` để đồng bộ hai chiều.
- Có thể chạy headless Chrome thật trên máy này (`C:\Program Files\Google\Chrome\Application\chrome.exe --headless --screenshot=...`) để chụp ảnh trang đang chạy ở Vite dev server và so sánh trực tiếp với ảnh mẫu — đã dùng cách này để phát hiện & xác nhận sửa: (1) ảnh sản phẩm ban đầu hiển thị icon vỡ vì ảnh test trước đó chỉ là 10 byte giả (không phải JPEG thật), không phải lỗi code; (2) dữ liệu category bị mã hóa sai do test qua curl/bash không UTF-8 — đã sửa lại tên category qua API. Lưu ý cho các lần sau: `--screenshot` cần kèm `--virtual-time-budget=5000` (hoặc cao hơn) để đợi Vue fetch dữ liệu async xong trước khi chụp, nếu không ảnh sẽ trống.

**Cập nhật bổ sung (2026-06-21) — Skeleton loading + infinite scroll (UI thuần frontend, không đổi API)**:
- Thay toàn bộ trạng thái loading dạng chữ "Đang tải..." bằng skeleton (khung xám shimmer) ở 6 trang khách hàng có gọi API: `ProductListView`, `ProductDetailView`, `CartView`, `CheckoutView`, `OrderHistoryView`, `OrderDetailView`. Hiệu ứng shimmer dùng chung 1 class `.skeleton-block` + `@keyframes skeleton-shimmer` khai báo trong `client/src/style.css` (global, không scoped) để 6 file không phải lặp lại cùng đoạn CSS animation.
- Theo phản hồi của user ("Shopee không chia page, cứ lăn chuột là load thêm"), bỏ pagination dạng số trang ở `ProductListView.vue`, thay bằng infinite scroll: dùng `IntersectionObserver` theo dõi 1 `div.scroll-sentinel` ở cuối danh sách, lăn tới gần đó thì tự gọi trang kế tiếp và nối thêm vào `products` (không thay thế danh sách cũ). Đổi sang đường: `resetAndLoad()` (dùng khi đổi filter/category/sort, reset về trang 1) và `loadMore()` (dùng khi sentinel intersect, cộng dồn trang tiếp theo) — bỏ hẳn `goToPage`/nút số trang. Lỗi khi tải thêm (`loadMoreErrorMessage`) hiển thị riêng, không che mất danh sách đã tải trước đó (khác với lỗi tải lần đầu `errorMessage` vẫn thay cả danh sách).
- `dotnet build`/`dotnet test` không đổi (chỉ sửa frontend); `npm run build` pass. Verify qua headless Chrome: chụp với `--virtual-time-budget` thấp (~1) bắt được đúng khung skeleton lúc đang fetch; chụp với viewport cao + budget cao xác nhận cascade tự tải hết 31/31 sản phẩm và hiện "Đã hiển thị tất cả sản phẩm." khi hết trang.

---

## Phase 3 — Cart & Checkout/Orders

- Bắt đầu: 2026-06-20
- Hoàn thành: 2026-06-20

- [x] Entity `Cart` / `CartItem` theo user
- [x] API thêm/sửa/xóa item trong giỏ (`GET/POST /api/cart`, `POST /api/cart/items`, `PUT/DELETE /api/cart/items/{cartItemId}` — đổi từ `{productId}` sang `{cartItemId}` khi bổ sung ProductVariant, xem ghi chú bổ sung dưới)
- [x] Flow checkout: chọn địa chỉ → tạo `Order` từ cart → mock trạng thái thanh toán (`POST /api/orders`)
- [x] Trạng thái đơn hàng (Pending/Confirmed/Shipped/Delivered/Cancelled) — `OrderStatus` enum, Order luôn tạo ở `Pending`; đổi trạng thái (Admin xác nhận/giao hàng) để dành cho Phase 5
- [x] Frontend: trang giỏ hàng (`CartView.vue`)
- [x] Frontend: trang checkout (`CheckoutView.vue`)
- [x] Frontend: trang lịch sử đơn hàng + chi tiết đơn (`OrderHistoryView.vue`, `OrderDetailView.vue`)
- [x] Test luồng tạo order (trừ tồn kho, validate giỏ hàng rỗng...) — `OrderServiceTests`, `OrdersControllerTests`

**Ghi chú / vấn đề phát sinh**:
- Giỏ hàng gắn theo user đã đăng nhập (đúng quyết định trong `CLAUDE.md`) nên Phase này là **lần đầu tiên cần trang Đăng Nhập/Đăng Ký thật ở frontend** — API đã có từ Phase 1, chỉ thiếu UI. Thêm `LoginView.vue`/`RegisterView.vue`, `api/auth.ts` (wrapper mới), và `AppHeader.vue` đổi từ text tĩnh "Đăng Nhập/Đăng Ký" sang `RouterLink` thật + nút "Đăng Xuất" có tác dụng.
- Phát hiện và sửa lỗi tồn tại từ Phase 1: `stores/auth.ts` chỉ persist `accessToken` vào localStorage, không persist `user` → sau khi F5 trang, `user` bị mất nên tên hiển thị/`isAdmin` sai. Thêm action `hydrate()` gọi `GET /api/users/me` khi có token nhưng chưa có `user` (gọi từ `App.vue` lúc khởi động).
- `AuthUser` (frontend) thiếu field `phoneNumber`/`address` so với `UserDto` (backend) — bổ sung để `CheckoutView` đọc địa chỉ mặc định từ hồ sơ user.
- **Đơn giản hóa có chủ đích**: trừ tồn kho + tạo `Order` + xóa cart thực hiện tuần tự qua các repository method có sẵn (mỗi method tự `SaveChangesAsync`), không dùng transaction/Unit-of-Work — giữ đúng pattern đơn giản của Phase 1/2, phù hợp quy mô demo/portfolio. Rủi ro: nếu một bước giữa đường lỗi, có thể để lại trạng thái không nhất quán (ví dụ trừ tồn kho rồi nhưng tạo Order thất bại) — chấp nhận được ở giai đoạn này, không phải production thật.
- **Bug thực phát hiện qua kiểm thử curl thủ công** (không phải qua test tự động, vì test client và server dùng cùng default JSON options nên che lỗi): `CreateOrderRequest.PaymentMethod` là enum, nhưng `Program.cs` chưa cấu hình `JsonStringEnumConverter` cho System.Text.Json — mặc định STJ chỉ nhận enum dưới dạng số khi đọc JSON body, nên frontend gửi `"paymentMethod":"MockPaid"` (string) sẽ bị 400. Đã thêm `.AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))` vào `AddControllers()` trong `Program.cs`. Đây là nhắc nhở: **luôn verify bằng JSON string thực tế giống frontend gửi, không chỉ tin tưởng test tự động nếu test client serialize cùng kiểu mặc định với server**.
- Đã verify toàn bộ flow qua curl trên server thật: đăng ký → thêm sản phẩm vào giỏ → đặt hàng (`MockPaid`) → tồn kho giảm đúng số lượng → giỏ hàng tự xóa sau khi đặt → đơn hàng xuất hiện trong lịch sử → đặt lại với giỏ trống bị chặn (400) → user khác không xem được đơn hàng của user này (404). Đã chụp ảnh `/login` và trang chi tiết sản phẩm (có ô chọn số lượng + nút "Thêm Vào Giỏ") qua headless Chrome để xác nhận UI thật render đúng; route guard `requiresAuth` xác nhận chuyển hướng đúng sang `/login` khi vào `/cart` lúc chưa đăng nhập.
- `dotnet build`, `dotnet test` (52/52 pass — 31 unit + 21 integration), `npm run build` đều pass.

**Cập nhật bổ sung (2026-06-20) — Tùy chọn màu sắc (ProductVariant) cho sản phẩm**:
- Theo yêu cầu của user thêm option chọn màu ở trang chi tiết sản phẩm, và xác nhận qua `AskUserQuestion` muốn **tồn kho riêng theo từng màu** (không phải chỉ hiển thị) — đây là thay đổi kiến trúc thật: entity `ProductVariant` mới (`ProductId`, `ColorName`, `ImageUrl?`, `Stock`), quan hệ 1-nhiều với `Product` (cascade delete).
- `CartItem` thêm `ProductVariantId` (Guid?, FK Restrict); unique index đổi từ `(CartId, ProductId)` → `(CartId, ProductId, ProductVariantId)` để 2 màu của cùng sản phẩm là 2 dòng giỏ riêng. `OrderItem` thêm `ColorName` (string?) — snapshot tên màu lúc mua, theo đúng pattern snapshot đã dùng cho `ProductName`/`UnitPrice`.
- **Hệ quả phải sửa Cart đã có ở Phase 3**: vì 1 sản phẩm có thể có nhiều dòng giỏ hàng (mỗi màu 1 dòng), không thể dùng `productId` định danh dòng giỏ cho update/remove nữa. Đổi `PUT/DELETE /api/cart/items/{id}` từ định danh theo `productId` sang `cartItemId` (Id riêng của từng dòng) — breaking change có chủ đích, `CartItemDto` trả thêm field `id`.
- Validate thêm giỏ hàng: nếu `product.Variants.Count > 0` → bắt buộc `ProductVariantId` (lỗi "Vui lòng chọn màu." nếu thiếu), tồn kho kiểm tra theo `variant.Stock`. Sản phẩm không có variant giữ nguyên logic dùng `Product.Stock` như cũ (không ảnh hưởng các sản phẩm phụ kiện không có màu).
- Checkout trừ đúng `ProductVariant.Stock` khi dòng giỏ có chọn màu, snapshot `OrderItem.ColorName`. `ProductDto.Stock` đổi thành tính toán: tổng `Variants.Sum(Stock)` nếu có variant, ngược lại dùng `Product.Stock` gốc — tránh phải đồng bộ 2 nguồn tồn kho.
- Thêm 4 endpoint Admin-only quản lý variant (`POST/PUT/DELETE /api/products/{id}/variants/...` + upload ảnh riêng theo màu) theo đúng pattern CRUD Product/Category đã có — chưa có UI Admin (thuộc Phase 5), quản lý qua Swagger/script.
- **Bug phát hiện qua test tích hợp**: `ProductService.AddVariantAsync` ban đầu gọi `product.Variants.Add(variant)` thủ công sau khi `_productRepository.AddVariantAsync(variant)` — nhưng EF Core đã tự động "fixup" navigation collection (vì `variant.ProductId` khớp `product.Id` đang được track trong cùng context), gây nhân đôi variant trong list trả về. Phát hiện qua `result.Single(...)` ném `InvalidOperationException` trong integration test, không phải qua unit test (mock không có change tracker). Đã xóa dòng `Add` thủ công thừa.
- **Bug phát hiện qua kiểm thử curl thủ công** (giống bài học ở Phase 3): gõ trực tiếp "Đen"/"Trắng" vào chuỗi `-d` của curl trong Git Bash trên Windows bị mã hóa sai (`?en`, `Tr?ng`) — không phải lỗi server. Nhắc nhở: khi cần gửi JSON có tiếng Việt có dấu qua curl trên Windows, phải viết payload ra file UTF-8 (vd. qua Write tool) rồi dùng `curl --data-binary @file.json`, không gõ trực tiếp vào `-d`.
- Đã seed màu mẫu cho 2 sản phẩm smartphone có sẵn từ Phase 2 (Smartphone Pro 256GB: Đen/Trắng/Xanh; Smartphone Lite 128GB: Đen/Trắng), chia nhỏ tồn kho gốc theo màu qua API thật.
- Verify đầy đủ qua curl trên server thật: thêm giỏ hàng thiếu màu → 400 "Vui lòng chọn màu."; thêm đúng màu → giỏ hàng trả về dòng riêng có `colorName`/`stock` theo đúng variant; đặt hàng → `OrderItem.ColorName` lưu đúng, `ProductVariant.Stock` giảm đúng số lượng, `Product.Stock` (tổng hợp) giảm theo. Đã chụp ảnh `ProductDetailView` qua headless Chrome xác nhận UI chọn màu hiển thị đúng tên màu tiếng Việt (không lỗi font/encoding) và "Tồn kho" phản ứng đúng theo lựa chọn.
- `dotnet build`, `dotnet test` (66/66 pass — 40 unit + 26 integration, tăng từ 52 do thêm test cho variant ở Cart/Order/Products), `npm run build` đều pass.

**Cập nhật bổ sung (2026-06-20) — Rebuild ProductVariant thành mô hình 2 chiều phân loại (thay thế model màu sắc 1 chiều ở trên)**:
- User xem ảnh chụp Shopee thật (ô "Loại Tây Cầm" có icon + nhiều biến thể như "PS2 ko rung", "Xbox 360 PC"...) và xác nhận muốn mô hình variant **thật của Shopee**: sản phẩm có thể có tối đa **2 chiều phân loại độc lập** (ví dụ Màu sắc × Dung lượng), khách chọn từng chiều riêng rồi hệ thống tự khớp ra đúng SKU/tồn kho — không phải chỉ 1 danh sách màu phẳng như vừa làm ở mục trên. Đây là **thay thế hoàn toàn**, không phải mở rộng thêm field, nên model `ProductVariant { ColorName, ImageUrl }` ở bản trước bị bỏ.
- Domain mới: `ProductVariantOption { ProductId, Name, DisplayOrder }` (1 chiều, ví dụ "Màu sắc" = order 1, "Dung lượng" = order 2, tối đa 2/sản phẩm — validate ở Application, không phải DB constraint) và `ProductVariantOptionValue { ProductVariantOptionId, Value, ImageUrl? }` (1 giá trị cụ thể, ví dụ "Đen"; chỉ chiều thứ 1 thực sự dùng `ImageUrl` làm thumbnail ở UI, giống Shopee). `ProductVariant` đổi vai trò thành **SKU**: bỏ `ColorName`/`ImageUrl` trực tiếp, thay bằng `OptionValue1Id` (bắt buộc) + `OptionValue2Id` (Guid?, có khi sản phẩm có chiều thứ 2) + `Stock`.
- `OrderItem.ColorName` → đổi tên thành `VariantDescription` (string?) — snapshot gộp cả 2 chiều lúc mua (ví dụ "Đen, 256GB"), build qua helper `BuildVariantLabel` lặp lại ở cả `CartService` và `OrderService` (không tách class riêng vì chỉ 3 dòng, đúng tinh thần không over-engineer). `CartItemDto.ColorName` → `VariantDescription` tương tự.
- **Cart/Order hầu như không đổi logic**: vì `CartItem.ProductVariantId` vẫn trỏ vào SKU theo Id như cũ, toàn bộ luồng "bắt buộc chọn variant nếu sản phẩm có variant, kiểm tra tồn kho theo SKU, trừ đúng SKU khi checkout" giữ nguyên — chỉ đổi cách build label hiển thị.
- 4 endpoint variant cũ (`POST/PUT/DELETE /api/products/{id}/variants...` theo `ColorName`) thay bằng **8 endpoint mới**: `POST .../variant-options` (tạo cả option + values đầu cùng lúc), `POST .../variant-options/{optionId}/values` (thêm value sau), `POST .../variant-options/{optionId}/values/{valueId}/image` (ảnh riêng theo value, thay cho ảnh riêng theo SKU ở bản trước), `DELETE` cho value và option (chặn xóa nếu còn đang được SKU dùng/còn value — giữ đúng tiền lệ "Category chặn xóa nếu còn con", không cascade ngầm), và 3 endpoint SKU (`POST/PUT/DELETE .../variants`, body đổi từ `{ colorName, stock }` sang `{ optionValue1Id, optionValue2Id?, stock }`).
- **Bug phát hiện qua test tích hợp (giống lỗi đã gặp ở bản trước, lần này tránh được ngay vì đã rút kinh nghiệm)**: ban đầu định gọi `product.Variants.Add(variant)` thủ công sau khi `_productRepository.AddVariantAsync(variant)` giống lỗi cũ — đã chủ động bỏ ngay từ đầu, chỉ gán `variant.OptionValue1 = value1` (reference, không mutate list) để tránh nhân đôi do EF fixup.
- Migration `RestructureProductVariants` báo "may result in loss of data" (đúng như dự đoán) — khi áp dụng, các SKU màu cũ (Đen/Trắng/Xanh của Smartphone Pro, Đen/Trắng của Smartphone Lite, seed ở mục trên) vi phạm FK mới (`OptionValue1Id` cũ trỏ tới giá trị không tồn tại ở bảng `ProductVariantOptionValues` mới) nên migration fail nửa đường — đã rollback tự động (SQL Server transactional DDL), sau đó xóa thủ công 5 row `ProductVariants` cũ qua `sqlcmd` (đã xác nhận trước không còn `CartItem` nào tham chiếu) rồi chạy lại migration thành công. Đây là mất dữ liệu demo có chủ đích, đã báo trước cho user trong kế hoạch.
- **Bug thực thứ 2 phát hiện qua thao tác tay**: gõ trực tiếp "Đen"/"Trắng"/"256GB" vào JSON string của `curl -d` trong Git Bash trên Windows tiếp tục bị mã hóa sai (lặp lại đúng lỗi đã ghi ở mục trên) — lần này tránh bằng cách viết toàn bộ script seed bằng **Node.js (`fetch` built-in)** với chuỗi tiếng Việt nằm trực tiếp trong source `.js` (Node đọc file nguồn dạng UTF-8 theo mặc định, không đi qua encoding của shell) thay vì truyền qua tham số dòng lệnh — không còn lỗi mã hóa.
- Đã re-seed dữ liệu mẫu bằng model mới: Smartphone Pro 256GB có **2 chiều** (Màu sắc: Đen/Trắng/Xanh × Dung lượng: 128GB/256GB → 6 SKU, tồn kho 15 chia theo tổ hợp) để chứng minh đúng mô hình 2 chiều hoạt động; Smartphone Lite 128GB giữ **1 chiều** (Màu sắc: Đen/Trắng → 2 SKU, tồn kho 25) để chứng minh sản phẩm chỉ cần 1 chiều vẫn hoạt động bình thường trong cùng model.
- Frontend `ProductDetailView.vue` viết lại: render tối đa 2 dòng chọn (1 dòng/option theo `displayOrder`) — dòng chiều 1 có thumbnail ảnh (giữ thiết kế ô vuông từ bản trước), dòng chiều 2 chỉ là pill text thuần (đúng quy ước Shopee — chỉ Tier 1 có ảnh); `selectedVariant` tính bằng cách tìm trong `product.variants` khớp cả `optionValue1Id` và `optionValue2Id`; nút chọn ở dòng 2 tự disable nếu tổ hợp với chiều 1 đã chọn không có SKU/hết hàng.
- Verify qua curl trên server thật: thêm giỏ hàng không chọn gì → 400 "Vui lòng chọn đầy đủ phân loại."; thêm đúng tổ hợp Đen + 128GB → giỏ hàng trả `variantDescription: "Đen, 128GB"`; đặt hàng → SKU đó giảm đúng từ 3 còn 1, `Product.Stock` tổng giảm từ 15 xuống 13, `OrderItem.VariantDescription` lưu đúng "Đen, 128GB". Đã chụp ảnh `ProductDetailView` qua headless Chrome xác nhận 2 dòng "Màu sắc"/"Dung lượng" hiển thị đúng, tiếng Việt không lỗi font, đúng layout giống ảnh Shopee mẫu user cung cấp.
- `dotnet build`, `dotnet test` (79/79 pass — 51 unit + 28 integration, tăng từ 66 do viết lại toàn bộ test variant cho model 2 chiều + thêm test giới hạn tối đa 2 option, chặn trùng tổ hợp, chặn xóa value/option đang dùng), `npm run build` đều pass.

---

## Phase 4 — Review & Rating

- Bắt đầu: 2026-06-21
- Hoàn thành: 2026-06-21

- [x] Entity `Review` (rating 1-5 + comment), gắn `ProductId` + `UserId` (snapshot thêm `UserName` lúc review)
- [x] API tạo review (`POST /api/products/{productId}/reviews`)
- [x] API lấy danh sách review theo sản phẩm + rating trung bình (`GET /api/products/{productId}/reviews`; rating trung bình + số lượng review trả kèm trong `ProductDto.AverageRating`/`ReviewCount` ở `GET /api/products/{id}`)
- [x] Frontend: hiển thị sao trung bình + danh sách review ở trang chi tiết sản phẩm — `ProductDetailView.vue`
- [x] Frontend: form gửi review (kèm sửa/xóa review của chính mình)
- [x] Test tính rating trung bình + validate (không review trùng) — `ReviewServiceTests`, `ReviewsControllerTests`, `ProductServiceTests`

**Ghi chú / vấn đề phát sinh**:
- Trước khi code, dùng `AskUserQuestion` chốt 3 quyết định nghiệp vụ không có trong `CLAUDE.md`: (1) cho phép Update + Delete review của chính mình (không chỉ Create+Read); (2) bắt buộc **verified purchase** — chỉ được review sản phẩm đã từng mua và đơn hàng đã `Delivered`; (3) quyền Admin xóa review (kiểm duyệt) để dành cho Phase 5, không làm ở đây.
- Verified purchase cần thêm method mới `IOrderRepository.HasDeliveredOrderForProductAsync(userId, productId)` (`OrderRepository` dùng `SelectMany(o => o.Items).AnyAsync(...)`) — vì hiện tại **chưa có** endpoint Admin đổi trạng thái Order sang `Delivered` (thuộc Phase 5), nên test integration phải set `Order.Status` trực tiếp qua `AppDbContext` (lấy qua `factory.Services.CreateScope()`) thay vì gọi API.
- `Review.UserName` là snapshot (giống pattern `OrderItem.ProductName`/`VariantDescription` đã dùng từ Phase 3) lấy từ `IIdentityService.GetUserByIdAsync` lúc tạo review, để tránh phải query `UserManager` lại mỗi lần hiển thị danh sách review (N+1) và để tên hiển thị không đổi ngược nếu user đổi `FullName` sau đó.
- `ProductDto` thêm `AverageRating`/`ReviewCount`, chỉ tính trong `ProductService.GetByIdAsync` (trang chi tiết) — không tính ở `GetPagedAsync` (trang danh sách) vì phạm vi Phase 4 chỉ yêu cầu hiển thị ở trang chi tiết sản phẩm, tránh N+1 query không cần thiết khi liệt kê nhiều sản phẩm.
- Unique index `(ProductId, UserId)` trên bảng `Reviews` đảm bảo 1 user chỉ review 1 sản phẩm 1 lần ở tầng DB, kết hợp check trùng ở `ReviewService.CreateAsync` (trả lỗi rõ ràng "Bạn đã đánh giá sản phẩm này rồi." thay vì để DB ném lỗi constraint).
- Xóa Product cascade xóa luôn Review liên quan (`ReviewConfiguration` dùng `DeleteBehavior.Cascade` tới `Product`, khác với `CartItem`/`OrderItem` dùng `Restrict`) — chấp nhận được vì Review không phải dữ liệu giao dịch cần giữ lại như Order.
- Verify đầy đủ qua API thật (Node script tạo category/product/order, `sqlcmd` set `Order.Status = 3` để giả lập Delivered vì chưa có endpoint Admin) + chụp ảnh `ProductDetailView` qua headless Chrome: rating trung bình "★★★★★ 5.0 (1 đánh giá)" hiển thị đúng cạnh tên sản phẩm, danh sách review hiện đúng tên người mua + sao + nội dung + ngày, hint "Đăng nhập để viết đánh giá" hiện đúng khi chưa đăng nhập. Đã dọn lại category/product demo sau khi verify xong (cascade xóa luôn review demo).
- `dotnet build`, `dotnet test` (95/95 pass — 61 unit + 34 integration, tăng từ 79 nhờ thêm `ReviewServiceTests` + `ReviewsControllerTests` + 1 test rating trung bình trong `ProductServiceTests`), `npm run build` đều pass.

---

## Phase 5 — Admin Dashboard

- Bắt đầu: 2026-06-22
- Hoàn thành: 2026-06-22

- [x] Route `/admin/*` + route guard role Admin (đã có từ trước, chỉ chuyển sang nested route dưới `AdminLayout.vue`)
- [x] CRUD Category (UI) — `AdminCategoriesView.vue` (bảng + modal thêm/sửa, chặn xóa nếu còn con/sản phẩm theo lỗi trả từ API có sẵn từ Phase 2)
- [x] CRUD Product kèm upload ảnh (UI) — `AdminProductsView.vue` (danh sách, tìm kiếm, filter category) + `AdminProductFormView.vue` (thông tin cơ bản, upload ảnh, quản lý 2 chiều phân loại + SKU/tồn kho theo đúng model đã có từ Phase 3)
- [x] Quản lý Order: danh sách + đổi trạng thái — `AdminOrdersView.vue`, API mới `GET /api/orders/all`, `PUT /api/orders/{id}/status`
- [x] Quản lý User: danh sách + khóa/mở tài khoản — `AdminUsersView.vue`, API mới `GET /api/users`, `PUT /api/users/{id}/lockout`
- [x] Thống kê cơ bản: doanh thu, số đơn theo trạng thái — `AdminDashboardView.vue`, API mới `GET /api/statistics/dashboard`
- [x] Test endpoint quản trị (chỉ Admin gọi được) — `OrdersControllerTests`, `UsersControllerTests`, `StatisticsControllerTests` (mới) + bổ sung `OrderServiceTests`/`UserServiceTests`

**Ghi chú / vấn đề phát sinh**:
- Trước khi code, khảo sát kỹ backend hiện có: CRUD Category/Product (kèm variant 2 chiều) đã đầy đủ API từ Phase 2/3, chỉ thiếu 3 nhóm: Order (list tất cả + đổi trạng thái), User (list tất cả + khóa/mở), Statistics — nên Phase 5 backend chỉ cần bổ sung đúng 3 nhóm này, không phải viết lại CRUD Category/Product.
- `OrderService.UpdateOrderStatusAsync` áp dụng validate chuyển trạng thái hợp lệ (`Pending→Confirmed/Cancelled`, `Confirmed→Shipped/Cancelled`, `Shipped→Delivered`, `Delivered`/`Cancelled` là trạng thái cuối không đổi được nữa) — quyết định chủ động thêm rule này (CLAUDE.md chỉ ghi "đổi trạng thái" chung) để tránh admin đặt trạng thái vô lý qua API; frontend (`AdminOrdersView.vue`) replicate đúng map này để disable lựa chọn không hợp lệ trong dropdown.
- **Không** làm tự động cộng trả tồn kho khi hủy đơn (`Cancelled`): phát hiện `OrderItem` (từ Phase 3) chỉ lưu `ProductId` + `VariantDescription` (string snapshot), không lưu `ProductVariantId`, nên không thể xác định đúng SKU để hoàn tồn kho mà không đổi schema — nằm ngoài phạm vi yêu cầu của Phase 5 ("xem danh sách, đổi trạng thái"), ghi nhận là hạn chế biết trước.
- Endpoint admin list Order (`GET /api/orders/all`) trả thêm `CustomerEmail` bằng cách gọi `IIdentityService.GetUserByIdAsync` cho từng order trong trang hiện tại (N+1 có chủ đích, chấp nhận được vì `pageSize` nhỏ và đây là trang quản trị ít truy cập, không phải API public) — không thêm snapshot email vào `Order` entity để tránh đổi schema không cần thiết.
- Tính năng khóa user dùng đúng cơ chế `LockoutEnabled`/`LockoutEnd` có sẵn của ASP.NET Core Identity (không thêm field tùy biến) — khóa bằng `SetLockoutEndDateAsync(DateTimeOffset.MaxValue)`, mở khóa bằng `SetLockoutEndDateAsync(null)`. Phát hiện và sửa thêm: `AuthService.LoginAsync` (Phase 1) gọi `UserManager.CheckPasswordAsync` trực tiếp — hàm này **không** tự chặn user bị lockout (chỉ `PasswordSignInAsync` của `SignInManager` mới chặn) — phải thêm check `user.IsLocked` thủ công trước khi check password, nếu không tính năng khóa tài khoản sẽ vô nghĩa (user bị khóa vẫn đăng nhập được). Có test integration xác nhận (`UsersControllerTests.SetLockout_AsAdmin_LocksTargetUserAndBlocksFutureLogin`).
- Chặn admin tự khóa chính mình (`UserService.SetLockoutAsync` so sánh `currentUserId == targetUserId` khi `locked = true`, vẫn cho tự mở khóa) — tránh tình huống admin duy nhất tự lock tài khoản và không ai mở lại được qua API.
- Frontend: tạo `AdminLayout.vue` (sidebar đen + topbar trắng, route `/admin/*` chuyển thành children dưới layout này) thay cho header/footer khách hàng — sửa `App.vue` ẩn `AppHeader`/`AppFooter` khi `route.path` bắt đầu bằng `/admin`. CSS chung cho mọi trang admin đặt ở `client/src/styles/admin.css` (global, không scoped) và chỉ `import` 1 lần trong `AdminLayout.vue` — vì layout này được lazy-load theo route `/admin` nên CSS không lẫn vào bundle của trang khách hàng.
- `AdminProductFormView.vue` là phần phức tạp nhất: quản lý đúng model "phân loại 2 chiều" (tối đa 2 `ProductVariantOption`, mỗi SKU là 1 `ProductVariant` khớp `OptionValue1`+`OptionValue2`) đã xây ở Phase 3 — UI cho thêm/xóa phân loại, thêm/xóa giá trị (kèm upload ảnh riêng cho chiều đầu tiên theo đúng quy ước "chỉ Tier 1 có ảnh"), và bảng quản lý SKU (thêm SKU mới theo tổ hợp, sửa tồn kho, xóa).
- Môi trường verify: phát hiện 1 process `ShopeeClone.API.exe` cũ và 1 `node.exe` (Vite) cũ còn giữ port từ session trước — đã hỏi và được xác nhận trước khi dừng. Vite mặc định build phụ thuộc đúng port `5173` vì backend CORS (`Cors:AllowedOrigins`) cấu hình cứng origin này; nếu port 5173 bị chiếm, Vite tự nhảy sang port khác (vd. 5174) và toàn bộ gọi API từ trang admin sẽ bị CORS chặn (lỗi hiển thị chung "Không thể tải số liệu thống kê." dù backend hoàn toàn khỏe) — đây không phải bug code mà là vấn đề môi trường, nhưng dễ gây hiểu lầm khi debug nếu không để ý đúng port Vite đang chạy.
- Verify thật bằng Chrome headless điều khiển qua CDP (WebSocket, không cần cài thêm puppeteer/playwright): set `localStorage` (`accessToken`/`authUser`) bằng token lấy qua `curl login` thật để vào được các trang `/admin/*` cần đăng nhập Admin, chụp ảnh từng trang (Dashboard, Categories, Products, Orders, Users, Product form, Product edit có sẵn 2 chiều variant của "Smartphone Pro 256GB"). Đã test thêm 2 luồng ghi **thật qua UI** (không chỉ gọi API): đổi trạng thái 1 đơn từ `Pending`→`Confirmed` bằng cách dispatch event lên đúng `<select>`/click nút trong DOM rồi xác nhận qua API là đã đổi thật; khóa rồi mở khóa lại 1 user test qua nút "Khóa/Mở khóa" — cả 2 đều phản ánh đúng lên UI sau khi reload.
- `dotnet build`, `dotnet test` (117/117 pass — 71 unit + 46 integration, tăng từ 95 nhờ thêm test cho Order admin/User admin/Statistics), `npm run build` đều pass. Không cần migration mới (không đổi entity/DbContext, chỉ thêm DTO/repository method và dùng field có sẵn của Identity).

---

## Phase 6 — Backlog / Future

Mỗi mục làm độc lập, không theo thứ tự cố định. Mục nào chưa làm vẫn để `[ ]`, không có checklist con cho tới khi bắt đầu.

- [x] Voucher / mã giảm giá
- [x] Wishlist / sản phẩm yêu thích
- [x] Flash sale / khung giờ vàng
- [ ] Tích hợp cổng thanh toán thật (VNPay/Momo/ZaloPay)
- [x] Notification (email/push)
- [ ] Multi-vendor marketplace — **lưu ý**: mâu thuẫn trực tiếp với quyết định đã chốt "single-vendor" ở `CLAUDE.md` §4; nếu làm mục này cần xác nhận lại quyết định kiến trúc trước, không tự ý đổi.
- [x] Chat giữa khách và shop
- [x] Recommendation engine

### Chat giữa khách và shop

- Bắt đầu: 2026-06-22
- Hoàn thành: 2026-06-22

- [x] Real-time chat 1-1 giữa Customer và "Shop" (shared inbox — mọi Admin xem/trả lời được, đúng mô hình single-vendor)
- [x] Backend: Entity `Conversation` (1 conversation/Customer, unique index `CustomerId`) + `ChatMessage`, SignalR Hub, REST API
- [x] Frontend: bong bóng chat nổi cho Customer (`ChatWidget.vue`), trang quản lý chat cho Admin (`AdminChatView.vue`, route `/admin/chat`)
- [x] Test: `ChatServiceTests` (11 test) + `ChatControllerTests` (7 test)

**Ghi chú / vấn đề phát sinh**:
- Trước khi code, dùng `AskUserQuestion` chốt 2 quyết định không có trong `CLAUDE.md`: (1) real-time qua SignalR (không phải polling) để đúng kỹ thuật chuẩn của .NET cho chat; (2) mô hình shared inbox (1 Customer = 1 Conversation, mọi Admin dùng chung) thay vì gán cố định 1 Admin/conversation — khớp đúng mô hình single-vendor đã chốt.
- `ChatHub` đặt ở `API/Hubs/ChatHub.cs` (không phải `Infrastructure`) vì `Infrastructure` dùng `Microsoft.NET.Sdk` thường (không phải `Sdk.Web`), không có sẵn SignalR types; Hub về bản chất là presentation layer giống Controller nên đặt ở API là hợp lý, không phá dependency rule.
- JWT của dự án dùng Bearer header bình thường, nhưng browser WebSocket không tự gửi header được — phải thêm `JwtBearerEvents.OnMessageReceived` đọc token từ query string `?access_token=...`, chỉ áp dụng cho path `/hubs/chat` (`Infrastructure/DependencyInjection.cs`), không ảnh hưởng API thường. CORS phải thêm `.AllowCredentials()` (trước đó chỉ `AllowAnyHeader/AllowAnyMethod`).
- Giữ đúng dependency rule (Application không phụ thuộc framework) bằng cách thêm interface `IChatNotifier` (`NotifyMessageAsync`, `NotifyConversationUpdatedAsync`) ở Application, implement ở `API/Hubs/SignalRChatNotifier.cs` dùng `IHubContext<ChatHub>` — `ChatService` chỉ biết interface, không biết SignalR.
- Hub chỉ làm 2 việc: join/leave group theo `conversationId` (method `JoinConversation` tái dùng `IChatService.GetMessagesAsync(..., page=1, pageSize=1)` để kiểm tra quyền truy cập trước khi cho join — customer chỉ join được conversation của mình, admin join được bất kỳ — tránh phải thêm method mới chỉ để check quyền), và nhận broadcast từ `IChatNotifier`. Gửi tin nhắn vẫn qua REST (`ChatController`) thay vì qua Hub method có chủ đích, để giữ nguyên pattern test xUnit + `WebApplicationFactory` đã dùng toàn dự án (không cần thêm SignalR test client riêng) — vẫn có real-time đầy đủ vì `ChatService` luôn gọi `IChatNotifier` sau khi persist, bất kể request đến từ REST hay Hub.
- Migration `AddChat`: 2 bảng mới `Conversations`, `ChatMessages`, unique index `Conversations.CustomerId` (đảm bảo 1 Customer chỉ có 1 conversation ở tầng DB).
- Frontend: `stores/chat.ts` (Pinia) làm trung tâm quản lý 1 `HubConnection` dùng chung (`accessTokenFactory` đọc từ `authStore`), xử lý 2 event `ReceiveMessage`/`ConversationUpdated`. `ChatWidget.vue` chỉ hiện khi đã đăng nhập, không phải Admin, và không phải route `/admin` (mount trong `App.vue`).
- Verify bằng 2 script Node tạm (đã xóa sau khi xác nhận, không commit vào repo): (1) dùng `@microsoft/signalr` thật mở 2 `HubConnection` (giả lập Customer + Admin) lên server dev thật, xác nhận JWT qua query string hoạt động, quyền join đúng, và khi 1 phía gửi tin nhắn qua REST thì phía kia nhận được `ReceiveMessage`/`ConversationUpdated` qua WebSocket **không cần reload** — cả 2 chiều đều pass; (2) Chrome headless điều khiển qua CDP (kỹ thuật đã dùng ở Phase 5) set `localStorage` bằng token thật rồi chụp ảnh, xác nhận bong bóng chat + panel cho Customer và layout 2 cột (danh sách hội thoại + khung chat) cho Admin đều render đúng.
- Gặp lại đúng vấn đề môi trường đã ghi nhận ở Phase 5: 1 process `ShopeeClone.API.exe` và 1 `node.exe` (giữ đúng port 5173 mà CORS backend cho phép) còn sót từ session trước, chặn build và buộc Vite nhảy sang port khác — đã xin xác nhận người dùng trước khi tắt cả hai, không tự ý kill process nền ngoài phạm vi task.
- `dotnet build`, `dotnet test` (135/135 pass — 82 unit + 53 integration, tăng từ 117 nhờ thêm `ChatServiceTests`/`ChatControllerTests`), `npm run build` đều pass.

**Cập nhật bổ sung (2026-06-22) — Hiển thị số lượng tin nhắn chưa đọc (thay cho dấu chấm đỏ)**:
- Theo yêu cầu của user, đổi `Conversation.UnreadByAdmin`/`UnreadByCustomer` (bool) thành `UnreadCountForAdmin`/`UnreadCountForCustomer` (int) — tăng dần mỗi khi phía kia gửi tin nhắn (`ChatService.SendMessageAsync`), reset về 0 khi mark-as-read. Migration mới `ChatUnreadCounts` (drop 2 cột bool, add 2 cột int default 0 — chấp nhận mất dữ liệu demo cũ, đúng tiền lệ đã làm ở Phase 3 khi restructure ProductVariant).
- Frontend: bong bóng chat của Customer hiện badge số (vd "3", hoặc "9+" nếu lớn hơn 9) thay vì dấu chấm; trang Admin Chat hiện badge số theo từng hội thoại trong danh sách **và** tổng số tin nhắn chưa đọc trên nav "Chat" ở sidebar — badge nav này hiện được trên mọi trang `/admin/*` (không chỉ trang Chat) vì `AdminLayout.vue` giờ tự `connect()` SignalR + `loadConversationsForAdmin()` ngay lúc mount, không đợi vào riêng trang Chat.
- **Bug thực phát hiện qua tự verify (không phải qua test tự động)**: ban đầu badge của Customer chỉ tải dữ liệu hội thoại khi người dùng bấm mở widget — nghĩa là badge không bao giờ hiện được vì chưa mở thì chưa có dữ liệu. Sửa bằng cách tách `loadMyConversation()` (cũ) thành `loadMyConversationMeta()` (chỉ gọi `GET /api/chat/conversation`, gọi ngay lúc `ChatWidget.vue` mount) và load tin nhắn/mark-read riêng khi thực sự mở panel.
- Để badge của Customer cập nhật real-time mà không cần họ tự mở widget để "join" group hội thoại, đổi `ChatHub.OnConnectedAsync`: Admin tự join group `admins` (như cũ), Customer giờ tự động được join vào group `conversation-{id}` riêng của họ ngay lúc connect (gọi `IChatService.GetOrCreateMyConversationAsync` ngay trong `OnConnectedAsync`) — không cần đợi gọi `JoinConversation` thủ công.
- **Bug thực nghiêm trọng phát hiện qua chính việc tự verify thay đổi trên** (race condition thật, không phải lỗi test): `ChatWidget.vue` gọi `chatStore.connect()` (mở SignalR, kích hoạt `OnConnectedAsync` ở server) không `await`, chạy song song với `await chatStore.loadMyConversationMeta()` (gọi REST `GET /api/chat/conversation`, cũng tự tạo conversation nếu chưa có) — 2 đường này có thể cùng lúc thấy "chưa có conversation" và cùng insert, vi phạm unique index `Conversations.CustomerId`, ném `DbUpdateException` và làm sập kết nối SignalR. Tình huống này xảy ra thật mỗi khi khách hàng tải lại trang lần đầu (hoặc mở 2 tab cùng lúc), không chỉ trong script test. Sửa tại `ChatRepository.AddConversationAsync` (đổi sang trả về `Task<Conversation>`): bắt `DbUpdateException` khi insert trùng, fallback sang `GetByCustomerIdAsync` để lấy đúng row đã được tạo bởi nhánh thắng race — giữ logic race-handling ở Infrastructure (nơi biết về EF/SQL), không để Application phụ thuộc kiểu exception của EF Core.
- Test cập nhật: `ChatServiceTests` (2 test đổi sang assert số đếm tăng/reset thay vì bool, sửa mock `AddConversationAsync` trả về `Conversation` thay vì `Task.CompletedTask`), `ChatControllerTests` (đổi assertion sang số, thêm bước gửi 2 tin nhắn để xác nhận đếm đúng 2 trước khi mark-read).
- Verify thủ công bằng 4 script Node tạm (đã xóa sau khi xác nhận, không commit vào repo): xác nhận số đếm tăng đúng theo từng tin nhắn và reset đúng khi mark-as-read (cả 2 chiều); xác nhận Customer nhận được `ConversationUpdated` qua WebSocket dù không gọi `JoinConversation` thủ công; xác nhận race condition đã hết bằng cách lặp lại đúng kịch bản gây lỗi (connect song song với gọi REST) và thấy chạy ổn định; chụp ảnh xác nhận trực quan: bong bóng chat Customer hiện badge "3" ngay khi tải trang (không cần mở widget), trang Admin Chat hiện badge "2" trên nav sidebar và đúng badge "2" cạnh tên khách trong danh sách hội thoại.
- `dotnet build`, `dotnet test` vẫn 135/135 pass (82 unit + 53 integration, số lượng test không đổi — chỉ sửa nội dung assertion), `npm run build` pass.

### Notification (email/push) — Thông báo đơn hàng cho user khi đổi trạng thái

- Bắt đầu: 2026-06-22
- Hoàn thành: 2026-06-22

- [x] Backend: Entity `Notification` (UserId, OrderId, Message, IsRead) + repository + service, gắn vào `OrderService.UpdateOrderStatusAsync`
- [x] Kênh in-app real-time: SignalR `NotificationHub` (group theo `user-{userId}`), `NotificationsController` (list/unread-count/mark-as-read/mark-all-as-read)
- [x] Kênh email thật: `SmtpEmailSender` (MailKit) cấu hình qua `Email:*` trong `appsettings.json`, mặc định trỏ Gmail SMTP (`smtp.gmail.com:587`)
- [x] Frontend: `NotificationBell.vue` (icon chuông + badge + dropdown) gắn vào `AppHeader.vue`, chỉ hiện cho Customer đã đăng nhập (không hiện cho Admin/khi chưa đăng nhập)
- [x] Test: `NotificationServiceTests` (6 test) + `NotificationsControllerTests` (4 test)

**Ghi chú / vấn đề phát sinh**:
- Trước khi code, dùng `AskUserQuestion` chốt phạm vi: phần "Notification" trong backlog `CLAUDE.md` ghi chung "email/push", nhưng yêu cầu thực tế của user chỉ là thông báo đổi trạng thái đơn hàng cho Customer — chốt làm **cả 2 kênh** (in-app real-time + email thật), và dùng **Gmail SMTP thật** (không phải email giả lập/log) để demo được luồng SMTP thật khi phỏng vấn.
- Kiến trúc tái dùng đúng pattern đã có ở tính năng Chat (Phase 6 mục trên): `INotificationPusher` (Application) ↔ `SignalRNotificationPusher` (API, dùng `IHubContext<NotificationHub>`) giữ Application không phụ thuộc SignalR, giống `IChatNotifier`/`SignalRChatNotifier`. Khác với Chat (group theo conversation), `NotificationHub` join group theo `user-{userId}` vì thông báo đơn hàng là riêng theo từng Customer, không có khái niệm hội thoại.
- Thêm `IEmailSender` (Application) ↔ `SmtpEmailSender` (Infrastructure, dùng package MailKit 4.17.0) theo đúng pattern `IFileStorageService`/`LocalFileStorageService` đã có (interface định nghĩa ở Application, implement ở Infrastructure để giữ dependency rule). `EmailSettings` đọc từ section `Email` trong `appsettings.json`; `User`/`Password` (Gmail App Password) để trống trong `appsettings.json` — phải set qua `dotnet user-secrets` (không commit credential thật vào repo).
- **Quyết định chủ động**: `SmtpEmailSender.SendAsync` tự skip (không thử kết nối) nếu `Host`/`User` chưa cấu hình, thay vì luôn thử kết nối SMTP thật — tránh mỗi lần đổi trạng thái đơn hàng trong dev/test đều phải chờ network timeout tới Gmail khi chưa setup credential. `NotificationService` cũng tự bắt exception quanh việc gửi email (log warning, không throw) để 1 lần gửi email lỗi (SMTP down, sai mật khẩu...) không làm hỏng luồng đổi trạng thái đơn hàng chính.
- Phát hiện thiếu package khi build: `Application` project chưa có `Microsoft.Extensions.Logging.Abstractions` (cần cho `ILogger<NotificationService>`) — đã thêm version khớp với các package `Microsoft.Extensions.*` khác trong project (10.0.9).
- Môi trường: gặp lại đúng vấn đề đã ghi nhận ở Phase 5/Chat — 1 process `ShopeeClone.API.exe` và vài `node.exe` cũ còn sót từ session trước giữ lock file build, đã xin xác nhận người dùng trước khi tắt.
- Verify đầy đủ bằng Node script tạm (đã xóa sau khi xác nhận, không commit vào repo) gọi API thật: đăng ký Customer → Admin đổi trạng thái đơn hàng → `GET /api/notifications` trả đúng thông báo tiếng Việt (`Đơn hàng #XXXXXXXX đã chuyển sang trạng thái "Đã xác nhận".`) và `unread-count` = 1. Verify real-time bằng Chrome headless điều khiển qua CDP (kỹ thuật đã dùng ở Phase 5/Chat, lần này dùng `WebSocket` built-in của Node 26 thay vì cần thêm package): đăng nhập Customer qua localStorage, mở trang `/products` (kết nối SignalR ngay vì `NotificationBell` mount trong `AppHeader`), sau đó gọi API đổi trạng thái đơn hàng từ phía Admin **trong khi trang Customer vẫn mở, không reload** — badge chuông tự nhảy từ ẩn sang "1" và panel hiện đúng nội dung thông báo, xác nhận push 2 chiều hoạt động đúng. Đã dọn category/product demo tạo ra trong lúc verify.
- `dotnet build`, `dotnet test` (145/145 pass — 88 unit + 57 integration, tăng từ 135), `npm run build` pass.

### Wishlist / sản phẩm yêu thích

- Bắt đầu: 2026-06-23
- Hoàn thành: 2026-06-23

- [x] Backend: Entity `Wishlist` (1/user) + `WishlistItem` (unique theo `WishlistId`+`ProductId`), repository, service, controller (`GET /api/wishlist`, `POST/DELETE /api/wishlist/items/...`)
- [x] Frontend: store Pinia (`stores/wishlist.ts`), icon tim ở `AppHeader.vue` (badge số lượng), nút toggle ở thẻ sản phẩm (`ProductListView.vue`) + trang chi tiết (`ProductDetailView.vue`), trang `WishlistView.vue` (route `/wishlist`)
- [x] Test: `WishlistServiceTests` (5 test) + `WishlistControllerTests` (5 test)

**Ghi chú / vấn đề phát sinh**:
- Wishlist chỉ theo dõi ở mức sản phẩm (không theo SKU/variant) — đúng với yêu cầu "lưu sản phẩm yêu thích" đơn giản, không cần chọn phân loại trước khi lưu.
- `WishlistRepository.GetOrCreateAsync` tái dùng đúng kỹ thuật xử lý race-condition đã viết cho `ChatRepository.AddConversationAsync` ở mục Chat phía trên (bắt `DbUpdateException` khi vi phạm unique index do 2 request cùng tạo wishlist lần đầu, fallback query lại bản đã được tạo bởi nhánh thắng) — áp dụng ngay từ đầu vì đã biết trước nguy cơ này, không phải sửa lại sau khi gặp lỗi như ở Chat.
- `AddAsync` idempotent (thêm sản phẩm đã có trong wishlist không báo lỗi, không tạo dòng trùng) để khớp hành vi nút "toggle" (tim đặc/tim rỗng) ở frontend.
- `client/src/stores/wishlist.ts` viết theo đúng khuôn `stores/cart.ts` đã có (state + `fetchWishlist()` + action `toggle(productId)` optimistic + getter `has(productId)`), tái dùng quy ước đã quen của dự án thay vì tạo pattern mới.

### Recommendation engine

- Bắt đầu: 2026-06-23
- Hoàn thành: 2026-06-23

- [x] Backend: mở rộng `IProductRepository` (`GetByCategoryAsync`, `GetTopRatedAsync`), `IOrderRepository` (`GetPurchasedCategoryIdsAsync`), `RecommendationService` (sản phẩm liên quan theo category + gợi ý cá nhân hóa theo lịch sử mua, fallback top-rated cho khách/người chưa có lịch sử)
- [x] API: `GET /api/products/{id}/related-products` (gắn vào `ProductsController` có sẵn), `GET /api/recommendations` (controller mới, public — đọc claim JWT nếu có để cá nhân hóa, không bắt buộc đăng nhập)
- [x] Frontend: mục "Sản phẩm liên quan" ở trang chi tiết sản phẩm, mục "Gợi ý cho bạn" ở đầu trang danh sách (chỉ hiện khi chưa lọc theo từ khóa/category)
- [x] Test: `RecommendationServiceTests` (5 test) + `RecommendationsControllerTests` (2 test) + bổ sung `ProductsControllerTests`

**Ghi chú / vấn đề phát sinh**:
- Không cần entity/migration mới — tái sử dụng hoàn toàn dữ liệu `Product`/`Review`/`OrderItem` đã có từ các phase trước.
- `ProductService.MapToDto` đổi từ `private static` sang `internal static` để `RecommendationService` dùng lại đúng logic map DTO (tránh viết lại mapping ở 2 nơi) — theo đúng tiền lệ `CartService.BuildVariantLabel` cũng là `internal static` dùng chung giữa Cart và Order.
- `ProductRepository.GetTopRatedAsync` thiết kế tự bù sản phẩm mới nhất nếu không đủ số lượng sản phẩm đã có review để lấp đầy `take` — tránh mục "Gợi ý cho bạn" bị trống ở database mới chưa có review nào.
- `RecommendationsController.GetRecommendations` không gắn `[Authorize]` nhưng vẫn đọc được `User.FindFirstValue(ClaimTypes.NameIdentifier)` nếu request có gửi token — vì `UseAuthentication()` luôn populate `User` bất kể action có yêu cầu `[Authorize]` hay không, nên 1 endpoint vẫn phục vụ được cả khách (gợi ý top-rated) và user đã đăng nhập (gợi ý cá nhân hóa) mà không cần 2 route riêng.
- `dotnet test` tăng từ 145 lên 164 sau Wishlist + Recommendation (99 unit + 65 integration).

### Voucher / mã giảm giá

- Bắt đầu: 2026-06-23
- Hoàn thành: 2026-06-23

- [x] Backend: Entity `Voucher` (loại % hoặc số tiền cố định, đơn tối thiểu, giới hạn tổng lượt dùng, giới hạn lượt/user, ngày hết hạn) + `VoucherRedemption` (theo dõi lượt dùng/user); `Order` thêm `Subtotal`/`DiscountAmount`/`VoucherId`/`VoucherCode` (snapshot)
- [x] `VoucherService.ValidateAsync` kiểm đủ chuỗi điều kiện (không tồn tại → tắt → hết hạn → dưới đơn tối thiểu → vượt giới hạn tổng → vượt giới hạn/user), tính `DiscountAmount` chặn không vượt quá subtotal
- [x] `OrderService.CreateOrderFromCartAsync` re-validate voucher bằng subtotal thật phía server (không tin giá trị frontend gửi), áp dụng giảm giá + redeem nếu hợp lệ, từ chối toàn bộ đơn nếu mã không hợp lệ
- [x] API: `POST /api/vouchers/validate` (preview giảm giá dựa trên giỏ hiện tại), CRUD `GET/POST/PUT/DELETE /api/vouchers` (Admin)
- [x] Frontend: ô nhập mã giảm giá + bảng tóm tắt Tạm tính/Giảm giá/Tổng thanh toán ở `CheckoutView.vue`, hiển thị lại ở `OrderDetailView.vue`; `AdminVouchersView.vue` (bảng + modal CRUD theo khuôn `AdminCategoriesView.vue`), thêm route/nav `/admin/vouchers`
- [x] Test: `VoucherServiceTests` (14 test), `VouchersControllerTests` (7 test), bổ sung `OrderServiceTests`/`OrdersControllerTests` cho luồng đặt hàng có/không có voucher hợp lệ

**Ghi chú / vấn đề phát sinh**:
- `OrderService` inject trực tiếp `IVoucherService` (không qua class validator riêng) — theo đúng tiền lệ `OrderService` đã phụ thuộc `INotificationService` từ trước; giữ toàn bộ logic validate (hết hạn, đơn tối thiểu, giới hạn lượt dùng) ở 1 chỗ (`VoucherService.ValidateAsync`) dùng chung cho cả endpoint preview và lúc tạo đơn thật.
- Theo đúng convention sẵn có của project, cột decimal (`Subtotal`, `DiscountAmount`, `Voucher.DiscountValue`...) khai báo bằng `.HasPrecision(18, 2)` (không dùng `.HasColumnType("decimal(18,2)")` như bản đầu viết) — soát lại `ProductConfiguration`/`OrderConfiguration` cũ để khớp đúng quy ước đã có.
- Migration `AddVouchers` (2 bảng mới `Vouchers`/`VoucherRedemptions`, 4 cột mới trên `Orders`) áp dụng sạch, không cần xử lý dữ liệu cũ vì là cột thêm mới có default.
- `dotnet test` tăng từ 164 lên 187 sau khi thêm Voucher (113 unit + 74 integration).

### Flash sale / khung giờ vàng

- Bắt đầu: 2026-06-23
- Hoàn thành: 2026-06-23

- [x] Backend: Entity `FlashSale` (tên, khung giờ bắt đầu/kết thúc, bật/tắt) + `FlashSaleItem` (giá sale, giới hạn số lượng riêng `QuantityLimit`/`QuantitySold` — **không** dùng chung tồn kho thường của `Product`)
- [x] `FlashSaleRepository.TryIncrementQuantitySoldAsync` dùng EF Core `ExecuteUpdateAsync` với điều kiện `WHERE QuantitySold + qty <= QuantityLimit` trong 1 câu lệnh UPDATE duy nhất — chống bán vượt số lượng giới hạn khi có nhiều checkout đồng thời, không cần transaction/lock riêng
- [x] `ProductDto` thêm `FlashSalePrice`/`FlashSaleQuantityRemaining`/`FlashSaleEndsAt`; `ProductService` decorate hàng loạt bằng 1 query gộp (`GetActiveItemsForProductsAsync`) — theo đúng pattern đã dùng để gắn `AverageRating`/`ReviewCount` ở Phase 4, tránh N+1
- [x] `OrderService` (inject trực tiếp `IFlashSaleRepository`, không qua service riêng vì chỉ cần tra cứu + tăng số lượng nguyên tử, không có logic nghiệp vụ phức tạp cần tái dùng như Voucher): khi tạo đơn, nếu sản phẩm đang trong flash sale còn suất thì snapshot giá sale vào `OrderItem.UnitPrice` và tăng `QuantitySold`; nếu hết suất giữa lúc checkout thì tự rơi về giá thường, không chặn đơn hàng
- [x] API: `GET /api/flash-sales/active` (public), CRUD Admin (`GET/POST/DELETE /api/flash-sales`, `POST/PUT/DELETE /api/flash-sales/{id}/items/...`)
- [x] Frontend: banner "⚡ Flash Sale" (đếm ngược, giá gạch ngang, thanh tiến độ đã bán) ở đầu `ProductListView.vue`, giá sale gạch ngang trên thẻ sản phẩm; price box riêng (đếm ngược + số suất còn lại) ở `ProductDetailView.vue`; `AdminFlashSalesView.vue` (danh sách chương trình + trạng thái Sắp diễn ra/Đang diễn ra/Đã kết thúc tính theo giờ thật, modal quản lý sản phẩm trong từng chương trình), thêm route/nav `/admin/flash-sales`
- [x] Test: `FlashSaleServiceTests` (16 test), `FlashSalesControllerTests` (8 test), bổ sung `ProductServiceTests`/`OrderServiceTests`/`OrdersControllerTests` cho việc decorate giá sale + snapshot lúc đặt hàng + fallback khi hết suất

**Ghi chú / vấn đề phát sinh**:
- **Bug tự phát hiện khi viết integration test (flaky test, không phải lỗi logic nghiệp vụ)**: test ban đầu tạo 1 flash sale đang hoạt động rồi assert qua `GET /api/flash-sales/active` (endpoint trả "1 flash sale đang active" theo `StartsAt` sớm nhất) — nhưng vì các test trong cùng 1 class chạy chung 1 DB test (collection fixture tuần tự, không cô lập theo từng test), một test khác cũng tạo flash sale "đang active" cùng lúc khiến `/active` có thể trả về flash sale của test khác, làm assert theo `Id` thất bại ngẫu nhiên. Sửa bằng cách không còn assert theo `Id` cụ thể qua `/active` nữa — chuyển sang `GET /api/flash-sales/{id}` (theo Id rõ ràng, không mơ hồ) hoặc `GET /api/products/{id}` (decorate theo đúng `productId` của sản phẩm đang test, không phụ thuộc "ai đang active toàn cục") để kiểm tra dữ liệu; giữ riêng 1 test nhỏ (`GetActive_IsPublic_ReturnsOk`) chỉ xác nhận endpoint public trả 200, không khẳng định nội dung cụ thể.
- **Hạn chế biết trước, ghi nhận chứ chưa sửa**: `WishlistItemDto` (mục Wishlist phía trên) chưa được decorate giá flash sale — trang "Yêu Thích" vẫn hiển thị giá gốc của sản phẩm dù sản phẩm đó đang trong flash sale. Không phải lỗi do tính năng Flash Sale gây ra mà là phạm vi chưa làm tới; có thể bổ sung sau nếu cần.
- Model cho phép nhiều flash sale "đang active" cùng lúc tồn tại trong DB (không có constraint chặn chồng lấp thời gian) — endpoint `/active` chỉ trả về 1 chương trình (theo `StartsAt` sớm nhất) để khớp với banner đơn ở trang chủ giống Shopee thật (chỉ có 1 khung giờ vàng nổi bật tại 1 thời điểm), nhưng việc *trang chi tiết sản phẩm hiển thị giá sale* không phụ thuộc vào chương trình nào "thắng" ở banner — chỉ cần sản phẩm đó có ở bất kỳ chương trình đang active nào.
- Đã verify trực quan qua headless Chrome + seed dữ liệu thật qua API (đăng nhập Admin, tạo flash sale, thêm sản phẩm, set giá sale thấp hơn giá gốc): banner Flash Sale ở trang danh sách hiện đúng đếm ngược + giá gạch ngang + thanh "Đã bán 0/20"; trang chi tiết sản phẩm hiện đúng price box riêng + "Còn 20 suất ưu đãi"; trang Admin Flash Sale + Admin Voucher (seed thêm 1 voucher demo) hiện đúng dữ liệu trong bảng. Dùng kỹ thuật tạo 1 trang `_test-login.html` tạm trong `client/public/` (set `localStorage` bằng token lấy qua API thật rồi tự chuyển trang) để vào được các trang yêu cầu đăng nhập mà không cần điều khiển form qua CDP — đã xóa file này cùng các file JSON tạm chứa token sau khi xác nhận xong, không commit vào repo.
- Môi trường: lại gặp đúng vấn đề port 5173 bị 1 `node.exe` cũ từ session trước chiếm (khiến Vite phải nhảy sang 5174 và bị CORS chặn) — đã xin xác nhận người dùng trước khi tắt, giống các phase trước.
- `dotnet test` tăng từ 187 lên 214 sau khi thêm Flash Sale (130 unit + 84 integration). `npm run build` (vue-tsc + vite build) pass sau cả 4 tính năng (Wishlist, Recommendation, Voucher, Flash Sale).
