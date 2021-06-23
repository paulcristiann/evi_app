
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EVI_App.Models;
using Microsoft.AspNetCore.SignalR;
using EVI_App.Hubs;
using evi_app.Data;

namespace EVI_App.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MessagesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<MessagesHub> _hubContext;

        public MessagesController(ApplicationDbContext context, IHubContext<MessagesHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        // POST: api/Messages
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Message>> PostMessage(Message message)
        {
            var evi_user_id = _context.UserCorespondence.Where(x => x.SavtaId == message.UserId).First().EviId;
            var evi_username = _context.Users.Where(x => x.Id == evi_user_id).First().Email;

            message.Timestamp = DateTime.Now;
            message.UserId = evi_username;
            _context.MessagesItems.Add(message);
            await _context.SaveChangesAsync();

            //Send to the signalr hub
            await _hubContext.Clients.All.SendAsync("ReceiveMessage", evi_username, message.Xml, message.FiscalTaxId);

            return Ok();
        }

        private bool MessageExists(long id)
        {
            return _context.MessagesItems.Any(e => e.Id == id);
        }
    }
}
