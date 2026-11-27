using RestaurantManagement.Business.Services;

namespace RestaurantManagement.Web.Services;

public class BookingExpirationBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BookingExpirationBackgroundService> _logger;

    public BookingExpirationBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<BookingExpirationBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
                var expiredCount = await bookingService.ExpirePendingBookingsAsync(stoppingToken);
                if (expiredCount > 0)
                {
                    _logger.LogInformation("Đã tự động hủy {Count} đơn đặt bàn quá hạn 15 phút.", expiredCount);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi khi kiểm tra giải phóng đơn đặt bàn quá hạn.");
            }

            await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
        }
    }
}

