using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

public class NotificationHub : Hub
{
    // Phương thức này sẽ gửi thông báo đến các client đã kết nối với hub
    public async Task SendOrderNotification(int orderId)
    {
        // Gửi thông báo đến tất cả các admin (hoặc người dùng cần nhận thông báo)
        await Clients.All.SendAsync("ReceiveNotification", orderId);
    }
}
