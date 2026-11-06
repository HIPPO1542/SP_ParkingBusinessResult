using ParkingBusinessResult.Application.Features.BusinessReports.Queries.List;
using ParkingBusinessResult.Domain;
using ParkingBusinessResult.Application.Features.BusinessReports.Common;

namespace ParkingBusinessResult.Application.Features.BusinessReports.Commands.Export;

public sealed class ExportBusinessReportHandler(ListBusinessReportHandler listHandler, IBusinessReportParkingLotSearch parkingLots,
    IBusinessReportDocumentWriter writer, IBusinessReportExportStore store)
{
    public SavedBusinessReport Handle(ExportBusinessReportCommand command)
    {
        IReadOnlyList<ParkingBusinessResultRecord> records = listHandler.Handle(new ListBusinessReportQuery(
            command.ReportingPeriod, command.ParkingLotId, command.Access, District: command.District));
        IReadOnlyDictionary<Guid, ReportParkingLot> lots = parkingLots.GetParkingLots(records.Select(record => record.ParkingLotId).ToHashSet());
        BusinessReportRow[] rows = records.Select(record => new BusinessReportRow(record.ParkingLotId, record.ReportingPeriod,
            record.Revenue, record.CompletedVehicleCount, lots[record.ParkingLotId].Code, lots[record.ParkingLotId].Name)).ToArray();
        ReportDocument document = writer.Write(command.FileFormat, command.ReportTitle, command.ReportingPeriod, rows);
        return store.Save(command.Actor, command.FileFormat, document, records.Count);
    }
}
