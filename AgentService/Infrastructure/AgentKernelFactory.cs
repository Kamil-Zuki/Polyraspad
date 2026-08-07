using AgentService.Options;
using AgentService.Plugins;
using AgentService.Services;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Pvs.Content.Grpc;

namespace AgentService.Infrastructure;

public class AgentKernelFactory
{
    private readonly IOptions<AiOptions> _aiOptions;

    public AgentKernelFactory(IOptions<AiOptions> aiOptions)
    {
        _aiOptions = aiOptions;
    }

    public Kernel CreateKernel(
        IVocabularyGrpcClient vocabularyClient,
        Guid userId, 
        Guid projectId, 
        IEnumerable<string> roles)
    {
        var builder = Kernel.CreateBuilder();
        var options = _aiOptions.Value;

        if (!options.Enabled)
        {
            throw new InvalidOperationException("AI completion is disabled in configuration.");
        }

        builder.AddOpenAIChatCompletion(
            modelId: options.Model,
            apiKey: options.ApiKey,
            endpoint: new Uri(options.BaseUrl));

        builder.Plugins.AddFromObject(
            new VocabularyPlugin(vocabularyClient, userId, projectId, roles));
        
        builder.Plugins.AddFromObject(
            new UiActionsPlugin());

        return builder.Build();
    }
}
