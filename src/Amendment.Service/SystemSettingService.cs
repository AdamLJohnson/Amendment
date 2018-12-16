using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Amendment.Model.DataModel;
using Amendment.Repository;
using Amendment.Repository.Infrastructure;
using Amendment.Service.Infrastructure;
using Microsoft.Extensions.Caching.Memory;

namespace Amendment.Service
{
    public interface ISystemSettingService : IDataService<SystemSetting>
    {
        Task<SystemSetting> GetSettingAsync(string key);
    }

    public class SystemSettingService : BaseDataService<SystemSetting>, ISystemSettingService
    {
        private readonly ISystemSettingRepository _repo;
        private readonly IMemoryCache _memoryCache;
        private readonly IClientNotifier _clientNotifier;

        public SystemSettingService(ISystemSettingRepository repo, IUnitOfWork unitOfWork
            , IMemoryCache memoryCache, IClientNotifier clientNotifier) : base(repo, unitOfWork)
        {
            _repo = repo;
            _memoryCache = memoryCache;
            _clientNotifier = clientNotifier;
        }

        public Task<SystemSetting> GetSettingAsync(string key)
        {
            return _repo.GetSettingAsync(key);
        }

        public override async Task<IOperationResult> UpdateAsync(SystemSetting item, int userId)
        {
            var output = await base.UpdateAsync(item, userId);
            if (output.Succeeded)
            {
                await _clientNotifier.SendToAllAsync(DestinationHub.Screen, ClientNotifierMethods.RefreshSetting, item);
                await _clientNotifier.SendToAllAsync(DestinationHub.Amendment, ClientNotifierMethods.RefreshSetting, item);
            }
            return output;
        }
    }
}
