﻿using Amendment.Server.Extensions;
using Amendment.Server.Mediator.Commands.AmendmentCommands;
using Amendment.Server.Mediator.Queries.AmendmentQueries;
using Amendment.Shared.Requests;
using Amendment.Shared.Responses;
using Amendment.Shared;
using Mapster;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IO;

namespace Amendment.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AmendmentController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AmendmentController(IMediator mediator)
        {
            _mediator = mediator;
        }

        protected int SignedInUserId
        {
            get
            {
                if (int.TryParse(User?.Claims.FirstOrDefault(c => c?.Type == "id")?.Value, out int result))
                    return result;

                return 0;
            }
        }

        [HttpGet]
        public async Task<IResult> Get()
        {
            var results = await _mediator.Send(new GetAllAmendmentsQuery());
            return results.ToResult();
        }

        [HttpGet("Live")]
        [AllowAnonymous]
        public async Task<IResult> GetLive()
        {
            var results = await _mediator.Send(new GetLiveAmendmentQuery());
            return results.ToResult();
        }

        [HttpGet("{id}")]
        public async Task<IResult> Get(int id)
        {
            var results = await _mediator.Send(new GetSingleAmendmentQuery(id));
            return results.ToResult();
        }

        [HttpPost]
        public async Task<IResult> Post([FromBody] AmendmentRequest model)
        {
            if (model.Title == "server not allowed")
                return Results.BadRequest("Title is not allowed");
            if (model.Title == "server error")
                throw new Exception("Server error");

            var command = model.Adapt<CreateAmendmentCommand>();
            command.SavingUserId = SignedInUserId;
            var results = await _mediator.Send(command);
            if (!results.IsSuccess)
                return results.ToResult();

            if (results is ApiSuccessResult<AmendmentResponse> typedResults)
            {
                var url = Url.Action(nameof(Get), new { id = typedResults.Result?.Id }) ?? "";
                return results.ToResult(url);
            }
            return results.ToResult();
        }

        [HttpPut("{id}")]
        public async Task<IResult> Put(int id, [FromBody] AmendmentRequest model)
        {
            var command = model.Adapt<UpdateAmendmentCommand>();
            command.SavingUserId = SignedInUserId;
            command.Id = id;
            var results = await _mediator.Send(command);

            return results.ToResult();
        }

        [HttpDelete("{id}")]
        public async Task<IResult> Delete(int id)
        {
            var command = new DeleteAmendmentCommand(id, SignedInUserId);
            var results = await _mediator.Send(command);
            return results.ToResult();
        }

        [HttpPost("{id}/Clone")]
        [Authorize(Roles = RoleGroups.AdminAmendEditor)]
        public async Task<IResult> Clone(int id, [FromBody] CloneAmendmentRequest model)
        {
            var command = new CloneAmendmentCommand
            {
                SavingUserId = SignedInUserId,
                SourceAmendmentId = id,
                Title = model.Title,
                Author = model.Author,
                Motion = model.Motion,
                Source = model.Source,
                LegisId = model.LegisId
            };

            var results = await _mediator.Send(command);
            if (!results.IsSuccess)
                return results.ToResult();

            if (results is ApiSuccessResult<AmendmentResponse> typedResults)
            {
                var url = Url.Action(nameof(Get), new { id = typedResults.Result?.Id }) ?? "";
                return results.ToResult(url);
            }
            return results.ToResult();
        }

        [HttpPost("ExportExcel")]
        public async Task<IActionResult> ExportToExcel([FromBody] List<int> amendmentIds)
        {
            var command = new ExportAmendmentsToExcelCommand { AmendmentIds = amendmentIds ?? new List<int>() };
            var result = await _mediator.Send(command);

            if (result is ApiSuccessResult<MemoryStream> successResult && successResult.Result != null)
            {
                return File(
                    successResult.Result.ToArray(),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    "Amendments.xlsx");
            }

            return BadRequest(result);
        }

        [HttpPost("ExportPdf")]
        public async Task<IActionResult> ExportToPdf([FromBody] List<int> amendmentIds)
        {
            var command = new ExportAmendmentsToPdfCommand { AmendmentIds = amendmentIds ?? new List<int>() };
            var result = await _mediator.Send(command);

            if (result is ApiSuccessResult<MemoryStream> successResult && successResult.Result != null)
            {
                return File(
                    successResult.Result.ToArray(),
                    "application/pdf",
                    "Amendments.pdf");
            }

            return BadRequest(result);
        }
    }
}
