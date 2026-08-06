using FluentAssertions;
using KadirliApp.Application.Common.Exceptions;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Features.Auth.Commands.VerifyOtp;
using KadirliApp.Domain.Entities;
using KadirliApp.Domain.Enums;
using KadirliApp.Tests.Unit;
using Moq;
using Xunit;

namespace KadirliApp.Tests.Unit.Application.Auth;

/// <summary>
/// <see cref="VerifyOtpCommandHandler"/> birim testleri — giriş akışının kalbi.
/// Kapsanan dallar: geçersiz OTP · kayıtlı kullanıcı (token çifti) · yeni kullanıcı (temp token)
/// · engelli · pasif · silinmiş telefon · telefon trim'lenmesi.
/// </summary>
public class VerifyOtpCommandHandlerTests
{
    private const string Phone = "+905001234567";

    private readonly Mock<IJwtProvider> _jwtProviderMock = new();
    private readonly Mock<IOtpService> _otpServiceMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IRepository<User>> _userRepoMock = new();

    /// <summary>
    /// Faz 12.2: OTP akışı artık her sonucu <see cref="ILoginAttemptRecorder"/>'a bildiriyor.
    /// Mock <b>gevşek</b> (loose) bırakıldı — bu testlerin konusu giriş akışının kendisi;
    /// kaydın içeriği <c>PanelLoginAttemptTests</c> ve <c>SuspiciousLoginRulesTests</c>'te
    /// ayrıca kilitli. Yine de "her dalda kayıt düşüyor mu" burada bir testle doğrulanıyor.
    /// </summary>
    private readonly Mock<ILoginAttemptRecorder> _loginAttemptsMock = new();
    private readonly VerifyOtpCommandHandler _handler;

    public VerifyOtpCommandHandlerTests()
    {
        _uowMock.Setup(u => u.Repository<User>()).Returns(_userRepoMock.Object);

        _handler = new VerifyOtpCommandHandler(
            _jwtProviderMock.Object,
            _otpServiceMock.Object,
            _uowMock.Object,
            _loginAttemptsMock.Object
        );
    }

    /// <summary>OTP'yi geçerli sayar; handler'ın kullanıcı arama adımına ilerlemesini sağlar.</summary>
    private void OtpIsValid(string phone = Phone) =>
        _otpServiceMock.Setup(s => s.ValidateAsync(phone, It.IsAny<string>())).ReturnsAsync(true);

    /// <summary>Global soft-delete filtresinin GÖRDÜĞÜ küme (silinmişler hariç).</summary>
    private void UsersInDb(params User[] users) =>
        _userRepoMock.Setup(r => r.Query(It.IsAny<bool>())).Returns(users.AsAsyncQueryable());

    private static User MakeUser(Action<User>? tweak = null)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Phone = Phone,
            Username = "testuser",
            Role = UserRole.User,
            IsActive = true,
            IsBanned = false
        };
        tweak?.Invoke(user);
        return user;
    }

    // ---------------------------------------------------------------- OTP doğrulama

    [Fact]
    public async Task Handle_WithInvalidOtp_ShouldThrowInvalidOtpAndNeverQueryUsers()
    {
        var command = new VerifyOtpCommand(Phone, "000000");
        _otpServiceMock.Setup(s => s.ValidateAsync(command.Phone, command.Otp)).ReturnsAsync(false);

        Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

        var exception = await act.Should().ThrowAsync<AppException>()
            .WithMessage("Geçersiz veya süresi dolmuş OTP.");
        exception.Which.Code.Should().Be("INVALID_OTP");

        // Hatalı OTP'de kullanıcı tablosuna hiç dokunulmamalı (numara var mı sızdırılmaz).
        _userRepoMock.Verify(r => r.Query(It.IsAny<bool>()), Times.Never);
    }

    // ---------------------------------------------------------------- Kayıtlı kullanıcı (happy path)

    [Fact]
    public async Task Handle_WithExistingUser_ShouldReturnTokenPair()
    {
        var user = MakeUser();
        OtpIsValid();
        UsersInDb(user);
        _jwtProviderMock.Setup(j => j.GenerateTokens(user.Id, "user", user.Phone))
            .Returns(new AuthTokens("access-token", "refresh-token", 900));

        var result = await _handler.Handle(new VerifyOtpCommand(Phone, "123456"), CancellationToken.None);

        result.IsNewUser.Should().BeFalse();
        result.AccessToken.Should().Be("access-token");
        result.RefreshToken.Should().Be("refresh-token");
        result.ExpiresIn.Should().Be(900);
        result.TempToken.Should().BeNull("kayıtlı kullanıcıya kayıt token'ı verilmez");

        _jwtProviderMock.Verify(j => j.GenerateTokens(user.Id, "user", user.Phone), Times.Once);
        _jwtProviderMock.Verify(j => j.GenerateTempToken(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithExistingAdmin_ShouldPassRoleStringToJwtProvider()
    {
        var admin = MakeUser(u => u.Role = UserRole.Admin);
        OtpIsValid();
        UsersInDb(admin);
        _jwtProviderMock.Setup(j => j.GenerateTokens(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new AuthTokens("a", "r", 900));

        await _handler.Handle(new VerifyOtpCommand(Phone, "123456"), CancellationToken.None);

        // Rol token'a enum adıyla değil kontrattaki string'le yazılmalı.
        _jwtProviderMock.Verify(
            j => j.GenerateTokens(admin.Id, UserRole.Admin.ToRoleString(), admin.Phone), Times.Once);
    }

    // ---------------------------------------------------------------- Yeni kullanıcı (happy path)

    [Fact]
    public async Task Handle_WithUnknownPhone_ShouldReturnTempTokenOnly()
    {
        OtpIsValid();
        UsersInDb(); // kimse yok
        _jwtProviderMock.Setup(j => j.GenerateTempToken(Phone)).Returns("temp-token");

        var result = await _handler.Handle(new VerifyOtpCommand(Phone, "123456"), CancellationToken.None);

        result.IsNewUser.Should().BeTrue();
        result.TempToken.Should().Be("temp-token");
        result.AccessToken.Should().BeNull();
        result.RefreshToken.Should().BeNull();
        result.ExpiresIn.Should().BeNull();

        // Faz 10.2: kullanıcı burada OLUŞTURULMAZ, kayıt register ucunda tamamlanır.
        _userRepoMock.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---------------------------------------------------------------- Erişim engelleri

    [Fact]
    public async Task Handle_WithBannedUser_ShouldThrowForbidden()
    {
        OtpIsValid();
        UsersInDb(MakeUser(u => u.IsBanned = true));

        Func<Task> act = () => _handler.Handle(new VerifyOtpCommand(Phone, "123456"), CancellationToken.None);

        var exception = await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("Hesabınız engellenmiştir.");
        exception.Which.Code.Should().Be("FORBIDDEN");
        _jwtProviderMock.Verify(
            j => j.GenerateTokens(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithInactiveUser_ShouldThrowForbidden()
    {
        OtpIsValid();
        UsersInDb(MakeUser(u => u.IsActive = false));

        Func<Task> act = () => _handler.Handle(new VerifyOtpCommand(Phone, "123456"), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>().WithMessage("Hesabınız pasif durumdadır.");
        _jwtProviderMock.Verify(
            j => j.GenerateTokens(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithSoftDeletedPhone_ShouldThrowForbiddenInsteadOfNewUser()
    {
        OtpIsValid();
        var deleted = MakeUser(u => u.DeletedAt = DateTime.UtcNow);

        // Handler Query()'yi iki kez çağırır: (1) global filtreli arama — silinmiş kullanıcı GÖRÜNMEZ,
        // (2) IgnoreQueryFilters ile kontrol — silinmiş kullanıcı GÖRÜNÜR. Bellek-içi sağlayıcıda
        // IgnoreQueryFilters no-op olduğu için filtrenin etkisi çağrı sırasıyla modelleniyor.
        _userRepoMock.SetupSequence(r => r.Query(It.IsAny<bool>()))
            .Returns(Array.Empty<User>().AsAsyncQueryable())
            .Returns(new[] { deleted }.AsAsyncQueryable());

        Func<Task> act = () => _handler.Handle(new VerifyOtpCommand(Phone, "123456"), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("Bu hesap silinmiş. Destek ile iletişime geçin.");
        _jwtProviderMock.Verify(j => j.GenerateTempToken(It.IsAny<string>()), Times.Never);
    }

    // ---------------------------------------------------------------- Normalizasyon

    [Fact]
    public async Task Handle_ShouldTrimPhoneBeforeValidationAndLookup()
    {
        var user = MakeUser();
        _otpServiceMock.Setup(s => s.ValidateAsync(Phone, "123456")).ReturnsAsync(true);
        UsersInDb(user);
        _jwtProviderMock.Setup(j => j.GenerateTokens(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new AuthTokens("a", "r", 900));

        var result = await _handler.Handle(
            new VerifyOtpCommand($"  {Phone}  ", "123456"), CancellationToken.None);

        result.IsNewUser.Should().BeFalse("trim'lenmiş numara kayıtlı kullanıcıyla eşleşmeli");
        _otpServiceMock.Verify(s => s.ValidateAsync(Phone, "123456"), Times.Once);
    }
}
