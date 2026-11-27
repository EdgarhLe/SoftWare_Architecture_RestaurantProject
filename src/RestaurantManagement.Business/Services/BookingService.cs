using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RestaurantManagement.Business.DTOs;
using RestaurantManagement.Business.Options;
using RestaurantManagement.Business.Policies;
using RestaurantManagement.Common.Enums;
using RestaurantManagement.Common.Results;
using RestaurantManagement.Common.Validation;
using RestaurantManagement.DataAccess.Entities;
using RestaurantManagement.DataAccess.UnitOfWork;

namespace RestaurantManagement.Business.Services;

public interface IBookingService
{
    Task<Result<List<AvailableTableDto>>> SearchAvailableTablesAsync(SearchAvailableTablesQueryDto query, CancellationToken cancellationToken = default);
    Task<Result<BookingConfirmationDto>> CreateBookingAsync(CreateBookingDto dto, CancellationToken cancellationToken = default);
    Task<Result<BookingConfirmationDto>> ConfirmBookingPaymentAsync(int bookingId, string transactionRef, string provider, CancellationToken cancellationToken = default);
    Task<Result<BookingConfirmationDto>> GetBookingByIdAsync(int bookingId, CancellationToken cancellationToken = default);
    Task<Result<BookingConfirmationDto>> GetBookingByCodeAsync(string bookingCode, string phoneNumber, CancellationToken cancellationToken = default);
    Task<Result<CancelBookingResultDto>> CancelBookingAsync(CancelBookingDto dto, CancellationToken cancellationToken = default);
    Task<List<BookingConfirmationDto>> GetTodayBookingsAsync(CancellationToken cancellationToken = default);
    Task<List<BookingConfirmationDto>> GetBookingsByDateAsync(DateOnly date, CancellationToken cancellationToken = default);
    Task<int> ExpirePendingBookingsAsync(CancellationToken cancellationToken = default);
}

public class BookingService : IBookingService
{
    private readonly IUnitOfWork _uow;
    private readonly DepositPolicy _depositPolicy;
    private readonly RefundPolicy _refundPolicy;
    private readonly RestaurantRulesOptions _options;
    private readonly TimeZoneInfo _timeZone;
    private readonly IEmailService _emailService;

    public BookingService(
        IUnitOfWork uow,
        DepositPolicy depositPolicy,
        RefundPolicy refundPolicy,
        IOptions<RestaurantRulesOptions> options,
        IEmailService emailService)
    {
        _uow = uow;
        _depositPolicy = depositPolicy;
        _refundPolicy = refundPolicy;
        _options = options.Value;
        _emailService = emailService;

        try
        {
            _timeZone = TimeZoneInfo.FindSystemTimeZoneById(_options.TimeZone);
        }
        catch
        {
            _timeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }
    }

    public async Task<Result<List<AvailableTableDto>>> SearchAvailableTablesAsync(SearchAvailableTablesQueryDto query, CancellationToken cancellationToken = default)
    {
        var localBookingDateTime = query.BookingDate.ToDateTime(query.BookingTime);
        var bookingDateTimeUtc = TimeZoneInfo.ConvertTimeToUtc(localBookingDateTime, _timeZone);
        var minBookingTimeUtc = DateTime.UtcNow.AddMinutes(_options.Booking.MinLeadMinutes);

        if (bookingDateTimeUtc < minBookingTimeUtc)
        {
            return Result<List<AvailableTableDto>>.Failure(Error.Validation(
                "Booking.MinLeadTime",
                $"Thời gian đặt bàn phải trước thời điểm hiện tại tối thiểu {_options.Booking.MinLeadMinutes} phút."));
        }

        if (bookingDateTimeUtc > DateTime.UtcNow.AddDays(365))
        {
            return Result<List<AvailableTableDto>>.Failure(Error.Validation(
                "Booking.MaxAdvanceDays",
                "Chỉ hỗ trợ đặt bàn trước tối đa 365 ngày."));
        }

        if (query.GuestCount < 1 || query.GuestCount > 50)
        {
            return Result<List<AvailableTableDto>>.Failure(Error.Validation("Booking.InvalidGuests", "Số lượng khách phải từ 1 đến 50 người."));
        }

        var tablesQuery = _uow.Tables.GetAll()
            .Include(t => t.Area)
            .Where(t => t.Status != TableStatus.OutOfService && t.Capacity >= query.GuestCount);

        if (query.AreaId.HasValue && query.AreaId.Value > 0)
        {
            tablesQuery = tablesQuery.Where(t => t.AreaId == query.AreaId.Value);
        }

        var candidateTables = await tablesQuery.ToListAsync(cancellationToken);

        var slotHours = _options.Booking.SlotHours;
        var slotStartUtc = bookingDateTimeUtc.AddHours(-slotHours);
        var slotEndUtc = bookingDateTimeUtc.AddHours(slotHours);
        var nowUtc = DateTime.UtcNow;

        var conflictingBookings = await _uow.Bookings.GetAll()
            .Where(b => (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.CheckedIn || (b.Status == BookingStatus.PendingPayment && b.HoldExpiresAt > nowUtc))
                     && b.StartTime >= slotStartUtc
                     && b.StartTime <= slotEndUtc)
            .Select(b => b.TableId)
            .Distinct()
            .ToListAsync(cancellationToken);

        // Nếu giờ đặt gần với thời điểm hiện tại (trong vòng slotHours), loại trừ các bàn đang có khách ngồi ăn
        if (bookingDateTimeUtc <= nowUtc.AddHours(slotHours))
        {
            var currentlyOccupiedTableIds = await _uow.Orders.GetAll()
                .Where(o => o.ClosedAt == null)
                .Select(o => o.TableId)
                .Distinct()
                .ToListAsync(cancellationToken);

            conflictingBookings = conflictingBookings.Union(currentlyOccupiedTableIds).ToList();
        }

        var availableTables = candidateTables
            .Where(t => !conflictingBookings.Contains(t.Id))
            .OrderBy(t => t.Area.DisplayOrder)
            .ThenBy(t => t.Capacity)
            .ThenBy(t => t.TableNumber)
            .Select(t => new AvailableTableDto(
                t.Id,
                t.TableNumber,
                t.AreaId,
                t.Area.AreaName,
                t.Capacity
            ))
            .ToList();

        return Result<List<AvailableTableDto>>.Success(availableTables);
    }

    public async Task<Result<BookingConfirmationDto>> CreateBookingAsync(CreateBookingDto dto, CancellationToken cancellationToken = default)
    {
        if (!ValidationRules.IsValidPersonName(dto.CustomerName))
            return Result<BookingConfirmationDto>.Failure(Error.Validation("Customer.InvalidName", ValidationRules.NameErrorMessage));

        var normalizedPhone = ValidationRules.NormalizePhoneNumber(dto.PhoneNumber);
        if (!ValidationRules.IsValidPhoneNumber(normalizedPhone))
            return Result<BookingConfirmationDto>.Failure(Error.Validation("Customer.InvalidPhone", ValidationRules.PhoneErrorMessage));

        var trimmedEmail = dto.Email?.Trim();
        if (!string.IsNullOrWhiteSpace(trimmedEmail) && !ValidationRules.IsValidEmail(trimmedEmail))
            return Result<BookingConfirmationDto>.Failure(Error.Validation("Customer.InvalidEmail", ValidationRules.EmailErrorMessage));

        if (dto.GuestCount < 1 || dto.GuestCount > 50)
            return Result<BookingConfirmationDto>.Failure(Error.Validation("Booking.InvalidGuests", "Số lượng khách phải từ 1 đến 50 người."));

        var localBookingDateTime = dto.BookingDate.ToDateTime(dto.BookingTime);
        var bookingDateTimeUtc = TimeZoneInfo.ConvertTimeToUtc(localBookingDateTime, _timeZone);
        var nowUtc = DateTime.UtcNow;

        if (bookingDateTimeUtc < nowUtc.AddMinutes(_options.Booking.MinLeadMinutes))
        {
            return Result<BookingConfirmationDto>.Failure(Error.Validation(
                "Booking.MinLeadTime",
                $"Thời gian đặt bàn phải trước thời điểm hiện tại tối thiểu {_options.Booking.MinLeadMinutes} phút."));
        }

        if (bookingDateTimeUtc > nowUtc.AddDays(365))
        {
            return Result<BookingConfirmationDto>.Failure(Error.Validation(
                "Booking.MaxAdvanceDays",
                "Chỉ hỗ trợ đặt bàn trước tối đa 365 ngày."));
        }

        var depositAmount = _depositPolicy.CalculateDeposit(dto.GuestCount);

        return await _uow.ExecuteInTransactionAsync(async () =>
        {
            // Khóa bi quan mức dòng trên bảng dining_tables (SELECT ... FOR UPDATE) để chống đặt trùng đồng thời
            var table = await _uow.Tables.GetForUpdateAsync(dto.TableId, cancellationToken);

            if (table == null)
                return Result<BookingConfirmationDto>.Failure(Error.NotFound("Table.NotFound", "Bàn đã chọn không tồn tại."));

            if (table.Status == TableStatus.OutOfService)
                return Result<BookingConfirmationDto>.Failure(Error.Validation("Table.OutOfService", $"Bàn {table.TableNumber} đang tạm ngưng phục vụ để bảo trì."));

            if (dto.GuestCount > table.Capacity)
                return Result<BookingConfirmationDto>.Failure(Error.Validation("Table.OverCapacity", $"Số lượng khách ({dto.GuestCount}) vượt quá sức chứa bàn ({table.Capacity})."));

            var slotHours = _options.Booking.SlotHours;
            var slotStartUtc = bookingDateTimeUtc.AddHours(-slotHours);
            var slotEndUtc = bookingDateTimeUtc.AddHours(slotHours);

            var hasConflict = await _uow.Bookings.GetAll()
                .AnyAsync(b => b.TableId == table.Id
                            && (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.CheckedIn || (b.Status == BookingStatus.PendingPayment && b.HoldExpiresAt > nowUtc))
                            && b.StartTime >= slotStartUtc
                            && b.StartTime <= slotEndUtc, cancellationToken);

            if (!hasConflict && bookingDateTimeUtc <= nowUtc.AddHours(slotHours))
            {
                var isCurrentlyOccupied = await _uow.Orders.GetAll().AnyAsync(o => o.TableId == table.Id && o.ClosedAt == null, cancellationToken);
                if (isCurrentlyOccupied || table.Status == TableStatus.Occupied)
                {
                    hasConflict = true;
                }
            }

            if (hasConflict)
            {
                return Result<BookingConfirmationDto>.Failure(Error.Conflict(
                    "Booking.Conflict",
                    $"Bàn {table.TableNumber} đã có người đặt hoặc đang có khách phục vụ trong khung giờ này. Vui lòng chọn bàn khác hoặc khung giờ khác."));
            }

            var customer = await _uow.Customers.GetAll()
                .FirstOrDefaultAsync(c => c.PhoneNumber == normalizedPhone, cancellationToken);

            if (customer == null)
            {
                customer = new Customer
                {
                    CustomerName = dto.CustomerName.Trim(),
                    PhoneNumber = normalizedPhone,
                    Email = trimmedEmail,
                    CreatedAt = nowUtc
                };
                await _uow.Customers.AddAsync(customer, cancellationToken);
                await _uow.SaveChangesAsync(cancellationToken);
            }
            else if (!string.IsNullOrWhiteSpace(trimmedEmail))
            {
                customer.Email = trimmedEmail;
            }

            var bookingCode = $"BK-{localBookingDateTime:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..4].ToUpper()}";
            var holdExpiresAt = nowUtc.AddMinutes(_options.Booking.HoldMinutes);

            var booking = new Booking
            {
                BookingCode = bookingCode,
                TableId = table.Id,
                CustomerId = customer.Id,
                CustomerName = dto.CustomerName.Trim(),
                PhoneNumber = normalizedPhone,
                GuestCount = dto.GuestCount,
                StartTime = bookingDateTimeUtc,
                EndTime = bookingDateTimeUtc.AddHours(2),
                DepositAmount = depositAmount,
                Status = BookingStatus.PendingPayment,
                HoldExpiresAt = holdExpiresAt,
                SpecialRequests = dto.SpecialRequest?.Trim(),
                CreatedAt = nowUtc
            };

            await _uow.Bookings.AddAsync(booking, cancellationToken);
            await _uow.SaveChangesAsync(cancellationToken);

            return Result<BookingConfirmationDto>.Success(
                MapToConfirmationDto(booking, table.TableNumber, table.Area.AreaName, customer.Email));
        }, cancellationToken);
    }

    public async Task<Result<BookingConfirmationDto>> ConfirmBookingPaymentAsync(int bookingId, string transactionRef, string provider, CancellationToken cancellationToken = default)
    {
        var booking = await _uow.Bookings.GetAll()
            .Include(b => b.Table)
                .ThenInclude(t => t.Area)
            .Include(b => b.Customer)
            .FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);

        if (booking == null)
            return Result<BookingConfirmationDto>.Failure(Error.NotFound("Booking.NotFound", "Không tìm thấy thông tin đặt bàn."));

        if (booking.Status == BookingStatus.Confirmed)
            return Result<BookingConfirmationDto>.Success(MapToConfirmationDto(booking));

        if (booking.Status != BookingStatus.PendingPayment)
            return Result<BookingConfirmationDto>.Failure(Error.Validation("Booking.InvalidStatus", $"Đơn đặt bàn ở trạng thái không thể thanh toán ({booking.Status})."));

        if (DateTime.UtcNow > booking.HoldExpiresAt)
        {
            booking.Status = BookingStatus.Cancelled;
            await _uow.SaveChangesAsync(cancellationToken);
            return Result<BookingConfirmationDto>.Failure(Error.Validation("Booking.Expired", "Đơn đặt bàn đã quá hạn 15 phút thanh toán cọc và đã bị hủy."));
        }

        return await _uow.ExecuteInTransactionAsync(async () =>
        {
            booking.Status = BookingStatus.Confirmed;

            var transaction = new PaymentTransaction
            {
                BookingId = booking.Id,
                Amount = booking.DepositAmount,
                Type = PaymentTransactionType.DepositCharge,
                Method = PaymentMethod.BankTransfer,
                Status = TransactionStatus.Success,
                TxnRef = transactionRef,
                Provider = provider,
                CreatedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow
            };

            await _uow.PaymentTransactions.AddAsync(transaction, cancellationToken);
            await _uow.SaveChangesAsync(cancellationToken);

            return Result<BookingConfirmationDto>.Success(MapToConfirmationDto(booking));
        }, cancellationToken);
    }

    public async Task<Result<BookingConfirmationDto>> GetBookingByCodeAsync(string bookingCode, string phoneNumber, CancellationToken cancellationToken = default)
    {
        var normalizedPhone = ValidationRules.NormalizePhoneNumber(phoneNumber);
        if (!ValidationRules.IsValidPhoneNumber(normalizedPhone))
            return Result<BookingConfirmationDto>.Failure(Error.Validation("Booking.InvalidPhone", ValidationRules.PhoneErrorMessage));

        var normalizedCode = bookingCode.Trim().ToUpperInvariant();

        var booking = await _uow.Bookings.GetAll()
            .Include(b => b.Table)
                .ThenInclude(t => t.Area)
            .Include(b => b.Customer)
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.BookingCode == normalizedCode && b.PhoneNumber == normalizedPhone, cancellationToken);

        if (booking == null)
            return Result<BookingConfirmationDto>.Failure(Error.NotFound("Booking.NotFound", "Không tìm thấy đơn đặt bàn với mã và số điện thoại đã cung cấp."));

        return Result<BookingConfirmationDto>.Success(MapToConfirmationDto(booking));
    }

    public async Task<Result<BookingConfirmationDto>> GetBookingByIdAsync(int bookingId, CancellationToken cancellationToken = default)
    {
        var booking = await _uow.Bookings.GetAll()
            .Include(b => b.Table)
                .ThenInclude(t => t.Area)
            .Include(b => b.Customer)
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);

        if (booking == null)
            return Result<BookingConfirmationDto>.Failure(Error.NotFound("Booking.NotFound", "Không tìm thấy thông tin đặt bàn."));

        return Result<BookingConfirmationDto>.Success(MapToConfirmationDto(booking));
    }

    public async Task<Result<CancelBookingResultDto>> CancelBookingAsync(CancelBookingDto dto, CancellationToken cancellationToken = default)
    {
        var normalizedPhone = ValidationRules.NormalizePhoneNumber(dto.PhoneNumber);
        if (!ValidationRules.IsValidPhoneNumber(normalizedPhone))
            return Result<CancelBookingResultDto>.Failure(Error.Validation("Booking.InvalidPhone", ValidationRules.PhoneErrorMessage));

        var normalizedCode = dto.BookingCode.Trim().ToUpperInvariant();

        var booking = await _uow.Bookings.GetAll()
            .Include(b => b.Table)
            .Include(b => b.Customer)
            .FirstOrDefaultAsync(b => b.BookingCode == normalizedCode && b.PhoneNumber == normalizedPhone, cancellationToken);

        if (booking == null)
            return Result<CancelBookingResultDto>.Failure(Error.NotFound("Booking.NotFound", "Không tìm thấy đơn đặt bàn khớp thông tin."));

        if (booking.Status == BookingStatus.Cancelled || booking.Status == BookingStatus.Expired)
            return Result<CancelBookingResultDto>.Failure(Error.Validation("Booking.AlreadyCancelled", "Đơn đặt bàn này đã bị hủy trước đó."));

        if (booking.Status == BookingStatus.CheckedIn || booking.Status == BookingStatus.Completed)
            return Result<CancelBookingResultDto>.Failure(Error.Validation("Booking.InvalidStatus", "Đơn đặt bàn đang được phục vụ hoặc đã hoàn tất, không thể hủy."));

        var nowUtc = DateTime.UtcNow;
        var (rate, refundAmount, reason) = _refundPolicy.CalculateRefund(booking.DepositAmount, booking.StartTime, nowUtc);

        var cancelResult = await _uow.ExecuteInTransactionAsync(async () =>
        {
            booking.Status = BookingStatus.Cancelled;
            booking.CancelledAt = nowUtc;

            if (refundAmount > 0)
            {
                var refundTxn = new PaymentTransaction
                {
                    BookingId = booking.Id,
                    Amount = refundAmount,
                    Type = PaymentTransactionType.DepositRefund,
                    Method = PaymentMethod.BankTransfer,
                    Status = TransactionStatus.Success,
                    TxnRef = $"REFUND-{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
                    Provider = "AutoRefundGateway",
                    CreatedAt = nowUtc,
                    CompletedAt = nowUtc
                };
                await _uow.PaymentTransactions.AddAsync(refundTxn, cancellationToken);
            }

            await _uow.SaveChangesAsync(cancellationToken);

            return new CancelBookingResultDto(
                booking.BookingCode,
                booking.DepositAmount,
                rate,
                refundAmount,
                reason
            );
        }, cancellationToken);

        var customerEmail = booking.Customer?.Email;
        if (!string.IsNullOrWhiteSpace(customerEmail))
        {
            try
            {
                await _emailService.SendBookingCancellationEmailAsync(cancelResult, customerEmail, booking.CustomerName, cancellationToken);
            }
            catch
            {
                // Email failure must not roll back already committed cancellation
            }
        }

        return Result<CancelBookingResultDto>.Success(cancelResult);
    }

    public Task<List<BookingConfirmationDto>> GetTodayBookingsAsync(CancellationToken cancellationToken = default)
    {
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _timeZone);
        return GetBookingsByDateAsync(DateOnly.FromDateTime(localNow), cancellationToken);
    }

    public async Task<List<BookingConfirmationDto>> GetBookingsByDateAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        var startLocal = date.ToDateTime(TimeOnly.MinValue);
        var endLocal = date.AddDays(1).ToDateTime(TimeOnly.MinValue);

        var startUtc = TimeZoneInfo.ConvertTimeToUtc(startLocal, _timeZone);
        var endUtc = TimeZoneInfo.ConvertTimeToUtc(endLocal, _timeZone);

        var bookings = await _uow.Bookings.GetAll()
            .Include(b => b.Table)
                .ThenInclude(t => t.Area)
            .Include(b => b.Customer)
            .Where(b => b.StartTime >= startUtc && b.StartTime < endUtc)
            .OrderBy(b => b.StartTime)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return bookings.Select(b => MapToConfirmationDto(b)).ToList();
    }

    public async Task<int> ExpirePendingBookingsAsync(CancellationToken cancellationToken = default)
    {
        var nowUtc = DateTime.UtcNow;
        var expiredBookings = await _uow.Bookings.GetAll()
            .Where(b => b.Status == BookingStatus.PendingPayment && b.HoldExpiresAt <= nowUtc)
            .ToListAsync(cancellationToken);

        if (expiredBookings.Count == 0)
            return 0;

        foreach (var booking in expiredBookings)
        {
            booking.Status = BookingStatus.Expired;
        }

        await _uow.SaveChangesAsync(cancellationToken);
        return expiredBookings.Count;
    }

    private static BookingConfirmationDto MapToConfirmationDto(
        Booking booking,
        string? tableNumber = null,
        string? areaName = null,
        string? email = null) => new(
        booking.Id,
        booking.BookingCode,
        tableNumber ?? booking.Table.TableNumber,
        areaName ?? booking.Table.Area.AreaName,
        booking.CustomerName,
        booking.PhoneNumber,
        booking.StartTime,
        booking.GuestCount,
        booking.DepositAmount,
        booking.Status,
        booking.HoldExpiresAt,
        email ?? booking.Customer?.Email
    );
}
