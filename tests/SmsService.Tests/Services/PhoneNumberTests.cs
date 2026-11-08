using SmsService.Api.Services;

namespace SmsService.Tests.Services;

public class PhoneNumberTests
{
    [Theory]
    [InlineData("0522222223")]
    [InlineData("052-2222223")]
    [InlineData("052 222 2223")]
    public void Valid_numbers(string phone) => Assert.True(PhoneNumber.IsValid(phone));

    [Theory]
    [InlineData("")]
    [InlineData("051234")]        // too short
    [InlineData("05222222234")]   // too long
    [InlineData("0322222223")]    // not a mobile number
    [InlineData("+972522222223")] // international format is out of scope
    [InlineData("05abcdefgh")]
    public void Invalid_numbers(string phone) => Assert.False(PhoneNumber.IsValid(phone));

    [Fact]
    public void Normalize_removes_dashes_and_spaces() =>
        Assert.Equal("0522222223", PhoneNumber.Normalize(" 052-222 2223 "));
}
