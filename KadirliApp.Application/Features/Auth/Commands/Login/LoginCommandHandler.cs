using KadirliApp.Application.Common.Interfaces;
using MediatR;

namespace KadirliApp.Application.Features.Auth.Commands.Login;

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, OtpRequestResult>
{
    private readonly IOtpService _otpService;

    public LoginCommandHandler(IOtpService otpService)
    {
        _otpService = otpService;
    }

    public async Task<OtpRequestResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Phone))
            throw new ArgumentException("Telefon numarası zorunludur.");

        return await _otpService.RequestAsync(request.Phone.Trim(), cancellationToken);
    }
}
