﻿namespace GfeCLIWoW
{
    public class Game
    {
        // https://github.com/Vladinator/wow-dbc-archive/blob/release/wow_latest/difficulty.csv
        private static readonly Dictionary<int, string> InstanceDifficulties = new()
        {
            { 1, "Normal" },
            { 2, "Heroic" },
            { 3, "10 Player" },
            { 4, "25 Player" },
            { 5, "10 Player (Heroic)" },
            { 6, "25 Player (Heroic)" },
            { 7, "Looking For Raid" },
            { 8, "Mythic Keystone" },
            { 9, "40 Player" },
            { 11, "Heroic Scenario" },
            { 12, "Normal Scenario" },
            { 14, "Normal" },
            { 15, "Heroic" },
            { 16, "Mythic" },
            { 17, "Looking For Raid" },
            { 18, "Event" },
            { 19, "Event" },
            { 20, "Event Scenario" },
            { 23, "Mythic" },
            { 24, "Timewalking" },
            { 25, "World PvP Scenario" },
            { 29, "PvEvP Scenario" },
            { 30, "Event" },
            { 32, "World PvP Scenario" },
            { 33, "Timewalking" },
            { 34, "PvP" },
            { 38, "Normal" },
            { 39, "Heroic" },
            { 40, "Mythic" },
            { 45, "PvP" },
            { 147, "Normal" },
            { 149, "Heroic" },
            { 150, "Normal" },
            { 151, "Looking For Raid" },
            { 152, "Visions of N'Zoth" },
            { 153, "Teeming Island" },
            { 167, "Torghast" },
            { 168, "Path of Ascension: Courage" },
            { 169, "Path of Ascension: Loyalty" },
            { 170, "Path of Ascension: Wisdom" },
            { 171, "Path of Ascension: Humility" },
            { 172, "World Boss" },
            { 192, "Challenge Level 1" }
        };

        public static string GetInstanceDifficultyName(int id)
        {
            if (InstanceDifficulties.TryGetValue(id, out var name))
            {
                return name;
            }
            return string.Empty;
        }
    }
}
