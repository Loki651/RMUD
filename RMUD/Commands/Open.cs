﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RMUD.Commands
{
	internal class Open : CommandFactory
	{
        public override void Create(CommandParser Parser)
        {
            Parser.AddCommand(
                new Sequence(
                    new KeyWord("OPEN", false),
                    new FailIfNoMatches(
                        new ObjectMatcher("SUBJECT", new InScopeObjectSource(),
                             (actor, openable) =>
                             {
                                 if (openable is OpenableRules && !(openable as OpenableRules).Open)
                                     return MatchPreference.Likely;
                                 return MatchPreference.Unlikely;
                             }),
                        "I don't see that here.\r\n")),
                new OpenProcessor(),
                "Open something",
                "SUBJECT-SCORE");
        }
	}
	
	internal class OpenProcessor : ICommandProcessor
	{
		public void Perform(PossibleMatch Match, Actor Actor)
		{
			var target = Match.Arguments["SUBJECT"] as OpenableRules;
			if (target == null)
			{
				if (Actor.ConnectedClient != null) 
					Mud.SendMessage(Actor, "I don't think the concept of 'open' and 'closed' applies to that.\r\n");
			}
			else
			{
                if (!Mud.IsVisibleTo(Actor, target as MudObject))
            {
                if (Actor.ConnectedClient != null)
                    Mud.SendMessage(Actor, "That doesn't seem to be here anymore.\r\n");
                return;
            }

                var checkRule = target.CheckOpen(Actor);
				if (checkRule.Allowed)
				{
                    if (target.HandleOpen(Actor) == RuleHandlerFollowUp.Continue)
                    {
                        target.Open = true;

                        var thing = target as Thing;
                        if (thing != null)
                        {
                            Mud.SendMessage(Actor, MessageScope.Single, "You open " + thing.Definite + "\r\n");
                            Mud.SendMessage(Actor, MessageScope.External, Actor.Short + " opens " + thing.Definite + "\r\n");

                            var source = Match.Arguments["SUBJECT-SOURCE"] as String;
                            if (source == "LINK")
                            {
                                var location = Actor.Location as Room;
                                var link = location.Links.FirstOrDefault(l => Object.ReferenceEquals(target, l.Door));
                                if (link != null)
                                {
                                    var otherRoom = Mud.GetObject(link.Destination);
                                    if (otherRoom != null)
                                    {
                                        Mud.SendMessage(otherRoom as Room, String.Format("{0} opens {1}.\r\n", Actor.Short, thing.Definite));
                                        Mud.MarkLocaleForUpdate(otherRoom);
                                    }
                                }
                            }
                        }
                    }

                    Mud.MarkLocaleForUpdate(target as MudObject);
				}
				else
				{
					Mud.SendMessage(Actor, MessageScope.Single, checkRule.ReasonDisallowed + "\r\n");
				}
			}
		}
	}
}
