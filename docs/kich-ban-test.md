# KỊCH BẢN KIỂM THỬ HỆ THỐNG THEO 4 TÁC NHÂN CHÍNH (NHÀ HÀNG SEN VIỆT)

> **Phạm vi:** Bộ kịch bản kiểm thử chức năng, ràng buộc nghiệp vụ, phân quyền (RBAC) và luồng liên tác nhân (End-to-End) cho **4 tác nhân chính** của Hệ thống Quản lý Đặt bàn Nhà hàng Sen Việt:
> 1. **Khách hàng** *(Tác nhân công khai — không cần đăng nhập)*
> 2. **Nhân viên phục vụ** *(Role: `Staff`)*
> 3. **Thu ngân** *(Role: `Cashier`)*
> 4. **Quản lý nhà hàng** *(Role: `Manager`)*

---

## 1. ĐIỀU KIỆN TIÊN QUYẾT & DỮ LIỆU MẪU KIỂM THỬ

Trước khi thực hiện kiểm thử, đảm bảo ứng dụng `RestaurantManagement.Web` đã khởi chạy thành công và `DataSeeder` đã nạp đủ dữ liệu nền vào PostgreSQL:

### 1.1. Tài khoản kiểm thử theo tác nhân
| Tác nhân | Mã NV | Username | Mật khẩu | Mã PIN phê duyệt | Phạm vi truy cập hợp lệ |
| :--- | :---: | :--- | :--- | :---: | :--- |
| **Khách hàng** | - | *(Không đăng nhập)* | *(Không có)* | - | Trang chủ, Đặt bàn (`BM12–BM14`), Tra cứu & Hủy bàn (`BM15`) |
| **Nhân viên phục vụ** | `NV0003` | `phucvu` | `Admin@123` | - | Sơ đồ bàn (`BM03`), Gọi món/KOT (`BM01`), Tạm tính (`BM02`), tự động chấm công khi đăng nhập |
| **Thu ngân** | `NV0002` | `thungan` | `Admin@123` | - | Sơ đồ bàn (`BM03`), Mở ca (`BM04`), Thanh toán hóa đơn (`BM05`), Đóng ca & kiểm quỹ (`BM06`), Lịch sử ca |
| **Quản lý nhà hàng** | `NV0001` | `quanly` | `Admin@123` | `1234` | Toàn quyền hệ thống (`BM00–BM11`), Phê duyệt PIN ngoại lệ, Hủy hóa đơn, Thực đơn, Nhân sự, Chấm công, Báo cáo |

### 1.2. Dữ liệu nền (Seeded Data)
- **4 Khu vực / 40 bàn:**
  - `Tầng trệt`: `T01`–`T15` (sức chứa `2`, `4`, `8` khách)
  - `Sân vườn`: `S01`–`S10` (sức chứa `4`, `6` khách)
  - `Tầng lầu`: `L01`–`L10` (sức chứa `2`, `4` khách)
  - `Phòng VIP`: `V01`–`V05` (sức chứa `12`, `20` khách)
- **Thực đơn mẫu (`M01`–`M13`) theo 3 trạm chế biến:**
  - `ColdKitchen` (Bếp lạnh): `M01` Gỏi cuốn tôm thịt (85.000đ), `M03` Nộm ngó sen tai heo (110.000đ), `M09` Chè hạt sen long nhãn (45.000đ), `M10` Bánh flan cốt dừa (35.000đ), `M11` Trái cây thập cẩm (60.000đ)
  - `HotKitchen` (Bếp nóng): `M02` Chả giò Sen Việt (95.000đ), `M04` Bò lúc lắc khoai tây (220.000đ), `M05` Cá hồi nướng sốt chanh leo (280.000đ), `M06` Gà hấp lá chanh (190.000đ), `M07` Sườn non nướng mật ong (240.000đ), `M08` Cơm chiên hải sản (130.000đ)
  - `Bar` (Quầy bar): `M12` Trà sen vàng (55.000đ), `M13` Nước ép cam tươi (50.000đ)
- **Quy tắc nghiệp vụ chính (`appsettings.json`):**
  - Đặt bàn trước tối thiểu `60 phút`, tối đa `365 ngày`; khung giờ chống trùng `±2 giờ`; giữ chỗ thanh toán cọc `15 phút`.
  - Tiền cọc: `< 6` khách = `200.000đ`; `>= 6` khách = `500.000đ`.
  - Hoàn cọc: `>= 2 giờ` trước giờ hẹn = `100%`; `0 <= Δt < 2 giờ` = `50%`; quá giờ hẹn = `0%`.
  - Ca trực: Quỹ đầu ca mặc định `2.000.000đ`; ngưỡng đi trễ `> 15 phút` (Ca ngày `08:00`, Ca tối `16:00`).
  - Hóa đơn: VAT `8%` trên `(Tiền món + Phụ thu)`; phụ thu giờ cao điểm `50.000đ` (Thứ 7, Chủ nhật `18:00–21:00`); làm tròn đến `1.000đ`.

---

## 2. KỊCH BẢN TEST CHO TÁC NHÂN 1: KHÁCH HÀNG

| Mã Test | Tên kịch bản | Loại | Điều kiện tiên quyết | Các bước thực hiện | Kết quả mong đợi |
| :--- | :--- | :---: | :--- | :--- | :--- |
| **TC-KH-01** | Tra cứu bàn trống theo khu vực và số khách | Positive | Trang web đang chạy, chưa cần đăng nhập | 1. Truy cập `/Booking/Search`.<br>2. Chọn ngày = ngày mai, giờ = `18:30`, số khách = `4`, khu vực = `Tầng trệt`.<br>3. Nhấn **Tìm bàn trống**. | Hệ thống hiển thị danh sách bàn thuộc `Tầng trệt` có `Capacity >= 4` (ví dụ `T02`, `T03`, `T04`, `T06`...) đang trống trong khung giờ `16:30–20:30`. |
| **TC-KH-02** | Đặt bàn nhóm nhỏ (`< 6` khách) và thanh toán cọc `200.000đ` | Positive | Bàn `T02` (4 chỗ) trống vào khung giờ đã chọn | 1. Từ kết quả `TC-KH-01`, chọn bàn `T02`.<br>2. Nhập Họ tên: `Nguyễn Văn An`, SĐT: `0912345678`, Email: `an.nguyen@gmail.com`, Ghi chú: `Ghế trẻ em`.<br>3. Nhấn **Xác nhận đặt bàn**.<br>4. Tại màn hình `Payment` (`BM14`), kiểm tra số tiền cọc và mã QR VietQR.<br>5. Nhấn xác nhận thanh toán cọc. | - Bước 3: Chuyển sang `/Booking/Payment`, hiển thị tiền cọc `200.000đ`, trạng thái `PendingPayment`, đếm ngược giữ chỗ `15 phút`.<br>- Bước 5: Chuyển sang trang `Lookup`, trạng thái đơn là `Confirmed`, sinh mã `BK-yyyyMMdd-XXXX`, gửi email vé điện tử (hoặc lưu trong thư mục `SentEmails` nếu chạy chế độ local). |
| **TC-KH-03** | Đặt bàn nhóm lớn (`>= 6` khách) áp dụng mức cọc `500.000đ` | Positive | Bàn `V01` (12 chỗ) trống | 1. Tra cứu bàn với `8` khách, khu vực `Phòng VIP`.<br>2. Chọn bàn `V01`, điền thông tin khách hợp lệ.<br>3. Xác nhận đặt bàn. | Hệ thống tự động tính tiền cọc là `500.000đ` (thay vì `200.000đ`) trên màn hình tạo phiếu và trang thanh toán `BM14`. |
| **TC-KH-04** | Kiểm tra ràng buộc thời gian đặt bàn (`< 60 phút` hoặc quá `365 ngày`) | Negative | Đang ở `/Booking/Search` | 1. Nhập ngày hôm nay và giờ hiện tại `+ 30 phút`.<br>2. Nhấn **Tìm bàn**. | Hệ thống từ chối truy vấn và thông báo lỗi: *"Thời gian đặt bàn phải trước thời điểm hiện tại tối thiểu 60 phút."* |
| **TC-KH-05** | Kiểm tra validation họ tên, SĐT và email khi đặt bàn | Negative | Đang ở `/Booking/Create` | 1. Nhập Họ tên: `Nguyen123` hoặc SĐT: `0123456789` (sai đầu số) hoặc Email: `abc@gmail` (thiếu domain).<br>2. Nhấn **Xác nhận đặt bàn**. | Hệ thống không tạo đơn, hiển thị thông báo lỗi validation tương ứng cho từng trường sai định dạng. |
| **TC-KH-06** | Chống đặt trùng bàn trong cùng khung giờ `±2 giờ` | Negative | Bàn `T02` đã có đơn `Confirmed` lúc `18:30` ngày mai (từ `TC-KH-02`) | 1. Một khách hàng khác tra cứu bàn vào ngày mai lúc `19:30` (trong khoảng `±2 giờ` so với `18:30`) cho `4` khách.<br>2. Kiểm tra danh sách bàn trống hoặc thử gửi request đặt trực tiếp bàn `T02`. | Bàn `T02` không xuất hiện trong kết quả tìm kiếm. Nếu cố tình POST tạo đơn cho `T02`, hệ thống trả về xung đột: *"Bàn T02 đã có người đặt hoặc đang có khách phục vụ trong khung giờ này..."* |
| **TC-KH-07** | Tra cứu và hủy đặt bàn trước giờ hẹn `>= 2 giờ` (hoàn `100%` cọc) | Positive | Có đơn `Confirmed` hẹn ngày mai (cách hiện tại `> 2 giờ`), cọc `200.000đ` | 1. Truy cập `/Booking/Lookup`.<br>2. Nhập mã đặt chỗ `BK-...` và SĐT `0912345678` → Nhấn **Tra cứu**.<br>3. Kiểm tra chính sách ước lượng hoàn tiền (`100%`).<br>4. Nhấn **Hủy đặt bàn**. | - Trạng thái đơn chuyển thành `Cancelled`.<br>- Hiển thị thông báo hoàn `100%` = `200.000đ`.<br>- Bàn `T02` được giải phóng, có thể đặt lại cho khung giờ đó. |
| **TC-KH-08** | Hủy đặt bàn sát giờ (`0 <= Δt < 2 giờ`) và quá giờ (`Δt < 0`) | Boundary | Có 2 đơn `Confirmed`:<br>- Đơn A còn `1 giờ 15 phút` tới giờ hẹn (cọc `200.000đ`)<br>- Đơn B đã quá giờ hẹn `15 phút` | 1. Tra cứu và hủy Đơn A.<br>2. Tra cứu và hủy Đơn B. | - Đơn A: Hoàn `50%` tiền cọc (`100.000đ`), ghi nhận giao dịch `DepositRefund`.<br>- Đơn B: Hoàn `0%` tiền cọc (`0đ`), thông báo không hoàn cọc theo quy định. |

---

## 3. KỊCH BẢN TEST CHO TÁC NHÂN 2: NHÂN VIÊN PHỤC VỤ

| Mã Test | Tên kịch bản | Loại | Điều kiện tiên quyết | Các bước thực hiện | Kết quả mong đợi |
| :--- | :--- | :---: | :--- | :--- | :--- |
| **TC-PV-01** | Đăng nhập trạm phục vụ và tự động ghi nhận chấm công (`BM00`, `BM11`) | Positive | Tài khoản `phucvu` đang kích hoạt | 1. Truy cập `/Account/Login`.<br>2. Nhập `phucvu` / `Admin@123` → Nhấn **Đăng nhập**. | - Đăng nhập thành công, chuyển hướng về `/Floor/Index` (`BM03`).<br>- Thanh điều hướng hiển thị badge `Phục vụ bàn (NV0003)`.<br>- Hệ thống tự động tạo bản ghi điểm danh ngày hôm nay trong `Attendances` (tự động gắn `IsLate = true` nếu trễ `> 15 phút` so với `08:00` hoặc `16:00`). |
| **TC-PV-02** | Khóa tài khoản khi nhập sai mật khẩu 5 lần liên tiếp | Negative | Tài khoản `phucvu` chưa bị khóa | 1. Tại `/Account/Login`, nhập `phucvu` và mật khẩu sai `WrongPass` 5 lần liên tiếp.<br>2. Lần thứ 6 nhập đúng `phucvu` / `Admin@123`. | - Lần 1–4: Hiển thị số lần thử còn lại (`4, 3, 2, 1`).<br>- Lần 5: Khóa tài khoản `15 phút`.<br>- Lần 6 (dù đúng mật khẩu): Bị từ chối với thông báo tài khoản đang bị khóa, vui lòng thử lại sau. |
| **TC-PV-03** | Xếp bàn cho khách vãng lai (`Walk-in`) và chặn vượt sức chứa | Positive / Negative | Bàn `T01` (2 chỗ) và `T04` (4 chỗ) đang ở trạng thái `Available`, không có booking sắp tới | 1. Tại `/Floor/Index`, thử xếp `4` khách vào bàn `T01` (2 chỗ).<br>2. Sau đó xếp `4` khách vào bàn `T04` (4 chỗ). | - Bước 1: Hệ thống báo lỗi vượt quá sức chứa (`2 chỗ`).<br>- Bước 2: Bàn `T04` chuyển sang `Occupied`, tạo đơn `ORD-...`, tự động chuyển sang trang `/Order/Table?tableId=...`. |
| **TC-PV-04** | Chặn xếp khách vãng lai vào bàn sắp có lịch đặt trước | Negative | Bàn `T06` đang `Available` nhưng có booking `Confirmed` trong vòng `2 giờ` tới | 1. Tại `/Floor/Index`, chọn bàn `T06` để xếp khách vãng lai. | Hệ thống từ chối và cảnh báo: *"Bàn T06 đã được đặt trước lúc HH:mm bởi khách ... Vui lòng chọn bàn khác..."* |
| **TC-PV-05** | Tiếp nhận khách đã đặt bàn trước (`Check-in`) | Positive | Có đơn `Confirmed` cho bàn `T02` hôm nay | 1. Tại `/Floor/Index`, tìm đơn đặt của khách ở danh sách đặt bàn hôm nay hoặc trên bàn `T02`.<br>2. Nhấn **Check-in / Xác nhận khách vào bàn**. | - Booking chuyển trạng thái `CheckedIn`.<br>- Bàn `T02` chuyển sang `Occupied`, tạo Order liên kết `BookingId` (giữ thông tin tiền cọc để khấu trừ khi thanh toán). |
| **TC-PV-06** | Ghi nhận gọi món tại bàn và gửi lệnh in báo bếp KOT đa trạm (`BM01`) | Positive | Bàn `T04` đang `Occupied`, có Order mở | 1. Tại `/Order/Table`, thêm:<br>   - `2` phần `M04` Bò lúc lắc (`HotKitchen`), ghi chú `Ít tiêu`<br>   - `1` dĩa `M01` Gỏi cuốn (`ColdKitchen`)<br>   - `2` ly `M12` Trà sen vàng (`Bar`)<br>2. Nhấn **Gửi báo bếp (SendToKitchen)**. | - Trước khi gửi: các món ở trạng thái `Pending`, tổng tiền cập nhật đúng `2*220.000 + 85.000 + 2*55.000 = 635.000đ`.<br>- Sau khi gửi: sinh **3 phiếu KOT** riêng biệt theo 3 trạm (`HotKitchen`, `ColdKitchen`, `Bar`); các món chuyển sang `Cooking`. |
| **TC-PV-07** | Sửa/xóa món chưa gửi bếp vs chặn sửa trực tiếp món đã gửi bếp | Positive / Negative | Đơn bàn `T04` có 1 món `Pending` và 1 món `Cooking` | 1. Giảm số lượng hoặc xóa món đang `Pending`.<br>2. Thử cập nhật số lượng trực tiếp trên món đang `Cooking`. | - Bước 1: Cập nhật thành công, tổng tiền tính lại chính xác.<br>- Bước 2: Hệ thống từ chối và yêu cầu dùng chức năng **Đổi món** hoặc **Trả món**. |
| **TC-PV-08** | Đổi món & Trả món đã gửi bếp có phê duyệt mã PIN Quản lý | Negative / Positive | Món `M04` đang ở trạng thái `Cooking` | 1. Chọn **Đổi món** `M04` sang `M05`, nhập lý do `Khách đổi vị`, nhập sai PIN `9999`.<br>2. Thực hiện lại với PIN Quản lý đúng `1234`.<br>3. Chọn **Trả món** với `M01`, nhập lý do `Món nguội` và PIN `1234`. | - Bước 1: Thất bại, báo lỗi thiếu/sai PIN Quản lý.<br>- Bước 2: Món cũ `Cancelled`, thêm món mới `M05` trạng thái `Pending`, ghi `AuditLog` (`ChangeOrderItem`).<br>- Bước 3: Món `M01` chuyển `Returned`, đơn giá về `0đ` (lỗi nhà hàng), tổng tiền đơn giảm tương ứng, ghi `AuditLog` (`ReturnOrderItem`). |
| **TC-PV-09** | Đánh dấu đã lên món, in phiếu tạm tính (`BM02`), yêu cầu thanh toán & dọn bàn | Positive | Đơn bàn `T04` có món đã chế biến, sau đó Thu ngân thanh toán xong | 1. Nhấn **Đã lên món** (`MarkItemServed`) cho món phục vụ.<br>2. Mở `/Order/PreBill` xem phiếu tạm tính (`VAT 8%`, trừ cọc nếu có).<br>3. Nhấn **Yêu cầu thanh toán** (`RequestPayment`).<br>4. *(Sau khi Thu ngân thanh toán)* Bàn ở trạng thái `Cleaning`, tại `/Floor/Index` nhấn **Hoàn tất dọn bàn**. | - Bước 2: Hiển thị phiếu tạm tính chuẩn khổ in nhiệt.<br>- Bước 3: Bàn chuyển sang `PreCheck` (Chờ thanh toán).<br>- Bước 4: Bàn chuyển từ `Cleaning` về `Available` (Trống). |
| **TC-PV-10** | Kiểm tra giới hạn phân quyền RBAC của Nhân viên phục vụ | Negative | Đang đăng nhập bằng tài khoản `phucvu` | 1. Truy cập trực tiếp URL `/Shift/Open`, `/Invoice/Checkout?orderId=1`, `/Menu/Index`, `/Employee/Index`, `/Report/Revenue`. | Tất cả các URL trên đều bị từ chối và chuyển hướng về `/Account/AccessDenied`. Menu điều hướng không hiển thị mục `Quầy thu ngân` và `Quản trị`. |

---

## 4. KỊCH BẢN TEST CHO TÁC NHÂN 3: THU NGÂN

| Mã Test | Tên kịch bản | Loại | Điều kiện tiên quyết | Các bước thực hiện | Kết quả mong đợi |
| :--- | :--- | :---: | :--- | :--- | :--- |
| **TC-TN-01** | Mở ca làm việc đầu ngày (`BM04`) và chặn mở ca trùng | Positive / Negative | Đăng nhập `thungan` / `Admin@123`; hiện không có ca nào `Active` | 1. Truy cập `/Shift/Open`.<br>2. Kiểm tra quầy mặc định `POS-01`, tiền đầu ca `2.000.000đ` → Nhấn **Mở ca**.<br>3. Thử mở thêm một ca mới khi ca hiện tại vẫn đang `Active`. | - Bước 2: Tạo `ShiftSession` mã `SH-...`, trạng thái `Active`, `TheoreticalCash = 2.000.000đ`.<br>- Bước 3: Hệ thống không cho mở ca thứ 2, chuyển hướng về ca đang mở với thông báo yêu cầu đóng ca hiện tại trước. |
| **TC-TN-02** | Chặn lập hóa đơn khi chưa mở ca làm việc | Negative | Không có ca nào `Active`; có bàn `PreCheck` cần thanh toán | 1. Đăng nhập `thungan`.<br>2. Truy cập `/Invoice/Checkout?orderId=...` trước khi mở ca. | Hệ thống tự động chuyển hướng sang `/Shift/Open` và thông báo: *"Bạn cần mở ca làm việc (BM04) trước khi thanh toán và lập hóa đơn."* |
| **TC-TN-03** | Thanh toán hóa đơn bằng Tiền mặt (`Cash`) và kiểm tra tiền thừa / thiếu | Negative / Positive | Ca đang `Active`; bàn có đơn tổng món `500.000đ` (ngày thường, không cọc) | 1. Vào `/Invoice/Checkout` cho đơn hàng.<br>2. Chọn `Tiền mặt`, nhập tiền khách đưa `400.000đ` → Nhấn **Thanh toán**.<br>3. Nhập lại tiền khách đưa `600.000đ` → Nhấn **Thanh toán**. | - Bước 2: Báo lỗi không đủ tiền thanh toán.<br>- Bước 3: Thành công! `VAT 8% = 40.000đ`, `FinalAmount = 540.000đ`, `ChangeAmount = 60.000đ`. Tạo hóa đơn `INV-...` (`Paid`), đóng Order (`ClosedAt != null`), chuyển bàn sang `Cleaning`. |
| **TC-TN-04** | Thanh toán hóa đơn bằng chuyển khoản VietQR động (`BankTransfer`) | Positive | Ca đang `Active`; bàn có đơn cần thanh toán | 1. Vào `/Invoice/Checkout`.<br>2. Chọn phương thức `Chuyển khoản (VietQR)`.<br>3. Kiểm tra ảnh/URL VietQR hiển thị đúng số tiền cần trả và nội dung `THANHTOAN ORD-...`.<br>4. Nhấn **Xác nhận đã thu tiền**. | Hóa đơn lưu với `PaymentMethod = BankTransfer`, `Status = Paid`, ghi nhận doanh thu chuyển khoản vào thống kê ca hiện tại; bàn chuyển sang `Cleaning`. |
| **TC-TN-05** | Kiểm tra công thức tính phụ thu giờ cao điểm cuối tuần, VAT `8%` và trừ cọc | Positive | Đơn mở vào Thứ 7/CN trong khung `18:00–21:00`, tổng món `500.000đ`, có booking cọc `200.000đ` | 1. Vào `/Invoice/Checkout` cho đơn này.<br>2. Đối chiếu bảng chi tiết tính tiền trước khi bấm thanh toán. | - Tiền món: `500.000đ`<br>- Phụ thu cao điểm: `50.000đ`<br>- Thuế VAT (8% trên `550.000đ`): `44.000đ`<br>- Khấu trừ tiền cọc: `-200.000đ`<br>- Thực thu: `394.000đ`. |
| **TC-TN-06** | Đóng ca và kiểm quỹ cuối ca khớp 100% lý thuyết (`BM06`) | Positive | Ca đang `Active` với đầu ca `2.000.000đ`, đã thu tiền mặt `540.000đ` → Lý thuyết = `2.540.000đ` | 1. Vào `/Shift/Close`.<br>2. Nhập số lượng tờ tiền theo mệnh giá sao cho tổng = `2.540.000đ` (ví dụ: 5 tờ 500k + 2 tờ 20k).<br>3. Để trống giải trình và PIN Quản lý → Nhấn **Đóng ca**. | Đóng ca thành công (`Difference = 0đ`), không yêu cầu giải trình hay PIN Quản lý, lưu `ShiftStatus.Closed` và chuyển về `/Shift/History`. |
| **TC-TN-07** | Đóng ca khi phát hiện chênh lệch két tiền mặt (`Difference != 0`) | Negative / Positive | Tiền mặt lý thuyết ca = `2.540.000đ` | 1. Tại `/Shift/Close`, nhập tổng tiền thực tế = `2.500.000đ` (thiếu `40.000đ`).<br>2. Bấm **Đóng ca** khi chưa nhập giải trình.<br>3. Nhập giải trình *"Thối nhầm cho khách bàn T04"*, nhập sai PIN Quản lý → Bấm **Đóng ca**.<br>4. Nhập giải trình + PIN Quản lý đúng `1234` → Bấm **Đóng ca**. | - Bước 2: Chặn đóng ca, yêu cầu bắt buộc nhập giải trình lý do chênh lệch.<br>- Bước 3: Chặn đóng ca, báo lỗi mã PIN Quản lý không chính xác.<br>- Bước 4: Đóng ca thành công, ghi nhận `Difference = -40.000đ`, `ApprovedBy = Nguyễn Văn Quản Lý`, lưu vết `AuditLog` (`CloseShift`). |
| **TC-TN-08** | Kiểm tra giới hạn phân quyền RBAC của Thu ngân | Negative | Đang đăng nhập bằng tài khoản `thungan` | 1. Thử hủy một hóa đơn bằng cách POST `/Invoice/Cancel`.<br>2. Truy cập `/Menu/Index`, `/Employee/Index`, `/Attendance/Index`, `/Report/Revenue`. | Bị chặn hoàn toàn và chuyển hướng `/Account/AccessDenied` (vì các chức năng này thuộc policy `ManagerOnly`). |

---

## 5. KỊCH BẢN TEST CHO TÁC NHÂN 4: QUẢN LÝ NHÀ HÀNG

| Mã Test | Tên kịch bản | Loại | Điều kiện tiên quyết | Các bước thực hiện | Kết quả mong đợi |
| :--- | :--- | :---: | :--- | :--- | :--- |
| **TC-QL-01** | Đăng nhập quản trị và kiểm tra menu toàn quyền (`BM00`) | Positive | Tài khoản `quanly` / `Admin@123` | 1. Đăng nhập với `quanly` / `Admin@123`.<br>2. Kiểm tra thanh điều hướng trên cùng. | Đăng nhập thành công, badge `Quản lý (NV0001)`, hiển thị đầy đủ 3 phân hệ: `Sơ đồ bàn`, `Quầy thu ngân`, `Quản trị` (Thực đơn, Nhân viên, Chấm công, Báo cáo doanh thu, Món bán chạy). |
| **TC-QL-02** | Thêm món mới vào thực đơn và kiểm tra trùng mã/tên món (`BM07`) | Positive / Negative | Đang ở `/Menu/Index` | 1. Vào `/Menu/Create`, nhập trùng mã `M01` → Lưu.<br>2. Đổi mã mới `M14`, tên `Lẩu hải sản Sen Việt`, danh mục `Món chính`, giá `350.000đ`, trạm `HotKitchen` → Lưu. | - Bước 1: Báo lỗi xung đột mã món đã tồn tại.<br>- Bước 2: Thêm món `M14` thành công và hiển thị trong thực đơn. |
| **TC-QL-03** | Điều chỉnh giá món ăn có mã PIN và lưu lịch sử giá (`MenuPriceHistories`) | Negative / Positive | Món `M14` đang giá `350.000đ` | 1. Tại `/Menu/Index`, thực hiện **Cập nhật giá** (`UpdatePrice`) lên `380.000đ` với PIN sai `0000`.<br>2. Nhập PIN đúng `1234`. | - Bước 1: Từ chối cập nhật do sai PIN Quản lý.<br>- Bước 2: Cập nhật giá mới `380.000đ`, lưu bản ghi mới vào `MenuPriceHistories` (`OldPrice = 350.000`, `NewPrice = 380.000`) và `AuditLogs`. Các đơn đã gọi trước đó vẫn giữ nguyên giá cũ. |
| **TC-QL-04** | Bật/tắt trạng thái món tạm hết nguyên liệu (`ToggleAvailability`) | Positive | Món `M05` đang `IsAvailable = true` | 1. Tại `/Menu/Index`, nhấn **Tạm ngưng** cho món `M05`.<br>2. Mở màn hình gọi món `/Order/Table` của một bàn đang phục vụ để kiểm tra. | - Món `M05` chuyển `IsAvailable = false`.<br>- Trên màn hình gọi món (`onlyAvailable: true`), món `M05` không còn cho phép gọi mới. |
| **TC-QL-05** | Quản lý hồ sơ nhân sự: Tạo, validate CCCD/SĐT và vô hiệu hóa tài khoản (`BM08`) | Negative / Positive | Đang ở `/Employee/Index` | 1. Vào `/Employee/Create`, nhập CCCD `11 số` hoặc SĐT sai định dạng → Lưu.<br>2. Nhập hợp lệ: `NV0005`, `Phạm Thị Lan`, `phucvu2`, Role `Staff`, SĐT `0987654321`, CCCD `079090000005`, Pass `Staff@123` → Lưu.<br>3. Vào `/Employee/Edit` của `phucvu2`, bỏ chọn `IsActive` → Lưu.<br>4. Thử đăng nhập bằng `phucvu2`. | - Bước 1: Báo lỗi validation (`CCCD phải gồm đúng 12 chữ số`).<br>- Bước 2: Tạo nhân viên thành công.<br>- Bước 3–4: Khi `IsActive = false`, đăng nhập `phucvu2` bị từ chối: *"Tài khoản nhân viên này đã bị ngưng hoạt động."* |
| **TC-QL-06** | Duyệt giải trình chấm công đi trễ bằng mã PIN (`BM11`) | Negative / Positive | Có bản ghi chấm công bị gắn cờ `IsLate = true` trong ngày | 1. Vào `/Attendance/Index`.<br>2. Chọn dòng nhân viên đi trễ, nhập lý do `Kẹt xe`, nhập sai PIN → Nhấn **Duyệt**.<br>3. Nhập lại PIN đúng `1234` → Nhấn **Duyệt**. | - Bước 2: Từ chối duyệt.<br>- Bước 3: Duyệt thành công, gỡ cờ đi trễ (`IsLate = false`), ghi nhận `ApprovedBy` là Quản lý và lý do giải trình. |
| **TC-QL-07** | Hủy hóa đơn đã thanh toán (`Voided`) có kiểm soát PIN & mở lại đơn | Negative / Positive | Có hóa đơn `INV-...` đã `Paid` | 1. Vào `/Invoice/Detail?invoiceId=...`.<br>2. Thực hiện **Hủy hóa đơn** với PIN sai.<br>3. Thực hiện lại với lý do `Thu ngân chọn nhầm phương thức` và PIN `1234`. | - Bước 2: Từ chối hủy.<br>- Bước 3: Hóa đơn chuyển `Voided`, lưu `VoidReason` và `VoidedAt`, Order tương ứng được mở lại (`ClosedAt = null`, bàn về `Occupied`), ghi `AuditLog` (`CancelInvoice`). Hóa đơn bị hủy không còn tính vào doanh thu báo cáo. |
| **TC-QL-08** | Xem báo cáo doanh thu và xuất file CSV (`BM09`) | Positive | Có ít nhất 1 hóa đơn `Paid` tiền mặt và 1 hóa đơn `Paid` VietQR trong khoảng ngày lọc | 1. Vào `/Report/Revenue`, chọn khoảng ngày chứa các hóa đơn đã thanh toán.<br>2. Kiểm tra các chỉ số tổng hợp và bảng chi tiết theo ngày.<br>3. Nhấn **Xuất CSV** (`ExportRevenueCsv`). | - Hiển thị khớp số hóa đơn, tổng tiền món, phụ thu, VAT 8%, giảm giá, thực thu thuần, tách đúng `Tiền mặt` vs `Chuyển khoản VietQR` (không tính hóa đơn `Voided`).<br>- Tải về file `BaoCaoDoanhThu_yyyyMMdd_yyyyMMdd.csv` đúng encoding UTF-8 không lỗi font tiếng Việt. |
| **TC-QL-09** | Phân tích món bán chạy & món tiêu thụ kém (`BM10`) | Positive | Có các đơn hàng đã thanh toán/đóng bàn trong kỳ | 1. Vào `/Report/BestSellers`, chọn khoảng ngày lọc.<br>2. Kiểm tra danh sách Top bán chạy và danh sách món bán chậm (`< 5` phần). | - Top bán chạy sắp xếp giảm dần theo số lượng bán và doanh thu (tối đa 10 món).<br>- Danh sách món tiêu thụ kém bao gồm cả các món có doanh số `0` hoặc `< 5` phần từ bảng thực đơn, không tính món đã bị `Cancelled`. |

---

## 6. KỊCH BẢN KIỂM THỬ LIÊN TÁC NHÂN (END-TO-END FLOWS)

### `TC-E2E-01`: Chu trình khép kín Đặt bàn cọc → Phục vụ → Phê duyệt ngoại lệ → Thu ngân chốt ca → Quản lý báo cáo
1. **[Khách hàng]**: Tìm bàn trống cho `6` người vào ngày mai tại `Sân vườn` → Chọn bàn `S02` (6 chỗ) → Đặt bàn & thanh toán cọc `500.000đ` → Nhận mã `BK-...`.
2. **[Quản lý]**: Đăng nhập `quanly` kiểm tra sơ đồ thấy bàn `S02` có lịch đặt; đảm bảo các món đặt đều đang mở bán.
3. **[Thu ngân]**: Đăng nhập `thungan` → Mở ca `POS-01` với quỹ đầu ca `2.000.000đ`.
4. **[Phục vụ]**: Đăng nhập `phucvu` → Check-in mã đặt bàn `BK-...` vào bàn `S02` → Gọi `2` Bò lúc lắc (`M04`), `2` Trà sen vàng (`M12`) → Gửi bếp (sinh 2 KOT: Bếp nóng & Bar) → Đổi `1` Bò lúc lắc sang Cá hồi (`M05`) bằng PIN Quản lý `1234` → Gửi bếp món mới → Đánh dấu đã lên món → In tạm tính → Bấm **Yêu cầu thanh toán**.
5. **[Thu ngân]**: Mở màn hình thanh toán bàn `S02` → Kiểm tra tự động khấu trừ `500.000đ` tiền cọc → Thu phần chênh lệch bằng Tiền mặt → Hoàn tất thanh toán → Đóng ca cuối ngày bằng cách đếm đúng tiền mặt lý thuyết.
6. **[Phục vụ]**: Xác nhận dọn dẹp xong bàn `S02` → Bàn quay về `Available`.
7. **[Quản lý]**: Vào Báo cáo doanh thu & Món bán chạy → Kiểm tra doanh thu hóa đơn mới, lịch sử đóng ca và `AuditLogs` ghi nhận thao tác đổi món đều khớp 100%.

### `TC-E2E-02`: Phục vụ khách vãng lai khung giờ cao điểm cuối tuần (18:00–21:00 Thứ 7/Chủ nhật)
1. **[Thu ngân]**: Mở ca làm việc `POS-01` (`2.000.000đ`).
2. **[Phục vụ]**: Xếp `2` khách vãng lai vào bàn `T01` (vào khung `18:30` Thứ 7) → Gọi `1` Gà hấp lá chanh (`M06` = `190.000đ`) + `2` Nước cam (`M13` = `100.000đ`) → Gửi KOT → Phục vụ → Yêu cầu thanh toán.
3. **[Thu ngân]**: Lập hóa đơn bàn `T01` → Kiểm tra hệ thống tự động cộng **phụ thu cao điểm `50.000đ`**, tính **VAT 8%** trên `290.000 + 50.000 = 340.000đ` (`27.000đ` sau làm tròn `1.000đ`), tổng thực thu `367.000đ` → Thanh toán bằng VietQR.
4. **[Phục vụ]**: Hoàn tất dọn bàn `T01`.

### `TC-E2E-03`: Xử lý xung đột giữ bàn đồng thời giữa 2 khách hàng / nhân viên phục vụ
1. **[Khách hàng A & Khách hàng B]**: Cùng tìm kiếm bàn `T08` vào cùng một ngày/giờ và cùng mở form đặt bàn.
2. **[Khách hàng A]**: Bấm xác nhận đặt bàn `T08` trước → Thành công (trạng thái `PendingPayment` giữ chỗ `15 phút`).
3. **[Khách hàng B]**: Bấm xác nhận đặt bàn `T08` ngay sau đó → Bị từ chối với thông báo xung đột lịch (`Booking.Conflict`).
4. **[Phục vụ]**: Nếu `T08` sắp đến giờ hẹn của Khách A (`<= 2 giờ`), thử xếp khách vãng lai vào `T08` → Bị hệ thống chặn để bảo vệ quyền lợi khách đã đặt cọc.

---

## 7. MA TRẬN PHÂN QUYỀN KIỂM THỬ (RBAC MATRIX)

| Chức năng / Đường dẫn | Khách hàng (Ẩn danh) | Phục vụ (`phucvu`) | Thu ngân (`thungan`) | Quản lý (`quanly`) |
| :--- | :---: | :---: | :---: | :---: |
| Tra cứu, đặt bàn, cọc, hủy đặt (`/Booking/*`) | Cho phép | Cho phép | Cho phép | Cho phép |
| Sơ đồ bàn, Check-in, Dọn bàn (`/Floor/*`) | Chặn (Login) | Cho phép | Cho phép | Cho phép |
| Gọi món, Gửi KOT, Đổi/Trả món, Tạm tính (`/Order/*`) | Chặn (Login) | Cho phép *(Đổi/Trả đã nấu cần PIN QL)* | Cho phép *(Đổi/Trả đã nấu cần PIN QL)* | Cho phép *(Có PIN duyệt)* |
| Mở ca, Đóng ca, Lịch sử ca (`/Shift/*`) | Chặn (Login) | Chặn (`AccessDenied`) | Cho phép *(Lệch quỹ cần PIN QL)* | Cho phép |
| Lập hóa đơn, Thanh toán (`/Invoice/Checkout`, `Detail`) | Chặn (Login) | Chặn (`AccessDenied`) | Cho phép | Cho phép |
| Hủy hóa đơn đã thanh toán (`/Invoice/Cancel`) | Chặn (Login) | Chặn (`AccessDenied`) | Chặn (`AccessDenied`) | Cho phép *(Cần PIN QL)* |
| Quản lý thực đơn & giá (`/Menu/*`) | Chặn (Login) | Chặn (`AccessDenied`) | Chặn (`AccessDenied`) | Cho phép *(UpdatePrice cần PIN QL)* |
| Quản lý nhân sự (`/Employee/*`) | Chặn (Login) | Chặn (`AccessDenied`) | Chặn (`AccessDenied`) | Cho phép |
| Duyệt chấm công (`/Attendance/*`) | Chặn (Login) | Chặn (`AccessDenied`) | Chặn (`AccessDenied`) | Cho phép *(Cần PIN QL)* |
| Báo cáo doanh thu & Món bán chạy (`/Report/*`) | Chặn (Login) | Chặn (`AccessDenied`) | Chặn (`AccessDenied`) | Cho phép |

