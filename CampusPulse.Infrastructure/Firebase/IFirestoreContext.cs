using Google.Cloud.Firestore;

namespace CampusPulse.Infrastructure.Firebase;

public interface IFirestoreContext
{
    bool IsFirestoreConfigured { get; }

    bool UseLocalFallback { get; }

    string ProjectId { get; }

    string StorageBucket { get; }

    Task<FirestoreDb?> GetDatabaseAsync(CancellationToken cancellationToken = default);
}