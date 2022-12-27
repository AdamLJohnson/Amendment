﻿using Amendment.Shared;
using Amendment.Shared.Enums;
using Amendment.Shared.Responses;
using MediatR;

namespace Amendment.Server.Mediator.Commands.AmendmentBodyCommands;

public sealed class UpdateAmendmentBodyCommand : IRequest<IApiResult<AmendmentBodyResponse>>
{
    public int SavingUserId { get; set; }
    public int Id { get; set; }
    public int AmendId { get; set; }
    public int LanguageId { get; set; }
    public string AmendBody { get; set; }
    public AmendmentBodyStatus AmendStatus { get; set; }
    public bool IsLive { get; set; }
}