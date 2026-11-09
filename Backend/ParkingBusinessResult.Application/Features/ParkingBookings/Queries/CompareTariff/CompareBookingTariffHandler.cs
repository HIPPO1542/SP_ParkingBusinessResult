using ParkingBusinessResult.Application.Features.ParkingBookings.Common;
using ParkingBusinessResult.Domain.ValueObjects;

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
        if (booking.Rules.Count != 1)
            return new(false, "Không tìm thấy duy nhất một mức giá phù hợp với loại xe và loại ngày.", null, booking.CollectedAmount, null);
        ParkingTariffRule rule = booking.Rules[0];
        try
        {
            decimal expected = ParkingFee.Calculate(booking.EnteredAt, booking.ExitedAt.Value,
                rule.InitialHours, rule.InitialPrice, rule.AdditionalHourPrice).Amount;
            return new(true, "Đã đối chiếu với mức giá phù hợp.", expected, booking.CollectedAmount,
                booking.CollectedAmount is null ? null : booking.CollectedAmount - expected);
        }
        catch (ArgumentException error) { return new(false, error.Message, null, booking.CollectedAmount, null); }
    }
}
