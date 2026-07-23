using System;
using KadirliApp.Application.Features.Places.Dtos;
using MediatR;

namespace KadirliApp.Application.Features.Places.Queries;

/// <summary>OnlyActive=true (public uç): pasif mekan id bilinse bile dönmez (null → 404).</summary>
public record GetPlaceByIdQuery(Guid Id, bool OnlyActive = false) : IRequest<PlaceResponseDto>;
