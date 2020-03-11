using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;

namespace SignalRServer.Hubs
{
    [Authorize]
    [HubName("mail")]
    public class MailHub : Hub
    {
        public void Hello()
        {
            Clients.User(Context.User.Identity.Name).hello();
        }

        public void Authorize()
        {
            Clients.Groups(new List<string> { "user", "user2" }).authorize();
            Clients.Clients(new List<string> { Context.ConnectionId, Context.ConnectionId + "01" }).authorize();
            Clients.Users(new List<string> { "user", "user2" }).authorize();
        }

        [Authorize(Roles = "Admin")]
        public void Admin()
        {
            Clients.All.authorize();
        }

        public override Task OnConnected()
        {
            Groups.Add(Context.ConnectionId, "user");

            return base.OnConnected();
        }
    }
}