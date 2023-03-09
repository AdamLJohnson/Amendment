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
    public interface IRoleRepository : IHttpRepository<RoleRequest, RoleResponse>
    {

    }
    public class RoleRepository : HttpRepository<RoleRequest, RoleResponse>, IRoleRepository
    {
        protected override string _baseUrl { get; set; } = "api/Role";
        private IEnumerable<RoleResponse> _roles = null;
        public RoleRepository(HttpClient client) : base(client)
        {
        }

        public override async Task<IEnumerable<RoleResponse>> GetAsync()
        {
            if (_roles != null)
                return _roles;

            var dbRoles = await base.GetAsync();
            _roles = dbRoles;
            return _roles;
        }
    }
}
