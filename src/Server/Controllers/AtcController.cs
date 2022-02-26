namespace AtcDemo.Server.Controllers;

using AtcDemo.Shared;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("[controller]")]
public class AtcController : ControllerBase
{
    private readonly AtcDataService _service;

    public AtcController(AtcDataService service)
    {
        _service = service;
    }

    [HttpGet]
    public IEnumerable<Atc.Chemical> Get()
    {
        return _service.GetAtcChemicals();
    }
}
