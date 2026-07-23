using FluentAssertions;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Features.Auth.Commands.Login;
using Moq;
using Xunit;

namespace KadirliApp.Tests.Unit.Application.Auth;

public class LoginCommandHandlerTests
{
    private readonly Mock<IOtpService> _otpServiceMock = new();

    [Fact]
    public async Task Handle_ShouldTrimPhone_AndReturnOtpRequestResult()
    {
        // Arrange — Faz 9.2: handler artık düz OTP string'i değil OtpRequestResult döner
        var expected = new OtpRequestResult(300, 60, DevOtp: null);
        _otpServiceMock.Setup(s => s.RequestAsync("+905001234567", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var handler = new LoginCommandHandler(_otpServiceMock.Object);

        // Act
        var result = await handler.Handle(new LoginCommand(" +905001234567 "), CancellationToken.None);

        // Assert
        result.Should().Be(expected);
        _otpServiceMock.Verify(s => s.RequestAsync("+905001234567", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithEmptyPhone_ShouldThrowArgumentException()
    {
        var handler = new LoginCommandHandler(_otpServiceMock.Object);

        Func<Task> act = () => handler.Handle(new LoginCommand("  "), CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
        _otpServiceMock.Verify(s => s.RequestAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
