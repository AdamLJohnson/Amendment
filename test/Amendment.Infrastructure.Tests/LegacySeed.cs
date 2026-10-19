using System.Security.Cryptography;
using System.Text;
using Amendment.Domain.Languages;
using Amendment.Model.DataModel;
using Amendment.Repository;
using Amendment.Repository.Infrastructure;
using Amendment.SeedDynamoDb;
using Amendment.Server;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using LegacyAmendment = Amendment.Model.DataModel.Amendment;

namespace Amendment.Infrastructure.Tests;

/// <summary>Runs the real legacy seed (EF <c>HasData</c> + <c>SeedDatabase</c>) against an in-memory database.</summary>
public sealed record LegacySeed(List<Language> Languages, List<SystemSetting> Settings, List<LegacyAmendment> Amendments)
{
    public static readonly DateTimeOffset SeededAt = new(2018, 1, 1, 0, 0, 0, TimeSpan.Zero);
    public const string SeededBy = "system";

    public static async Task<LegacySeed> RunAsync()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var dbName = Guid.NewGuid().ToString("N");
        services.AddDbContext<AmendmentContext>(o => o
            .UseInMemoryDatabase(dbName)
            .ConfigureWarnings(b => b.Ignore(InMemoryEventId.TransactionIgnoredWarning)));
        services.AddScoped<IDbFactory, DbFactory>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped(typeof(IRepository<>), typeof(GenericRepository<>));
        await using var provider = services.BuildServiceProvider();

        using (var scope = provider.CreateScope())
            await scope.ServiceProvider.GetRequiredService<AmendmentContext>().Database.EnsureCreatedAsync();
        await new SeedDatabase(provider).Seed();

        using var read = provider.CreateScope();
        var db = read.ServiceProvider.GetRequiredService<AmendmentContext>();
        return new LegacySeed(
            await db.Set<Language>().OrderBy(l => l.Id).ToListAsync(),
            await db.Set<SystemSetting>().OrderBy(s => s.Id).ToListAsync(),
            await db.Set<LegacyAmendment>()
                .Include(a => a.PrimaryLanguage)
                .Include(a => a.AmendmentBodies).ThenInclude(b => b.Language)
                .OrderBy(a => a.Id).ToListAsync());
    }

    public static string Code(Language language) => LanguageCode.Parse(language.LanguageCode).Value;

    /// <summary>Builds the fixture from this legacy run. Ids are ULIDs with fixed timestamps and randomness.</summary>
    public SeedFixture ToFixture()
    {
        var amendments = Amendments.Select((a, index) =>
        {
            var createdAt = SeededAt.AddSeconds(index);
            var randomness = SHA256.HashData(Encoding.UTF8.GetBytes(a.Title))[..10];
            return new SeedAmendment
            {
                Id = Ulid.NewUlid(createdAt, randomness).ToString(),
                LegacyId = a.Id,
                CreatedAt = createdAt,
                Title = a.Title,
                Author = a.Author,
                Motion = a.Motion,
                LegisId = a.LegisId,
                Source = a.Source,
                PrimaryLanguage = Code(a.PrimaryLanguage),
                Bodies = a.AmendmentBodies.OrderBy(b => b.LanguageId)
                    .Select(b => new SeedBody(Code(b.Language), b.AmendStatus.ToString(), b.Pages, b.AmendBody.ReplaceLineEndings("\n"))).ToList()
            };
        }).ToList();

        var live = Amendments.Single(a => a.IsLive);
        return new SeedFixture
        {
            SeededAt = SeededAt,
            SeededBy = SeededBy,
            Languages = Languages.Select(l => new SeedLanguage(Code(l), l.LanguageName, l.LanguageCode, l.Id)).ToList(),
            Settings = Settings.Select(s => new SeedSetting(s.Key, s.Value, s.Id)).ToList(),
            Amendments = amendments,
            Presentation = new SeedPresentation(
                amendments[Amendments.IndexOf(live)].Id,
                live.AmendmentBodies.OrderBy(b => b.LanguageId)
                    .ToDictionary(b => Code(b.Language), b => new SeedLanguageView(b.IsLive, b.Page)))
        };
    }

    public static string RepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Amendment.sln")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new DirectoryNotFoundException("Amendment.sln not found above the test output directory.");
    }

    public static string FixtureSourcePath => Path.Combine(RepositoryRoot(), "scripts", "seed-dynamodb", SeedFixture.FileName);
}
