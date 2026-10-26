using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RestaurantManagement.Business.DTOs;
using RestaurantManagement.Business.Options;

namespace RestaurantManagement.Business.Services;

public class EmailService : IEmailService
{
    private readonly EmailOptions _options;
    private readonly TimeZoneInfo _timeZone;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        IOptions<EmailOptions> emailOptions,
        IOptions<RestaurantRulesOptions> rulesOptions,
        ILogger<EmailService>? logger = null)
    {
        _options = emailOptions.Value;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<EmailService>.Instance;

        var tzName = rulesOptions.Value?.TimeZone ?? "SE Asia Standard Time";
        try
        {
            _timeZone = TimeZoneInfo.FindSystemTimeZoneById(tzName);
        }
        catch
        {
            _timeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }
    }

    public async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(toEmail))
        {
            _logger.LogWarning("Email recipient address is empty. Skipping email sending.");
            return false;
        }

        // Chế độ lưu file cục bộ (khi test hoặc khi chưa cấu hình mật khẩu SMTP)
        bool useLocalPreview = _options.SaveToLocalFolder || string.IsNullOrWhiteSpace(_options.Password);

        if (useLocalPreview)
        {
            return await SaveEmailToLocalFolderAsync(toEmail, subject, htmlBody);
        }

        try
        {
            using var message = new MailMessage
            {
                From = new MailAddress(_options.SenderEmail, _options.SenderName, Encoding.UTF8),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true,
                BodyEncoding = Encoding.UTF8,
                SubjectEncoding = Encoding.UTF8,
                HeadersEncoding = Encoding.UTF8
            };
            message.To.Add(new MailAddress(toEmail));

            using var client = new SmtpClient(_options.SmtpHost, _options.SmtpPort)
            {
                EnableSsl = _options.EnableSsl,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(
                    string.IsNullOrWhiteSpace(_options.Username) ? _options.SenderEmail : _options.Username,
                    _options.Password.Replace(" ", ""))
            };

            await client.SendMailAsync(message, cancellationToken);
            _logger.LogInformation("Đã gửi email thành công tới {ToEmail} qua máy chủ SMTP {Host}:{Port}", toEmail, _options.SmtpHost, _options.SmtpPort);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gửi email tới {ToEmail} qua SMTP thất bại. Chuyển sang lưu file dự phòng.", toEmail);
            await SaveEmailToLocalFolderAsync(toEmail, subject, htmlBody);
            return false;
        }
    }

    public async Task<bool> SendBookingConfirmationEmailAsync(BookingConfirmationDto booking, string customerEmail, CancellationToken cancellationToken = default)
    {
        var localTime = TimeZoneInfo.ConvertTimeFromUtc(booking.BookingDateTimeUtc, _timeZone);
        var subject = $"[Sen Việt] Xác nhận đặt bàn thành công - Mã #{booking.BookingCode}";

        var htmlBuilder = new StringBuilder();
        htmlBuilder.Append(@"
<!DOCTYPE html>
<html lang=""vi"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Xác nhận đặt bàn</title>
</head>
<body style=""margin: 0; padding: 0; background-color: #f4f1ea; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; color: #333333;"">
    <table border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""background-color: #f4f1ea; padding: 25px 0;"">
        <tr>
            <td align=""center"">
                <table border=""0"" cellpadding=""0"" cellspacing=""0"" width=""600"" style=""background-color: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 15px rgba(0,0,0,0.08);"">
                    <!-- Header -->
                    <tr>
                        <td align=""center"" style=""background: linear-gradient(135deg, #8B1E1E 0%, #5E1212 100%); padding: 30px 20px; color: #ffffff;"">
                            <h1 style=""margin: 0 0 5px 0; font-size: 26px; font-weight: bold; letter-spacing: 1px; color: #D4AF37;"">NHÀ HÀNG SEN VIỆT</h1>
                            <p style=""margin: 0; font-size: 13px; text-transform: uppercase; letter-spacing: 2px; color: #f2d68a;"">Tinh Hoa Ẩm Thực Truyền Thống</p>
                        </td>
                    </tr>
                    
                    <!-- Content -->
                    <tr>
                        <td style=""padding: 30px;"">
                            <div style=""text-align: center; margin-bottom: 25px;"">
                                <span style=""background-color: #e8f5e9; color: #2e7d32; font-weight: bold; font-size: 14px; padding: 6px 16px; border-radius: 20px; display: inline-block;"">
                                    &#10003; ĐẶT BÀN &amp; THANH TOÁN CỌC THÀNH CÔNG
                                </span>
                                <h2 style=""color: #8B1E1E; margin: 15px 0 5px 0; font-size: 22px;"">PHIẾU XÁC NHẬN ĐẶT CHỖ</h2>
                                <p style=""color: #666; margin: 0; font-size: 14px;"">Kính chào Quý khách <strong>").Append(booking.CustomerName).Append(@"</strong>, Sen Việt rất hân hạnh được đón tiếp Quý khách!</p>
                            </div>

                            <!-- Booking Card -->
                            <table border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""background-color: #fcfbf9; border: 1px solid #e8e3d7; border-radius: 8px; margin-bottom: 25px;"">
                                <tr>
                                    <td style=""padding: 20px;"">
                                        <table border=""0"" cellpadding=""6"" cellspacing=""0"" width=""100%"" style=""font-size: 14px;"">
                                            <tr>
                                                <td width=""40%"" style=""color: #777;"">Mã Đặt Bàn:</td>
                                                <td width=""60%"" style=""font-weight: bold; color: #8B1E1E; font-size: 16px;"">").Append(booking.BookingCode).Append(@"</td>
                                            </tr>
                                            <tr>
                                                <td style=""color: #777;"">Vị trí bàn ăn:</td>
                                                <td style=""font-weight: bold;"">Bàn ").Append(booking.TableNumber).Append(@" (").Append(booking.AreaName).Append(@")</td>
                                            </tr>
                                            <tr>
                                                <td style=""color: #777;"">Thời gian đón khách:</td>
                                                <td style=""font-weight: bold; color: #2e7d32;"">").Append(localTime.ToString("HH:mm - dddd, dd/MM/yyyy")).Append(@"</td>
                                            </tr>
                                            <tr>
                                                <td style=""color: #777;"">Số lượng khách:</td>
                                                <td style=""font-weight: bold;"">").Append(booking.GuestCount).Append(@" người</td>
                                            </tr>
                                            <tr>
                                                <td style=""color: #777;"">Số điện thoại liên hệ:</td>
                                                <td>").Append(booking.PhoneNumber).Append(@"</td>
                                            </tr>
                                            <tr>
                                                <td style=""color: #777;"">Tiền cọc đã thanh toán:</td>
                                                <td style=""font-weight: bold; color: #d32f2f; font-size: 15px;"">").Append(booking.DepositAmount.ToString("N0")).Append(@" VNĐ</td>
                                            </tr>
                                        </table>
                                    </td>
                                </tr>
                            </table>

                            <!-- QR Code Check-in -->
                            <div style=""text-align: center; margin: 25px 0; padding: 20px; background-color: #fafafa; border-radius: 8px;"">
                                <p style=""margin: 0 0 10px 0; font-size: 13px; color: #666; font-weight: 600;"">MÃ CHECK-IN NHANH TẠI TRẠM ĐÓN TIẾP:</p>
                                <img src=""https://api.qrserver.com/v1/create-qr-code/?size=150x150&amp;data=").Append(booking.BookingCode).Append(@""" alt=""Mã QR Đặt Bàn"" width=""140"" height=""140"" style=""border: 4px solid #ffffff; box-shadow: 0 2px 8px rgba(0,0,0,0.1); border-radius: 6px;"" />
                                <p style=""margin: 8px 0 0 0; font-size: 12px; color: #888;"">Quý khách chỉ cần xuất trình mã QR này hoặc cung cấp SĐT cho nhân viên tiếp đón.</p>
                            </div>

                            <!-- Notes -->
                            <div style=""background-color: #fff9e6; border-left: 4px solid #ffb300; padding: 12px 16px; border-radius: 4px; font-size: 13px; line-height: 1.6; color: #5d4037;"">
                                <strong>&#9432; Lưu ý quan trọng:</strong><br/>
                                &bull; Số tiền cọc <strong>").Append(booking.DepositAmount.ToString("N0")).Append(@" VNĐ</strong> sẽ được trừ trực tiếp vào hóa đơn thanh toán bữa ăn.<br/>
                                &bull; Bàn ăn sẽ được giữ tối đa trong vòng <strong>30 phút</strong> tính từ giờ hẹn.<br/>
                                &bull; Quý khách có thể tra cứu hoặc hủy bàn trước 2 tiếng để nhận hoàn 100% tiền cọc tại mục <em>Tra cứu đặt bàn (BM15)</em>.
                            </div>
                        </td>
                    </tr>

                    <!-- Footer -->
                    <tr>
                        <td style=""background-color: #2c2523; color: #b7aba4; padding: 25px 30px; text-align: center; font-size: 12px; line-height: 1.6;"">
                            <strong style=""color: #D4AF37; font-size: 14px;"">NHÀ HÀNG SEN VIỆT</strong><br/>
                            123 Đường Nam Kỳ Khởi Nghĩa, Phường Bến Nghé, Quận 1, TP. Hồ Chí Minh<br/>
                            Hotline đặt bàn &amp; CSKH: <strong>1900 6868</strong> &bull; Email: lienhe@senviet.vn<br/>
                            <em>Cảm ơn Quý khách đã tin tưởng và lựa chọn Sen Việt!</em>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>");

        return await SendEmailAsync(customerEmail, subject, htmlBuilder.ToString(), cancellationToken);
    }

    public async Task<bool> SendBookingCancellationEmailAsync(CancelBookingResultDto cancelInfo, string customerEmail, string customerName, CancellationToken cancellationToken = default)
    {
        var subject = $"[Sen Việt] Thông báo hủy đặt bàn & Hoàn cọc - Mã #{cancelInfo.BookingCode}";

        var htmlBuilder = new StringBuilder();
        htmlBuilder.Append(@"
<!DOCTYPE html>
<html lang=""vi"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Thông báo hủy đặt bàn</title>
</head>
<body style=""margin: 0; padding: 0; background-color: #f4f1ea; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; color: #333333;"">
    <table border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""background-color: #f4f1ea; padding: 25px 0;"">
        <tr>
            <td align=""center"">
                <table border=""0"" cellpadding=""0"" cellspacing=""0"" width=""600"" style=""background-color: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 15px rgba(0,0,0,0.08);"">
                    <!-- Header -->
                    <tr>
                        <td align=""center"" style=""background: linear-gradient(135deg, #424242 0%, #212121 100%); padding: 30px 20px; color: #ffffff;"">
                            <h1 style=""margin: 0 0 5px 0; font-size: 24px; font-weight: bold; letter-spacing: 1px; color: #D4AF37;"">NHÀ HÀNG SEN VIỆT</h1>
                            <p style=""margin: 0; font-size: 13px; text-transform: uppercase; letter-spacing: 2px; color: #bdbdbd;"">Xác Nhận Hủy Đặt Bàn &amp; Hoàn Cọc</p>
                        </td>
                    </tr>
                    
                    <!-- Content -->
                    <tr>
                        <td style=""padding: 30px;"">
                            <div style=""text-align: center; margin-bottom: 25px;"">
                                <span style=""background-color: #ffebee; color: #c62828; font-weight: bold; font-size: 14px; padding: 6px 16px; border-radius: 20px; display: inline-block;"">
                                    &#10007; ĐÃ HỦY ĐẶT BÀN
                                </span>
                                <h2 style=""color: #333333; margin: 15px 0 5px 0; font-size: 20px;"">BIÊN NHẬN HỦY DỊCH VỤ</h2>
                                <p style=""color: #666; margin: 0; font-size: 14px;"">Kính chào Quý khách <strong>").Append(customerName).Append(@"</strong>, Sen Việt đã tiếp nhận yêu cầu hủy bàn của Quý khách.</p>
                            </div>

                            <!-- Refund Card -->
                            <table border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""background-color: #fafafa; border: 1px solid #eeeeee; border-radius: 8px; margin-bottom: 25px;"">
                                <tr>
                                    <td style=""padding: 20px;"">
                                        <table border=""0"" cellpadding=""6"" cellspacing=""0"" width=""100%"" style=""font-size: 14px;"">
                                            <tr>
                                                <td width=""40%"" style=""color: #777;"">Mã Đặt Bàn:</td>
                                                <td width=""60%"" style=""font-weight: bold; font-size: 15px;"">").Append(cancelInfo.BookingCode).Append(@"</td>
                                            </tr>
                                            <tr>
                                                <td style=""color: #777;"">Tiền cọc ban đầu:</td>
                                                <td>").Append(cancelInfo.DepositAmount.ToString("N0")).Append(@" VNĐ</td>
                                            </tr>
                                            <tr>
                                                <td style=""color: #777;"">Tỷ lệ hoàn tiền:</td>
                                                <td style=""font-weight: bold; color: #1976d2;"">").Append((cancelInfo.RefundRate * 100).ToString("N0")).Append(@"%</td>
                                            </tr>
                                            <tr>
                                                <td style=""color: #777;"">Số tiền hoàn lại:</td>
                                                <td style=""font-weight: bold; color: #2e7d32; font-size: 17px;"">").Append(cancelInfo.RefundAmount.ToString("N0")).Append(@" VNĐ</td>
                                            </tr>
                                            <tr>
                                                <td style=""color: #777;"">Chính sách áp dụng:</td>
                                                <td style=""font-style: italic; color: #555;"">").Append(cancelInfo.Explanation).Append(@"</td>
                                            </tr>
                                        </table>
                                    </td>
                                </tr>
                            </table>

                            <div style=""background-color: #e3f2fd; border-left: 4px solid #1976d2; padding: 12px 16px; border-radius: 4px; font-size: 13px; line-height: 1.6; color: #0d47a1;"">
                                <strong>&#9432; Thông tin hoàn tiền:</strong><br/>
                                Tiền hoàn lại (nếu có) sẽ được chuyển trực tiếp về tài khoản ngân hàng Quý khách đã dùng để thanh toán cọc trong vòng <strong>1 đến 3 ngày làm việc</strong>.
                            </div>
                        </td>
                    </tr>

                    <!-- Footer -->
                    <tr>
                        <td style=""background-color: #2c2523; color: #b7aba4; padding: 25px 30px; text-align: center; font-size: 12px; line-height: 1.6;"">
                            <strong style=""color: #D4AF37; font-size: 14px;"">NHÀ HÀNG SEN VIỆT</strong><br/>
                            123 Đường Nam Kỳ Khởi Nghĩa, Phường Bến Nghé, Quận 1, TP. Hồ Chí Minh<br/>
                            Hotline hỗ trợ: <strong>1900 6868</strong> &bull; Email: lienhe@senviet.vn
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>");

        return await SendEmailAsync(customerEmail, subject, htmlBuilder.ToString(), cancellationToken);
    }

    private async Task<bool> SaveEmailToLocalFolderAsync(string toEmail, string subject, string htmlBody)
    {
        try
        {
            var folder = Path.Combine(Directory.GetCurrentDirectory(), _options.LocalFolderPath);
            Directory.CreateDirectory(folder);

            var safeSubject = string.Join("_", subject.Split(Path.GetInvalidFileNameChars())).Replace(" ", "_");
            if (safeSubject.Length > 40) safeSubject = safeSubject[..40];

            var fileName = $"Email_{DateTime.Now:yyyyMMdd_HHmmss}_{safeSubject}.html";
            var filePath = Path.Combine(folder, fileName);

            var contentWithMeta = $@"<!--
Gửi đến: {toEmail}
Tiêu đề: {subject}
Thời gian tạo: {DateTime.Now:dd/MM/yyyy HH:mm:ss}
-->
{htmlBody}";

            await File.WriteAllTextAsync(filePath, contentWithMeta, Encoding.UTF8);
            _logger.LogInformation("Đã lưu email xem trước vào file cục bộ: {FilePath} (Người nhận: {ToEmail})", filePath, toEmail);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lưu email xem trước cục bộ cho {ToEmail}", toEmail);
            return false;
        }
    }
}
