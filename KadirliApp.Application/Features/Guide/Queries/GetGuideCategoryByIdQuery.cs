using System;
using KadirliApp.Application.Features.Guide.Dtos;
using MediatR;

namespace KadirliApp.Application.Features.Guide.Queries;

public record GetGuideCategoryByIdQuery(Guid Id) : IRequest<GuideCategoryResponseDto?>;
