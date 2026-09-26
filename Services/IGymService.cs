using ADHUNIK_BARI.Models;
using ADHUNIK_BARI.ViewModels;

namespace ADHUNIK_BARI.Services
{
    public interface IGymService
    {
        Task<GymSetting> GetGymSettingsAsync();

        Task<bool> UpdateGymSettingsAsync(GymSettingsViewModel model, string? managerUserId);

        Task<ResidentGymDashboardViewModel> GetResidentGymDashboardAsync(string userId);

        Task<(bool Success, string Message)> ApplyForGymMembershipAsync(string userId, GymApplicationRequest request);

        Task<(bool Success, string Message)> RequestGymRenewalAsync(string userId, int membershipId);

        Task<(bool Success, string Message)> RequestGymCancellationAsync(string userId, GymCancellationRequest request);

        Task<ManagerGymDashboardViewModel> GetManagerGymDashboardAsync();

        Task<(bool Success, string Message)> ApproveMembershipAsync(string managerUserId, GymApprovalRequest request);

        Task<(bool Success, string Message)> RejectMembershipAsync(string managerUserId, GymRejectionRequest request);

        Task<(bool Success, string Message)> CancelMembershipByManagerAsync(string managerUserId, int membershipId, string? reason);

        Task<(bool Success, string Message)> RenewMembershipByManagerAsync(string managerUserId, int membershipId, int durationMonths = 1);

        Task<GymPaySlipViewModel?> GetGymPaySlipDetailsAsync(int membershipId, string? requesterUserId, bool isManager);

        Task<(bool Success, string Message)> PayGymPaySlipAsync(string userId, GymPaymentRequest request);

        Task<(bool Success, string Message)> ConfirmGymStripePaymentAsync(string userId, int membershipId, string? paymentIntentId);

        Task<(bool Success, string Message)> MarkGymFeePaidAsync(string managerUserId, int membershipId, string? paymentMethod = "Cash to Management");

        Task<GymIdCardViewModel?> GetGymIdCardDetailsAsync(int membershipId, string? requesterUserId, bool isManager, string? baseUrl = null);

        Task<GymVerificationViewModel> VerifyGymPassAsync(string code);

        Task<(bool Success, string Message)> UpdateMemberPhotoAsync(string? requesterUserId, int membershipId, string photoUrl, bool isManager = false);

        Task<int> GetPendingMembershipRequestsCountAsync();

        Task<int> GetResidentPendingGymCountAsync(string userId);
    }
}
