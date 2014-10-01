﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RMUD
{
    public enum EnumerateObjectsControl
    {
        Stop,
        Continue
    }

    public enum EnumerateObjectsDepth
    {
        None,
        Shallow,
        Deep
    }

    public static partial class Mud
    {
        public static EnumerateObjectsControl EnumerateObjects(MudObject Source, EnumerateObjectsDepth Depth, Func<MudObject, RelativeLocations, EnumerateObjectsControl> Callback)
        {
            var container = Source as IContainer;
            if (container == null) return EnumerateObjectsControl.Continue;

            return container.EnumerateObjects(RelativeLocations.Everything, (subObject, loc) =>
            {
                if (Callback(subObject, loc) == EnumerateObjectsControl.Stop) return EnumerateObjectsControl.Stop;

                if (Depth == EnumerateObjectsDepth.Deep)
                {
                    if (EnumerateObjects(subObject, EnumerateObjectsDepth.Deep, Callback) == EnumerateObjectsControl.Stop)
                        return EnumerateObjectsControl.Stop;
                }
                else if (Depth == EnumerateObjectsDepth.Shallow)
                {
                    if (EnumerateObjects(subObject, EnumerateObjectsDepth.None, Callback) == EnumerateObjectsControl.Stop)
                        return EnumerateObjectsControl.Stop;
                }

                return EnumerateObjectsControl.Continue;
            });
        }
    }

}