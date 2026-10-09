namespace NamePlate;

/// <summary>
/// The library's prestige medal (icons/library/prestige-medal.png) at 16x16 for the startup banner:
/// one row per string, six hex digits per pixel, "......" where it's see-through.
/// Made by scripts/make_banner_art.py; regenerate rather than edit by hand.
/// </summary>
internal static class MedalArt
{
    public const int Size = 16;

    public static readonly string[] Rows =
    [
        "................................................................................................",
        "........................0F0F0D685C26AE9431C4A32DC5A32EB196316D602714120E........................",
        "..................3B3318D3B13EE8BD35C2A040CCB472CDB573C1A043E6BB34D8B53F43391A..................",
        "............3B3318EFC142DDAF30B7A164DBE0E5F9FFFFFAFFFFDEE3E9B7A36CDBAD2FF2C444453A1B............",
        "......13110FCFA738DDAC2ED6AF49EDF1F3FDFFFFF3F6F8F3F6F9FCFFFFEEF3F8D7B251DAAA2DD7AD3A1A1610......",
        "......65521FE5B947DBB348D0B97DF1F9FFF4F7F9FAFEFFF8FCFFF4F7FAF4FCFFCFBB86DCB346E4B949705B21......",
        "......A07F28DBB655E2C781A195739BA1A86A6B6BB1B5B8BFC3C66C6D6E8F949BA49A7CDFC37BDBB95CA98629......",
        "......B28E2DD7B14BDCBD6A64593C86898F2C2D2D949699A6AAAC2B2C2C7B7E84665D43D5B665D9B450B9932E......",
        "......AE8A2CD1AC4ADCBF74493C1CAEB2B6B2B5B996999B929597B1B4B7B4B8BD483D20D5B96ED3AF50B58F2C......",
        "......967726CAA84CE4CC8A46360A3932218C8C88888B8E86888B90908D3D37253F2F06E1C886CBAA529F7E26......",
        "......5D4C1CC8A23ED1B059735C1E170F005B584D7A7D807A7C805F5D5319110069531AD2B157C7A24068541E......",
        "......15130FB28E2FBB9329A5842E251C0464625973757971747868665F231B059F7F2CBA9129B8932F1B1711......",
        "............382F17C29B34B89330614D172C281F51504E52524F2D2A225A4715B6912FC59D34413719............",
        "..................382F17AE8B2FBB96317D621D5B46125A4611795F1BB99330B28E303F3518000006............",
        "........................13130E5B4A1E95782BA7862FA8872F96792C5F4D1F171710........................",
        "................................................................................................",
    ];
}
