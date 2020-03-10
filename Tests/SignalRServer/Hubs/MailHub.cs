using System;
using System.Collections.Generic;
using System.Linq;
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
            Clients.All.authorize();
        }

        [Authorize(Roles = "Admin")]
        public void Admin()
        {
            Clients.All.authorize();
        }
    }
}