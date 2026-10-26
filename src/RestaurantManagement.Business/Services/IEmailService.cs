using RestaurantManagement.Business.DTOs;

namespace RestaurantManagement.Business.Services;

public interface IEmailService
{
    Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default);
    Task<bool> SendBookingConfirmationEmailAsync(BookingConfirmationDto booking, string customerEmail, CancellationToken cancellationToken = default);
    Task<bool> SendBookingCancellationEmailAsync(CancelBookingResultDto cancelInfo, string customerEmail, string customerName, CancellationToken cancellationToken = default);
}
