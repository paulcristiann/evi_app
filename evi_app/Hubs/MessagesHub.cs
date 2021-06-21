using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EVI_App.Hubs
{
    public class MessagesHub : Hub
    {
        public async Task SendMessage(string user, string message, string fiscal_id)
        {
            await Clients.All.SendAsync("ReceiveMessage", user, message, fiscal_id);
        }
    }
}
