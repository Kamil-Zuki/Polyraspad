using System.Text;
using AggregatorService.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace AggregatorService.Tests;

public class DocumentTextExtractorTests
{
    [Fact]
    public async Task ExtractAsync_WhenPdfHasNoTextLayer_ShouldFallbackToOcr()
    {
        var ocr = new Mock<IOcrService>(MockBehavior.Strict);
        ocr.Setup(s => s.RecognizePdfAsync(It.IsAny<byte[]>(), "ru", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OcrRecognizeResult(
                "Scanned hello",
                [new OcrPageResult(1, "Scanned hello")],
                PageCount: 1,
                Warning: null));

        var extractor = new DocumentTextExtractor(ocr.Object);
        await using var ms = new MemoryStream(CreateEmptyTextLayerPdf());

        var result = await extractor.ExtractAsync(ms, "scan.pdf", "application/pdf", "ru");

        result.UsedOcr.Should().BeTrue();
        result.SourceFormat.Should().Be("pdf+ocr");
        result.Text.Should().Be("Scanned hello");
        result.Pages.Should().ContainSingle(p => p.PageNumber == 1 && p.Text == "Scanned hello");
        ocr.VerifyAll();
    }

    [Theory]
    [InlineData("en", "en")]
    [InlineData("ru-RU", "ru")]
    [InlineData("ko", "ko")]
    [InlineData("de", "en")]
    public void MapOcrLanguage_ShouldNormalizeSupportedCodes(string input, string expected)
    {
        OcrGrpcService.MapOcrLanguage(input).Should().Be(expected);
    }

    private static byte[] CreateEmptyTextLayerPdf()
    {
        // Minimal PDF 1.4: one blank page, no text operators (PdfPig yields empty page.Text).
        const string pdf =
            """
            %PDF-1.4
            1 0 obj<< /Type /Catalog /Pages 2 0 R >>endobj
            2 0 obj<< /Type /Pages /Kids [3 0 R] /Count 1 >>endobj
            3 0 obj<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R /Resources << >> >>endobj
            4 0 obj<< /Length 0 >>stream
            endstream
            endobj
            xref
            0 5
            0000000000 65535 f 
            0000000009 00000 n 
            0000000058 00000 n 
            0000000115 00000 n 
            0000000214 00000 n 
            trailer<< /Size 5 /Root 1 0 R >>
            startxref
            263
            %%EOF
            """;
        return Encoding.ASCII.GetBytes(pdf.Replace("\r\n", "\n"));
    }
}
