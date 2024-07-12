#nullable enable
using System;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Characters;
using Constants;
using System.Reflection;
using System.Xml.Linq;
using StardewValley.Tools;
using Microsoft.Xna.Framework.Graphics;
using System.Diagnostics;

namespace Aging
{
    /// <summary>The mod entry point.</summary>
    internal sealed class ModEntry : Mod
    {
        List<NPC> villagers;
        List<string> ageableVillagers = new List<string>();

        IModHelper helper;

        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            this.helper = helper;

            helper.Events.GameLoop.DayStarted += this.OnDayStarted;
            helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Raised after the player loads a save slot and the world is initialized.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            this.villagers = this.GetVillagers();
            this.SetAges();
        }

        /// <summary>Raised after the game begins a new day (including when the player loads a save).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnDayStarted(object? sender, DayStartedEventArgs e)
        {
            foreach (NPC npc in villagers)
            {
                if (ageableVillagers.Contains(npc.Name) && npc.isBirthday())
                {
                    npc.Age++;
                    this.Monitor.Log($"Aging {npc.Name} to {npc.Age}. Happy Birthday!", LogLevel.Debug);
                }
            }
        }

        /// <summary>Simplified method from GitHub user zeldela. https://github.com/zeldela/sdv-mods/blob/06d8e2cf2ce54207dd87a10aa1dcb5645e552df5/sdv-mods/RememberBirthdays/BirthdayHandler.cs#L96</summary>
        private List<NPC> GetVillagers()
        {
            List<NPC> npcs = new List<NPC>();

            foreach (GameLocation location in Game1.locations)
            {
                foreach (NPC npc in location.characters)
                {
                    npcs.Add(npc);
                }
            }
            this.Monitor.Log("GetVillagers Done", LogLevel.Debug);
            this.Monitor.Log($"GetVillagers {npcs.Count}", LogLevel.Debug);

            return npcs;
        }

        /// <summary>Sets villagers with birthdays ages based on current date</summary>
        private void SetAges(int ageModifier = 0)
        {
            foreach (NPC npc in villagers)
            {
                if (npc.Birthday_Day != 0)
                {
                    this.Monitor.Log($"Setting age for {npc.Name}", LogLevel.Debug);
                    HandleAge(npc);
                }
            }
        }

        private void HandleAge(NPC npc)
        {

            FieldInfo field = typeof(Ages).GetField(npc.Name, BindingFlags.Public | BindingFlags.Static);
            if (field != null)
            {
                ageableVillagers.Add(npc.Name);
                int age = (int)field.GetValue(null);

                int ageModifier = Game1.year - 1;

                if (IsAfterBirthday(npc)) ageModifier++;
                if (npc.Name == "Alex") age = 56;

                this.Monitor.Log($"{npc.Name} age is {age + ageModifier}", LogLevel.Debug);
                npc.Age = age + ageModifier;

                SetPortrait(npc);
            }
            else
            {
                this.Monitor.Log($"{npc.Name} is not found in Ages class.", LogLevel.Debug);
            }
        }

        private void SetPortrait(NPC npc)
        {
            double x = npc.Age / 5;
            int y = (int)Math.Floor((double)(npc.Age / 5));
            this.Monitor.Log($"age {npc.Age} x {x} y {y}", LogLevel.Debug);
            int ageMilestone = (int)Math.Floor((double)(npc.Age / 5)) * 5;
            //string filePath = $"C:\\Users\\amybc\\Documents\\Projects\\StardewValleyMods\\Aging\\Aging\\Portraits\\{npc.Name}\\{npc.Name}_{ageMilestone}.png";
            string filePath = Path.Combine(this.helper.DirectoryPath, $"Portraits\\{npc.Name}\\{npc.Name}_{ageMilestone}.png");

            if (File.Exists(filePath))
            {
                npc.Portrait = this.helper.ModContent.Load<Texture2D>(filePath);
            }
            else
            {
                this.Monitor.Log(filePath + " does not exist", LogLevel.Debug);
            }
        }

        private bool IsAfterBirthday(NPC npc)
        {
            // this.Monitor.Log($"{npc.Name} birthday is {npc.Birthday_Day} {npc.Birthday_Season}", LogLevel.Debug);
            // this.Monitor.Log($"day is {Game1.dayOfMonth} {(int)Game1.season}  birthday is {npc.Birthday_Day} {SeasonOrder(npc.Birthday_Season)}", LogLevel.Debug);
            return ((int)Game1.season > SeasonOrder(npc.Birthday_Season) ||
                (SeasonOrder(npc.Birthday_Season) == (int)Game1.season && Game1.dayOfMonth > npc.Birthday_Day));
        }

        private int SeasonOrder(string season)
        {
            Enum.TryParse(char.ToUpper(season[0]) + season.Substring(1), out Season pSeason);
            // this.Monitor.Log($"{season} in {pSeason} out", LogLevel.Debug);

            switch (pSeason)
            {
                case Season.Spring: return 0;
                case Season.Summer: return 1;
                case Season.Fall: return 2;
                case Season.Winter: return 3;
                default: return 4;
            }
        }
    }
}