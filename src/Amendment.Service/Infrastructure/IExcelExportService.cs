using Amendment.Model.DataModel;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Amendment.Service.Infrastructure
{
    /// <summary>
    /// Service for exporting amendments to Excel format
    /// </summary>
    public interface IExcelExportService
    {
        /// <summary>
        /// Exports amendments to Excel format with rich text formatting
        /// </summary>
        /// <param name="amendments">List of amendments to export</param>
        /// <returns>MemoryStream containing the Excel file</returns>
        Task<MemoryStream> ExportAmendmentsToExcelAsync(IEnumerable<Model.DataModel.Amendment> amendments);
    }
}
