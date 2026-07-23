using System.Threading.Tasks;
using KadirliApp.Application.Features.Lookups;
using Microsoft.AspNetCore.Mvc;

namespace KadirliApp.Api.Controllers;

// Faz 10.4: mobil kayıt (register mahalle seçimi) ve duyuru hedefleme gösterimi için public lookup.
[Route("v1/neighborhoods")]
public class NeighborhoodsController : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Success(await Sender.Send(new GetNeighborhoodsQuery()));
}
