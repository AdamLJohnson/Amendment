using Amendment.Server.Hubs;
using Amendment.Server.Mediator.Commands;
using Amendment.Server.Mediator.Commands.AmendmentBodyCommands;
using Amendment.Server.Mediator.Commands.AmendmentCommands;
using Amendment.Server.Mediator.Commands.SystemSettingCommands;
using Amendment.Shared;
using Amendment.Shared.Enums;
using Amendment.Shared.Responses;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using ValidationException = FluentValidation.ValidationException;

namespace Amendment.Server.PipelineBehaviors;

public class ClientNotificationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    private readonly IHubContext<AmendmentHub> _amendmentHub;

    public ClientNotificationBehavior(IHubContext<AmendmentHub> amendmentHub)
    {
        _amendmentHub = amendmentHub;
    }
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var result = await next();
        switch (request)
        {
            case CreateAmendmentCommand cac:
                await NotifyAmendmentChange(OperationType.Create, (IApiResult<AmendmentResponse>)result!);
                break;
            case UpdateAmendmentCommand uac:
                await NotifyAmendmentChange(OperationType.Update, (IApiResult<AmendmentResponse>)result!);
                break;
            case DeleteAmendmentCommand dac:
                await NotifyAmendmentDelete(dac.Id);
                break;
            case CreateAmendmentBodyCommand cabc:
                await NotifyAmendmentBodyChange(OperationType.Create, (IApiResult<AmendmentBodyResponse>)result!);
                break;
            case UpdateAmendmentBodyCommand uacb:
                await NotifyAmendmentBodyChange(OperationType.Update, (IApiResult<AmendmentBodyResponse>)result!);
                break;
            case DeleteAmendmentBodyCommand dabc:
                await NotifyAmendmentBodyDelete(dabc.Id, dabc.AmendmentId);
                break;
            case UpdateSystemSettingCommand ussc:
                await NotifySystemSettingUpdate(OperationType.Update, (IApiResult<SystemSettingResponse>)result!);
                break;
            case ClearScreensCommand csc:
                break;
        }
        return result;
    }
    
    private Task NotifyAmendmentChange(OperationType operationType, IApiResult<AmendmentResponse>? result)
    {
        return _amendmentHub.Clients.All.SendAsync("AmendmentUpdate", new SignalRResponse<AmendmentResponse>(operationType, result.Result));
    }
    private Task NotifyAmendmentDelete(int id)
    {
        return _amendmentHub.Clients.All.SendAsync("AmendmentUpdate", new SignalRResponse<AmendmentResponse>(OperationType.Delete, new AmendmentResponse() { Id = id }));
    }
    private Task NotifyAmendmentBodyChange(OperationType operationType, IApiResult<AmendmentBodyResponse>? result)
    {
        return _amendmentHub.Clients.All.SendAsync("AmendmentBodyUpdate", new SignalRResponse<AmendmentBodyResponse>(operationType, result.Result));
    }
    private Task NotifyAmendmentBodyDelete(int id, int amendmentId)
    {
        return _amendmentHub.Clients.All.SendAsync("AmendmentBodyUpdate", new SignalRResponse<AmendmentBodyResponse>(OperationType.Delete, new AmendmentBodyResponse() { Id = id, AmendId = amendmentId }));
    }
    private Task NotifySystemSettingUpdate(OperationType update, IApiResult<SystemSettingResponse> result)
    {
        return _amendmentHub.Clients.All.SendAsync("SystemSettingUpdate", new SignalRResponse<SystemSettingResponse>(OperationType.Update, result.Result));
    }
}