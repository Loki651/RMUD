﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Reflection;

namespace RMUD
{
    public static partial class Core
    {
        internal static bool SilentFlag = false;

        internal static bool OutputQueryTriggered = false;

        public static void BeginOutputQuery()
        {
            OutputQueryTriggered = false;
        }

        public static bool CheckOutputQuery()
        {
            return OutputQueryTriggered;
        }

        public static String UnformattedItemList(int StartIndex, int Count)
        {
            var builder = new StringBuilder();
            for (int i = StartIndex; i < StartIndex + Count; ++i)
            {
                builder.Append("<a" + i + ">");
                if (i != (StartIndex + Count - 1)) builder.Append(", ");
            }
            return builder.ToString();
        }
    }

    public partial class MudObject
    {
        public static void SendMessage(Actor Actor, String Message, params Object[] MentionedObjects)
        {
            if (String.IsNullOrEmpty(Message)) return;
            if (Core.SilentFlag) return;
            Core.OutputQueryTriggered = true;

            if (Actor != null && Actor.ConnectedClient != null)
                Core.PendingMessages.Add(new RawPendingMessage(Actor.ConnectedClient, Core.FormatMessage(Actor, Message, MentionedObjects)));
        }

        public static void SendMessage(MudObject MudObject, String Message, params Object[] MentionedObjects)
        {
            if (String.IsNullOrEmpty(Message)) return;
            if (Core.SilentFlag) return;
            Core.OutputQueryTriggered = true;

            if (MudObject is Actor)
                SendMessage(MudObject as Actor, Message, MentionedObjects);
        }

        public static void SendLocaleMessage(MudObject Object, String Message, params Object[] MentionedObjects)
        {
            if (String.IsNullOrEmpty(Message)) return;
            if (Core.SilentFlag) return;
            Core.OutputQueryTriggered = true;

            var container = MudObject.FindLocale(Object) as Container;
            if (container != null)
                foreach (var actor in container.EnumerateObjects<Actor>().Where(a => a.ConnectedClient != null))
                    Core.PendingMessages.Add(new RawPendingMessage(actor.ConnectedClient, Core.FormatMessage(actor, Message, MentionedObjects)));
        }

        public static void SendExternalMessage(Actor Actor, String Message, params Object[] MentionedObjects)
        {
            if (String.IsNullOrEmpty(Message)) return;
            if (Core.SilentFlag) return;
            Core.OutputQueryTriggered = true;

            if (Actor == null) return;
            var location = Actor.Location as Room;
            if (location == null) return;

            foreach (var other in location.EnumerateObjects<Actor>().Where(a => !Object.ReferenceEquals(a, Actor) && (a.ConnectedClient != null)))
                Core.PendingMessages.Add(new RawPendingMessage(other.ConnectedClient, Core.FormatMessage(other, Message, MentionedObjects)));
                
        }

        public static void SendExternalMessage(MudObject Actor, String Message, params Object[] MentionedObjects)
        {
            if (String.IsNullOrEmpty(Message)) return;
            if (Core.SilentFlag) return;
            Core.OutputQueryTriggered = true;

            SendExternalMessage(Actor as Actor, Message, MentionedObjects);
        }


        public static void SendMessage(Client Client, String Message, params Object[] MentionedObjects)
        {
            if (String.IsNullOrEmpty(Message)) return;
            if (Core.SilentFlag) return;
            Core.OutputQueryTriggered = true;

            Core.PendingMessages.Add(new RawPendingMessage(Client, Core.FormatMessage(Client.Player, Message, MentionedObjects)));
        }
    }
}