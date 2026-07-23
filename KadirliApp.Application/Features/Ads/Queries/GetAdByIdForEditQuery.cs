using System;
using KadirliApp.Application.Features.Ads.Commands.UpdateAd;
using MediatR;

namespace KadirliApp.Application.Features.Ads.Queries;

public record GetAdByIdForEditQuery(Guid Id) : IRequest<UpdateAdCommand?>;
