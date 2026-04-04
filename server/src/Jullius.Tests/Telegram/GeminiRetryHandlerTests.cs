using System.Net;
using FluentAssertions;
using Jullius.ServiceApi.Telegram;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Jullius.Tests.Telegram;

public class GeminiRetryHandlerTests
{
    private static readonly TimeSpan[] ImmediateRetries = [TimeSpan.Zero, TimeSpan.Zero];

    [Fact]
    public async Task SendAsync_ShouldFallbackToStableModel_WhenPrimaryModelReturns503()
    {
        // Arrange
        var responses = new Queue<HttpResponseMessage>(
        [
            CreateResponse(HttpStatusCode.ServiceUnavailable),
            CreateResponse(HttpStatusCode.OK)
        ]);

        var recordingHandler = new RecordingHandler(responses);
        var handler = CreateHandler(recordingHandler, ImmediateRetries);

        using var request = CreateGeminiRequest("gemini-3-flash-preview");

        // Act
        var response = await handler.SendAsync(request, CancellationToken.None);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        recordingHandler.RequestUris.Should().HaveCount(2);
        recordingHandler.RequestUris[0].Should().Contain("gemini-3-flash-preview");
        recordingHandler.RequestUris[1].Should().Contain("gemini-2.5-flash");
        recordingHandler.FallbackHeaders[1].Should().Be("gemini-3-flash-preview->gemini-2.5-flash");
    }

    [Fact]
    public async Task SendAsync_ShouldRetrySameModel_WhenStableModelReturns429()
    {
        // Arrange
        var responses = new Queue<HttpResponseMessage>(
        [
            CreateResponse(HttpStatusCode.TooManyRequests),
            CreateResponse(HttpStatusCode.TooManyRequests),
            CreateResponse(HttpStatusCode.OK)
        ]);

        var recordingHandler = new RecordingHandler(responses);
        var handler = CreateHandler(recordingHandler, ImmediateRetries);

        using var request = CreateGeminiRequest("gemini-2.5-flash");

        // Act
        var response = await handler.SendAsync(request, CancellationToken.None);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        recordingHandler.RequestUris.Should().HaveCount(3);
        recordingHandler.RequestUris.Should().OnlyContain(uri => uri.Contains("gemini-2.5-flash"));
        recordingHandler.FallbackHeaders.Should().OnlyContain(value => value == "(none)");
    }

    [Fact]
    public async Task SendAsync_ShouldReturnLast429_WhenQuotaStillExceededAfterRetries()
    {
        // Arrange
        var responses = new Queue<HttpResponseMessage>(
        [
            CreateResponse(HttpStatusCode.TooManyRequests),
            CreateResponse(HttpStatusCode.TooManyRequests),
            CreateResponse(HttpStatusCode.TooManyRequests)
        ]);

        var recordingHandler = new RecordingHandler(responses);
        var handler = CreateHandler(recordingHandler, ImmediateRetries);

        using var request = CreateGeminiRequest("gemini-2.5-flash");

        // Act
        var response = await handler.SendAsync(request, CancellationToken.None);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        recordingHandler.RequestUris.Should().HaveCount(3);
    }

    private static HttpMessageInvoker CreateHandler(HttpMessageHandler innerHandler, IEnumerable<TimeSpan> retryDelays)
    {
        var handler = new GeminiRetryHandler(Mock.Of<ILogger<GeminiRetryHandler>>(), retryDelays)
        {
            InnerHandler = innerHandler
        };

        return new HttpMessageInvoker(handler);
    }

    private static HttpRequestMessage CreateGeminiRequest(string modelId)
    {
        return new HttpRequestMessage(
            HttpMethod.Post,
            $"https://generativelanguage.googleapis.com/v1beta/models/{modelId}:generateContent")
        {
            Content = new StringContent("{}")
        };
    }

    private static HttpResponseMessage CreateResponse(HttpStatusCode statusCode)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent("{}")
        };
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses;

        public RecordingHandler(Queue<HttpResponseMessage> responses)
        {
            _responses = responses;
        }

        public List<string> RequestUris { get; } = [];
        public List<string> FallbackHeaders { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUris.Add(request.RequestUri!.ToString());
            FallbackHeaders.Add(
                request.Headers.TryGetValues("X-Jullius-Gemini-Fallback", out var values)
                    ? values.Single()
                    : "(none)");

            return Task.FromResult(_responses.Dequeue());
        }
    }
}
