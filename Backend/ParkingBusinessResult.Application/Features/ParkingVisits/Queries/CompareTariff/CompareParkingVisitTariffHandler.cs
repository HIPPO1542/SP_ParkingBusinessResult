using ParkingBusinessResult.Application.Features.ParkingVisits.Common;

namespace ParkingBusinessResult.Application.Features.ParkingVisits.Queries.CompareTariff;

public sealed record TariffComparison(bool CanCompare, string Message, decimal? ExpectedAmount,
    decimal? CollectedAmount, decimal? Difference);

public sealed class CompareParkingVisitTariffHandler(IParkingVisitTariffRepository repository)
{
    public TariffComparison Handle(CompareParkingVisitTariffQuery query)
    {
        ParkingVisitTariffData? visit = repository.Find(query.VisitId, query.DayType, query.Access);
        if (visit is null) return new(false, "Không tìm thấy lượt xe hoặc không có quyền truy cập.", null, null, null);
        if (visit.ExitedAt is null) return new(false, "Xe chưa ra khỏi bãi.", null, visit.CollectedAmount, null);
        DateTime exit = visit.ExitedAt.Value;
        if (visit.EnteredAt.Date != exit.Date)
            return new(false, "Chưa có quy tắc tính phí qua ngày.", null, visit.CollectedAmount, null);
        bool weekend = visit.EnteredAt.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
        if ((query.DayType == "WEEKDAY" && weekend) || (query.DayType == "WEEKEND" && !weekend))
            return new(false, "Loại ngày không phù hợp với thứ trong tuần.", null, visit.CollectedAmount, null);
        long ticks = (exit - visit.EnteredAt).Ticks;
        if (ticks <= 0 || ticks % TimeSpan.TicksPerHour != 0)
            return new(false, "Chưa có quy tắc làm tròn thời gian lẻ.", null, visit.CollectedAmount, null);
        if (visit.Rules.Count != 1)
            return new(false, "Không tìm thấy duy nhất một mức giá phù hợp với loại xe và loại ngày.", null, visit.CollectedAmount, null);
        ParkingTariffRule rule = visit.Rules[0];
        if (rule.InitialHours < 1 || rule.InitialHours != decimal.Truncate(rule.InitialHours) || rule.InitialPrice < 0 || rule.AdditionalHourPrice < 0)
            return new(false, "Mức giá không hợp lệ hoặc có số giờ đầu chưa được hỗ trợ.", null, visit.CollectedAmount, null);
        decimal hours = ticks / TimeSpan.TicksPerHour;
        decimal expected = rule.InitialPrice + Math.Max(0, hours - rule.InitialHours) * rule.AdditionalHourPrice;
        return new(true, "Đã đối chiếu với mức giá phù hợp.", expected, visit.CollectedAmount,
            visit.CollectedAmount is null ? null : visit.CollectedAmount - expected);
    }
}
