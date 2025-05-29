﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Amendment.Client.Repository.Infrastructure;
using Amendment.Shared;
using Amendment.Shared.Requests;
using Amendment.Shared.Responses;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Amendment.Client.Repository
{
    public interface IAmendmentRepository : IHttpRepository<AmendmentRequest, AmendmentResponse>
    {
        Task<AmendmentFullBodyResponse?> GetLiveAsync();
        Task<bool> ExportToExcelAsync(List<int> amendmentIds);
        Task<bool> ExportToExcelAsync(int amendmentId);
        Task<bool> ExportToPdfAsync(List<int> amendmentIds);
        Task<bool> ExportToPdfAsync(int amendmentId);
    }
    public class AmendmentRepository : HttpRepository<AmendmentRequest, AmendmentResponse>, IAmendmentRepository
    {
        private readonly ILogger<AmendmentRepository> _logger;
        private readonly INotificationServiceWrapper _notificationServiceWrapper;
        private readonly IJSRuntime _jsRuntime;
        protected override string BaseUrl { get; set; } = "api/Amendment";

        public AmendmentRepository(ILogger<AmendmentRepository> logger, HttpClient client,
            INotificationServiceWrapper notificationServiceWrapper, IJSRuntime jsRuntime)
            : base(logger, client, notificationServiceWrapper)
        {
            _logger = logger;
            _notificationServiceWrapper = notificationServiceWrapper;
            _jsRuntime = jsRuntime;
        }

        public async Task<AmendmentFullBodyResponse?> GetLiveAsync()
        {
            var url = $"api/Amendment/Live";
            try
            {
                var response = await Client.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();
                response.EnsureSuccessStatusCode();

                var result = JsonSerializer.Deserialize<ApiResult<AmendmentFullBodyResponse?>>(content, Options);
                return result?.Result;
            }
            catch (Exception e)
            {
                _logger.LogError(new EventId(1000, "RepositoryError"), e, "An error has occurred while trying to trying to access this url: GET {url}", url);
                await _notificationServiceWrapper.Error("An error has occurred. Please try again.", "Server Error");
                return null;
            }
        }

        public async Task<bool> ExportToExcelAsync(int amendmentId)
        {
            return await ExportToExcelAsync(new List<int> { amendmentId });
        }

        public async Task<bool> ExportToExcelAsync(List<int> amendmentIds)
        {
            return await ExportFileAsync(amendmentIds, "ExportExcel", "Excel", "Amendments.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        }

        public async Task<bool> ExportToPdfAsync(int amendmentId)
        {
            return await ExportToPdfAsync(new List<int> { amendmentId });
        }

        public async Task<bool> ExportToPdfAsync(List<int> amendmentIds)
        {
            return await ExportFileAsync(amendmentIds, "ExportPdf", "PDF", "Amendments.pdf", "application/pdf");
        }

        private async Task<bool> ExportFileAsync(List<int> ids, string endpoint, string fileType, string fileName, string contentType)
        {
            var url = $"{BaseUrl}/{endpoint}";
            try
            {
                // Create a POST request with the amendment IDs
                var response = await Client.PostAsJsonAsync(url, ids);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError(new EventId(1000, "ExportError"), $"Failed to export amendments to {fileType}: {{ErrorContent}}", errorContent);
                    await _notificationServiceWrapper.Error($"Failed to export amendments to {fileType}.", "Export Error");
                    return false;
                }

                // Get the file as a byte array
                var fileBytes = await response.Content.ReadAsByteArrayAsync();

                // Use JSRuntime to save the file
                await SaveAsFile(fileName, contentType, fileBytes);

                await _notificationServiceWrapper.Success($"Amendments exported to {fileType} successfully.", "Export Complete");
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(new EventId(1000, "RepositoryError"), e, "An error has occurred while trying to export amendments: POST {url}", url);
                await _notificationServiceWrapper.Error($"An error has occurred during {fileType} export. Please try again.", "Export Error");
                return false;
            }
        }

        private async Task SaveAsFile(string fileName, string contentType, byte[] data)
        {
            // Use JSRuntime to trigger a file download
            await _jsRuntime.InvokeVoidAsync("downloadFileFromBytes", fileName, contentType, Convert.ToBase64String(data));
        }
    }
}
