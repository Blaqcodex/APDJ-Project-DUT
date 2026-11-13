using CampusPulse.Infrastructure.Configuration;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CampusPulse.Infrastructure.Firebase;

public sealed class FirestoreContext : IFirestoreContext, IDisposable
{
    private readonly FirebaseOptions _options;
    private readonly ILogger<FirestoreContext> _logger;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private FirestoreDb? _firestoreDb;

    public FirestoreContext(
        IOptions<FirebaseOptions> options,
        ILogger<FirestoreContext> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public bool IsFirestoreConfigured =>
        !string.IsNullOrWhiteSpace(_options.ProjectId);

    public bool UseLocalFallback =>
        !IsFirestoreConfigured && _options.EnableLocalFallback;

    public string ProjectId => _options.ProjectId.Trim();

    public string StorageBucket => _options.StorageBucket.Trim();

    public async Task<FirestoreDb?> GetDatabaseAsync(CancellationToken cancellationToken = default)
    {
        if (!IsFirestoreConfigured)
        {
            if (_options.EnableLocalFallback)
            {
                return null;
            }

            throw new InvalidOperationException(
                "Firebase:ProjectId is not configured and local fallback is disabled.");
        }

        if (_firestoreDb is not null)
        {
            return _firestoreDb;
        }

        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_firestoreDb is not null)
            {
                return _firestoreDb;
            }

            var builder = new FirestoreDbBuilder
            {
                ProjectId = ProjectId
            };

            if (!string.IsNullOrWhiteSpace(_options.ServiceAccountPath))
            {
                string resolvedPath = ResolveCredentialsPath(_options.ServiceAccountPath.Trim());
                if (!File.Exists(resolvedPath))
                {
                    throw new FileNotFoundException(
                        $"Firebase service account file was not found at '{resolvedPath}'.",
                        resolvedPath);
                }

                builder.GoogleCredential = CredentialFactory
                    .FromFile<ServiceAccountCredential>(resolvedPath)
                    .ToGoogleCredential();
            }

            _firestoreDb = await builder.BuildAsync(cancellationToken);
            _logger.LogInformation(
                "Initialized Firestore client for project '{ProjectId}'.",
                ProjectId);

            return _firestoreDb;
        }
        finally
        {
            _initLock.Release();
        }
    }

    private static string ResolveCredentialsPath(string configuredPath)
    {
        if (Path.IsPathRooted(configuredPath))
        {
            return configuredPath;
        }

        string currentDirCandidate = Path.GetFullPath(
            Path.Combine(Directory.GetCurrentDirectory(), configuredPath));
        if (File.Exists(currentDirCandidate))
        {
            return currentDirCandidate;
        }

        return Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, configuredPath));
    }

    public void Dispose()
    {
        _initLock.Dispose();
    }
}