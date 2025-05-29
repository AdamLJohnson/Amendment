using Amendment.Model.DataModel;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Amendment.Service.Infrastructure
{
    /// <summary>
    /// Service for exporting amendments to PDF format
    /// </summary>
    public interface IPdfExportService
    {
        /// <summary>
        /// Exports amendments to PDF format with a three-column layout (English, Spanish, French)
        /// </summary>
        /// <param name="amendments">List of amendments to export</param>
        /// <returns>MemoryStream containing the PDF file</returns>
        Task<MemoryStream> ExportAmendmentsToPdfAsync(IEnumerable<Model.DataModel.Amendment> amendments);
    }
}
