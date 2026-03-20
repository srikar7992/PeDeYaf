using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using PeDeYaf.Application.Documents.Commands;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using System.Net.Http.Headers;

namespace PeDeYaf.Api.Integration.Tests;

public class DocumentsIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public DocumentsIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            // We can replace DB, Redis, services with in-memory or mocks here, skipping for brevity in blueprint
        });
    }

    [Fact]
    public async Task GenerateUploadUrl_Unauthorized_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/v1/documents/upload-url", new
        {
            fileName = "test.pdf",
            contentType = "application/pdf",
            folderId = (Guid?)null,
        });

        // The endpoint is protected by Authorize
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
