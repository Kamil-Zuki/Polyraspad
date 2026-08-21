using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Grpc.Core;

using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Pvs.Content.Grpc;
using VocabularyService.Grpc;
using VocabularyService.Services;
using Xunit;
using AutoMapper;
using FluentValidation;

namespace VocabularyService.Tests;

public class CardGrpcServiceTests
{
    [Fact]
    public async Task CaptureCard_LimitExceeded_ThrowsResourceExhausted()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<CardGrpcService>>();
        var cardServiceMock = new Mock<ICardService>();
        var noteTypeServiceMock = new Mock<INoteTypeService>();
        var billingLimitServiceMock = new Mock<IBillingLimitService>();
        var mapperMock = new Mock<IMapper>();
        var validatorMock = new Mock<IValidator<BulkCreateCardsRequest>>();

        var service = new CardGrpcService(
            loggerMock.Object,
            cardServiceMock.Object,
            noteTypeServiceMock.Object,
            billingLimitServiceMock.Object,
            mapperMock.Object,
            validatorMock.Object);

        var userId = Guid.NewGuid();
        
        var requestHeaders = new Metadata
        {
            { "user_id", userId.ToString() }
        };

        var contextMock = new Mock<ServerCallContext>();
        contextMock.Protected().Setup<Metadata>("RequestHeadersCore").Returns(requestHeaders);
        var context = contextMock.Object;

        billingLimitServiceMock
            .Setup(x => x.CanCreateCardAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false); // limit exceeded

        billingLimitServiceMock
            .Setup(x => x.GetMaxCardsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(500);

        var request = new CaptureCardRequest
        {
            ProjectId = Guid.NewGuid().ToString(),
            DeckId = Guid.NewGuid().ToString()
        };

        // Act
        Func<Task> act = async () => await service.CaptureCard(request, context);

        // Assert
        var ex = await act.Should().ThrowAsync<RpcException>();
        ex.Where(e => e.StatusCode == StatusCode.ResourceExhausted);
        ex.Where(e => e.Status.Detail.Contains("Billing limit exceeded: maxCards"));
    }
}
