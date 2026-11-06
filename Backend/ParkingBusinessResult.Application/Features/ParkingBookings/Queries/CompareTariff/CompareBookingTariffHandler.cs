using ParkingBusinessResult.Application.Features.ParkingBookings.Common;

namespace ParkingBusinessResult.Application.Features.ParkingBookings.Queries.CompareTariff;

public sealed record TariffComparison(bool CanCompare, string Message, decimal? ExpectedAmount,
    decimal? CollectedAmount, decimal? Difference);

public sealed class CompareBookingTariffHandler(IBookingTariffRepository repository)
{
    public TariffComparison Handle(CompareBookingTariffQuery query)
    {
        BookingTariffData? booking = repository.Find(query.BookingId, query.DayType, query.Access);
        if (booking is null) return new(false, "Không tìm thấy lượt xe hoặc không có quyền truy cập.", null, null, null);
        if (booking.ExitedAt is null) return new(false, "Xe chưa ra khỏi bãi.", null, booking.CollectedAmount, null);
        DateTime exit = booking.ExitedAt.Value;
        if (booking.EnteredAt.Date != exit.Date)
            return new(false, "Chưa có quy tắc tính phí qua ngày.", null, booking.CollectedAmount, null);
        bool weekend = booking.EnteredAt.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
        if ((query.DayType == "WEEKDAY" && weekend) || (query.DayType == "WEEKEND" && !weekend))
            return new(false, "Loại ngày không phù hợp với thứ trong tuần.", null, booking.CollectedAmount, null);
        long ticks = (exit - booking.EnteredAt).Ticks;
        if (ticks <= 0 || ticks % TimeSpan.TicksPerHour != 0)
            return new(false, "Chưa có quy tắc làm tròn thời gian lẻ.", null, booking.CollectedAmount, null);
        if (booking.Rules.Count != 1)
            return new(false, "Không tìm thấy duy nhất một mức giá phù hợp với loại xe và loại ngày.", null, booking.CollectedAmount, null);
        ParkingTariffRule rule = booking.Rules[0];
        if (rule.InitialHours < 1 || rule.InitialHours != decimal.Truncate(rule.InitialHours) || rule.InitialPrice < 0 || rule.AdditionalHourPrice < 0)
            return new(false, "Mức giá không hợp lệ hoặc có số giờ đầu chưa được hỗ trợ.", null, booking.CollectedAmount, null);
        decimal hours = ticks / TimeSpan.TicksPerHour;
        decimal expected = rule.InitialPrice + Math.Max(0, hours - rule.InitialHours) * rule.AdditionalHourPrice;
        return new(true, "Đã đối chiếu với mức giá phù hợp.", expected, booking.CollectedAmount,
            booking.CollectedAmount is null ? null : booking.CollectedAmount - expected);
    }
}
