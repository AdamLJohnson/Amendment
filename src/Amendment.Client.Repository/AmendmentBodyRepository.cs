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
    public interface IAmendmentBodyRepository
    {
        Task<IEnumerable<AmendmentBodyResponse>> GetAsync(int amendmentId);
        Task<AmendmentBodyResponse> GetAsync(int amendmentId, int id);
        Task<AmendmentBodyResponse> PostAsync(int amendmentId, AmendmentBodyRequest request);
        Task<AmendmentBodyResponse> PutAsync(int amendmentId, int id, AmendmentBodyRequest request);
        Task DeleteAsync(int amendmentId, int id);
    }
    public class AmendmentBodyRepository : HttpRepository<AmendmentBodyRequest, AmendmentBodyResponse>, IAmendmentBodyRepository
    {
        protected override string _baseUrl { get; set; } = "api/AmendmentBody";
        public AmendmentBodyRepository(HttpClient client) : base(client)
        {
        }

        Task<IEnumerable<AmendmentBodyResponse>> IAmendmentBodyRepository.GetAsync(int amendmentId)
        {
            _baseUrl = $"api/Amendment/{amendmentId}";
            return base.GetAsync();
        }

        public Task<AmendmentBodyResponse> GetAsync(int amendmentId, int id)
        {
            _baseUrl = $"api/Amendment/{amendmentId}/Body";
            return base.GetAsync(id);
        }

        public Task<AmendmentBodyResponse> PostAsync(int amendmentId, AmendmentBodyRequest request)
        {
            _baseUrl = $"api/Amendment/{amendmentId}/Body";
            return base.PostAsync(request);
        }

        public Task<AmendmentBodyResponse> PutAsync(int amendmentId, int id, AmendmentBodyRequest request)
        {
            _baseUrl = $"api/Amendment/{amendmentId}/Body";
            return base.PutAsync(id, request);
        }

        public Task DeleteAsync(int amendmentId, int id)
        {
            _baseUrl = $"api/Amendment/{amendmentId}/Body";
            return base.DeleteAsync(id);
        }





        public override Task<IEnumerable<AmendmentBodyResponse>> GetAsync()
        {
            throw new NotImplementedException();
        }

        public override Task<AmendmentBodyResponse> GetAsync(int id)
        {
            throw new NotImplementedException();
        }

        public override Task<AmendmentBodyResponse> PostAsync(AmendmentBodyRequest request)
        {
            throw new NotImplementedException();
        }

        public override Task<AmendmentBodyResponse> PutAsync(int id, AmendmentBodyRequest request)
        {
            throw new NotImplementedException();
        }

        public override Task DeleteAsync(int id)
        {
            throw new NotImplementedException();
        }
    }
}
