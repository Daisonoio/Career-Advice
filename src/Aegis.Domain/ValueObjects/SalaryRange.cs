namespace Aegis.Domain.ValueObjects;

public record SalaryRange(decimal Min, decimal Max, string Currency)
{
    public bool IsValid() => Min >= 0 && Max >= Min && !string.IsNullOrWhiteSpace(Currency);
    public decimal Midpoint() => (Min + Max) / 2;
}
