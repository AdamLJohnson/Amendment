using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Amendment.Model.DataModel;
using Amendment.Repository.Infrastructure;
using Microsoft.Extensions.Caching.Memory;

namespace Amendment.Repository
{
    public interface ISystemSettingRepository : IRepository<Model.DataModel.SystemSetting>
    {
        Task<SystemSetting> GetSettingAsync(string key);
    }

    public class SystemSettingRepository : BaseRepository<Model.DataModel.SystemSetting>, ISystemSettingRepository
    {
        private readonly IMemoryCache _memoryCache;
        public SystemSettingRepository(IDbFactory dbFactory, IMemoryCache memoryCache) : base(dbFactory)
        {
            _memoryCache = memoryCache;
        }

        public async Task<SystemSetting> GetSettingAsync(string key)
        {
            SystemSetting output;
            if (!_memoryCache.TryGetValue(key, out output))
            {
                var setting = await base.GetAsync(s => s.Key == key);
                if (setting == null)
                    return null;

                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(5));

                _memoryCache.Set(setting.Key, setting, cacheEntryOptions);
                output = setting;
            }
            return output;
        }

        public override void Update(SystemSetting entity)
        {
            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(5));

            _memoryCache.Set(entity.Key, entity, cacheEntryOptions);

            base.Update(entity);
        }

        public override void Delete(SystemSetting entity)
        {
            _memoryCache.Remove(entity.Key);
            base.Delete(entity);
        }
    }
}
