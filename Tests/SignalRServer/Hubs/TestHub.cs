using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;

namespace SignalRServer.Hubs
{
    [HubName("test")]
    public class TestHub : Hub
    {
        public void Hello()
        {
            Clients.All.hello();
        }

        [Authorize]
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