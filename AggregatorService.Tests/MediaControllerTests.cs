using System.Net;
using AggregatorService.Controllers;
using AggregatorService.Services;
using FluentAssertions;
using AggregatorService.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Pvs.Content.Grpc;
using Xunit;

namespace AggregatorService.Tests;

public class MediaControllerTests
{
    /// <summary>
    /// Регрессия: ответ загрузки должен содержать JSON с url (MinIO / S3), иначе обложка колоды и редактор не получают ссылку.
    /// </summary>
    [Fact]
    public async Task UploadImage_WhenGrpcReturnsUrl_ShouldReturn201WithUrlInBody()
    {
        const string expectedUrl = "http://localhost:9000/polyraspad-media/images/11111111-1111-1111-1111-111111111111";
        var mock = new Mock<IVocabularyServiceClient>(MockBehavior.Strict);
        mock
            .Setup(c => c.UploadImageAsync(
                It.IsAny<UploadImageRequest>(),
                It.IsAny<Guid>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UploadImageResponse { Url = expectedUrl, ImageId = "11111111-1111-1111-1111-111111111111" });

        var httpClientFactory = new Mock<IHttpClientFactory>(MockBehavior.Loose);
        var controller = new MediaController(mock.Object, httpClientFactory.Object, NullLogger<MediaController>.Instance);

        var pngHeader = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        await using var ms = new MemoryStream(pngHeader);
        IFormFile file = new FormFile(ms, 0, pngHeader.Length, "file", "cover.png") { Headers = new HeaderDictionary(), ContentType = "image/png" };

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-User-Id"] = "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee";
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        var actionResult = await controller.UploadImage(file);
        var objectResult = actionResult.Result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(201);
        var dto = objectResult.Value.Should().BeOfType<UploadImageResponseDto>().Subject;
        dto.Url.Should().Be(expectedUrl);
        dto.ImageId.Should().Be("11111111-1111-1111-1111-111111111111");
    }

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
