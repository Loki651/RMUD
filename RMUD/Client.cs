﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RMUD
{
    public class Client
    {
        public virtual void Send(String message) { }
        public virtual void Disconnect() { }

        public Actor Player;
		public IClientCommandHandler CommandHandler;
    }
}
