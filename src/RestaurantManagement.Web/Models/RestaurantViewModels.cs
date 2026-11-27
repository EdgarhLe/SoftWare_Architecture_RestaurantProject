using System.ComponentModel.DataAnnotations;
using RestaurantManagement.Business.DTOs;
using RestaurantManagement.Common.Enums;
using RestaurantManagement.Common.Validation;

namespace RestaurantManagement.Web.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
    [Display(Name = "Tên đăng nhập")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu")]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

public class FloorIndexViewModel
{
    public List<AreaWithTablesDto> FloorPlan { get; set; } = new();
    public List<BookingConfirmationDto> TodayBookings { get; set; } = new();
    public int? SelectedAreaId { get; set; }
    public DateOnly SelectedDate { get; set; }
    public bool IsToday => SelectedDate == DateOnly.FromDateTime(DateTime.Now);
}

public class SeatWalkInViewModel
{
    public int TableId { get; set; }
    public string TableCode { get; set; } = string.Empty;
    public int Capacity { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập số lượng khách")]
    [Range(1, 30, ErrorMessage = "Số lượng khách từ 1 đến 30 người")]
    public int GuestCount { get; set; } = 2;

    [StringLength(100, MinimumLength = 2, ErrorMessage = "Tên khách hàng từ 2 đến 100 ký tự")]
    [RegularExpression(ValidationRules.NamePattern, ErrorMessage = ValidationRules.NameErrorMessage)]
    [Display(Name = "Tên khách hàng (tùy chọn)")]
    public string? CustomerName { get; set; }

    [RegularExpression(ValidationRules.PhonePattern, ErrorMessage = ValidationRules.PhoneErrorMessage)]
    [Display(Name = "Số điện thoại (tùy chọn)")]
    public string? PhoneNumber { get; set; }
}

public class CheckInBookingViewModel
{
    public int BookingId { get; set; }
    public string BookingCode { get; set; } = string.Empty;
    public int TableId { get; set; }
    public string TableCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public int GuestCount { get; set; }
}

public class OrderTableViewModel
{
    public OrderDto Order { get; set; } = null!;
    public TableDto Table { get; set; } = null!;
    public List<CategoryDto> Categories { get; set; } = new();
    public List<MenuItemDto> MenuItems { get; set; } = new();
    public int? SelectedCategoryId { get; set; }
}

public class AddOrderItemViewModel
{
    public int OrderId { get; set; }
    public int MenuItemId { get; set; }

    [Range(1, 50, ErrorMessage = "Số lượng món phải từ 1 đến 50")]
    public int Quantity { get; set; } = 1;

    [StringLength(200, ErrorMessage = "Ghi chú tối đa 200 ký tự")]
    public string? Note { get; set; }
}

public class ChangeOrderItemViewModel
{
    public int OrderDetailId { get; set; }
    public int OrderId { get; set; }
    public int NewMenuItemId { get; set; }
    [Range(1, 50, ErrorMessage = "Số lượng mới từ 1 đến 50")]
    public int NewQuantity { get; set; } = 1;
    public string Reason { get; set; } = string.Empty;
    public string ManagerPin { get; set; } = string.Empty;
}

public class ReturnOrderItemViewModel
{
    public int OrderDetailId { get; set; }
    public int OrderId { get; set; }
    public int ReturnQuantity { get; set; }
    public bool RestaurantFault { get; set; } = true;
    public string Reason { get; set; } = string.Empty;
    public string ManagerPin { get; set; } = string.Empty;
}

public class PreBillViewModel
{
    public PreBillDto PreBill { get; set; } = null!;
}

public class OpenShiftViewModel
{
    public int StaffId { get; set; }
    public string StaffName { get; set; } = string.Empty;

    [Display(Name = "Tiền mặt đầu ca (VNĐ)")]
    [Range(0, 100000000, ErrorMessage = "Tiền mặt không hợp lệ")]
    public decimal InitialCash { get; set; } = 2000000m;

    [Display(Name = "Mã quầy POS")]
    public string TerminalId { get; set; } = "POS-01";
}

public class CashDenominationInput
{
    public int Denomination { get; set; }

    [Range(0, 100000, ErrorMessage = "Số lượng tờ tiền không được âm")]
    public int Quantity { get; set; }

    public decimal Total => (decimal)Denomination * Quantity;
}

public class CloseShiftViewModel
{
    public ShiftSessionDto? Shift { get; set; }
    public List<CashDenominationInput> Denominations { get; set; } = new();

    public decimal TotalCashCounted => Denominations?.Sum(d => d.Total) ?? 0;
    public decimal ExpectedCash => Shift?.TheoreticalCash ?? 0;
    public decimal Discrepancy => TotalCashCounted - ExpectedCash;

    [Display(Name = "Lý do chênh lệch (bắt buộc nếu chênh lệch)")]
    public string? DiscrepancyReason { get; set; }

    [Display(Name = "Mã PIN Quản lý (xác nhận)")]
    public string? ManagerPin { get; set; }
}

public class CheckoutViewModel
{
    public InvoiceDto? Invoice { get; set; }
    public int OrderId { get; set; }

    [RegularExpression(ValidationRules.PhonePattern, ErrorMessage = ValidationRules.PhoneErrorMessage)]
    public string? CustomerPhone { get; set; }

    [Display(Name = "Phương thức thanh toán")]
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;

    [Display(Name = "Tiền khách đưa (VNĐ)")]
    [Range(0, 1000000000, ErrorMessage = "Tiền khách đưa không hợp lệ")]
    public decimal CashGiven { get; set; }

    public decimal FinalAmount { get; set; }

    public decimal ChangeAmount => CashGiven >= (Invoice?.FinalAmount ?? FinalAmount) ? CashGiven - (Invoice?.FinalAmount ?? FinalAmount) : 0;
    public string? VietQrUrl { get; set; }
}

public class CancelInvoiceViewModel
{
    public int InvoiceId { get; set; }
    public string InvoiceCode { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string ManagerPin { get; set; } = string.Empty;
}

public class SearchAvailableTablesViewModel
{
    [Display(Name = "Ngày dùng bữa")]
    public DateOnly BookingDate { get; set; } = DateOnly.FromDateTime(DateTime.Now.AddDays(1));

    [Display(Name = "Giờ dùng bữa")]
    public TimeOnly BookingTime { get; set; } = new TimeOnly(18, 30);

    [Display(Name = "Số lượng khách")]
    [Range(1, 50, ErrorMessage = "Số khách phải từ 1 đến 50")]
    public int GuestCount { get; set; } = 4;

    [Display(Name = "Khu vực ưu tiên (tùy chọn)")]
    public int? PreferredAreaId { get; set; }

    public List<AvailableTableDto> AvailableTables { get; set; } = new();
    public bool HasSearched { get; set; }
}

public class CreateBookingViewModel
{
    public int TableId { get; set; }
    public string TableCode { get; set; } = string.Empty;
    public string AreaName { get; set; } = string.Empty;
    public int Capacity { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập họ tên")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Họ tên từ 2 đến 100 ký tự")]
    [RegularExpression(ValidationRules.NamePattern, ErrorMessage = ValidationRules.NameErrorMessage)]
    [Display(Name = "Họ và tên")]
    public string CustomerName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
    [RegularExpression(ValidationRules.PhonePattern, ErrorMessage = ValidationRules.PhoneErrorMessage)]
    [Display(Name = "Số điện thoại")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập địa chỉ email để tự động nhận vé điện tử sau khi cọc")]
    [RegularExpression(ValidationRules.EmailPattern, ErrorMessage = ValidationRules.EmailErrorMessage)]
    [Display(Name = "Email (nhận xác nhận)")]
    public string? Email { get; set; }

    [Display(Name = "Ngày đặt")]
    public DateOnly BookingDate { get; set; }

    [Display(Name = "Giờ đặt")]
    public TimeOnly BookingTime { get; set; }

    [Range(1, 50, ErrorMessage = "Số khách phải từ 1 đến 50")]
    [Display(Name = "Số khách")]
    public int GuestCount { get; set; }

    [StringLength(300, ErrorMessage = "Yêu cầu đặc biệt tối đa 300 ký tự")]
    [Display(Name = "Yêu cầu đặc biệt")]
    public string? SpecialRequests { get; set; }

    public decimal DepositAmount { get; set; }
}

public class BookingConfirmationViewModel
{
    public BookingConfirmationDto Booking { get; set; } = null!;
    public string VietQrUrl { get; set; } = string.Empty;
    public int HoldRemainingMinutes { get; set; } = 15;
}

public class LookupBookingViewModel
{
    [Display(Name = "Mã đặt bàn (Ví dụ: BK-20261014-XXXX)")]
    public string? BookingCode { get; set; }

    [RegularExpression(ValidationRules.PhonePattern, ErrorMessage = ValidationRules.PhoneErrorMessage)]
    [Display(Name = "Số điện thoại")]
    public string? PhoneNumber { get; set; }

    public BookingConfirmationDto? Booking { get; set; }
    public decimal EstimatedRefund { get; set; }
    public decimal RefundPercentage { get; set; }
    public string? RefundPolicyNote { get; set; }
    public bool HasSearched { get; set; }
}

public class MenuManagementViewModel
{
    public List<CategoryDto> Categories { get; set; } = new();
    public List<MenuItemDto> Items { get; set; } = new();
    public int? SelectedCategoryId { get; set; }
}

public class MenuItemFormViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Mã món không được trống")]
    [RegularExpression(ValidationRules.ItemCodePattern, ErrorMessage = ValidationRules.ItemCodeErrorMessage)]
    [Display(Name = "Mã món")]
    public string ItemCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tên món không được trống")]
    [StringLength(120, MinimumLength = 2, ErrorMessage = "Tên món từ 2 đến 120 ký tự")]
    [Display(Name = "Tên món ăn / đồ uống")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Mô tả tối đa 500 ký tự")]
    [Display(Name = "Mô tả")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập đơn giá")]
    [Range(1000, 100000000, ErrorMessage = "Đơn giá từ 1.000 VNĐ đến 100.000.000 VNĐ")]
    [Display(Name = "Đơn giá (VNĐ)")]
    public decimal Price { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn danh mục")]
    [Display(Name = "Danh mục")]
    public int CategoryId { get; set; }

    [Display(Name = "Trạm chế biến")]
    public KitchenStation Station { get; set; } = KitchenStation.HotKitchen;

    [Required(ErrorMessage = "Đơn vị tính không được trống")]
    [StringLength(30, MinimumLength = 1, ErrorMessage = "Đơn vị tính từ 1 đến 30 ký tự")]
    [Display(Name = "Đơn vị tính")]
    public string Unit { get; set; } = "Phần";

    [Display(Name = "Đang kinh doanh")]
    public bool IsAvailable { get; set; } = true;
}

public class UpdatePriceViewModel
{
    public int MenuItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public decimal CurrentPrice { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập giá mới")]
    [Range(1000, 100000000, ErrorMessage = "Đơn giá phải từ 1.000 VNĐ đến 100.000.000 VNĐ")]
    [Display(Name = "Giá mới (VNĐ)")]
    public decimal NewPrice { get; set; }

    [Required(ErrorMessage = "Yêu cầu mã PIN Quản lý để đổi giá")]
    [Display(Name = "Mã PIN Quản lý")]
    public string ManagerPin { get; set; } = string.Empty;
}

public class EmployeeListViewModel
{
    public List<StaffDto> Staffs { get; set; } = new();
}

public class EmployeeFormViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Mã nhân viên không được trống")]
    [RegularExpression(ValidationRules.StaffCodePattern, ErrorMessage = ValidationRules.StaffCodeErrorMessage)]
    [Display(Name = "Mã nhân viên")]
    public string StaffCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Họ tên không được trống")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Họ tên từ 2 đến 100 ký tự")]
    [RegularExpression(ValidationRules.NamePattern, ErrorMessage = ValidationRules.NameErrorMessage)]
    [Display(Name = "Họ và tên")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tên đăng nhập không được trống")]
    [RegularExpression(ValidationRules.UsernamePattern, ErrorMessage = ValidationRules.UsernameErrorMessage)]
    [Display(Name = "Tên đăng nhập")]
    public string Username { get; set; } = string.Empty;

    [Display(Name = "Mật khẩu (để trống nếu không đổi)")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
    [DataType(DataType.Password)]
    public string? Password { get; set; }

    [Display(Name = "Vai trò")]
    public StaffRole Role { get; set; } = StaffRole.Staff;

    [Required(ErrorMessage = "Số điện thoại không được trống")]
    [RegularExpression(ValidationRules.PhonePattern, ErrorMessage = ValidationRules.PhoneErrorMessage)]
    [Display(Name = "Số điện thoại")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Số CCCD không được trống")]
    [RegularExpression(ValidationRules.CitizenIdPattern, ErrorMessage = ValidationRules.CitizenIdErrorMessage)]
    [Display(Name = "Số CCCD / CMND")]
    public string CitizenId { get; set; } = string.Empty;

    [RegularExpression(ValidationRules.EmailPattern, ErrorMessage = ValidationRules.EmailErrorMessage)]
    [Display(Name = "Email")]
    public string? Email { get; set; }

    [RegularExpression(ValidationRules.ManagerPinPattern, ErrorMessage = ValidationRules.ManagerPinErrorMessage)]
    [Display(Name = "Mã PIN Quản lý (nếu là Quản lý)")]
    public string? ManagerPin { get; set; }

    [Display(Name = "Đang hoạt động")]
    public bool IsActive { get; set; } = true;
}

public class AttendanceListViewModel
{
    public DateOnly SelectedDate { get; set; } = DateOnly.FromDateTime(DateTime.Now);
    public List<AttendanceDto> Attendances { get; set; } = new();
}

public class ApproveAttendanceViewModel
{
    public int AttendanceId { get; set; }
    public string StaffName { get; set; } = string.Empty;
    public int MinutesLate { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập lý do duyệt đi trễ")]
    [Display(Name = "Lý do duyệt")]
    public string Reason { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập PIN Quản lý")]
    [Display(Name = "Mã PIN Quản lý")]
    public string ManagerPin { get; set; } = string.Empty;
}

public class RevenueReportViewModel
{
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public RevenueReportDto Report { get; set; } = null!;
}

public class BestSellersReportViewModel
{
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public BestSellersReportDto Report { get; set; } = null!;
}
