using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Routing;

namespace KadirliApp.Api;

/// <summary>
/// Faz 10.13: route token dönüştürücü — `[controller]` token'ını kebab-case'e çevirir
/// (PowerOutages → power-outages, Announcements → announcements). Böylece ApiControllerBase'in
/// `[Route("v1/[controller]")]`'ını miras alan controller'lar da explicit-route'lu diğerleriyle
/// (v1/ads, v1/admin/...) tutarlı, tamamen küçük-harf/kebab public path üretir. openapi.json / Flutter
/// codegen için tek biçim. Not: routing zaten case-insensitive; eski PascalCase çağrılar da çalışmaya devam eder.
/// </summary>
public sealed class SlugifyParameterTransformer : IOutboundParameterTransformer
{
    public string? TransformOutbound(object? value)
        => value is null ? null : Regex.Replace(value.ToString()!, "([a-z0-9])([A-Z])", "$1-$2").ToLowerInvariant();
}
