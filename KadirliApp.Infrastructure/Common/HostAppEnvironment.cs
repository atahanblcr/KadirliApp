using KadirliApp.Application.Common.Interfaces;
using Microsoft.Extensions.Hosting;

namespace KadirliApp.Infrastructure.Common;

/// <summary>
/// Faz 12.19a — <see cref="IAppEnvironment"/>'ın host gerçeklemesi: ASP.NET Core'un
/// <see cref="IHostEnvironment"/>'ını sarar.
/// </summary>
/// <remarks>
/// 📌 Sarmalayıcı ince olduğu için "gereksiz" görünebilir; değil. Application katmanı
/// <c>Microsoft.Extensions.Hosting</c>'e referans vermez (§1) ve vermemeli — ortam bilgisi
/// bir <i>barındırma</i> ayrıntısıdır. Bu sınıf o sınırın üzerinde durur ve iki host'un
/// (Api · Web) aynı cevabı vermesini garanti eder: <c>AddInfrastructure</c> ikisinde de
/// çağrıldığı için ikinci bir kayıt yeri yok.
/// </remarks>
public sealed class HostAppEnvironment : IAppEnvironment
{
    private readonly IHostEnvironment _env;

    public HostAppEnvironment(IHostEnvironment env) => _env = env;

    public string Name => _env.EnvironmentName;

    public bool IsDevelopment => _env.IsDevelopment();
}
