using Aegis.Domain.Enums;

namespace Aegis.Domain.Entities;

public class User : Entity
{
    public string Email { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public SubscriptionTier SubscriptionTier { get; private set; } = SubscriptionTier.Free;
    public DateTime? GdprConsentAt { get; private set; }
    public bool IsActive { get; private set; } = true;

    public UserProfile? Profile { get; private set; }
    public IReadOnlyList<Assessment> Assessments => _assessments.AsReadOnly();
    private readonly List<Assessment> _assessments = [];

    private User() { }

    public static User Create(string email, string passwordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);

        return new User
        {
            Email = email.ToLowerInvariant(),
            PasswordHash = passwordHash,
            GdprConsentAt = DateTime.UtcNow
        };
    }

    public void Upgrade(SubscriptionTier tier)
    {
        SubscriptionTier = tier;
        Touch();
    }

    public void Deactivate() { IsActive = false; Touch(); }
}
