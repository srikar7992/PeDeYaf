using System.Threading;
using System.Threading.Tasks;

namespace PeDeYaf.Application.Common.Interfaces;

public interface ISmsService
{
    Task SendOtpAsync(string phone, string otp, CancellationToken ct = default);
}
