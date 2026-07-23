using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using KadirliApp.Application.Features.PowerOutages.Queries.GetPowerOutages;
using KadirliApp.Application.Features.PowerOutages.Queries.GetPowerOutageById;

namespace KadirliApp.Api.Controllers;

// Faz 10.1: AdminPanel korumalı POST/PUT/DELETE kopyaları kaldırıldı — admin karşılıkları
// v1/admin/power-outages'ta. Public yüzey mobil için salt-okunur.
public class PowerOutagesController : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return Success(await Sender.Send(new GetPowerOutagesQuery()));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        return Success(await Sender.Send(new GetPowerOutageByIdQuery { Id = id }));
    }
}
