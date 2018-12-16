using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amendment.Model.DataModel;
using Amendment.Model.ViewModel.ScreenView;
using Amendment.Service;
using Amendment.Service.Infrastructure;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;

namespace Amendment.Web.Hubs
{
    public class ScreenHub : Hub
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IAmendmentService _amendmentService;
        private readonly IMapper _mapper;
        private readonly IReadOnlyDataService<Language> _languageService;

        public ScreenHub(IHttpContextAccessor httpContextAccessor, IAmendmentService amendmentService, IMapper mapper
            , IReadOnlyDataService<Language> languageService)
        {
            _httpContextAccessor = httpContextAccessor;
            _amendmentService = amendmentService;
            _mapper = mapper;
            _languageService = languageService;
        }

        public override Task OnConnectedAsync()
        {
            //Context.Items
            var httpContext = _httpContextAccessor.HttpContext;
            var languageId = httpContext.Request.Query["languageId"];
            Groups.AddToGroupAsync(Context.ConnectionId, $"language_{languageId}");
            return base.OnConnectedAsync();
        }

        public async Task RefreshLanguage(int languageId)
        {
            var amendment = await _amendmentService.GetLiveAsync() ?? new Model.DataModel.Amendment();
            var amendmentBody = amendment.AmendmentBodies.FirstOrDefault(b => b.LanguageId == languageId);

            var amendmentModel = _mapper.Map<AmendmentViewViewModel>(amendment);
            var amendmentBodyModel = _mapper.Map<AmendmentBodyViewViewModel>(amendmentBody);

            var model = new
            {
                Amendment = amendmentModel,
                AmendmentBody = amendmentBodyModel
            };
            await Clients.Caller.SendAsync(ClientNotifierMethods.RefreshLanguage, model);
        }
    }
}
