namespace RestaurantManagement.Business.Options;

public class EmailOptions
{
    public const string SectionName = "EmailSettings";

    public string SmtpHost { get; set; } = "smtp.gmail.com";
    public int SmtpPort { get; set; } = 587;
    public string SenderEmail { get; set; } = "nhahang.senviet@gmail.com";
    public string SenderName { get; set; } = "Nhà Hàng Sen Việt";
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool EnableSsl { get; set; } = true;

    /// <summary>
    /// Nếu đặt là true hoặc khi chưa cấu hình mật khẩu SMTP, email sẽ tự động được lưu thành file .html trong thư mục để xem trước ngay trên máy tính mà không gây lỗi.
    /// </summary>
    public bool SaveToLocalFolder { get; set; } = false;
    public string LocalFolderPath { get; set; } = "SentEmails";
}
