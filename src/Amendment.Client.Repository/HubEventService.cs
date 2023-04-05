using Amendment.Shared.Responses;

namespace Amendment.Client.Repository
{
    public interface IHubEventService
    {
        event EventHandler<SignalRResponse<AmendmentResponse>> AmendmentUpdated;
        event EventHandler<SignalRResponse<AmendmentBodyResponse>> AmendmentBodyUpdated;
        event EventHandler<SignalRResponse<List<AmendmentBodyResponse>>> AmendmentBodyUpdatedMany;
        event EventHandler<SignalRResponse<SystemSettingResponse>> SystemSettingUpdated;
        event EventHandler ClearScreens;
        void OnAmendmentUpdated(SignalRResponse<AmendmentResponse> e);
        void OnAmendmentBodyUpdated(SignalRResponse<AmendmentBodyResponse> e);
        void OnAmendmentBodyUpdatedMany(SignalRResponse<List<AmendmentBodyResponse>> e);
        void OnSystemSettingUpdated(SignalRResponse<SystemSettingResponse> e);
        void OnClearScreens();
    }
    public sealed class HubEventService : IHubEventService
    {
        public event EventHandler<SignalRResponse<AmendmentResponse>> AmendmentUpdated = null!;
        public event EventHandler<SignalRResponse<AmendmentBodyResponse>> AmendmentBodyUpdated = null!;
        public event EventHandler<SignalRResponse<List<AmendmentBodyResponse>>> AmendmentBodyUpdatedMany = null!;
        public event EventHandler<SignalRResponse<SystemSettingResponse>> SystemSettingUpdated = null!;
        public event EventHandler ClearScreens = null!;

        public void OnAmendmentUpdated(SignalRResponse<AmendmentResponse> e)
        {
            AmendmentUpdated?.Invoke(this, e);
        }

        public void OnAmendmentBodyUpdated(SignalRResponse<AmendmentBodyResponse> e)
        {
            AmendmentBodyUpdated?.Invoke(this, e);
        }

        public void OnAmendmentBodyUpdatedMany(SignalRResponse<List<AmendmentBodyResponse>> e)
        {
            AmendmentBodyUpdatedMany?.Invoke(this, e);
        }

        public void OnSystemSettingUpdated(SignalRResponse<SystemSettingResponse> e)
        {
            SystemSettingUpdated?.Invoke(this, e);
        }

        public void OnClearScreens()
        {
            ClearScreens?.Invoke(this, EventArgs.Empty);
        }
    }
}
