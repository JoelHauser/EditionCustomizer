using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Utils;

namespace NamePlate;

/// <summary>Your Name Plate, from the plugin: the icons.json entry you show, or null to stop sharing.</summary>
public record ShareRequest : IRequestData
{
    [JsonPropertyName("nameplate")] public JsonElement? NamePlate { get; init; }

    /// <summary>The icon PNG, base64, when it isn't one every install has (the library, a game icon).</summary>
    [JsonPropertyName("picture")] public string? Picture { get; init; }
}

public record SharedRequest : IRequestData;

public record PictureRequest : IRequestData
{
    [JsonPropertyName("hash")] public string? Hash { get; init; }
}

public record ShareReply([property: JsonPropertyName("message")] string Message);

public record SharedReply([property: JsonPropertyName("nameplates")] List<SharedReply.Entry> NamePlates)
{
    public record Entry(
        [property: JsonPropertyName("profileId")] string ProfileId,
        [property: JsonPropertyName("nickname")] string Nickname,
        [property: JsonPropertyName("nameplate")] JsonElement NamePlate,
        [property: JsonPropertyName("picture")] string? Picture);
}

public record PictureReply([property: JsonPropertyName("picture")] string? Picture);

public record MineRequest : IRequestData;

/// <summary>The Name Plate made on the host for the icon you show, or none (with why).</summary>
public record MineReply(
    [property: JsonPropertyName("category")] int Category,
    [property: JsonPropertyName("nameplate")] JsonNode? NamePlate,
    [property: JsonPropertyName("picture")] string? Picture,
    [property: JsonPropertyName("why")] string Why);

/// <summary>
/// Name Plate sharing's routes: share your own, get everyone's, fetch a picture by its hash
/// (pictures are sent once, then cached), and get the Name Plate made on the host for you.
/// </summary>
[Injectable(TypePriority = OnLoadOrder.Routers)]
public class ShareRouter(JsonUtil jsonUtil, SharedNamePlates shared, HostNamePlates host, HttpResponseUtil httpResponseUtil)
    : StaticRouter(
        jsonUtil,
        [
            new RouteAction<ShareRequest>(
                "/nameplate/share",
                (url, info, sessionId, output, cancellationToken) =>
                    new ValueTask<string>(httpResponseUtil.NoBody(new ShareReply(shared.Set(sessionId, info.NamePlate, info.Picture))))),

            new RouteAction<SharedRequest>(
                "/nameplate/shared",
                (url, info, sessionId, output, cancellationToken) =>
                    new ValueTask<string>(httpResponseUtil.NoBody(new SharedReply(
                        shared.All().Select(s => new SharedReply.Entry(s.ProfileId, s.Shared.Nickname, s.Shared.NamePlate, s.Shared.PictureHash)).ToList())))),

            new RouteAction<PictureRequest>(
                "/nameplate/picture",
                (url, info, sessionId, output, cancellationToken) =>
                    new ValueTask<string>(httpResponseUtil.NoBody(new PictureReply(shared.Picture(info.Hash))))),

            new RouteAction<MineRequest>(
                "/nameplate/mine",
                (url, info, sessionId, output, cancellationToken) =>
                {
                    var answer = host.For(sessionId);
                    return new ValueTask<string>(httpResponseUtil.NoBody(new MineReply(answer.Category, answer.NamePlate, answer.Picture, answer.Why)));
                }),
        ])
{
}
