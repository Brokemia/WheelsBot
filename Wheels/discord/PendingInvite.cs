using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WheelsGodot.discord {
    public class PendingInvite {
        public SocketUser From { get; set; }

        public SocketUser To { get; set; }
        
        public Rules Ruleset { get; set; }
    }
}
