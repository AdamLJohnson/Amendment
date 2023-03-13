using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Amendment.Client.Repository.Infrastructure;
using Amendment.Shared;
using Amendment.Shared.Requests;
using Amendment.Shared.Responses;

namespace Amendment.Client.Repository
{
    public interface ISystemSettingRepository : IHttpRepository<SystemSettingRequest, SystemSettingResponse>
    {
    }
    public class SystemSettingRepository : HttpRepository<SystemSettingRequest, SystemSettingResponse>, ISystemSettingRepository
    {
        protected override string _baseUrl { get; set; } = "api/SystemSetting";
        private IEnumerable<SystemSettingResponse>? _systemSettings;
        public SystemSettingRepository(HttpClient client) : base(client)
        {
        }

        public override async Task<IEnumerable<SystemSettingResponse>> GetAsync()
        {
            if (_systemSettings != null)
                return _systemSettings;

            var results = await base.GetAsync();
            _systemSettings = results;
            return _systemSettings;
        }
    }
}
