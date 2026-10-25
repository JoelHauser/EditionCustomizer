using System.Text.Json;
using System.Text.Json.Serialization;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Utils;

namespace NamePlate;

/// <summary>Your look, from the plugin: the icons.json entry you show, or null for the game's own.</summary>
public record SetLookRequest : IRequestData
{
    [JsonPropertyName("look")] public JsonElement? Look { get; init; }

    /// <summary>The icon PNG, base64, when it isn't one every install has (the library, a game icon).</summary>
    [JsonPropertyName("picture")] public string? Picture { get; init; }
}

public record LooksRequest : IRequestData;

public record PictureRequest : IRequestData
{
    [JsonPropertyName("hash")] public string? Hash { get; init; }
}

public record LookReply([property: JsonPropertyName("message")] string Message);

public record LooksReply([property: JsonPropertyName("looks")] List<LooksReply.Entry> Looks)
{
    public record Entry(
        [property: JsonPropertyName("profileId")] string ProfileId,
        [property: JsonPropertyName("nickname")] string Nickname,
        [property: JsonPropertyName("look")] JsonElement Look,
        [property: JsonPropertyName("picture")] string? Picture);
}

public record PictureReply([property: JsonPropertyName("picture")] string? Picture);

/// <summary>
/// The three routes the plugin uses to share looks between players on this server: set your own,
/// get everyone's, and fetch a picture by its hash (pictures are sent once, then cached).
/// </summary>
[Injectable(TypePriority = OnLoadOrder.Routers)]
public class LookRouter(JsonUtil jsonUtil, SharedLooks looks, HttpResponseUtil httpResponseUtil)
    : StaticRouter(
        jsonUtil,
        [
            new RouteAction<SetLookRequest>(
                "/nameplate/look",
                (url, info, sessionId, output, cancellationToken) =>
                    new ValueTask<string>(httpResponseUtil.NoBody(new LookReply(looks.Set(sessionId, info.Look, info.Picture))))),

            new RouteAction<LooksRequest>(
                "/nameplate/looks",
                (url, info, sessionId, output, cancellationToken) =>
                    new ValueTask<string>(httpResponseUtil.NoBody(new LooksReply(
                        looks.All().Select(l => new LooksReply.Entry(l.ProfileId, l.Look.Nickname, l.Look.Look, l.Look.PictureHash)).ToList())))),

            new RouteAction<PictureRequest>(
                "/nameplate/picture",
                (url, info, sessionId, output, cancellationToken) =>
                    new ValueTask<string>(httpResponseUtil.NoBody(new PictureReply(looks.Picture(info.Hash))))),
        ])
{
}
