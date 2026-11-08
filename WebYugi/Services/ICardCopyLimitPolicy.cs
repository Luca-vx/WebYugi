using WebYugi.Model.Enums;

namespace WebYugi.Services;

public interface ICardCopyLimitPolicy
{
    int GetMaximumCopies(int cardId);
}

/// <summary>Default unrestricted catalog policy. A restriction list can replace this service later.</summary>
public sealed class DefaultCardCopyLimitPolicy : ICardCopyLimitPolicy
{
    public int GetMaximumCopies(int cardId) => 3;
}
