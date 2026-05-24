using System.Text.RegularExpressions;

namespace AgentService.Helpers;

public static class AgentThreadTitleHelper
{
    public const string DefaultTitle = "New conversation";
    public const int MaxTitleLength = 60;
    public const int MaxMetadataBytes = 32 * 1024;

    private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);

    public static string DeriveTitle(string? userMessageContent)
    {
        if (string.IsNullOrWhiteSpace(userMessageContent))
            return DefaultTitle;

        var normalized = WhitespaceRegex.Replace(userMessageContent.Trim(), " ");
        if (normalized.Length <= MaxTitleLength)
            return normalized;

        return normalized[..(MaxTitleLength - 3)] + "...";
    }

    public static string? NormalizeMetadataJson(string? metadataJson)
    {
        if (string.IsNullOrEmpty(metadataJson))
            return null;

        if (System.Text.Encoding.UTF8.GetByteCount(metadataJson) <= MaxMetadataBytes)
            return metadataJson;

        return metadataJson[..MaxMetadataBytes];
    }

    public static string? BuildUserTextPreview(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return null;

        var normalized = WhitespaceRegex.Replace(content.Trim(), " ");
        return normalized.Length <= 200 ? normalized : normalized[..200];
    }
}
