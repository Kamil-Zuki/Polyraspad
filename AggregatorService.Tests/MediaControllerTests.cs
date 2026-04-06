using System.Net;
using AggregatorService.Controllers;
using AggregatorService.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AggregatorService.Tests;

public class MediaControllerTests
{
    [Fact]
    public async Task ServeImage_WhenUrlIsProvided_ShouldReturnImageBytes()
    {
        var expectedBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        var handler = new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(expectedBytes)
                {
                    Headers =
                    {
                        ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png")
                    }
                }
            });
        var httpClientFactory = new Mock<IHttpClientFactory>(MockBehavior.Strict);
        httpClientFactory
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient(handler));

        var controller = new MediaController(
            Mock.Of<IVocabularyServiceClient>(),
            httpClientFactory.Object,
            NullLogger<MediaController>.Instance);

        var result = await controller.ServeImage(url: "http://example.test/image.png", id: null, cancellationToken: CancellationToken.None);

        var file = result.Should().BeOfType<FileContentResult>().Subject;
        file.ContentType.Should().Be("image/png");
        file.FileContents.Should().Equal(expectedBytes);
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_handler(request));
        }
    }
}
