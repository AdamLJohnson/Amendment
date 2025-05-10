﻿using Amendment.Service;
using Amendment.Service.Infrastructure;
using Amendment.Shared;
using MediatR;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Amendment.Server.Mediator.Commands.AmendmentCommands
{
    public sealed class ExportAmendmentsToExcelCommand : IRequest<IApiResult>
    {
        public List<int> AmendmentIds { get; set; } = new List<int>();
    }

    public sealed class ExportAmendmentsToExcelCommandHandler : IRequestHandler<ExportAmendmentsToExcelCommand, IApiResult>
    {
        private readonly IAmendmentService _amendmentService;
        private readonly IExportService _exportService;

        public ExportAmendmentsToExcelCommandHandler(IAmendmentService amendmentService, IExportService exportService)
        {
            _amendmentService = amendmentService;
            _exportService = exportService;
        }

        public async Task<IApiResult> Handle(ExportAmendmentsToExcelCommand request, CancellationToken cancellationToken)
        {
            var amendments = new List<Amendment.Model.DataModel.Amendment>();

            // If no specific IDs are provided, export all amendments
            if (request.AmendmentIds.Count == 0)
            {
                amendments = await _amendmentService.GetAllAsync();
            }
            else
            {
                // Export only the specified amendments
                foreach (var id in request.AmendmentIds)
                {
                    var amendment = await _amendmentService.GetAsync(id);
                    if (amendment != null)
                    {
                        amendments.Add(amendment);
                    }
                }
            }

            try
            {
                var excelStream = await _exportService.ExportAmendmentsToExcelAsync(amendments);
                return new ApiSuccessResult<MemoryStream>(excelStream);
            }
            catch (System.Exception ex)
            {
                var error = new ValidationError();
                error.Message = $"Error exporting amendments to Excel: {ex.Message}";
                error.Name = "ExportError";
                return new ApiFailedResult<MemoryStream>(new List<ValidationError> { error }, System.Net.HttpStatusCode.InternalServerError);
            }
        }
    }
}
