using System.Globalization;
using System.Xml;
using EternalfestDesktop.Application;
using EternalfestDesktop.Domain;
using Microsoft.Extensions.Logging;

namespace EternalfestDesktop.Infrastructure.Quests;

/// <summary>Reads the <c>&lt;items&gt;</c> of a contrée's cached content XML.</summary>
public sealed partial class XmlContreeItems(GameStore store, ILogger<XmlContreeItems> logger) : ContreeItems
{
    /// @spec profile::complete-inventory
    /// @spec profile::unreadable-content
    public async Task<IReadOnlyList<int>> ListedIn(GameBuild build, CancellationToken cancellationToken)
    {
        if (build.Content is not { } content)
            return [];
        try
        {
            await using var stream = store.OpenBlob(content.Id);
            using var xml = XmlReader.Create(stream, new XmlReaderSettings { Async = true, DtdProcessing = DtdProcessing.Prohibit });
            var items = new HashSet<int>();
            var insideItems = 0;
            while (await xml.ReadAsync())
            {
                if (xml.NodeType == XmlNodeType.EndElement && xml.LocalName == "items")
                    insideItems--;
                if (xml.NodeType != XmlNodeType.Element)
                    continue;
                if (xml.LocalName == "items" && !xml.IsEmptyElement)
                    insideItems++;
                else if (xml.LocalName == "item" && insideItems > 0 && int.TryParse(xml.GetAttribute("id"), CultureInfo.InvariantCulture, out var id))
                    items.Add(id);
            }
            return items.ToList();
        }
        catch (Exception exception) when (exception is IOException or XmlException)
        {
            LogUnreadableContent(content.Id, exception);
            return [];
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Can't list the items of content {Content}: the complete profile only gets the items its quests require")]
    private partial void LogUnreadableContent(BlobId content, Exception exception);
}
