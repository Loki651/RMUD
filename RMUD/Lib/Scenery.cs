﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RMUD
{
	public class Scenery : MudObject, IDescribed, IMatchable, ITakeRules
	{
		public List<String> Nouns { get; set; }
		public DescriptiveText Long { get; set; }

		public Scenery()
		{
			Nouns = new List<string>();
		}

		bool ITakeRules.CanTake(Actor Actor)
		{
			return false;
		}

        RuleHandlerFollowUp ITakeRules.HandleTake(Actor Actor) { return RuleHandlerFollowUp.Continue; }
	}
}
