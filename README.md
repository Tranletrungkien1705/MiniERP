# MiniERP

Dự án demo nhỏ, gộp tính năng lõi từ các hệ ERP nội bộ (**DMS.Sales / ERP.V15 HTC** và **InBrand Skycic**),
viết lại bằng stack .NET mới nhất (.NET 10 / C# 13) theo Clean Architecture — đối lập với kiến trúc
WinForms + ASMX + string-bake SQL của các hệ gốc.

## Tính năng lấy từ đâu (feature mapping)

| Module trong MiniERP | Lấy ý tưởng từ | Nguồn |
|---|---|---|
| `Partner` (Principal/Dealer/Bank/Insurance/Transporter) | "Các loại đối tác trong hệ thống" + "Phân loại người dùng" | `2010.HTC/.../docs_nghiep_vu/01_TONG_QUAN_HE_THONG.md` |
| `DealerContract` (Draft→DealerSigned→ApprovedA1→ApprovedA2) | Bước [1] "Lập hợp đồng đại lý" (ký ĐL → duyệt A1 → HTC ký A2) | cùng file, mục 4 |
| `SalesOrder` (Demand→Supply→A1→A2→Completed) | Bước [2] "Lập đơn hàng" (Demand/Supply, duyệt A1→A2) | cùng file |
| `StockItem` (SerialNo = VIN, Reserve→Deliver) | Bước [3]/[7] "Gán VIN cho xe" + "Giao xe" (`Car_VIN`, `Sto_*`) | cùng file, mục 3/7 |
| `Guarantee` (IssueDate/ExpiryDate/IsExpiringSoon) | Bước [5] "Cấp bảo lãnh" (theo dõi ngày hết hạn) | cùng file, mục 5 |
| `Payment` (Deposit/GuaranteeFee) | Bước [4]/[8] "Thanh toán cọc" + "Thanh toán bảo lãnh" | cùng file |
| `Invoice` (Principal/Subsidiary/Other) | Bước [9] "Xuất hóa đơn VAT (TCG/HTC)" + e-invoice InBrand | ERP.V15 mục 6 + `2019.6.InBrandCloud/.../00_TONG_QUAN_HE_THONG.md` |
| RBAC theo `PartnerType` (role trong JWT) | "Phân loại người dùng" (HTC Admin/Dealer/Bank/Transporter/Insurance) | ERP.V15 mục 8 |
| `InventorySyncEtlJob` (BackgroundService, Extract→Transform→Load) | Module `idn.InBrand.ELTS.Biz.Web` (ETL/ELTS) | InBrand kiến trúc §2.1 |
| `InosIdentityProviderClient` (OAuth2 ROPC gọi thẳng `/OAuth/Token`) | Tích hợp SSO iNOS (`AccountService.RequestToken`/`ExchangeUserCredentialForToken`) | InBrand "EXTERNAL SYSTEMS: iNOS..."; xác nhận qua decompile `inos.common.dll` trong phiên audit trước |
| Báo cáo (`DealerSummaryReport`, `GuaranteeExpiringReport`) | Module "Báo cáo" (`Rpt_*`) | ERP.V15 mục 3, dòng 12 |

**Có chủ đích KHÔNG port**: UI WinForms, ASMX SOAP, string-bake SQL (`StringUtils.Replace('@param', ...)` —
nguồn SQLi đã audit ở `2023.3.DMS.Sales/_audit/SECURITY_SQLI_DealerCode_2026-08-06.md`), phân trang thủ công
qua `ResultRecordStart/Count`. MiniERP dùng EF Core (parameter hoá 100%) + phân trang chuẩn REST.

## Kiến trúc

```
MiniERP.Domain          Entity thuần (state machine, không phụ thuộc EF/ASP.NET)
MiniERP.Application     CQRS nhẹ tự viết (ICommand/IQuery + Dispatcher qua DI) — không cần MediatR
MiniERP.Infrastructure  EF Core (SQLite), OAuth2 client gọi thẳng IdP ngoài, BackgroundService ETL
MiniERP.Api             Minimal API .NET 10, OpenAPI native (Scalar UI), JWT RBAC
```

**Vì sao không dùng MediatR**: từ v13 MediatR chuyển sang thương mại license. `Application/Cqrs/`
tự cài đặt dispatcher ~30 dòng (resolve handler qua `IServiceProvider` + reflection) — đủ dùng cho quy mô
này, tránh phụ thuộc ngoài không cần thiết.

**Công nghệ mới nhất được dùng**:
- .NET 10 / C# 13 (primary constructors, `file`-scoped types, collection expressions)
- Minimal API + `MapGroup` (không Controller/MVC)
- `Microsoft.AspNetCore.OpenApi` (native, thay Swashbuckle) + **Scalar** (UI OpenAPI hiện đại, thay Swagger UI)
- `Microsoft.Extensions.Http.Resilience` (`AddStandardResilienceHandler`) — retry/circuit-breaker built-in .NET 8+, thay Polly thủ công
- EF Core 10 (SQLite cho demo — đổi connection string sang SQL Server cho production)
- JWT Bearer + role-based `RequireAuthorization` — RBAC theo `PartnerType`, mirror mô hình multi-partner của ERP gốc

## Luồng nghiệp vụ demo (end-to-end)

1. `POST /api/auth/token` — lấy JWT role `Principal` (demo, không cần password thật).
2. `POST /api/partners` — tạo Dealer + Bank.
3. `POST /api/contracts` → `POST /{id}/dealer-sign` → `/approve-a1` → `/approve-a2`.
4. `POST /api/orders` → `POST /{id}/demand` → `POST /{id}/lines/{lineId}/supply` → `/approve-a1` → `/approve-a2` → `/complete`.
5. `POST /api/inventory/receive` → `/reserve` → `/deliver` (SerialNo = VIN-like).
6. `POST /api/invoices` → `/issue`.
7. `GET /api/reports/dealer-summary`, `GET /api/reports/guarantee-expiring`.

## Chạy thử

```bash
dotnet restore
dotnet build
dotnet test
dotnet run --project src/MiniERP.Api
# mở https://localhost:xxxx/scalar/v1 (OpenAPI UI, chỉ Development)
```

Hoặc Docker:
```bash
docker compose up --build
```

## Tích hợp SSO thật (`IdentityProvider` trong appsettings)
Điền `AuthorityUrl`/`ClientId`/`ClientSecret` thật của IdP (mẫu iNOS: `AuthorityUrl=/OAuth/Token`,
`ClientId=SolutionCode`, `ClientSecret=SSOSecret` từ `web.config` hệ gốc — KHÔNG hardcode trong source,
luôn qua config/secret store). `POST /api/auth/login-sso` gọi thẳng IdP, không qua wrapper trung gian
tự dựng `WebServerClient` mỗi request như hệ cũ (đây là nguyên nhân chậm đã trace ở phiên trước).
