using Aegis.Domain.Enums;

namespace Aegis.Domain.Entities;

public class UserAlert : Entity
{
    public int UserId { get; private set; }
    public AlertType AlertType { get; private set; }
    public string Title { get; private set; } = default!;
    public string? Body { get; private set; }
    public string Severity { get; private set; } = default!;
    public DateTime? ReadAt { get; private set; }

    public bool IsRead => ReadAt.HasValue;

    public User User { get; private set; } = default!;

    private UserAlert() { }

    public static UserAlert Create(int userId, AlertType alertType, string title, string? body, string severity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(severity);
        return new UserAlert
        {
            UserId = userId,
            AlertType = alertType,
            Title = title,
            Body = body,
            Severity = severity
        };
    }

    public void MarkAsRead()
    {
        if (!IsRead)
        {
            ReadAt = DateTime.UtcNow;
            Touch();
        }
    }
}
