using CheapHelpers.Networking.Core;
using Microsoft.Extensions.Logging;
using System.Net.Sockets;

namespace CheapHelpers.Networking.Detection;

/// <summary>
/// Detects game servers on the network via TCP connect and UDP knock on known game ports.
/// Priority 35 — runs after SSH but before Windows services, since game detection
/// is a fun gimmick rather than a primary identification method.
/// </summary>
public class GameServerDetector(ILogger<GameServerDetector> logger, PortDetectionOptions portOptions) : IDeviceTypeDetector
{
    public int Priority => 35;

    /// <summary>
    /// Well-known game server ports. TCP ports use connect probing, UDP ports use a null-byte knock.
    /// Multiple ports per game listed in order of likelihood.
    /// Ordered by specificity — unique ports first, shared engine ports last to avoid false positives.
    /// </summary>
    private static readonly GameSignature[] KnownGames =
    [
        // ── Unique ports (high confidence, no ambiguity) ─────────────────────

        // Minecraft Java Edition
        new("Minecraft", [25565], []),
        // Minecraft Bedrock Edition
        new("Minecraft Bedrock", [], [19132, 19133]),
        // Factorio
        new("Factorio", [], [34197]),
        // Terraria
        new("Terraria", [7777], []),
        // Valheim
        new("Valheim", [], [2456, 2457]),
        // Rust
        new("Rust", [], [28015]),
        // 7 Days to Die
        new("7 Days to Die", [26900], [26900, 26901]),
        // Starbound
        new("Starbound", [21025], []),
        // Don't Starve Together
        new("Don't Starve Together", [], [10999]),
        // V Rising
        new("V Rising", [], [9876, 9877]),
        // Project Zomboid
        new("Project Zomboid", [16261], [16261]),
        // Palworld
        new("Palworld", [], [8211]),
        // Enshrouded
        new("Enshrouded", [], [15636, 15637]),
        // Among Us
        new("Among Us", [], [22023]),
        // Astroneer
        new("Astroneer", [], [8777]),

        // ── Classic id Tech / Quake engine ───────────────────────────────────

        // Quake (original)
        new("Quake", [], [26000]),
        // QuakeWorld
        new("QuakeWorld", [], [27500]),
        // Quake II
        new("Quake II", [], [27910]),
        // Quake III Arena / OpenArena / RTCW / Wolfenstein: ET
        new("Quake III / id Tech 3", [], [27960]),
        // Quake 4
        new("Quake 4", [], [28004]),
        // Doom (Zandronum / Skulltag source ports)
        new("Doom", [], [10666]),

        // ── Call of Duty (id Tech 3 derivative) ──────────────────────────────

        // CoD 1, CoD 2, CoD 4: MW, CoD: WaW
        new("Call of Duty", [], [28960]),

        // ── Blizzard / Battle.net classics ───────────────────────────────────

        // StarCraft / Brood War, Warcraft III, Diablo II
        new("Blizzard Classic", [6112], [6112]),
        // Diablo II (game server port)
        new("Diablo II", [4000], []),
        // WoW Private Server (TrinityCore / AzerothCore / MaNGOS)
        new("WoW Private Server", [8085, 3724], []),

        // ── Bohemia Interactive (Arma engine) ────────────────────────────────

        // Arma 2, Arma 3, DayZ
        new("Arma / DayZ", [], [2302, 2303]),

        // ── Survival / crafting (unique ports) ───────────────────────────────

        // Satisfactory
        new("Satisfactory", [], [15000, 15777]),
        // Eco
        new("Eco", [3000, 3001], []),
        // Vintage Story
        new("Vintage Story", [42420], []),
        // Empyrion: Galactic Survival
        new("Empyrion", [30000], [30000]),
        // Colony Survival
        new("Colony Survival", [27016], [27016]),
        // Rising World
        new("Rising World", [4255], [4255]),
        // Stardew Valley (co-op via SMAPI)
        new("Stardew Valley", [24642], []),
        // Space Engineers
        new("Space Engineers", [], [27016]),

        // ── Racing / sim ─────────────────────────────────────────────────────

        // Assetto Corsa
        new("Assetto Corsa", [9600], [9600]),
        // Assetto Corsa Competizione
        new("Assetto Corsa Competizione", [], [9231]),
        // TrackMania / ManiaPlanet
        new("TrackMania", [2350], [2350]),
        // rFactor 2
        new("rFactor 2", [], [64297]),
        // Live for Speed
        new("Live for Speed", [63392], [63392]),
        // BeamNG.drive (BeamMP mod)
        new("BeamNG.drive", [30814], []),
        // Wreckfest
        new("Wreckfest", [], [33540]),

        // ── GTA multiplayer mods ─────────────────────────────────────────────

        // FiveM (GTA V)
        new("FiveM", [30120], [30120]),
        // Multi Theft Auto (GTA:SA)
        new("Multi Theft Auto", [22003], [22003]),
        // SA-MP (San Andreas Multiplayer)
        new("SA-MP", [], [7777]),

        // ── Tactical / military FPS ──────────────────────────────────────────

        // Squad
        new("Squad", [], [7787]),
        // SWAT 4
        new("SWAT 4", [10480], [10481]),
        // Killing Floor
        new("Killing Floor", [], [7707]),
        // Medal of Honor: Allied Assault
        new("Medal of Honor: AA", [], [12203]),
        // Serious Sam (First/Second Encounter, HD)
        new("Serious Sam", [], [25600]),

        // ── RPG / adventure servers ──────────────────────────────────────────

        // Neverwinter Nights 1 & 2
        new("Neverwinter Nights", [5121], [5121]),
        // Ragnarok Online (rAthena / Hercules)
        new("Ragnarok Online", [6900, 6121, 5121], []),
        // Lineage II (L2J)
        new("Lineage II", [2106, 7777], []),

        // ── Simulation / transport ───────────────────────────────────────────

        // OpenTTD
        new("OpenTTD", [3979], [3979]),
        // KSP (Luna Multiplayer mod)
        new("Kerbal Space Program", [8800], [8800]),

        // ── Modern survival/co-op (dedicated server) ─────────────────────────

        // Abiotic Factor
        new("Abiotic Factor", [], [7777]),
        // Soulmask
        new("Soulmask", [], [7777]),
        // SCP: Secret Laboratory
        new("SCP: Secret Laboratory", [], [7777]),
        // Hell Let Loose
        new("Hell Let Loose", [], [7777]),
        // Risk of Rain 2
        new("Risk of Rain 2", [], [27015]),
        // Barotrauma
        new("Barotrauma", [], [27015]),
        // The Forest
        new("The Forest", [], [27015]),
        // Insurgency: Sandstorm
        new("Insurgency: Sandstorm", [], [27102]),

        // ── Age of Empires ───────────────────────────────────────────────────

        // Age of Empires II (original / Voobly, DirectPlay)
        new("Age of Empires II", [], [2300]),

        // ── Battlefield ──────────────────────────────────────────────────────

        // Battlefield 1942
        new("Battlefield 1942", [], [14567]),
        // Battlefield 2 / 2142
        new("Battlefield 2", [], [16567]),

        // ── Shared engine ports (last — catch-all, lower specificity) ────────

        // Valve Source Engine: CS2, CS:GO, CS:S, TF2, L4D2, Garry's Mod, HL2:DM, DoD:S, Unturned
        new("Source Engine", [], [27015]),
        // Unreal Engine (generic): UT99, UT2004, UT3, Killing Floor 2, ARK, Conan Exiles, Ground Branch
        new("Unreal Engine", [], [7777]),
    ];

    /// <summary>
    /// Max ports to probe per game to avoid hammering the target — inspired by LigoLAN's approach.
    /// </summary>
    private const int MaxProbesPerGame = 2;

    public async Task<string?> DetectDeviceTypeAsync(string ipAddress)
    {
        try
        {
            var matchedGames = new List<string>();

            foreach (var game in KnownGames)
            {
                if (await ProbeGameAsync(ipAddress, game))
                {
                    matchedGames.Add(game.Name);

                    // Cap at 3 detected games — if a host has more, they know what they're running
                    if (matchedGames.Count >= 3) break;
                }
            }

            if (matchedGames.Count == 0)
                return null;

            var gamesLabel = string.Join(", ", matchedGames);
            var detectedType = $"Game Server ({gamesLabel})";

            logger.LogInformation("Game server detected at {IpAddress}: {Games}", ipAddress, gamesLabel);
            return detectedType;
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Game server detection failed for {IpAddress}", ipAddress);
            return null;
        }
    }

    private async Task<bool> ProbeGameAsync(string ipAddress, GameSignature game)
    {
        var probeCount = 0;

        // TCP connect probe — most reliable
        foreach (var tcpPort in game.TcpPorts)
        {
            if (probeCount >= MaxProbesPerGame) break;
            probeCount++;

            if (await IsTcpPortOpenAsync(ipAddress, tcpPort))
                return true;
        }

        // UDP knock — send a null byte and see if anything comes back
        foreach (var udpPort in game.UdpPorts)
        {
            if (probeCount >= MaxProbesPerGame) break;
            probeCount++;

            if (await IsUdpRespondingAsync(ipAddress, udpPort))
                return true;
        }

        return false;
    }

    private async Task<bool> IsTcpPortOpenAsync(string ipAddress, int port)
    {
        try
        {
            using var client = new TcpClient();
            var connectTask = client.ConnectAsync(ipAddress, port);

            if (await Task.WhenAny(connectTask, Task.Delay(portOptions.PortConnectionTimeoutMs)) != connectTask)
                return false;

            return client.Connected;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> IsUdpRespondingAsync(string ipAddress, int port)
    {
        try
        {
            using var udpClient = new UdpClient();
            udpClient.Client.ReceiveTimeout = portOptions.PortConnectionTimeoutMs;
            udpClient.Connect(ipAddress, port);

            // Null-byte knock — minimal probe to see if anything is listening
            await udpClient.SendAsync([0x00], 1);

            var receiveTask = udpClient.ReceiveAsync();
            if (await Task.WhenAny(receiveTask, Task.Delay(portOptions.PortConnectionTimeoutMs)) != receiveTask)
                return false;

            // Any response at all means something is listening
            return true;
        }
        catch
        {
            return false;
        }
    }

    private readonly record struct GameSignature(string Name, int[] TcpPorts, int[] UdpPorts);
}
