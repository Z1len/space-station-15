using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Content.Server.Administration.Managers;
using Content.Server.Administration.Notes;
using Content.Server.Database;
using Content.Server.Discord;
using Content.Server.GameTicking;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Content.Shared.Players.PlayTimeTracking;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Console;


namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Ban)]
public sealed class BanCommand : LocalizedCommands
{

    [Dependency] private readonly IPlayerLocator _locator = default!;
    [Dependency] private readonly IBanManager _bans = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly DiscordWebhook _discord = default!;

    public override string Command => "ban";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        string target;
        string reason;
        uint minutes;
        WebhookIdentifier? webhookIdentifier = null;

        _cfg.OnValueChanged(CCVars.DiscordRoundUpdateWebhook, value =>
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                _discord.GetWebhook(value, data => webhookIdentifier = data.ToIdentifier());
            }
        }, true);

        if (!Enum.TryParse(_cfg.GetCVar(CCVars.ServerBanDefaultSeverity), out NoteSeverity severity))
        {
            Logger.WarningS("admin.server_ban", "Server ban severity could not be parsed from config! Defaulting to high.");
            severity = NoteSeverity.High;
        }

        switch (args.Length)
        {
            case 2:
                target = args[0];
                reason = args[1];
                minutes = 0;
                break;
            case 3:
                target = args[0];
                reason = args[1];

                if (!uint.TryParse(args[2], out minutes))
                {
                    shell.WriteLine(Loc.GetString("cmd-ban-invalid-minutes", ("minutes", args[2])));
                    shell.WriteLine(Help);
                    return;
                }

                break;
            case 4:
                target = args[0];
                reason = args[1];

                if (!uint.TryParse(args[2], out minutes))
                {
                    shell.WriteLine(Loc.GetString("cmd-ban-invalid-minutes", ("minutes", args[2])));
                    shell.WriteLine(Help);
                    return;
                }

                if (!Enum.TryParse(args[3], ignoreCase: true, out severity))
                {
                    shell.WriteLine(Loc.GetString("cmd-ban-invalid-severity", ("severity", args[3])));
                    shell.WriteLine(Help);
                    return;
                }

                break;
            default:
                shell.WriteLine(Loc.GetString("cmd-ban-invalid-arguments"));
                shell.WriteLine(Help);
                return;
        }

        var located = await _locator.LookupIdByNameOrIdAsync(target);
        var player = shell.Player as IPlayerSession;

        if (located == null)
        {
            shell.WriteError(Loc.GetString("cmd-ban-player"));
            return;
        }

        var targetUid = located.UserId;
        var targetHWid = located.LastHWId;

        _bans.CreateServerBan(targetUid, target, player?.UserId, null, targetHWid, minutes, severity, reason);
        var embed = new WebhookEmbed
        {
            Description = Loc.GetString("discord-ban-user", ("user", located.Username), ("admin", player?.Data.UserName ?? "server"), ("reason", reason), ("time", minutes))
        };
        var payload = new WebhookPayload{ Embeds = new() {embed} };

        if(webhookIdentifier == null)
            return;

        await _discord.CreateMessage(webhookIdentifier.Value, payload);
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            var options = _playerManager.ServerSessions.Select(c => c.Name).OrderBy(c => c).ToArray();
            return CompletionResult.FromHintOptions(options, LocalizationManager.GetString("cmd-ban-hint"));
        }

        if (args.Length == 2)
            return CompletionResult.FromHint(LocalizationManager.GetString("cmd-ban-hint-reason"));

        if (args.Length == 3)
        {
            var durations = new CompletionOption[]
            {
                new("0", LocalizationManager.GetString("cmd-ban-hint-duration-1")),
                new("1440", LocalizationManager.GetString("cmd-ban-hint-duration-2")),
                new("4320", LocalizationManager.GetString("cmd-ban-hint-duration-3")),
                new("10080", LocalizationManager.GetString("cmd-ban-hint-duration-4")),
                new("20160", LocalizationManager.GetString("cmd-ban-hint-duration-5")),
                new("43800", LocalizationManager.GetString("cmd-ban-hint-duration-6")),
            };

            return CompletionResult.FromHintOptions(durations, LocalizationManager.GetString("cmd-ban-hint-duration"));
        }

        if (args.Length == 4)
        {
            var severities = new CompletionOption[]
            {
                new("none", Loc.GetString("admin-note-editor-severity-none")),
                new("minor", Loc.GetString("admin-note-editor-severity-low")),
                new("medium", Loc.GetString("admin-note-editor-severity-medium")),
                new("high", Loc.GetString("admin-note-editor-severity-high")),
            };

            return CompletionResult.FromHintOptions(severities, Loc.GetString("cmd-ban-hint-severity"));
        }

        return CompletionResult.Empty;
    }
}
