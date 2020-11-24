using Microsoft.AspNetCore.Mvc;

namespace PaymentGateway.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public abstract class BaseApiController : Controller
    {
    }
}