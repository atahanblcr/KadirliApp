using System;
using KadirliApp.Application.Features.Ads.Dtos;
using MediatR;

namespace KadirliApp.Application.Features.Ads.Queries;

/// <summary>
/// Faz 10.5: public ilan detayı. Yalnız "approved" ilanlar herkese açıktır;
/// RequesterId ilan sahibiyse pending/rejected ilan da döner (mobil "ilanım onay bekliyor" ekranı).
/// Aksi hâlde 404 — ilanın varlığı sızdırılmaz.
/// </summary>
public record GetAdByIdQuery(Guid Id, Guid? RequesterId = null) : IRequest<AdDetailDto>;
