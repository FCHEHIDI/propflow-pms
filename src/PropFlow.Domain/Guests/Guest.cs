using PropFlow.Domain.Common;
using PropFlow.Domain.Errors;

namespace PropFlow.Domain.Guests;

public sealed class Guest : AggregateRoot
{
    public string FirstName { get; private set; } = default!;
    public string LastName { get; private set; } = default!;
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    /// <summary>ISO 3166-1 alpha-2.</summary>
    public string? Nationality { get; private set; }
    public string? DocumentType { get; private set; }
    /// <summary>Invariant: tokenised reference only — never the raw document number. GDPR compliance.</summary>
    public string? DocumentRef { get; private set; }
    public GuestStatus Status { get; private set; }
    public GuestPreferences? Preferences { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Guest() { }

    public static Guest Create(Guid tenantId, string firstName, string lastName, string? email = null)
    {
        if (string.IsNullOrWhiteSpace(firstName)) throw DomainError.Validation("FirstName is required.");
        if (string.IsNullOrWhiteSpace(lastName))  throw DomainError.Validation("LastName is required.");

        return new Guest
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            Status = email is null ? GuestStatus.Anonymous : GuestStatus.Profiled,
            CreatedAt = DateTime.UtcNow,
        };
    }

    public void Verify()
    {
        if (Status == GuestStatus.Blacklisted)
            throw DomainError.Forbidden("Blacklisted guest cannot be verified.");
        Status = GuestStatus.Verified;
    }

    public void Blacklist()
    {
        if (Status == GuestStatus.Blacklisted) return;
        Status = GuestStatus.Blacklisted;
    }

    public void SetPreferences(GuestPreferences preferences) => Preferences = preferences;

    /// <summary>Invariant: ref must be tokenised before being stored here.</summary>
    public void SetDocument(string documentType, string tokenisedRef)
    {
        if (string.IsNullOrWhiteSpace(tokenisedRef))
            throw DomainError.Validation("Document reference must be tokenised.");
        DocumentType = documentType;
        DocumentRef = tokenisedRef;
    }
}

/// <summary>Best-effort preferences — no booking guarantee.</summary>
public sealed record GuestPreferences(
    int? PreferredFloor,
    string? PreferredWing,
    /// <summary>Best-effort — never a booking invariant.</summary>
    Guid? PreferredRoomId,
    bool SmokingRoom,
    bool AccessibilityRequired,
    string? SpecialRequests);
