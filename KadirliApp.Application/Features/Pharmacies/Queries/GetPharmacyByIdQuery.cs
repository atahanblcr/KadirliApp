using System;
using KadirliApp.Application.Features.Pharmacies.Dtos;
using MediatR;

namespace KadirliApp.Application.Features.Pharmacies.Queries;

/// <summary>OnlyActive=true (public uç): pasif eczane id bilinse bile dönmez (null → 404).</summary>
public record GetPharmacyByIdQuery(Guid Id, bool OnlyActive = false) : IRequest<PharmacyResponseDto?>;
