namespace ParkingBusinessResult.Domain.ValueObjects;

public sealed record ParkingFee(long BillableHours, decimal Amount)
{
    public static long RoundHours(DateTime enteredAt, DateTime exitedAt)
    {
        if (exitedAt <= enteredAt) throw new ArgumentException("Giờ ra phải sau giờ vào.");
        long ticks = (exitedAt - enteredAt).Ticks;
        long hours = ticks / TimeSpan.TicksPerHour;
        if (ticks % TimeSpan.TicksPerHour >= 30 * TimeSpan.TicksPerMinute) hours++;
        return Math.Max(1, hours);
    }

    public static ParkingFee Calculate(DateTime enteredAt, DateTime exitedAt,
        decimal initialHours, decimal initialPrice, decimal additionalHourPrice)
    {
        if (initialHours < 1 || initialHours != decimal.Truncate(initialHours) || initialPrice < 0 || additionalHourPrice < 0)
            throw new ArgumentException("Mức giá không hợp lệ.");
        long hours = RoundHours(enteredAt, exitedAt);
        decimal amount = initialPrice + Math.Max(0, hours - initialHours) * additionalHourPrice;
        return new(hours, amount);
    }
}
