using Robust.Shared.Configuration;

namespace Content.Shared.RoM.CCVar;

/// <summary>
///     Race of Minds modules console variables
/// </summary>
// ReSharper disable once InconsistentNaming

[CVarDefs]
public sealed class CCVars
{
/*
 * Discord
 */
/// <summary>
/// Discord webhook URL that sends a notification when a user is banned
/// </summary>
    public static readonly CVarDef<string> DiscordBanWebhook =
        CVarDef.Create("discord.discord_ban_webhook", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL);

}
