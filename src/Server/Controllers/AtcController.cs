namespace AtcDemo.Server.Controllers;

using AtcDemo.Shared;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("[controller]")]
public class AtcController : ControllerBase
{
    private readonly AtcService _service;

    public AtcController(AtcService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IEnumerable<Atc.Classification>> Get()
    {
        return await _service.GetAtcClassifications();
    }
}
