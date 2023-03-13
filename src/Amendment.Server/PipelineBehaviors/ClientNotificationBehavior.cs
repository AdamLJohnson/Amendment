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
    private readonly IHubContext<ScreenHub> _screenHub;

    public ClientNotificationBehavior(IHubContext<AmendmentHub> amendmentHub, IHubContext<ScreenHub> screenHub)
    {
        _amendmentHub = amendmentHub;
        _screenHub = screenHub;
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
                await NotifyClearScreens();
                break;
        }
        return result;
    }


    private async Task NotifyAmendmentChange(OperationType operationType, IApiResult<AmendmentResponse>? result)
    {
        var tasks = new List<Task>();
        tasks.Add(_amendmentHub.Clients.All.SendAsync("AmendmentUpdate", new SignalRResponse<AmendmentResponse>(operationType, result.Result)));

        if(result.Result.IsLive)
            tasks.Add(_screenHub.Clients.All.SendAsync("AmendmentUpdate", new SignalRResponse<AmendmentResponse>(operationType, result.Result)));

        await Task.WhenAll(tasks);
    }
    private Task NotifyAmendmentDelete(int id)
    {
        return _amendmentHub.Clients.All.SendAsync("AmendmentUpdate", new SignalRResponse<AmendmentResponse>(OperationType.Delete, new AmendmentResponse() { Id = id }));
    }
    private async Task NotifyAmendmentBodyChange(OperationType operationType, IApiResult<AmendmentBodyResponse>? result)
    {
        var tasks = new List<Task>();
        tasks.Add(_amendmentHub.Clients.All.SendAsync("AmendmentBodyUpdate", new SignalRResponse<AmendmentBodyResponse>(operationType, result.Result)));

        if (result.Result.IsLive)
            tasks.Add(_screenHub.Clients.All.SendAsync("AmendmentBodyUpdate", new SignalRResponse<AmendmentBodyResponse>(operationType, result.Result)));
        await Task.WhenAll(tasks);
    }
    private Task NotifyAmendmentBodyDelete(int id, int amendmentId)
    {
        return _amendmentHub.Clients.All.SendAsync("AmendmentBodyUpdate", new SignalRResponse<AmendmentBodyResponse>(OperationType.Delete, new AmendmentBodyResponse() { Id = id, AmendId = amendmentId }));
    }
    private Task NotifySystemSettingUpdate(OperationType update, IApiResult<SystemSettingResponse> result)
    {
        return _amendmentHub.Clients.All.SendAsync("SystemSettingUpdate", new SignalRResponse<SystemSettingResponse>(OperationType.Update, result.Result));
    }
    private async Task NotifyClearScreens()
    {
        var tasks = new List<Task>
        {
            _amendmentHub.Clients.All.SendAsync("ClearScreens"),
            _screenHub.Clients.All.SendAsync("ClearScreens")
        };
        await Task.WhenAll(tasks);
    }
}