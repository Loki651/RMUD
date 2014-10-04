﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RMUD
{
    internal class Path : CommandTokenMatcher
    {
        public String ArgumentName;

        internal Path(String ArgumentName)
        {
			this.ArgumentName = ArgumentName;
        }

        public List<PossibleMatch> Match(PossibleMatch State, MatchContext Context)
        {
            var r = new List<PossibleMatch>();
            if (State.Next != null)
                r.Add(State.AdvanceWith(ArgumentName, State.Next.Value));
			return r;
        }

		public String Emit() { return "[PATH]"; }
    }
}
