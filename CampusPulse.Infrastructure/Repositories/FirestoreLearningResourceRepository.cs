using System.Collections.Concurrent;
using CampusPulse.Application.DTOs.Learning;
using CampusPulse.Application.Interfaces;
using CampusPulse.Infrastructure.Firebase;
using Google.Cloud.Firestore;

namespace CampusPulse.Infrastructure.Repositories;

public sealed class FirestoreLearningResourceRepository : ILearningResourceRepository
{
    private const string CollectionName = "learning_resources";

    private readonly IFirestoreContext _firestoreContext;
    private readonly ConcurrentDictionary<string, LearningResourceDto> _fallbackStore = new(StringComparer.Ordinal);

    public FirestoreLearningResourceRepository(IFirestoreContext firestoreContext)
    {
        _firestoreContext = firestoreContext;
    }

    public async Task<IReadOnlyList<LearningResourceDto>> GetByModuleAsync(
        int moduleId,
        bool includeUnpublished,
        CancellationToken cancellationToken = default)
    {
        if (moduleId <= 0)
        {
            return Array.Empty<LearningResourceDto>();
        }

        FirestoreDb? db = await _firestoreContext.GetDatabaseAsync(cancellationToken);
        if (db is null)
        {
            return _fallbackStore.Values
                .Where(r => r.ModuleId == moduleId && (includeUnpublished || r.IsPublished))
                .Select(Clone)
                .OrderByDescending(r => r.CreatedAtUtc)
                .ToList();
        }

        Query query = db.Collection(CollectionName)
            .WhereEqualTo("moduleId", moduleId);

        QuerySnapshot snapshot = await query.GetSnapshotAsync(cancellationToken);
        return snapshot.Documents
            .Select(MapFromSnapshot)
            .Where(r => includeUnpublished || r.IsPublished)
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToList();
    }

    public async Task<IReadOnlyList<LearningResourceDto>> GetByModulesAsync(
        IEnumerable<int> moduleIds,
        bool includeUnpublished,
        CancellationToken cancellationToken = default)
    {
        var distinctIds = moduleIds
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        if (distinctIds.Count == 0)
        {
            return Array.Empty<LearningResourceDto>();
        }

        FirestoreDb? db = await _firestoreContext.GetDatabaseAsync(cancellationToken);
        if (db is null)
        {
            var idSet = distinctIds.ToHashSet();
            return _fallbackStore.Values
                .Where(r => idSet.Contains(r.ModuleId) && (includeUnpublished || r.IsPublished))
                .Select(Clone)
                .OrderByDescending(r => r.CreatedAtUtc)
                .ToList();
        }

        var results = new List<LearningResourceDto>();
        foreach (int[] batch in distinctIds.Chunk(10))
        {
            var boxedIds = batch.Cast<object>().ToArray();
            QuerySnapshot snapshot = await db.Collection(CollectionName)
                .WhereIn("moduleId", boxedIds)
                .GetSnapshotAsync(cancellationToken);

            foreach (DocumentSnapshot doc in snapshot.Documents)
            {
                LearningResourceDto item = MapFromSnapshot(doc);
                if (includeUnpublished || item.IsPublished)
                {
                    results.Add(item);
                }
            }
        }

        return results
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToList();
    }

    public async Task<LearningResourceDto?> GetByIdAsync(
        string resourceId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(resourceId))
        {
            return null;
        }

        string trimmedId = resourceId.Trim();
        FirestoreDb? db = await _firestoreContext.GetDatabaseAsync(cancellationToken);
        if (db is null)
        {
            return _fallbackStore.TryGetValue(trimmedId, out LearningResourceDto? found)
                ? Clone(found)
                : null;
        }

        DocumentSnapshot snapshot = await db.Collection(CollectionName)
            .Document(trimmedId)
            .GetSnapshotAsync(cancellationToken);

        return snapshot.Exists ? MapFromSnapshot(snapshot) : null;
    }

    public async Task<LearningResourceDto> CreateAsync(
        LearningResourceDto resource,
        CancellationToken cancellationToken = default)
    {
        DateTime now = DateTime.UtcNow;
        LearningResourceDto stored = Clone(resource);
        stored.ResourceId = string.IsNullOrWhiteSpace(stored.ResourceId)
            ? Guid.NewGuid().ToString("N")
            : stored.ResourceId.Trim();
        stored.CreatedAtUtc = now;
        stored.UpdatedAtUtc = now;

        FirestoreDb? db = await _firestoreContext.GetDatabaseAsync(cancellationToken);
        if (db is null)
        {
            _fallbackStore[stored.ResourceId] = Clone(stored);
            return stored;
        }

        await db.Collection(CollectionName)
            .Document(stored.ResourceId)
            .SetAsync(ToFirestoreDictionary(stored), cancellationToken: cancellationToken);

        return stored;
    }

    public async Task<LearningResourceDto?> UpdateAsync(
        LearningResourceDto resource,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(resource.ResourceId))
        {
            return null;
        }

        string trimmedId = resource.ResourceId.Trim();
        FirestoreDb? db = await _firestoreContext.GetDatabaseAsync(cancellationToken);
        if (db is null)
        {
            if (!_fallbackStore.TryGetValue(trimmedId, out LearningResourceDto? existing))
            {
                return null;
            }

            LearningResourceDto updated = Clone(resource);
            updated.ResourceId = trimmedId;
            updated.ModuleId = existing.ModuleId;
            updated.ModuleCode = existing.ModuleCode;
            updated.ModuleName = existing.ModuleName;
            updated.CreatedByUserId = existing.CreatedByUserId;
            updated.CreatedByName = existing.CreatedByName;
            updated.CreatedAtUtc = existing.CreatedAtUtc;
            updated.UpdatedAtUtc = DateTime.UtcNow;

            _fallbackStore[trimmedId] = Clone(updated);
            return updated;
        }

        DocumentReference docRef = db.Collection(CollectionName).Document(trimmedId);
        DocumentSnapshot snapshot = await docRef.GetSnapshotAsync(cancellationToken);
        if (!snapshot.Exists)
        {
            return null;
        }

        LearningResourceDto current = MapFromSnapshot(snapshot);
        LearningResourceDto next = Clone(resource);
        next.ResourceId = trimmedId;
        next.ModuleId = current.ModuleId;
        next.ModuleCode = current.ModuleCode;
        next.ModuleName = current.ModuleName;
        next.CreatedByUserId = current.CreatedByUserId;
        next.CreatedByName = current.CreatedByName;
        next.CreatedAtUtc = current.CreatedAtUtc;
        next.UpdatedAtUtc = DateTime.UtcNow;

        await docRef.SetAsync(ToFirestoreDictionary(next), cancellationToken: cancellationToken);
        return next;
    }

    public async Task<bool> DeleteAsync(
        string resourceId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(resourceId))
        {
            return false;
        }

        string trimmedId = resourceId.Trim();
        FirestoreDb? db = await _firestoreContext.GetDatabaseAsync(cancellationToken);
        if (db is null)
        {
            return _fallbackStore.TryRemove(trimmedId, out _);
        }

        DocumentReference docRef = db.Collection(CollectionName).Document(trimmedId);
        DocumentSnapshot snapshot = await docRef.GetSnapshotAsync(cancellationToken);
        if (!snapshot.Exists)
        {
            return false;
        }

        await docRef.DeleteAsync(cancellationToken: cancellationToken);
        return true;
    }

    private static Dictionary<string, object> ToFirestoreDictionary(LearningResourceDto item)
    {
        return new Dictionary<string, object>
        {
            ["resourceId"] = item.ResourceId,
            ["moduleId"] = item.ModuleId,
            ["moduleCode"] = item.ModuleCode,
            ["moduleName"] = item.ModuleName,
            ["title"] = item.Title,
            ["description"] = item.Description,
            ["resourceType"] = item.ResourceType,
            ["contentText"] = item.ContentText,
            ["externalUrl"] = item.ExternalUrl,
            ["fileName"] = item.FileName,
            ["contentType"] = item.ContentType,
            ["fileSizeBytes"] = item.FileSizeBytes,
            ["createdByUserId"] = item.CreatedByUserId,
            ["createdByName"] = item.CreatedByName,
            ["createdAtUtc"] = Timestamp.FromDateTime(DateTime.SpecifyKind(item.CreatedAtUtc, DateTimeKind.Utc)),
            ["updatedAtUtc"] = Timestamp.FromDateTime(DateTime.SpecifyKind(item.UpdatedAtUtc, DateTimeKind.Utc)),
            ["isPublished"] = item.IsPublished
        };
    }

    private static LearningResourceDto MapFromSnapshot(DocumentSnapshot doc)
    {
        return new LearningResourceDto
        {
            ResourceId = doc.TryGetValue("resourceId", out string id) && !string.IsNullOrWhiteSpace(id)
                ? id
                : doc.Id,
            ModuleId = doc.TryGetValue("moduleId", out long moduleId) ? (int)moduleId : 0,
            ModuleCode = doc.TryGetValue("moduleCode", out string moduleCode) ? moduleCode : string.Empty,
            ModuleName = doc.TryGetValue("moduleName", out string moduleName) ? moduleName : string.Empty,
            Title = doc.TryGetValue("title", out string title) ? title : string.Empty,
            Description = doc.TryGetValue("description", out string description) ? description : string.Empty,
            ResourceType = doc.TryGetValue("resourceType", out string resourceType)
                ? resourceType
                : LearningResourceTypes.Notes,
            ContentText = doc.TryGetValue("contentText", out string contentText) ? contentText : string.Empty,
            ExternalUrl = doc.TryGetValue("externalUrl", out string externalUrl) ? externalUrl : string.Empty,
            FileName = doc.TryGetValue("fileName", out string fileName) ? fileName : string.Empty,
            ContentType = doc.TryGetValue("contentType", out string contentType) ? contentType : string.Empty,
            FileSizeBytes = doc.TryGetValue("fileSizeBytes", out long size) ? size : 0L,
            CreatedByUserId = doc.TryGetValue("createdByUserId", out long createdByUserId) ? (int)createdByUserId : 0,
            CreatedByName = doc.TryGetValue("createdByName", out string createdByName) ? createdByName : string.Empty,
            CreatedAtUtc = doc.TryGetValue("createdAtUtc", out Timestamp createdTs)
                ? createdTs.ToDateTime()
                : DateTime.UtcNow,
            UpdatedAtUtc = doc.TryGetValue("updatedAtUtc", out Timestamp updatedTs)
                ? updatedTs.ToDateTime()
                : DateTime.UtcNow,
            IsPublished = !doc.TryGetValue("isPublished", out bool published) || published
        };
    }

    private static LearningResourceDto Clone(LearningResourceDto source)
    {
        return new LearningResourceDto
        {
            ResourceId = source.ResourceId,
            ModuleId = source.ModuleId,
            ModuleCode = source.ModuleCode,
            ModuleName = source.ModuleName,
            Title = source.Title,
            Description = source.Description,
            ResourceType = source.ResourceType,
            ContentText = source.ContentText,
            ExternalUrl = source.ExternalUrl,
            FileName = source.FileName,
            ContentType = source.ContentType,
            FileSizeBytes = source.FileSizeBytes,
            CreatedByUserId = source.CreatedByUserId,
            CreatedByName = source.CreatedByName,
            CreatedAtUtc = source.CreatedAtUtc,
            UpdatedAtUtc = source.UpdatedAtUtc,
            IsPublished = source.IsPublished
        };
    }
}