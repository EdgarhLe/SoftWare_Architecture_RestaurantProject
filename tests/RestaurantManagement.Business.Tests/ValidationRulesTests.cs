using RestaurantManagement.Common.Validation;
using Xunit;

namespace RestaurantManagement.Business.Tests;

public class ValidationRulesTests
{
    [Theory]
    [InlineData("0912345678", true)]
    [InlineData("0830002002", true)]
    [InlineData("0351234567", true)]
    [InlineData("0561234567", true)]
    [InlineData("0701234567", true)]
    [InlineData("+84912345678", true)]
    [InlineData("08300020020", false)] // 11 digits
    [InlineData("0801234567", false)]  // Invalid 080 prefix
    [InlineData("0123456789", false)]  // Invalid 01 prefix
    [InlineData("091234567", false)]   // 9 digits
    [InlineData("abcdefghij", false)]
    [InlineData("", false)]
    public void IsValidPhoneNumber_ShouldValidateVietnameseMobileNumbers(string phone, bool expected)
    {
        Assert.Equal(expected, ValidationRules.IsValidPhoneNumber(phone));
    }

    [Fact]
    public void NormalizePhoneNumber_ShouldConvertPlus84PrefixToZero()
    {
        Assert.Equal("0912345678", ValidationRules.NormalizePhoneNumber(" +84912345678 "));
        Assert.Equal("0912345678", ValidationRules.NormalizePhoneNumber("0912345678"));
    }

    [Theory]
    [InlineData("079090000001", true)]
    [InlineData("07909000001", false)]  // 11 digits
    [InlineData("0790900000012", false)] // 13 digits
    [InlineData("07909000000a", false)]
    public void IsValidCitizenId_ShouldRequireExactly12Digits(string cccd, bool expected)
    {
        Assert.Equal(expected, ValidationRules.IsValidCitizenId(cccd));
    }

    [Theory]
    [InlineData("Nguyễn Văn A", true)]
    [InlineData("Trần Minh Anh", true)]
    [InlineData("A", false)]
    [InlineData("Nguyen123", false)]
    [InlineData("   ", false)]
    public void IsValidPersonName_ShouldRejectInvalidNames(string name, bool expected)
    {
        Assert.Equal(expected, ValidationRules.IsValidPersonName(name));
    }

    [Theory]
    [InlineData("khachhang@gmail.com", true)]
    [InlineData("khachhang@gmail", false)]
    [InlineData("invalid-email", false)]
    [InlineData("", false)]
    public void IsValidEmail_ShouldRequireDomainWithDot(string email, bool expected)
    {
        Assert.Equal(expected, ValidationRules.IsValidEmail(email));
    }
}

