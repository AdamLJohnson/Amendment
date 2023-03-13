using Amendment.Shared.Responses;

namespace Amendment.Client.Repository
{
    public interface IAmendmentHubService
    {
        event EventHandler<SignalRResponse<AmendmentResponse>> AmendmentUpdated;
        event EventHandler<SignalRResponse<AmendmentBodyResponse>> AmendmentBodyUpdated;
        event EventHandler<SignalRResponse<SystemSettingResponse>> SystemSettingUpdated;
        void OnAmendmentUpdated(SignalRResponse<AmendmentResponse> e);
        void OnAmendmentBodyUpdated(SignalRResponse<AmendmentBodyResponse> e);
        void OnSystemSettingUpdated(SignalRResponse<SystemSettingResponse> e);
    }
    public sealed class AmendmentHubService : IAmendmentHubService
    {
        public event EventHandler<SignalRResponse<AmendmentResponse>> AmendmentUpdated;
        public event EventHandler<SignalRResponse<AmendmentBodyResponse>> AmendmentBodyUpdated;
        public event EventHandler<SignalRResponse<SystemSettingResponse>> SystemSettingUpdated;

        public void OnAmendmentUpdated(SignalRResponse<AmendmentResponse> e)
        {
            AmendmentUpdated?.Invoke(this, e);
        }

        public void OnAmendmentBodyUpdated(SignalRResponse<AmendmentBodyResponse> e)
        {
            AmendmentBodyUpdated?.Invoke(this, e);
        }

        public void OnSystemSettingUpdated(SignalRResponse<SystemSettingResponse> e)
        {
            SystemSettingUpdated?.Invoke(this, e);
        }
    }
}
