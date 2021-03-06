﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RMUD
{
    public class Actor : Container
    {
        public ClientCommandHandler CommandHandler;
        public Client ConnectedClient;
        public int Rank;

        [Persist(typeof(EnumSerializer<Gender>))]
        public Gender Gender { get; set; }

        public Actor()
            : base(RelativeLocations.Held | RelativeLocations.Worn, RelativeLocations.Held)
        {
            Gender = RMUD.Gender.Male;
            Nouns.Add("MAN", (a) => a.Gender == RMUD.Gender.Male);
            Nouns.Add("WOMAN", (a) => a.Gender == RMUD.Gender.Female);
        }

    }
}
