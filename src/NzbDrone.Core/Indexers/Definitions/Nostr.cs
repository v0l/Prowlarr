using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation.Results;
using MonoTorrent;
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

    public override IndexerCapabilities Capabilities
    {
        get => GetCapabilities();
        protected set { }
    }

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
        var filter = new NostrTorrentFilter()
        {
            Kinds = new[] { (NostrKind)2003 },
            HashTags = new[] { "music" },
            Search = searchCriteria.SearchTerm
        };
        if (searchCriteria.Limit.HasValue)
        {
            filter.Limit = searchCriteria.Limit.Value;
        }

        return FetchFilter(filter);
    }

    public override Task<IndexerPageableQueryResult> Fetch(TvSearchCriteria searchCriteria)
    {
        var filter = new NostrTorrentFilter()
        {
            Kinds = new[] { (NostrKind)2003 },
            HashTags = new[] { "tv" },
            Search = searchCriteria.SearchTerm
        };
        if (searchCriteria.Limit.HasValue)
        {
            filter.Limit = searchCriteria.Limit.Value;
        }

        return FetchFilter(filter);
    }

    public override Task<IndexerPageableQueryResult> Fetch(BookSearchCriteria searchCriteria)
    {
        var filter = new NostrTorrentFilter()
        {
            Kinds = new[] { (NostrKind)2003 },
            HashTags = new[] { "book" },
            Search = searchCriteria.SearchTerm
        };
        if (searchCriteria.Limit.HasValue)
        {
            filter.Limit = searchCriteria.Limit.Value;
        }

        return FetchFilter(filter);
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
        if (link.Scheme == "magnet")
        {
            return Task.FromResult(Encoding.GetBytes(link.AbsoluteUri));
        }

        throw new Exception("Link format not supported");
    }

    public override IndexerCapabilities GetCapabilities()
    {
        var caps = new IndexerCapabilities
        {
            TvSearchParams = new List<TvSearchParam>
            {
                TvSearchParam.Q, TvSearchParam.ImdbId
            },
            MovieSearchParams = new List<MovieSearchParam>
            {
                MovieSearchParam.Q, MovieSearchParam.ImdbId
            }
        };

        caps.Categories.AddCategoryMapping("video,movie", NewznabStandardCategory.Movies, "Movies");
        caps.Categories.AddCategoryMapping("video,movie,dvdr", NewznabStandardCategory.MoviesDVD, "DVDR Movies");
        caps.Categories.AddCategoryMapping("video,movie,4k", NewznabStandardCategory.MoviesUHD, "4K Movies");
        caps.Categories.AddCategoryMapping("video,movie,hd", NewznabStandardCategory.MoviesHD, "HD Movies");

        caps.Categories.AddCategoryMapping("video,tv", NewznabStandardCategory.TV, "TV");
        caps.Categories.AddCategoryMapping("video,tv,4k", NewznabStandardCategory.TVUHD, "4K TV");
        caps.Categories.AddCategoryMapping("video,tv,hd", NewznabStandardCategory.TVHD, "HD TV");

        caps.Categories.AddCategoryMapping("audio", NewznabStandardCategory.Audio, "Audio");
        caps.Categories.AddCategoryMapping("audio,music,flac", NewznabStandardCategory.AudioLossless, "FLAC Audio");
        caps.Categories.AddCategoryMapping("audio,audio-book", NewznabStandardCategory.AudioAudiobook, "Audio Book");

        caps.Categories.AddCategoryMapping("game,pc", NewznabStandardCategory.PCGames, "Games");
        caps.Categories.AddCategoryMapping("game,mac", NewznabStandardCategory.PCMac, "Mac Games");
        caps.Categories.AddCategoryMapping("game,unix", NewznabStandardCategory.PCGames, "Unix Games");
        caps.Categories.AddCategoryMapping("game,ios", NewznabStandardCategory.PCMobileiOS, "iOS Games");
        caps.Categories.AddCategoryMapping("game,android", NewznabStandardCategory.PCMobileiOS, "Android Games");

        caps.Categories.AddCategoryMapping("game,psx", NewznabStandardCategory.Console, "PSx Games");
        caps.Categories.AddCategoryMapping("game,xbox", NewznabStandardCategory.ConsoleXBox, "XBOX Games");
        caps.Categories.AddCategoryMapping("game,wii", NewznabStandardCategory.ConsoleWii, "Wii Games");

        caps.Categories.AddCategoryMapping("porn", NewznabStandardCategory.XXX, "Porn");
        caps.Categories.AddCategoryMapping("porn,movie,dvdr", NewznabStandardCategory.XXXDVD, "DVDR Porn");
        caps.Categories.AddCategoryMapping("porn,movie,hd", NewznabStandardCategory.XXXx264, "HD Porn");
        caps.Categories.AddCategoryMapping("porn,movie,4k", NewznabStandardCategory.XXXUHD, "4K Porn");
        caps.Categories.AddCategoryMapping("porn,picture", NewznabStandardCategory.XXXImageSet, "Porn Images");

        caps.Categories.AddCategoryMapping("other", NewznabStandardCategory.Other, "Other");
        caps.Categories.AddCategoryMapping("other,comic", NewznabStandardCategory.BooksComics, "Comics");
        caps.Categories.AddCategoryMapping("other,e-book", NewznabStandardCategory.BooksEBook, "E-Books");

        return caps;
    }

    protected override Task Test(List<ValidationFailure> failures)
    {
        // nothing to test
        return Task.CompletedTask;
    }

    public override bool SupportsRss => true;
    public override bool SupportsSearch => true;
    public override bool SupportsRedirect => true;
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
            try
            {
                var btih = ev.Tags?.FindFirstTagValue("btih");
                var title = ev.Tags?.FindFirstTagValue("title");
                if (string.IsNullOrEmpty(btih) || string.IsNullOrEmpty(title) || btih.Length != 40)
                {
                    return;
                }

                var files = ev.Tags?.Where(a => a.TagIdentifier == "file").ToArray() ?? Array.Empty<NostrEventTag>();
                var totalSize = files.Sum(a => long.TryParse(a.AdditionalData[1], out var s) ? s : 0);
                var catTags =
                    ev.Tags?.Where(a => a.TagIdentifier == "t").Select(a => a.AdditionalData[0].ToLowerInvariant())
                        .ToArray() ??
                    Array.Empty<string>();
                var cat = Capabilities.Categories.MapTrackerCatToNewznab(string.Join(",", catTags));
                if (cat.Count == 0)
                {
                    cat = Capabilities.Categories.MapTrackerCatToNewznab(string.Join(",", catTags.SkipLast(1)));
                }

                if (cat.Count == 0)
                {
                    cat = Capabilities.Categories.MapTrackerCatToNewznab(string.Join(",", catTags.SkipLast(2)));
                }

                if (cat.Count == 0)
                {
                    cat = Capabilities.Categories.MapTrackerCatToNewznab("other");
                }

                results.Releases.Add(new TorrentInfo()
                {
                    Guid = $"nostr-{ev.Id}",
                    Title = title,
                    Description = ev.Content,
                    Files = files.Length,
                    IndexerId = 1,
                    Categories = cat,
                    Size = totalSize,
                    MagnetUrl = new MagnetLink(InfoHash.FromHex(btih), title, null, null, totalSize).ToV1String(),
                    InfoHash = btih,
                    ImdbId = int.TryParse(ev.Tags?.FindFirstTagValue("imdb"), out var x) ? x : default,
                    PublishDate = ev.CreatedAt!.Value,
                    DownloadProtocol = DownloadProtocol.Torrent
                });
            }
            catch (Exception ex)
            {
                _logger.Warn("Failed to parse event {}", ex.Message);
            }
        });

        await client.Communicator.Start();
        client.Send(req);

        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        cts.Token.Register(() => { tcs.TrySetException(new TimeoutException()); });
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
