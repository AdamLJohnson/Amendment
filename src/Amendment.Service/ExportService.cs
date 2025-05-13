﻿using Amendment.Model.DataModel;
using Amendment.Service.Infrastructure;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Amendment.Service
{
    /// <summary>
    /// Service for exporting amendments to various formats
    /// This is a facade that delegates to specialized export services
    /// </summary>
    public class ExportService : IExportService
    {
        private readonly IExcelExportService _excelExportService;
        private readonly IPdfExportService _pdfExportService;

        public ExportService(
            IExcelExportService excelExportService,
            IPdfExportService pdfExportService)
        {
            _excelExportService = excelExportService;
            _pdfExportService = pdfExportService;
        }

        /// <summary>
        /// Exports amendments to Excel format with rich text formatting
        /// </summary>
        public async Task<MemoryStream> ExportAmendmentsToExcelAsync(IEnumerable<Model.DataModel.Amendment> amendments)
        {
            return await _excelExportService.ExportAmendmentsToExcelAsync(amendments);
        }

        /// <summary>
        /// Exports amendments to PDF format with a three-column layout (English, Spanish, French)
        /// </summary>
        public async Task<MemoryStream> ExportAmendmentsToPdfAsync(IEnumerable<Model.DataModel.Amendment> amendments)
        {
            return await _pdfExportService.ExportAmendmentsToPdfAsync(amendments);
        }
    }
}

