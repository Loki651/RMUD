﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RMUD;

namespace StandardActionsModule
{
	internal class Examine : CommandFactory
	{
        public override void Create(CommandParser Parser)
        {
            Parser.AddCommand(
                Sequence(
                    Or(
                        Or(KeyWord("EXAMINE"), KeyWord("X")),
                        Sequence(
                            Or(KeyWord("LOOK"), KeyWord("L")),
                            KeyWord("AT"))),
                    Object("OBJECT", InScope)))
                .Manual("Take a close look at an object.")
                .Check("can examine?", "ACTOR", "OBJECT")
                .Perform("describe", "ACTOR", "OBJECT");
        }

        public static void AtStartup(RuleEngine GlobalRules)
        {
            GlobalRules.DeclareCheckRuleBook<MudObject, MudObject>("can examine?", "[Actor, Item] : Can the viewer examine the item?", "actor", "item");
            GlobalRules.DeclarePerformRuleBook<MudObject, MudObject>("describe", "[Actor, Item] : Generates descriptions of the item.", "actor", "item");

            GlobalRules.Check<MudObject, MudObject>("can examine?")
                .First
                .Do((viewer, item) => MiscCustomization.CheckIsVisibleTo(viewer, item))
                .Name("Can't examine what isn't here rule.");

            GlobalRules.Check<MudObject, MudObject>("can examine?")
                .Last
                .Do((viewer, item) => CheckResult.Allow)
                .Name("Default can examine everything rule.");

            GlobalRules.Perform<MudObject, MudObject>("describe")
                .When((viewer, item) => !String.IsNullOrEmpty(item.Long))
                .Do((viewer, item) =>
                {
                    MudObject.SendMessage(viewer, item.Long);
                    return PerformResult.Continue;
                })
                .Name("Basic description rule.");

            GlobalRules.Perform<MudObject, MudObject>("describe")
                .When((viewer, item) => GlobalRules.ConsiderValueRule<bool>("openable", item))
                .Do((viewer, item) =>
                {
                    if (GlobalRules.ConsiderValueRule<bool>("is-open", item))
                        MudObject.SendMessage(viewer, "It's open.", item);
                    else
                        MudObject.SendMessage(viewer, "It's closed.", item);
                    return PerformResult.Continue;
                })
                .Name("Describe open or closed state rule.");

            GlobalRules.Perform<MudObject, MudObject>("describe")
                .When((viewer, item) => (item is Container) && ((item as Container).LocationsSupported & RelativeLocations.On) == RelativeLocations.On)
                .Do((viewer, item) =>
                {
                    var contents = (item as Container).GetContents(RelativeLocations.On);
                    if (contents.Count() > 0)
                    {
                        contents.Insert(0, item);
                        MudObject.SendMessage(viewer, "There's also " + Core.UnformattedItemList(1, contents.Count - 1) + " on it.", contents.ToArray());
                    }
                    return PerformResult.Continue;
                })
                .Name("List things on container in description rule.");

            GlobalRules.Perform<MudObject, MudObject>("describe")
                .When((viewer, item) => 
                    {
                        if (!(item is Container)) return false;
                        if (!GlobalRules.ConsiderValueRule<bool>("open?", item)) return false;
                        if ((item as Container).EnumerateObjects(RelativeLocations.In).Count() == 0) return false;
                        return true;
                    })
                .Do((viewer, item) =>
                {
                    var contents = (item as Container).GetContents(RelativeLocations.In);
                    if (contents.Count() > 0)
                    {
                        contents.Insert(0, item);
                        MudObject.SendMessage(viewer, "Inside it there's " + Core.UnformattedItemList(1, contents.Count - 1) + ".", contents.ToArray());
                    }
                    return PerformResult.Continue;
                })
                .Name("List things in open container in description rule.");
        }
    }
}
