using System;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Microsoft.Extensions.Logging;
using PeDeYaf.Application.Common.Interfaces;

namespace PeDeYaf.Infrastructure.Sms;

public class SnsService(
    IAmazonSimpleNotificationService snsClient,
    ILogger<SnsService> logger) : ISmsService
{
    public async Task SendOtpAsync(string phone, string otp, CancellationToken ct = default)
    {
        try
        {
            var request = new PublishRequest
            {
                Message = $"Your PeDeYaf verification code is: {otp}. It expires in 10 minutes.",
                PhoneNumber = phone,
            };

            await snsClient.PublishAsync(request, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send SMS to {Phone}", phone);
            throw; // Fail the request so user can retry
        }
    }
}
