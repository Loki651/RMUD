﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RMUD.Commands
{
	internal class Force : CommandFactory
	{
        public override void Create(CommandParser Parser)
        {
            Parser.AddCommand(
                Sequence(
                    RequiredRank(500),
                    KeyWord("!FORCE"),
                    MustMatch("Whom do you wish to command?",
                        FirstOf(
                            Object("OBJECT", InScope),
                            Path("PATH"))),
                    Rest("COMMAND")))
                .Manual("An administrative command that allows you to execute a command as if you were another actor or player. The other entity will see all output from the command, and rules restricting their access to the command are considered.")
                .ProceduralRule((match, actor) =>
                    {
                        if (match.ContainsKey("PATH"))
                        {
                            var target = Mud.GetObject(match["PATH"].ToString());
                            if (target == null)
                            {
                                Mud.SendMessage(actor, "I can't find whomever it is you want to submit to your foolish whims.");
                                return PerformResult.Stop;
                            }
                            match.Upsert("OBJECT", target);
                        }
                        return PerformResult.Continue;
                    }, "Convert path to object rule.")
                .ProceduralRule((match, actor) =>
                {
                    MudObject target = match["OBJECT"] as MudObject;
                    
                    var targetActor = target as Actor;
                    if (targetActor == null)
                    {
                        Mud.SendMessage(actor, "You can order inanimate objects about as much as you like, they aren't going to listen.");
                        return PerformResult.Stop;
                    }

                    var command = match["COMMAND"].ToString();
                    var matchedCommand = Mud.ParserCommandHandler.Parser.ParseCommand(command, targetActor);

                    if (matchedCommand != null)
                    {
                        if (matchedCommand.Matches.Count > 1)
                            Mud.SendMessage(actor, "The command was ambigious.");
                        else
                        {
                            Mud.SendMessage(actor, "Enacting your will.");
                            Mud.ProcessPlayerCommand(matchedCommand.Command, matchedCommand.Matches[0], targetActor);
                        }
                    }
                    else
                        Mud.SendMessage(actor, "The command did not match.");

                    return PerformResult.Continue;
                });
        }
	}
}
