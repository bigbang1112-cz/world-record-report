using BigBang1112.Data;
using BigBang1112.Models;
using BigBang1112.Models.Db;
using BigBang1112.Models.States;
using BigBang1112.Services;
using BigBang1112.WorldRecordReport.Data;
using BigBang1112.WorldRecordReport.Exceptions;
using BigBang1112.WorldRecordReport.Jobs;
using BigBang1112.WorldRecordReport.Models;
using Mapster;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using OneOf;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace BigBang1112.WorldRecordReport.Services;

public class LeaderboardsManialinkService : ILeaderboardsManialinkService
{
    private const int SeparationBetween = 50000;

    private readonly IFileHostService _fileHost;
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _config;
    private readonly IAccountsRepo _accountsRepo;
    private readonly IWrRepo _wrRepo;
    private readonly IWorldRecordTM2Service _worldRecordTM2Service;

    public LeaderboardsManialinkService(IFileHostService fileHost, IMemoryCache cache, IConfiguration config,
        IAccountsRepo accountsRepo, IWrRepo wrRepo, IWorldRecordTM2Service worldRecordTM2Service)
    {
        _fileHost = fileHost;
        _cache = cache;
        _config = config;
        _accountsRepo = accountsRepo;
        _wrRepo = wrRepo;
        _worldRecordTM2Service = worldRecordTM2Service;
    }

    public async Task<string> GetManialinkAsync(HttpContext httpContext, CancellationToken cancellationToken)
    {
        var xmlFile = _fileHost.GetFilePath("Data/LbManialink", "Leaderboards.xml");
        var lastWriteTime = File.GetLastWriteTimeUtc(xmlFile);

        httpContext.Response.Headers.AddLastModified(lastWriteTime);

        if (_cache.TryGetValue(CacheKeys.LeaderboardsManialinkLastModified, out DateTime prevDateTime)
            && lastWriteTime <= prevDateTime)
        {
            return _cache.Get<string>(CacheKeys.LeaderboardsManialink);
        }

        var scriptFile = _fileHost.GetFilePath("Data/LbManialink", "Leaderboards.Script.txt");

        var contentXmlTask = File.ReadAllTextAsync(xmlFile, Encoding.UTF8, cancellationToken);
        var contentScriptTask = File.ReadAllTextAsync(scriptFile, Encoding.UTF8, cancellationToken);
        var contentScript = await contentScriptTask;
        var contentXml = await contentXmlTask;

        contentXml = contentXml.Replace("ScoreMgr", "ParentApp.ScoreMgr");
        contentXml = contentXml.Replace("////", "");

        contentXml = Regex.Replace(contentXml, "persistent(.*?)for localuser", "persistent$1for Page", RegexOptions.IgnoreCase);

        var contentXmlWithAppendsBuilder = new StringBuilder(contentXml);

        contentXmlWithAppendsBuilder.Replace("{AuthClientId}", _config["OAuth2:ManiaPlanet:Id"]);
        contentXmlWithAppendsBuilder.Replace("{Host}", $"https://{httpContext.Request.Host.Host}");

        for (var i = SeparationBetween; i < contentXml.Length; i += SeparationBetween)
        {
            contentXmlWithAppendsBuilder.Insert(i, "\"\"\"^\"\"\"");
        }

        var content = contentScript.Replace("{0}", contentXmlWithAppendsBuilder.ToString());

        _cache.Set(CacheKeys.LeaderboardsManialink, content);
        _cache.Set(CacheKeys.LeaderboardsManialinkLastModified, lastWriteTime);

        return content;
    }

    public void Head(IHeaderDictionary headers)
    {
        var xmlFile = _fileHost.GetFilePath("Data/LbManialink", "Leaderboards.xml");
        var xmlFileInfo = new FileInfo(xmlFile);

        headers.AddLastModified(xmlFileInfo.LastWriteTimeUtc);
    }

    public async Task<OneOf<LbManialinkWorldRecordsResponse, AccountUnauthorized, AccountForbidden>>
        PostWorldRecordsAsync(HttpContext httpContext, LbManialinkMap lbManialinkMap)
    {
        if (!httpContext.Request.Headers.TryGetValue("X-ManiaPlanet-Token", out StringValues stringToken))
            return new AccountUnauthorized();

        var token = await DecryptAsync(stringToken.ToString());

        var mpAuth = await _accountsRepo.GetManiaPlanetAuthByAccessTokenAsync(token);

        if (mpAuth is null)
            return new AccountUnauthorized();

        if (!mpAuth.LbManialink.IsIWRUP)
            return new AccountForbidden();

        // can update wrs
        var leaderboard = lbManialinkMap.Adapt<Leaderboard>();

        await _worldRecordTM2Service.HandleLeaderboardAsync(lbManialinkMap.MapUid, leaderboard,
            isManialinkReport: true);

        return new LbManialinkWorldRecordsResponse();
    }

    public async Task<string> AuthorizeAsync(string redirectUri, string code, string state)
    {
        using var http = new HttpClient();

        var formContent = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "grant_type", "authorization_code" },
            { "client_id", _config["OAuth2:ManiaPlanet:Id"] },
            { "client_secret", _config["OAuth2:ManiaPlanet:Secret"] },
            { "code", code },
            { "redirect_uri", redirectUri }
        });

        using var accessTokenResponse = await http.PostAsync("https://prod.live.maniaplanet.com/login/oauth2/access_token", formContent);
        accessTokenResponse.EnsureSuccessStatusCode();
        var authResponse = await accessTokenResponse.Content.ReadFromJsonAsync<ManiaPlanetAuthResponse>();

        if (authResponse is null)
        {
            throw new Exception();
        }

        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResponse.AccessToken);

        using var meResponse = await http.GetAsync("https://prod.live.maniaplanet.com/webservices/me");
        meResponse.EnsureSuccessStatusCode();
        var me = await meResponse.Content.ReadFromJsonAsync<ManiaPlanetPlayer>();

        if (me is null)
        {
            throw new Exception();
        }

        var mpAuth = await _accountsRepo.GetManiaPlanetAuthByLoginAsync(me.Login);

        if (mpAuth is null)
        {
            mpAuth = new ManiaPlanetAuthModel
            {
                Login = me.Login
            };

            await _accountsRepo.AddManiaPlanetAuthAsync(mpAuth);

            var account = new AccountModel
            {
                Guid = Guid.NewGuid(),
                CreatedOn = DateTime.UtcNow,
                LastSeenOn = DateTime.UtcNow,
                ManiaPlanet = mpAuth
            };

            await _accountsRepo.AddAccountAsync(account);
        }

        mpAuth.Nickname = me.Nickname;
        mpAuth.RequestedOn = DateTime.UtcNow;
        mpAuth.AccessToken = authResponse.AccessToken;
        mpAuth.RefreshToken = authResponse.RefreshToken;
        mpAuth.ExpiresOn = DateTime.UtcNow + TimeSpan.FromSeconds(authResponse.ExpiresIn);

        var zoneModel = await _accountsRepo.GetOrAddZoneAsync(me.Zone);

        zoneModel.IsMP = true;

        mpAuth.Zone = zoneModel;

        if (mpAuth.LbManialink is null)
        {
            mpAuth.LbManialink = new ManiaPlanetLbManialinkModel
            {
                JoinedOn = DateTime.UtcNow,
                SecretKey = GenerateSecretKey()
            };
        }

        mpAuth.LbManialink.LastVisitedOn = DateTime.UtcNow;
        mpAuth.LbManialink.Visits++;

        await _accountsRepo.SaveAsync();

        var authManialink = await _cache.GetOrCreateAsync(CacheKeys.LeaderboardsManialinkAuth, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);

            var xmlFile = _fileHost.GetFilePath("Data/LbManialink", "LeaderboardsAuth.xml");
            return await File.ReadAllTextAsync(xmlFile, Encoding.UTF8);
        });

        var authManialinkBuilder = new StringBuilder(authManialink);
        authManialinkBuilder.Replace("{AccessToken}", await EncryptAsync(authResponse.AccessToken));
        authManialinkBuilder.Replace("{SecretKey}", mpAuth.LbManialink.SecretKey);

        return authManialinkBuilder.ToString();
    }

    public async Task<LbManialinkMember> GetMemberAsync(string login, CancellationToken cancellationToken)
    {
        var mpAuth = await _accountsRepo.GetManiaPlanetAuthByLoginAsync(login, cancellationToken);

        if (mpAuth is null)
        {
            return new LbManialinkMember
            {
                Login = login
            };
        }

        if (mpAuth.LbManialink is null)
        {
            return new LbManialinkMember
            {
                Login = mpAuth.Login,
                Nickname = mpAuth.Nickname,
                Zone = mpAuth.Zone.Name
            };
        }

        return new LbManialinkMember
        {
            Login = mpAuth.Login,
            Nickname = mpAuth.Nickname,
            Zone = mpAuth.Zone.Name,
            Visits = mpAuth.LbManialink.Visits,
            Joined = mpAuth.LbManialink.JoinedOn.ToUnix().ToString(),
            LastVisited = mpAuth.LbManialink.LastVisitedOn.ToUnix().ToString(),
            IsIWRUP = mpAuth.LbManialink.IsIWRUP
        };
    }

    public async Task<IEnumerable<LbManialinkMember>> GetMembersAsync(CancellationToken cancellationToken)
    {
        var members = await _accountsRepo.GetLbManialinkMembersAsync(cancellationToken);

        return members.Select(x => new LbManialinkMember
        {
            Login = x.Auth.Login,
            Nickname = x.Auth.Nickname,
            Visits = x.Visits,
            Zone = x.Auth.Zone.Name,
            Joined = x.JoinedOn.ToUnix().ToString(),
            LastVisited = x.LastVisitedOn.ToUnix().ToString()
        });
    }

    public async Task<IEnumerable<LbManialinkReport>> GetReportsAsync(string titleUid, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(titleUid))
            return Enumerable.Empty<LbManialinkReport>();

        var titleUidSplit = titleUid.Split('@');
        var titleId = titleUidSplit[0];
        var titleAuthor = titleUidSplit[1];

        var reports = await _wrRepo.GetReportsFromTitlePackAsync(titleId, titleAuthor, count: 12);

        return reports.Where(x => x.Player is not null).Select(x =>
        {
            if (x.Player is null)
            {
                throw new ThisShouldNotHappenException();
            }

            var report = new LbManialinkReport
            {
                MapName = x.Map.Name,
                MapUid = x.Map.MapUid,
                Time = x.Time,
                Login = x.Player.Name,
                Nickname = string.IsNullOrWhiteSpace(x.Player.Nickname) ? x.Player.Name : x.Player.Nickname,
                Timestamp = (int)x.DrivenOn.ToUnix()
            };

            if (x.PreviousWorldRecord is not null && !x.PreviousWorldRecord.Ignored)
            {
                if (x.PreviousWorldRecord.Player is null)
                {
                    throw new ThisShouldNotHappenException();
                }

                report.FormerTime = x.PreviousWorldRecord.Time;
                report.FormerLogin = x.PreviousWorldRecord.Player.Name;
                report.FormerNickname = x.PreviousWorldRecord.Player.Nickname;
                report.FormerTimestamp = (int)x.PreviousWorldRecord.DrivenOn.ToUnix();
            }

            return report;
        });
    }

    private static string GenerateSecretKey(int length = 16)
    {
        var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var stringChars = new char[length];
        var random = new Random();

        for (int i = 0; i < stringChars.Length; i++)
        {
            stringChars[i] = chars[random.Next(chars.Length)];
        }

        return new string(stringChars);
    }

    private async Task<string> EncryptAsync(string str)
    {
        var aes = Aes.Create();
        using var ms = new MemoryStream();
        using var cs = new CryptoStream(ms, aes.CreateEncryptor(
            Encoding.ASCII.GetBytes(_config["LbManialinkTokenEncryptionKey"]),
            Encoding.ASCII.GetBytes(_config["LbManialinkTokenEncryptionIV"])),
            CryptoStreamMode.Write);
        await cs.WriteAsync(Encoding.ASCII.GetBytes(str));
        await cs.FlushFinalBlockAsync();
        return Convert.ToBase64String(ms.ToArray());
    }

    private async Task<string> DecryptAsync(string str)
    {
        Aes aes = Aes.Create();
        using var ms = new MemoryStream(Convert.FromBase64String(str));
        using var cs = new CryptoStream(ms, aes.CreateDecryptor(
            Encoding.ASCII.GetBytes(_config["LbManialinkTokenEncryptionKey"]),
            Encoding.ASCII.GetBytes(_config["LbManialinkTokenEncryptionIV"])),
            CryptoStreamMode.Read);
        using var reader = new StreamReader(cs);
        return await reader.ReadToEndAsync();
    }
}
