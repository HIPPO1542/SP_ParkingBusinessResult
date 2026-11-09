namespace ParkingBusinessResult.Domain;

public sealed class Booking(Guid id, Guid parkingLotId, string licensePlate, string vehicleType,
    DateTime startAt, DateTime? endAt, string status, decimal? amount)
{
    public const string Reserved = "RESERVED";
    public const string CheckedIn = "CHECKED_IN";
    public const string Completed = "COMPLETED";

    public Guid Id { get; } = id;
    public Guid ParkingLotId { get; } = parkingLotId;
    public string LicensePlate { get; } = licensePlate;
    public string VehicleType { get; } = vehicleType;
    public DateTime StartAt { get; private set; } = startAt;
    public DateTime? EndAt { get; private set; } = endAt;
    public string Status { get; private set; } = status;
    public decimal? Amount { get; private set; } = amount;

    public static Booking Reserve(Guid lotId, string plate, string type, DateTime expectedArrivalAt, DateTime now)
    {
        if (lotId == Guid.Empty || string.IsNullOrWhiteSpace(plate) || plate.Trim().Length > 30 ||
            string.IsNullOrWhiteSpace(type) || type.Trim().Length > 100)
            throw new ArgumentException("Bãi xe, biển số và loại xe không hợp lệ.");
        if (expectedArrivalAt < now)
            throw new ArgumentException("Giờ đến dự kiến không được trước giờ hiện tại.");
        return new(Guid.NewGuid(), lotId, plate.Trim().ToUpperInvariant(), type.Trim(), expectedArrivalAt, null, Reserved, null);
    }

    public void CheckIn(DateTime now)
    {
        if (Status != Reserved) throw new InvalidOperationException("Chỉ booking đã đặt và chưa check-in mới được nhận xe.");
        StartAt = now;
        EndAt = null;
        Amount = null;
        Status = CheckedIn;
    }

    public void CheckOut(DateTime now, decimal amount)
    {
        if (Status != CheckedIn) throw new InvalidOperationException("Chỉ lượt đang gửi mới được check-out.");
        if (now <= StartAt || amount < 0) throw new ArgumentException("Giờ ra hoặc số tiền không hợp lệ.");
        EndAt = now;
        Amount = amount;
        Status = Completed;
    }
}
