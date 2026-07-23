using System;
using KadirliApp.Application.Features.Deaths.Dtos;
using MediatR;

namespace KadirliApp.Application.Features.Deaths.Queries;

/// <summary>
/// OnlyPublished=true (public uç): approved olmayan ilanı YALNIZ ekleyen (AddedBy == RequesterId) görür,
/// diğerlerine null → 404 (Ads detay emsali). Admin/panel varsayılanla (false) her statüyü görür.
/// </summary>
public record GetDeathNoticeByIdQuery(Guid Id, bool OnlyPublished = false, Guid? RequesterId = null) : IRequest<DeathNoticeResponseDto?>;
