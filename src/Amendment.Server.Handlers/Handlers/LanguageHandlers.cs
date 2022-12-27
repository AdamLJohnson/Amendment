using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amendment.Server.Mediator.Queries;
using Amendment.Service.Infrastructure;
using Amendment.Shared;
using Amendment.Shared.Responses;
using Mapster;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Amendment.Server.Mediator.Handlers;

public sealed class GetAllLanguagesHandler : IRequestHandler<GetAllLanguagesQuery, IApiResult<List<LanguageResponse>>>
{
    private readonly ILogger<GetAllLanguagesHandler> _logger;
    private readonly IReadOnlyDataService<Model.DataModel.Language> _languageService;

    public GetAllLanguagesHandler(ILogger<GetAllLanguagesHandler> logger, IReadOnlyDataService<Model.DataModel.Language> languageService)
    {
        _logger = logger;
        _languageService = languageService;
    }

    public async Task<IApiResult<List<LanguageResponse>>> Handle(GetAllLanguagesQuery request, CancellationToken cancellationToken)
    {
        var results = await _languageService.GetAllAsync();
        return new ApiSuccessResult<List<LanguageResponse>>(results.Adapt<List<LanguageResponse>>());
    }
}
