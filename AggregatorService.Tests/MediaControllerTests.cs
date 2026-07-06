using System.Net;
using System.Text;
using AggregatorService.Controllers;
using AggregatorService.Dtos.Auth;
using AggregatorService.Dtos;
using AggregatorService.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Pvs.Media.Grpc;
using Xunit;

namespace AggregatorService.Tests;

public class MediaControllerTests
{
    [Fact]
    public async Task UploadImage_WhenGrpcReturnsUrl_ShouldReturn201WithUrlInBody()
    {
        const string expectedUrl = "http://localhost:9000/polyraspad-media/images/11111111-1111-1111-1111-111111111111";
        var mock = new Mock<IMediaServiceClient>(MockBehavior.Strict);
        mock
            .Setup(c => c.UploadImageAsync(
                It.IsAny<UploadImageRequest>(),
                It.IsAny<Guid>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UploadImageResponse
            {
                Url = expectedUrl,
                ImageId = "11111111-1111-1111-1111-111111111111"
            });

        var controller = new MediaController(mock.Object, Mock.Of<IAuthorizationServiceClient>(), Mock.Of<IHttpClientFactory>(), new DocumentTextExtractor(Mock.Of<IOcrService>()), Mock.Of<ITtsAudioService>(), NullLogger<MediaController>.Instance);

        var pngHeader = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        await using var ms = new MemoryStream(pngHeader);
        IFormFile file = new FormFile(ms, 0, pngHeader.Length, "file", "cover.png")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/png"
        };

        controller.ControllerContext = new ControllerContext { HttpContext = CreateHttpContext() };

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
            Mock.Of<IMediaServiceClient>(),
            Mock.Of<IAuthorizationServiceClient>(),
            httpClientFactory.Object,
            new DocumentTextExtractor(Mock.Of<IOcrService>()),
            Mock.Of<ITtsAudioService>(),
            NullLogger<MediaController>.Instance);

        var result = await controller.ServeImage(id: null, url: "http://example.test/image.png", cancellationToken: CancellationToken.None);

        var file = result.Should().BeOfType<FileContentResult>().Subject;
        file.ContentType.Should().Be("image/png");
        file.FileContents.Should().Equal(expectedBytes);
    }

    [Fact]
    public async Task GetReaderLibrary_WhenGrpcReturnsBooks_ShouldReturnDtos()
    {
        var mock = new Mock<IMediaServiceClient>(MockBehavior.Strict);
        mock
            .Setup(c => c.ListReaderLibraryBooksAsync(
                "proj-1",
                It.IsAny<Guid>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ListReaderLibraryBooksResponse
            {
                Books =
                {
                    new ReaderLibraryBook
                    {
                        Id = "11111111-1111-1111-1111-111111111111",
                        Title = "Test Book",
                        FileName = "test.pdf",
                        Url = "http://localhost/documents/11111111-1111-1111-1111-111111111111",
                        DocumentId = "11111111-1111-1111-1111-111111111111",
                        PageCount = 12,
                        UploadedAt = "2026-04-20T00:00:00.0000000Z",
                        LastOpenedAt = "2026-04-20T01:00:00.0000000Z"
                    }
                }
            });

        var controller = new MediaController(mock.Object, Mock.Of<IAuthorizationServiceClient>(), Mock.Of<IHttpClientFactory>(), new DocumentTextExtractor(Mock.Of<IOcrService>()), Mock.Of<ITtsAudioService>(), NullLogger<MediaController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = CreateHttpContext() }
        };

        var actionResult = await controller.GetReaderLibrary("proj-1");
        var ok = actionResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        var books = ok.Value.Should().BeAssignableTo<IEnumerable<ReaderLibraryBookDto>>().Subject.ToList();

        books.Should().ContainSingle();
        books[0].Title.Should().Be("Test Book");
        books[0].PageCount.Should().Be(12);
    }

    [Fact]
    public async Task SaveReaderLibraryBook_WhenGrpcReturnsBook_ShouldReturnDto()
    {
        var mock = new Mock<IMediaServiceClient>(MockBehavior.Strict);
        mock
            .Setup(c => c.SaveReaderLibraryBookAsync(
                It.Is<SaveReaderLibraryBookRequest>(request =>
                    request.ProjectId == "proj-1" &&
                    request.Book.Id == "11111111-1111-1111-1111-111111111111"),
                It.IsAny<Guid>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SaveReaderLibraryBookResponse
            {
                Book = new ReaderLibraryBook
                {
                    Id = "11111111-1111-1111-1111-111111111111",
                    Title = "Saved Book",
                    FileName = "saved.pdf",
                    Url = "http://localhost/documents/11111111-1111-1111-1111-111111111111",
                    DocumentId = "11111111-1111-1111-1111-111111111111",
                    PageCount = 44,
                    UploadedAt = "2026-04-20T00:00:00.0000000Z",
                    LastOpenedAt = "2026-04-20T01:00:00.0000000Z"
                }
            });

        var authMock = new Mock<IAuthorizationServiceClient>();
        authMock
            .Setup(a => a.GetUserInfoAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserInfoDto
            {
                Id = "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
                UserName = "tester",
                Email = "tester@example.com"
            });

        var controller = new MediaController(mock.Object, authMock.Object, Mock.Of<IHttpClientFactory>(), new DocumentTextExtractor(Mock.Of<IOcrService>()), Mock.Of<ITtsAudioService>(), NullLogger<MediaController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = CreateHttpContext() }
        };

        var payload = new SaveReaderLibraryBookDto
        {
            Title = "Saved Book",
            FileName = "saved.pdf",
            DocumentId = "11111111-1111-1111-1111-111111111111",
            PageCount = 44,
            UploadedAt = "2026-04-20T00:00:00.0000000Z",
            LastOpenedAt = "2026-04-20T01:00:00.0000000Z"
        };

        var actionResult = await controller.SaveReaderLibraryBook("proj-1", "11111111-1111-1111-1111-111111111111", payload);
        var ok = actionResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = ok.Value.Should().BeOfType<ReaderLibraryBookDto>().Subject;

        dto.Title.Should().Be("Saved Book");
        dto.DocumentId.Should().Be("11111111-1111-1111-1111-111111111111");
    }

    [Fact]
    public async Task DeleteReaderLibraryBook_WhenValidRequest_ShouldReturnNoContent()
    {
        var mock = new Mock<IMediaServiceClient>(MockBehavior.Strict);
        mock
            .Setup(c => c.DeleteReaderLibraryBookAsync(
                "proj-1",
                "11111111-1111-1111-1111-111111111111",
                It.IsAny<Guid>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var controller = new MediaController(mock.Object, Mock.Of<IAuthorizationServiceClient>(), Mock.Of<IHttpClientFactory>(), new DocumentTextExtractor(Mock.Of<IOcrService>()), Mock.Of<ITtsAudioService>(), NullLogger<MediaController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = CreateHttpContext() }
        };

        var result = await controller.DeleteReaderLibraryBook("proj-1", "11111111-1111-1111-1111-111111111111");
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task ExtractDocumentText_WhenTxt_ShouldReturn200WithText()
    {
        var controller = new MediaController(
            Mock.Of<IMediaServiceClient>(),
            Mock.Of<IAuthorizationServiceClient>(),
            Mock.Of<IHttpClientFactory>(),
            new DocumentTextExtractor(Mock.Of<IOcrService>()),
            Mock.Of<ITtsAudioService>(),
            NullLogger<MediaController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = CreateHttpContext() }
        };

        var bytes = Encoding.UTF8.GetBytes("Hello reader pipeline.");
        await using var ms = new MemoryStream(bytes);
        IFormFile file = new FormFile(ms, 0, bytes.Length, "file", "lesson.txt")
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/plain"
        };

        var actionResult = await controller.ExtractDocumentText(file, CancellationToken.None);
        var ok = actionResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = ok.Value.Should().BeOfType<ExtractDocumentTextResponseDto>().Subject;
        dto.Text.Should().Be("Hello reader pipeline.");
        dto.SourceFormat.Should().Be("txt");
    }

    [Fact]
    public async Task ExtractDocumentText_WhenPdfHasNoTextLayer_ShouldReturn422WithStructuredBody()
    {
        var extractorMock = new Mock<IDocumentTextExtractor>(MockBehavior.Strict);
        extractorMock
            .Setup(e => e.ExtractAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NoExtractableDocumentTextException(
                "pdf",
                "PDF_NO_TEXT_LAYER",
                "This PDF has no selectable text layer (common for scanned pages). OCR is not supported in this release."));

        var controller = new MediaController(
            Mock.Of<IMediaServiceClient>(),
            Mock.Of<IAuthorizationServiceClient>(),
            Mock.Of<IHttpClientFactory>(),
            extractorMock.Object,
            Mock.Of<ITtsAudioService>(),
            NullLogger<MediaController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = CreateHttpContext() }
        };

        var dummyPdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };
        await using var ms = new MemoryStream(dummyPdfBytes);
        IFormFile file = new FormFile(ms, 0, dummyPdfBytes.Length, "file", "scan.pdf")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/pdf"
        };

        var actionResult = await controller.ExtractDocumentText(file, CancellationToken.None);
        var obj = actionResult.Result.Should().BeOfType<UnprocessableEntityObjectResult>().Subject;
        obj.StatusCode.Should().Be(422);
    }

    [Fact]
    public async Task GenerateAudio_WhenBodyNull_ShouldReturn400()
    {
        var tts = new Mock<ITtsAudioService>(MockBehavior.Strict);
        var controller = new MediaController(
            Mock.Of<IMediaServiceClient>(),
            Mock.Of<IAuthorizationServiceClient>(),
            Mock.Of<IHttpClientFactory>(),
            new DocumentTextExtractor(Mock.Of<IOcrService>()),
            tts.Object,
            NullLogger<MediaController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = CreateHttpContext() }
        };

        var actionResult = await controller.GenerateAudio(null);
        var bad = actionResult.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        bad.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task GenerateAudio_WhenTtsThrowsArgument_ShouldReturn400()
    {
        var tts = new Mock<ITtsAudioService>();
        tts
            .Setup(s => s.GenerateAndStoreAsync(
                It.IsAny<GenerateAudioRequestDto>(),
                It.IsAny<Guid>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Language must be one of: en, ru, ko."));

        var controller = new MediaController(
            Mock.Of<IMediaServiceClient>(),
            Mock.Of<IAuthorizationServiceClient>(),
            Mock.Of<IHttpClientFactory>(),
            new DocumentTextExtractor(Mock.Of<IOcrService>()),
            tts.Object,
            NullLogger<MediaController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = CreateHttpContext() }
        };

        var actionResult = await controller.GenerateAudio(new GenerateAudioRequestDto { Text = "x", Language = "de" });
        var bad = actionResult.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        bad.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task GenerateAudio_WhenTtsReturnsUrl_ShouldReturn201WithBody()
    {
        var expected = new GenerateAudioResponseDto
        {
            Url = "http://localhost:9000/polyraspad-media/audio/22222222-2222-2222-2222-222222222222",
            AudioId = "22222222-2222-2222-2222-222222222222",
            Provider = "openai-compatible",
            Language = "ko",
        };
        var tts = new Mock<ITtsAudioService>(MockBehavior.Strict);
        tts
            .Setup(s => s.GenerateAndStoreAsync(
                It.Is<GenerateAudioRequestDto>(r => r.Text.Trim() == "안녕" && r.Language == "ko"),
                It.IsAny<Guid>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var controller = new MediaController(
            Mock.Of<IMediaServiceClient>(),
            Mock.Of<IAuthorizationServiceClient>(),
            Mock.Of<IHttpClientFactory>(),
            new DocumentTextExtractor(Mock.Of<IOcrService>()),
            tts.Object,
            NullLogger<MediaController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = CreateHttpContext() }
        };

        var actionResult = await controller.GenerateAudio(new GenerateAudioRequestDto { Text = "안녕", Language = "ko" });
        var ok = actionResult.Result.Should().BeOfType<ObjectResult>().Subject;
        ok.StatusCode.Should().Be(201);
        var dto = ok.Value.Should().BeOfType<GenerateAudioResponseDto>().Subject;
        dto.Url.Should().Be(expected.Url);
        dto.AudioId.Should().Be(expected.AudioId);
        dto.Language.Should().Be("ko");
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-User-Id"] = "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee";
        return httpContext;
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
