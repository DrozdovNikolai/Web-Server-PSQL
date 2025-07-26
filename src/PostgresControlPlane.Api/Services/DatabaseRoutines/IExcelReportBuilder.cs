using PostgresControlPlane.Api.Models.Contracts;

namespace PostgresControlPlane.Api.Services.DatabaseRoutines;

public interface IExcelReportBuilder
{
    RoutineExcelFile Build(
        RoutineInvocationResponse result,
        ExcelExportSettings settings);
}
