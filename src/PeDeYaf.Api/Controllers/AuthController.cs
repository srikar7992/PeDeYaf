using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using PeDeYaf.Application.Auth.Commands;

namespace PeDeYaf.Api.Controllers;

[ApiController]
[Route("v1/[controller]")]
public class AuthController(IMediator mediator) : ControllerBase
{
    [HttpPost("otp/request")]
    public async Task<IActionResult> RequestOtp([FromBody] RequestOtpCommand command, CancellationToken ct)
    {
        await mediator.Send(command, ct);
        return Ok(new { message = "OTP sent" });
    }

    [HttpPost("otp/verify")]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return Ok(result);
    }
}
