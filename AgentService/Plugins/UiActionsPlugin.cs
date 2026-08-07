using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Text.Json;

namespace AgentService.Plugins;

public class UiActionsPlugin
{
    [KernelFunction, Description("Navigate the user to a page in the app.")]
    public string Navigate(
        [Description("Destination: reader, editor, study, vocabulary, library, import, shadowing")]
        string destination,
        [Description("Button label")] string label,
        [Description("Short description")] string? description = null)
    {
        return JsonSerializer.Serialize(new
        {
            actionType = "navigate",
            destination = "/" + destination.TrimStart('/'),
            label,
            description
        });
    }

    [KernelFunction, Description("Open the card editor with a pre-filled draft.")]
    public string OpenEditorDraft(
        [Description("Word")] string word,
        [Description("Example sentence")] string? expression = null,
        [Description("Translation")] string? translation = null,
        [Description("Button label")] string? label = null,
        [Description("Short description")] string? description = null)
    {
        return JsonSerializer.Serialize(new
        {
            actionType = "open_editor_draft",
            destination = "/editor",
            label = label ?? "Draft Card",
            description = description ?? "Draft a new card in the editor",
            payload = new
            {
                word,
                expression,
                translation
            }
        });
    }
}
