﻿using Amendment.Model.DataModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Amendment.Service.Infrastructure
{
    public interface IExportService
    {
        /// <summary>
        /// Exports amendments to Excel format
        /// </summary>
        /// <param name="amendments">List of amendments to export</param>
        /// <returns>MemoryStream containing the Excel file</returns>
        Task<MemoryStream> ExportAmendmentsToExcelAsync(IEnumerable<Model.DataModel.Amendment> amendments);
    }
}
