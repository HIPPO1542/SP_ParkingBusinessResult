namespace ParkingBusinessResult.Domain;

public sealed class ParkingLotNotFoundException : Exception
{
    public ParkingLotNotFoundException() : base("The specified parking lot does not exist.") { }
}
