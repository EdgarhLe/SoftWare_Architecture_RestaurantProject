using System.Text.RegularExpressions;

namespace RestaurantManagement.Common.Validation;

public static partial class ValidationRules
{
    public const string PhonePattern = @"^(0|\+84)(3[2-9]|5[25689]|7[06-9]|8[1-9]|9[0-9])[0-9]{7}$";
    public const string PhoneHtmlPattern = @"(0|\+84)(3[2-9]|5[25689]|7[06-9]|8[1-9]|9[0-9])[0-9]{7}";
    public const string PhoneErrorMessage = "Số điện thoại không hợp lệ. Phải là số di động Việt Nam 10 chữ số (ví dụ: 0912345678).";

    public const string CitizenIdPattern = @"^\d{12}$";
    public const string CitizenIdErrorMessage = "Số CCCD phải gồm đúng 12 chữ số.";

    public const string NamePattern = @"^[\p{L}\s']+$";
    public const string NameErrorMessage = "Họ tên chỉ được chứa chữ cái và khoảng trắng.";

    public const string EmailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
    public const string EmailErrorMessage = "Địa chỉ email không hợp lệ (ví dụ: example@gmail.com).";

    public const string StaffCodePattern = @"^[A-Za-z0-9_\-]{2,20}$";
    public const string StaffCodeErrorMessage = "Mã nhân viên từ 2 đến 20 ký tự, chỉ gồm chữ cái, số, dấu '-' hoặc '_'.";

    public const string UsernamePattern = @"^[a-zA-Z0-9._\-]{3,30}$";
    public const string UsernameErrorMessage = "Tên đăng nhập từ 3 đến 30 ký tự, chỉ gồm chữ, số, '.', '_', '-'.";

    public const string ItemCodePattern = @"^[A-Za-z0-9_\-]{2,20}$";
    public const string ItemCodeErrorMessage = "Mã món từ 2 đến 20 ký tự, chỉ gồm chữ cái, số, dấu '-' hoặc '_'.";

    public const string ManagerPinPattern = @"^\d{4,6}$";
    public const string ManagerPinErrorMessage = "Mã PIN Quản lý phải gồm 4 đến 6 chữ số.";

    public static string NormalizePhoneNumber(string? phone)
    {
        var trimmed = (phone ?? string.Empty).Trim();
        if (trimmed.StartsWith("+84", StringComparison.Ordinal))
        {
            return "0" + trimmed[3..];
        }
        return trimmed;
    }

    public static bool IsValidPhoneNumber(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return false;
        return PhoneRegex().IsMatch(phone.Trim());
    }

    public static bool IsValidCitizenId(string? citizenId)
    {
        if (string.IsNullOrWhiteSpace(citizenId)) return false;
        return CitizenIdRegex().IsMatch(citizenId.Trim());
    }

    public static bool IsValidPersonName(string? name)
    {
        var trimmed = name?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed) || trimmed.Length < 2 || trimmed.Length > 100)
            return false;
        return NameRegex().IsMatch(trimmed);
    }

    public static bool IsValidEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        return EmailRegex().IsMatch(email.Trim());
    }

    public static bool IsValidManagerPin(string? pin)
    {
        if (string.IsNullOrWhiteSpace(pin)) return false;
        return ManagerPinRegex().IsMatch(pin.Trim());
    }

    [GeneratedRegex(PhonePattern)]
    private static partial Regex PhoneRegex();

    [GeneratedRegex(CitizenIdPattern)]
    private static partial Regex CitizenIdRegex();

    [GeneratedRegex(NamePattern)]
    private static partial Regex NameRegex();

    [GeneratedRegex(EmailPattern)]
    private static partial Regex EmailRegex();

    [GeneratedRegex(ManagerPinPattern)]
    private static partial Regex ManagerPinRegex();
}

