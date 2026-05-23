#nullable enable
using System.Net;
using System.Text;
using System.Text.Json;
using AggregatorService.Options;
using AggregatorService.Services;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace AggregatorService.Tests;

public class TtsSpeechClientTests
{
    private static readonly byte[] SampleAudio = [0x49, 0x44, 0x33]; // minimal MP3-ish header

    [Fact]
    public async Task CreateSpeechAsync_Mistral_ReturnsDecodedBase64Audio()
    {
        var expectedPayload = Convert.ToBase64String(SampleAudio);
        var responseJson = JsonSerializer.Serialize(new { audio_data = expectedPayload });
        var handler = new StubHttpMessageHandler(
            (_, content) =>
            {
                using var doc = JsonDocument.Parse(content);
                doc.RootElement.GetProperty("model").GetString().Should().Be("voxtral-mini-tts-2603");
                doc.RootElement.GetProperty("voice_id").GetString().Should().Be("voice_abc123");
                doc.RootElement.GetProperty("input").GetString().Should().Be("hello");
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(responseJson, Encoding.UTF8, "application/json"),
                };
            });

        var client = CreateSpeechClient(handler, mistral: true);
        var bytes = await client.CreateSpeechAsync(
            "voxtral-mini-tts-2603",
            "hello",
            "voice_abc123",
            "mp3",
            speed: 1.0);

        bytes.Should().Equal(SampleAudio);
    }

    [Fact]
    public async Task CreateSpeechAsync_Mistral_WhenProviderReturnsInvalidModel_ThrowsHttpRequestException()
    {
        const string errorBody =
            """{"object":"error","message":"Invalid model: tts-1","type":"invalid_model","code":"1500"}""";
        var handler = new StubHttpMessageHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(errorBody, Encoding.UTF8, "application/json"),
            });

        var client = CreateSpeechClient(handler, mistral: true);
        var act = () => client.CreateSpeechAsync("tts-1", "hello", "voice_abc123", "mp3", 1.0);

        var ex = await act.Should().ThrowAsync<HttpRequestException>();
        ex.Which.Message.Should().Contain("400");
        ex.Which.Message.Should().Contain("Invalid model");
    }

    [Fact]
    public async Task CreateSpeechAsync_OpenAi_ReturnsRawBinaryBody()
    {
        var handler = new StubHttpMessageHandler((request, content) =>
        {
            request.Method.Should().Be(HttpMethod.Post);
            using var doc = JsonDocument.Parse(content);
            doc.RootElement.GetProperty("voice").GetString().Should().Be("alloy");
            var hasVoiceId = doc.RootElement.TryGetProperty("voice_id", out JsonElement voiceIdProp);
            hasVoiceId.Should().BeFalse();
            voiceIdProp.ValueKind.Should().Be(JsonValueKind.Undefined);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(SampleAudio),
            };
        });

        var client = CreateSpeechClient(handler, mistral: false);
        var bytes = await client.CreateSpeechAsync("tts-1", "hello", "alloy", "mp3", 1.0);

        bytes.Should().Equal(SampleAudio);
    }

    [Fact]
    public void PickVoice_Mistral_UsesConfiguredSavedVoiceId()
    {
        var options = new AiCompletionOptions
        {
            BaseUrl = "https://api.mistral.ai/v1",
            TtsVoiceId = "voice_abc123",
            TtsVoice = "alloy",
        };

        var voice = TtsVoiceResolver.PickVoice(null, "en", options);
        voice.Should().Be("voice_abc123");
    }

    [Fact]
    public void PickVoice_Mistral_PassesThroughRequestVoice()
    {
        var options = new AiCompletionOptions
        {
            BaseUrl = "https://api.mistral.ai/v1",
            TtsVoice = "alloy",
        };

        var voice = TtsVoiceResolver.PickVoice("request_voice_id", "en", options);
        voice.Should().Be("request_voice_id");
    }

    [Fact]
    public void PickVoice_Mistral_WhenOnlyOpenAiVoiceConfigured_ThrowsHelpfulConfigError()
    {
        var options = new AiCompletionOptions
        {
            BaseUrl = "https://api.mistral.ai/v1",
            TtsProvider = "mistral",
            TtsVoice = "alloy",
        };

        var act = () => TtsVoiceResolver.PickVoice(null, "en", options);
        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("*saved voice_id*AI_TTS_VOICE_ID*");
    }

    [Fact]
    public void ResolveProvider_Auto_MistralLlmWithoutVoiceId_FallsBackToEspeak()
    {
        var options = new AiCompletionOptions
        {
            BaseUrl = "https://api.mistral.ai/v1",
            TtsProvider = "auto",
            TtsVoice = "alloy",
        };

        TtsProviderHelper.ResolveProvider(options).Should().Be(TtsProvider.Espeak);
    }

    [Fact]
    public void ResolveProvider_Auto_MistralLlmWithSavedVoiceId_UsesMistral()
    {
        var options = new AiCompletionOptions
        {
            BaseUrl = "https://api.mistral.ai/v1",
            TtsVoiceId = "voice_abc123",
        };

        TtsProviderHelper.ResolveProvider(options).Should().Be(TtsProvider.Mistral);
    }

    [Fact]
    public void PickVoice_Auto_MistralLlmWithoutVoiceId_UsesEspeakDefaults()
    {
        var options = new AiCompletionOptions
        {
            BaseUrl = "https://api.mistral.ai/v1",
            TtsVoice = "alloy",
        };

        TtsVoiceResolver.PickVoice(null, "en", options).Should().Be("en-us");
        TtsVoiceResolver.PickVoice(null, "ru", options).Should().Be("ru");
    }

    [Fact]
    public void ResolveTtsModel_MistralBaseUrl_ReplacesOpenAiDefaultModel()
    {
        var options = new AiCompletionOptions
        {
            BaseUrl = "https://api.mistral.ai/v1",
            TtsProvider = "mistral",
            TtsModel = "tts-1",
        };

        TtsProviderHelper.ResolveTtsModel(options).Should().Be(TtsProviderHelper.MistralDefaultModel);
    }

    [Fact]
    public void ResolveTtsProvider_Espeak_UsesFreeOfflineDefaults()
    {
        var options = new AiCompletionOptions
        {
            BaseUrl = "https://api.mistral.ai/v1",
            TtsProvider = "espeak",
            TtsModel = "tts-1",
            TtsResponseFormat = "mp3",
        };

        TtsProviderHelper.ResolveProviderLabel(options).Should().Be("espeak");
        TtsProviderHelper.ResolveTtsModel(options).Should().Be(TtsProviderHelper.EspeakModel);
        TtsProviderHelper.ResolveResponseFormat(options).Should().Be("wav");
    }

    [Fact]
    public void PickVoice_Espeak_UsesLanguageVoiceWithoutMistralVoiceId()
    {
        var options = new AiCompletionOptions
        {
            BaseUrl = "https://api.mistral.ai/v1",
            TtsProvider = "espeak",
            TtsVoice = "alloy",
        };

        TtsVoiceResolver.PickVoice(null, "ru", options).Should().Be("ru");
        TtsVoiceResolver.PickVoice(null, "en", options).Should().Be("en-us");
    }

    [Fact]
    public void PickVoice_Espeak_SkipsOpenAiPerLanguageVoicesFromAppsettings()
    {
        var options = new AiCompletionOptions
        {
            TtsProvider = "espeak",
            TtsVoiceEn = "alloy",
            TtsVoiceRu = "nova",
            TtsVoiceKo = "shimmer",
        };

        TtsVoiceResolver.PickVoice(null, "en", options).Should().Be("en-us");
        TtsVoiceResolver.PickVoice(null, "ru", options).Should().Be("ru");
        TtsVoiceResolver.PickVoice(null, "ko", options).Should().Be("ko");
    }

    [Fact]
    public void ResolveTtsModel_OpenAiBaseUrl_KeepsConfiguredModel()
    {
        var options = new AiCompletionOptions
        {
            BaseUrl = "https://api.openai.com/v1",
            TtsModel = "tts-1-hd",
        };

        TtsProviderHelper.ResolveTtsModel(options).Should().Be("tts-1-hd");
    }

    private static OpenAiSpeechClient CreateSpeechClient(HttpMessageHandler handler, bool mistral)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(mistral ? "https://api.mistral.ai/v1/" : "https://api.openai.com/v1/"),
        };
        var options = Microsoft.Extensions.Options.Options.Create(new AiCompletionOptions
        {
            BaseUrl = mistral ? "https://api.mistral.ai/v1" : "https://api.openai.com/v1",
            TtsProvider = mistral ? "mistral" : "openai",
            ApiKey = "test-key",
            Enabled = true,
            TtsEnabled = true,
        });
        return new OpenAiSpeechClient(httpClient, options);
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, string, HttpResponseMessage> _handler;

        public StubHttpMessageHandler(Func<HttpRequestMessage, string, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var content = request.Content is null
                ? ""
                : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return _handler(request, content);
        }
    }
}
