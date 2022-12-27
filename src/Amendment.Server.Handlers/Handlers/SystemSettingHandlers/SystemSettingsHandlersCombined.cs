using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Amendment.Server.Mediator.Commands.SystemSettingCommands;
using Amendment.Server.Mediator.Queries.SystemSettingQueries;
using Amendment.Service;
using Amendment.Shared;
using Amendment.Shared.Responses;
using Mapster;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Amendment.Server.Mediator.Handlers.SystemSettingHandlers;

public sealed class GetAllSystemSettingsHandler : IRequestHandler<GetAllSystemSettingsQuery, IApiResult<List<SystemSettingResponse>>>
{
    private readonly ILogger<GetAllSystemSettingsHandler> _logger;
    private readonly ISystemSettingService _systemSettingService;

    public GetAllSystemSettingsHandler(ILogger<GetAllSystemSettingsHandler> logger, ISystemSettingService systemSettingService)
    {
        _logger = logger;
        _systemSettingService = systemSettingService;
    }

    public async Task<IApiResult<List<SystemSettingResponse>>> Handle(GetAllSystemSettingsQuery request, CancellationToken cancellationToken)
    {
        var results = await _systemSettingService.GetAllAsync();
        return new ApiSuccessResult<List<SystemSettingResponse>>(results.Adapt<List<SystemSettingResponse>>());
    }
}

public sealed class GetSingleSystemSettingHandler : IRequestHandler<GetSingleSystemSettingQuery, IApiResult<SystemSettingResponse>>
{
    private readonly ILogger<GetSingleSystemSettingHandler> _logger;
    private readonly ISystemSettingService _systemSettingService;

    public GetSingleSystemSettingHandler(ILogger<GetSingleSystemSettingHandler> logger, ISystemSettingService systemSettingService)
    {
        _logger = logger;
        _systemSettingService = systemSettingService;
    }

    public async Task<IApiResult<SystemSettingResponse>> Handle(GetSingleSystemSettingQuery request, CancellationToken cancellationToken)
    {
        var result = await _systemSettingService.GetAsync(request.Id);
        if (result == null)
            return new ApiFailedResult<SystemSettingResponse>(HttpStatusCode.NotFound);

        return new ApiSuccessResult<SystemSettingResponse>(result.Adapt<SystemSettingResponse>());
    }
}

public sealed class CreateSystemSettingCommandHandler : IRequestHandler<CreateSystemSettingCommand, IApiResult<SystemSettingResponse>>
{
    private readonly ILogger<CreateSystemSettingCommandHandler> _logger;
    private readonly ISystemSettingService _systemSettingService;

    public CreateSystemSettingCommandHandler(ILogger<CreateSystemSettingCommandHandler> logger, ISystemSettingService systemSettingService)
    {
        _logger = logger;
        _systemSettingService = systemSettingService;
    }

    public async Task<IApiResult<SystemSettingResponse>> Handle(CreateSystemSettingCommand request, CancellationToken cancellationToken)
    {
        var systemSetting = request.Adapt<Model.DataModel.SystemSetting>();
        systemSetting.EnteredBy = request.SavingUserId;
        systemSetting.LastUpdatedBy = request.SavingUserId;
        systemSetting.EnteredDate = DateTime.UtcNow;
        systemSetting.LastUpdated = DateTime.UtcNow;

        var result = await _systemSettingService.CreateAsync(systemSetting, request.SavingUserId);
        if (!result.Succeeded)
        {
            _logger.LogError(400, "Create SystemSetting Failed", result);
            return new ApiFailedResult<SystemSettingResponse>(result.Errors.Select(e => new ValidationError() { Message = e }), HttpStatusCode.BadRequest);
        }

        return new ApiSuccessResult<SystemSettingResponse>(systemSetting.Adapt<SystemSettingResponse>()) { StatusCode = HttpStatusCode.Created };
    }
}

public sealed class UpdateSystemSettingCommandHandler : IRequestHandler<UpdateSystemSettingCommand, IApiResult<SystemSettingResponse>>
{
    private readonly ILogger<UpdateSystemSettingCommandHandler> _logger;
    private readonly ISystemSettingService _systemSettingService;

    public UpdateSystemSettingCommandHandler(ILogger<UpdateSystemSettingCommandHandler> logger, ISystemSettingService systemSettingService)
    {
        _logger = logger;
        _systemSettingService = systemSettingService;
    }

    public async Task<IApiResult<SystemSettingResponse>> Handle(UpdateSystemSettingCommand request, CancellationToken cancellationToken)
    {
        var systemSetting = await _systemSettingService.GetAsync(request.Id);
        if (systemSetting == null)
            return new ApiFailedResult<SystemSettingResponse>(HttpStatusCode.NotFound);

        request.Adapt(systemSetting);

        var result = await _systemSettingService.UpdateAsync(systemSetting, request.SavingUserId);
        if (!result.Succeeded)
        {
            _logger.LogError(410, "Update SystemSetting Failed", result);
            return new ApiFailedResult<SystemSettingResponse>(result.Errors.Select(e => new ValidationError() { Message = e }), HttpStatusCode.BadRequest);
        }

        return new ApiSuccessResult<SystemSettingResponse>(systemSetting.Adapt<SystemSettingResponse>());
    }
}

public sealed class DeleteSystemSettingCommandHandler : IRequestHandler<DeleteSystemSettingCommand, IApiResult>
{
    private readonly ILogger<DeleteSystemSettingCommandHandler> _logger;
    private readonly ISystemSettingService _systemSettingService;

    public DeleteSystemSettingCommandHandler(ILogger<DeleteSystemSettingCommandHandler> logger, ISystemSettingService systemSettingService)
    {
        _logger = logger;
        _systemSettingService = systemSettingService;
    }

    public async Task<IApiResult> Handle(DeleteSystemSettingCommand request, CancellationToken cancellationToken)
    {
        var systemSetting = await _systemSettingService.GetAsync(request.Id);
        if (systemSetting == null)
            return new ApiFailedResult<SystemSettingResponse>(HttpStatusCode.NotFound);

        var result = await _systemSettingService.DeleteAsync(systemSetting, request.SavingUserId);
        if (!result.Succeeded)
        {
            _logger.LogError(420, "Delete SystemSetting Failed", result);
            return new ApiCommandFailedResult { StatusCode = HttpStatusCode.InternalServerError };
        }

        return new ApiCommandSuccessResult();
    }
}