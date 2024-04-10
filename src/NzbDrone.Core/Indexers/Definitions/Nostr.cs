using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation.Results;
using Newtonsoft.Json;
using NLog;
using Nostr.Client.Client;
using Nostr.Client.Communicator;
using Nostr.Client.Messages;
using Nostr.Client.Requests;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers.Settings;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Indexers.Definitions;

public class Nostr : IndexerBase<NostrSettings>
{
    public Nostr(IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
        : base(indexerStatusService, configService, logger)
    {
    }

    public override string Name => "nostr";
    public override IndexerCapabilities Capabilities { get; protected set; }
    public override string[] IndexerUrls => new[] { "wss://nos.lol", "wss://relay.nostr.band", "wss://relay.damus.io" };
    public override string[] LegacyUrls => Array.Empty<string>();
    public override string Description => "nostr torrent index";
    public override Encoding Encoding => Encoding.UTF8;
    public override string Language => "en";
    public override DownloadProtocol Protocol => DownloadProtocol.Torrent;
    public override IndexerPrivacy Privacy => IndexerPrivacy.Public;

    public override Task<IndexerPageableQueryResult> Fetch(MovieSearchCriteria searchCriteria)
    {
        var filter = new NostrTorrentFilter()
        {
            Kinds = new[] { (NostrKind)2003 },
            HashTags = new[] { "movie" },
            Search = searchCriteria.SearchTerm
        };
        if (searchCriteria.Limit.HasValue)
        {
            filter.Limit = searchCriteria.Limit.Value;
        }

        return FetchFilter(filter);
    }

    public override Task<IndexerPageableQueryResult> Fetch(MusicSearchCriteria searchCriteria)
    {
        throw new NotImplementedException();
    }

    public override Task<IndexerPageableQueryResult> Fetch(TvSearchCriteria searchCriteria)
    {
        throw new NotImplementedException();
    }

    public override Task<IndexerPageableQueryResult> Fetch(BookSearchCriteria searchCriteria)
    {
        throw new NotImplementedException();
    }

    public override Task<IndexerPageableQueryResult> Fetch(BasicSearchCriteria searchCriteria)
    {
        var filter = new NostrTorrentFilter()
        {
            Kinds = new[] { (NostrKind)2003 },
            Search = searchCriteria.SearchTerm
        };
        if (searchCriteria.Limit.HasValue)
        {
            filter.Limit = searchCriteria.Limit.Value;
        }

        return FetchFilter(filter);
    }

    public override Task<byte[]> Download(Uri link)
    {
        throw new NotImplementedException();
    }

    public override IndexerCapabilities GetCapabilities()
    {
        throw new NotImplementedException();
    }

    protected override Task Test(List<ValidationFailure> failures)
    {
        // nothing to test
        return Task.CompletedTask;
    }

    public override bool SupportsRss => true;
    public override bool SupportsSearch => true;
    public override bool SupportsRedirect => false;
    public override bool SupportsPagination => true;
    public override bool FollowRedirect => false;

    private async Task<IndexerPageableQueryResult> FetchFilter(NostrFilter filter)
    {
        using var client = new NostrWebsocketClient(new NostrWebsocketCommunicator(new Uri(Settings.BaseUrl)), null);
        var id = Guid.NewGuid().ToString();
        var req = new NostrRequest(id, filter);
        var results = new IndexerPageableQueryResult();
        var tcs = new TaskCompletionSource();
        var eoseSub = client.Streams.EoseStream.Subscribe(eoseEvent =>
        {
            if (eoseEvent.Subscription?.Equals(id) ?? false)
            {
                tcs.SetResult();
            }
        });
        var eventsSub = client.Streams.EventStream.Subscribe(subEvent =>
        {
            if (!(subEvent.Subscription?.Equals(id) ?? false) || subEvent.Event == default)
            {
                return;
            }

            var ev = subEvent.Event;
            var files = ev.Tags?.Where(a => a.TagIdentifier == "file").ToArray() ?? Array.Empty<NostrEventTag>();
            results.Releases.Add(new TorrentInfo()
            {
                Guid = $"nostr-{ev.Id}",
                Title = ev.Tags?.FindFirstTagValue("title"),
                Description = ev.Content,
                Files = files.Length,
                Size = files.Sum(a => long.TryParse(a.AdditionalData[1], out var s) ? s : 0),
                InfoHash = ev.Tags?.FindFirstTagValue("btih"),
                ImdbId = int.TryParse(ev.Tags?.FindFirstTagValue("imdb"), out var x) ? x : default,
                PublishDate = ev.CreatedAt!.Value,
                DownloadProtocol = DownloadProtocol.Torrent
            });
        });

        await client.Communicator.Start();
        client.Send(req);

        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        cts.Token.Register(() =>
        {
            tcs.TrySetException(new TimeoutException());
        });
        await tcs.Task;
        eoseSub.Dispose();
        eventsSub.Dispose();
        await client.Communicator.Stop(WebSocketCloseStatus.NormalClosure, string.Empty);

        return results;
    }
}

public class NostrSettings : NoAuthTorrentBaseSettings
{
}

public class NostrTorrentFilter : NostrFilter
{
    [JsonProperty("#t")]
    public string[] HashTags { get; init; }

    [JsonProperty("search")]
    public string Search { get; init; }
}
