using FluentAssertions;
using KadirliApp.Application.Common.Exceptions;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Features.Auth.Commands.VerifyOtp;
using KadirliApp.Domain.Entities;
using Moq;
using Xunit;

namespace KadirliApp.Tests.Unit.Application.Auth;

public class VerifyOtpCommandHandlerTests
{
    private readonly Mock<IJwtProvider> _jwtProviderMock;
    private readonly Mock<IOtpService> _otpServiceMock;
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<IRepository<User>> _userRepoMock;
    private readonly VerifyOtpCommandHandler _handler;

    public VerifyOtpCommandHandlerTests()
    {
        _jwtProviderMock = new Mock<IJwtProvider>();
        _otpServiceMock = new Mock<IOtpService>();
        _uowMock = new Mock<IUnitOfWork>();
        _userRepoMock = new Mock<IRepository<User>>();

        _uowMock.Setup(u => u.Repository<User>()).Returns(_userRepoMock.Object);

        _handler = new VerifyOtpCommandHandler(
            _jwtProviderMock.Object,
            _otpServiceMock.Object,
            _uowMock.Object
        );
    }

    [Fact]
    public async Task Handle_WithInvalidOtp_ShouldThrowAppException()
    {
        // Arrange
        var command = new VerifyOtpCommand("+905001234567", "000000");
        _otpServiceMock.Setup(s => s.ValidateAsync(command.Phone, command.Otp))
            .ReturnsAsync(false);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        var exception = await act.Should().ThrowAsync<AppException>();
        exception.WithMessage("Geçersiz veya süresi dolmuş OTP.");
        // The error code should be INVALID_OTP as updated in phase 8
        // Exception validation logic...
    }
}
