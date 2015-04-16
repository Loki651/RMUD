﻿using System.Linq;
using System.Collections.Generic;
using System;
using RMUD;

namespace AdminModule
{
    internal class Sonar : CommandFactory
    {
        private static int MapWidth = 50;
        private static int MapHeight = 25;

        public override void Create(CommandParser Parser)
        {
            Parser.AddCommand(KeyWord("SONAR"))
                .ProceduralRule((match, actor) =>
                {

                    var builder = new System.Text.StringBuilder();

                    var mapGrid = new int[MapWidth, MapHeight];
                    for (int y = 0; y < MapHeight; ++y)
                        for (int x = 0; x < MapWidth; ++x)
                            mapGrid[x, y] = ' ';

                    for (int y = 1; y < MapHeight - 1; ++y)
                    {
                        mapGrid[0, y] = '|';
                        mapGrid[MapWidth - 1, y] = '|';
                    }

                    for (int x = 1; x < MapWidth - 1; ++x)
                    {
                        mapGrid[x, 0] = '-';
                        mapGrid[x, MapHeight - 1] = '-';
                    }

                    mapGrid[0, 0] = '+';
                    mapGrid[0, MapHeight - 1] = '+';
                    mapGrid[MapWidth - 1, 0] = '+';
                    mapGrid[MapWidth - 1, MapHeight - 1] = '+';

                    var roomLegend = new Dictionary<int, String>();

                    if (actor.Location is RMUD.Room)
                        MapLocation(mapGrid, roomLegend, (MapWidth / 2), (MapHeight / 2), actor.Location as RMUD.Room, '@');

                    for (int y = 0; y < MapHeight; ++y)
                    {
                        for (int x = 0; x < MapWidth; ++x)
                            builder.Append((char)mapGrid[x, y]);
                        builder.Append("\r\n");
                    }

                    foreach (var entry in roomLegend)
                        builder.Append((char)entry.Key + " - " + entry.Value + "\r\n");

                    MudObject.SendMessage(actor, builder.ToString());
                    return RMUD.PerformResult.Continue;
                }, "Implement sonar device rule.");
        }

        private static void MapLocation(int[,] MapGrid, Dictionary<int, String> RoomLegend, int X, int Y, RMUD.Room Location, int Symbol)
        {
            if (X < 1 || X >= MapWidth - 1 || Y < 1 || Y >= MapHeight - 1) return;

            if (MapGrid[X, Y] != ' ') return;
            if (Symbol == ' ')
            {
                var spacer = Location.Short.LastIndexOf('-');
                if (spacer > 0 && spacer < Location.Short.Length - 2)
                    Symbol = Location.Short.ToUpper()[spacer + 2];
                else
                    Symbol = Location.Short.ToUpper()[0];
            }

            RoomLegend.Upsert(Symbol, Location.Short);
            MapGrid[X, Y] = Symbol;

            foreach (var link in Location.EnumerateObjects(RMUD.RelativeLocations.Links).Where(t => t.HasProperty("direction")))
            {
                var destinationName = link.GetProperty<string>("destination");
                var destination = MudObject.GetObject(destinationName) as RMUD.Room;
                var direction = link.GetPropertyOrDefault<RMUD.Direction>("direction", RMUD.Direction.NORTH);
                var directionVector = RMUD.Link.GetAsVector(direction);
                PlaceEdge(MapGrid, X + directionVector.X, Y + directionVector.Y, direction);

                if (destination.RoomType == Location.RoomType)
                    MapLocation(MapGrid, RoomLegend, X + (directionVector.X * 3), Y + (directionVector.Y * 3), destination, ' ');
            }
        }

        private static void PlaceEdge(int[,] MapGrid, int X, int Y, RMUD.Direction Direction)
        {
            if (X < 1 || X >= MapWidth - 1 || Y < 1 || Y >= MapHeight - 1) return;

            switch (Direction)
            {
                case RMUD.Direction.NORTH:
                case RMUD.Direction.SOUTH:
                    MapGrid[X, Y] = '|';
                    break;
                case RMUD.Direction.EAST:
                case RMUD.Direction.WEST:
                    MapGrid[X, Y] = '-';
                    break;
                case RMUD.Direction.NORTHEAST:
                case RMUD.Direction.SOUTHWEST:
                    MapGrid[X, Y] = '/';
                    break;
                case RMUD.Direction.NORTHWEST:
                case RMUD.Direction.SOUTHEAST:
                    MapGrid[X, Y] = '\\';
                    break;
                default:
                    MapGrid[X, Y] = '*';
                    break;
            }
        }
    }
}