using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace DomusMercatorisDotnetRest.Hubs
{
    public class PaymentHub : Hub
    {
        // This method can be called by client to join a group specific to an order
        public async Task JoinOrderGroup(string orderId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, orderId);
        }

        public async Task JoinCompanyGroup(int companyId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Company-{companyId}");
        }

        public async Task LeaveOrderGroup(string orderId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, orderId);
        }
    }
}
