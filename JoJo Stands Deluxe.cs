using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using SFDGameScriptInterface;

namespace SFDScript
{
    public partial class GameScript : GameScriptInterface
    {
        public GameScript() : base(null) { }

        /* -======================-SCRIPT START-======================- */

        /*

                  _            _          _____ _                  _       _____       _                
                 | |          | |        / ____| |                | |     |  __ \     | |               
                 | | ___      | | ___   | (___ | |_ __ _ _ __   __| |___  | |  | | ___| |_   ___  _____ 
             _   | |/ _ \ _   | |/ _ \   \___ \| __/ _` | '_ \ / _` / __| | |  | |/ _ \ | | | \ \/ / _ \
            | |__| | (_) | |__| | (_) |  ____) | || (_| | | | | (_| \__ \ | |__| |  __/ | |_| |>  <  __/
             \____/ \___/ \____/ \___/  |_____/ \__\__,_|_| |_|\__,_|___/ |_____/ \___|_|\__,_/_/\_\___|

                                                  _           _  _          __        
                                                 | |__ _  _  | \| |___ ___ / _|___ ___
                                                 | '_ \ || | | .` / -_) _ \  _/ _ (_-<
                                                 |_.__/\_, | |_|\_\___\___/_| \___/__/
                                                       |__/                           

        */

        /* 
           A MESSAGE TO CURIOUS ONES: 
           Thank you for downloading this script. The following code is filled with non-
           professionalism and questionable solutions, so maybe you don't want to dig too
           much here, but if you don't really care, you can use this code for whatever
           you need. I myself have used some of the code from Ebomb09's Bullets Deluxe/
           Grenades Deluxe scripts to implement some of the mechanics (Falling crates in
           the SD mode, Homing feature for the Emperor), also the name of this script is
           inspired by them. Ideas for stands were provided by denisrad4. He is also the
           main tester. And the author of the thumbnail.
        */

        #region -===================-DEFAULT SETTINGS-===================-

        // Game mode by default.
        private static Modes Mode = Modes.RoundStartStand;

        // Crate spawn chance by default in SD mode.
        public static int SDCrateSpawnChance = 75;

        // Stand start chance by default in RSS mode.
        public static int RSSStandStartChance = 40;

        // String representations of Modes enumeration constants.
        private static Dictionary<Modes, string> modeNames = new Dictionary<Modes, string>
        {
            { Modes.StandDeluxe, "Stand Deluxe (SD)" }, // Stand Deluxe crate idea by Ebomb09
            { Modes.RoundStartStand, "Round Start Stand (RSS)" },
            { Modes.Base, "Base" },
            { Modes.Off, "Off" }
        };

        // List of stands never to be seen in-game.
        public static List<string> BannedStands = new List<string>
        {

        };

        #endregion -=====================================================-

        // Every existing stand's name.
        public static readonly string[] StandNames =
        {
            "Magician's Red", "Hierophant Green", "Silver Chariot",
            "Ebony Devil", "Emperor", "Lovers", "Anubis", "Star Platinum",
            "The World", "Notorious B.I.G", "Bad Company", "Crazy Diamond",
            "The Hand", "Killer Queen", "Little Feet", "Purple Haze",
            "King Crimson", "Sticky Fingers", "Gold Experience"
        };

        public static readonly string[] StandReqNames =
{
            "Silver Chariot REQ", "Lovers REQ", "Gold Experience REQ"
        };

        /* Non-editable list of stands blocked for obtaining to prevent
           different players from getting the same stands in the same round. */
        public static List<string> BlockedStands = new List<string>();

        // List of players and their stands.
        public static Dictionary<IPlayer, Stand> PlayerStandList = new Dictionary<IPlayer, Stand>();

        // List of players and their stand bows.
        public static Dictionary<IPlayer, StandBow> PlayerStandBowList = new Dictionary<IPlayer, StandBow>();

        public static void OnStartup()
        {
            RestoreConfig();

            Game.ShowChatMessage("JoJo Stands Deluxe script by Neofos (realization) and " +
                "denisrad4 (ideas). " + string.Format("Current Mode: {0}. ", modeNames[Mode]) +
                "Use /JJSDHELP to get a list of commands. Use /STAND to see info about your stand.", Color.Yellow);

            // Start listening to players' command input.
            Events.UserMessageCallback.Start(Commands);

            // Remove the stand from the player if they're dead.
            Events.PlayerDeathCallback.Start((plr) =>
            { if (PlayerStandList.ContainsKey(plr) && !(PlayerStandList[plr] is Anubis)) PlayerStandList[plr].FinalizeTheStand(); });

            // Activate the ability if the player having the stand pressed the right keys.
            Events.PlayerKeyInputCallback.Start((player, keyEvents) =>
            { if (PlayerStandList.ContainsKey(player)) PlayerStandList[player].ActivateAbilities(player, keyEvents); });
        }

        public static void AfterStartup()
        {
            // Perform actions that depend on the game mode.
            switch (Mode)
            {
                // Give random player a stand bow if the game mode is base.
                case Modes.Base:
                    IPlayer[] players = Game.GetPlayers();
                    IPlayer randomPlayer = players[new Random().Next(0, players.Length)];
                    StandBow standBow = new StandBow(randomPlayer);
                    PlayerStandBowList.Add(randomPlayer, standBow);
                    break;
                // Give all players random stands if the game mode is RSS.
                case Modes.RoundStartStand:
                    Random rnd = new Random();
                    IPlayer[] plrs = Game.GetPlayers();
                    foreach (IPlayer plr in plrs)
                    {
                        if (rnd.Next(1, 101) <= RSSStandStartChance)
                        {
                            Stand searchedStand = Stand.GetStand(plr, "RANDOM");

                            if (searchedStand != null)
                                PlayerStandList.Add(plr, searchedStand);
                        }
                    }
                    break;
                // Start spawning stand crates if the game mode is SD.
                case Modes.StandDeluxe:
                    Events.ObjectCreatedCallback.Start(StandCrate.OnObjectCreated);
                    break;
            }

        }

        // Restoring the stuff remembered in the previous round.
        private static void RestoreConfig()
        {
            if (Game.LocalStorage.ContainsKey("MODE"))
                Mode = (Modes)Enum.Parse(typeof(Modes), (string)Game.LocalStorage.GetItem("MODE"));

            if (Game.LocalStorage.ContainsKey("BANNEDSTANDS"))
                BannedStands = ((string[])Game.LocalStorage.GetItem("BANNEDSTANDS")).ToList();

            if (Game.LocalStorage.ContainsKey("JJSDCRATECHANCE"))
                StandCrate.SpawnCrateChance = ((int)Game.LocalStorage.GetItem("JJSDCRATECHANCE"));

            if (Game.LocalStorage.ContainsKey("RSSSTANDCHANCE"))
                RSSStandStartChance = ((int)Game.LocalStorage.GetItem("RSSSTANDCHANCE"));
        }

        // Actions in response on custom commands.
        public static void Commands(UserMessageCallbackArgs args)
        {
            if (args.IsCommand)
            {
                string argumentUpper = args.CommandArguments.ToUpper();

                // Moderator's commands.
                if (args.User.IsModerator)
                {
                    switch (args.Command)
                    {
                        #region JJSDHELP command
                        case "JJSDHELP":
                            string[] commandos = new string[]
                            {
                                "/MODE MODEABBREVIATION - change the mode of the script",
                                "/STANDGIVE PLAYERNAME STANDNAME/RANDOM - give a stand to the player. " +
                                    "Add \"REQ\" after the STANDNAME to recieve requiem if available",
                                "/STANDREMOVE PLAYERNAME - remove a stand from the player",
                                "/STANDBAN STANDNAME - ban a stand",
                                "/STANDUNBAN STANDNAME - unban a stand",
                                "/STANDBOWGIVE PLAYERNAME - give a stand bow to the player",
                                "/CRATECHANCE 0-100 - increases or decreases the chance of a box " +
                                    "with a stand falling in SD mode",
                                "/CGS 0-100 - adjusts the chance of getting a stand at the beginning " +
                                    "of the round in RSS mode"
                            };

                            foreach (string commando in commandos)
                                Game.ShowChatMessage(commando, Color.Magenta, args.User.UserIdentifier);
                            break;
                        #endregion

                        #region MODE command
                        case "MODE":
                            Modes? newMode;

                            switch (argumentUpper)
                            {
                                case "SD":
                                    newMode = Modes.StandDeluxe;
                                    break;
                                case "RSS":
                                    newMode = Modes.RoundStartStand;
                                    break;
                                case "BASE":
                                    newMode = Modes.Base;
                                    break;
                                case "OFF":
                                    newMode = Modes.Off;
                                    break;
                                default:
                                    newMode = null;
                                    break;
                            }

                            Mode = newMode == null ? Mode : (Modes)newMode;

                            if (newMode != null)
                                Game.ShowChatMessage(string.Format("Mode set to {0}", modeNames[Mode]), Color.Cyan);
                            else
                                Game.ShowChatMessage(string.Format("Modes available: SD, RSS, Base, Off. Current mode: {0}",
                                    modeNames[Mode]), Color.Magenta, args.User.UserIdentifier);

                            break;
                        #endregion

                        #region STANDGIVE command
                        case "STANDGIVE":
                            // Splitting the text after the command into words.
                            string[] arguments = argumentUpper.Split(new char[] { ' ' });
                            bool commandIsValid = arguments.Length > 1;

                            if (commandIsValid)
                            {
                                string nameOfTheStand = null,
                                    nameOfThePlayer = null;

                                string[] allStandNames = StandNames.Union(StandReqNames).ToArray();

                                // Process of splitting stand's name and player's name.
                                foreach (string stando in allStandNames)
                                {
                                    bool lastWordIsStandsName = arguments[arguments.Length - 1] == stando.ToUpper() ||
                                            arguments[arguments.Length - 1] == "RANDOM",
                                        lastTwoWordsAreStandsName = arguments.Length >= 2 && arguments[arguments.Length - 2] + " " +
                                            arguments[arguments.Length - 1] == stando.ToUpper(),
                                        lastThreeWordsAreStandsName = arguments.Length >= 3 && arguments[arguments.Length - 3] + " " +
                                            arguments[arguments.Length - 2] + " " + arguments[arguments.Length - 1]
                                            == stando.ToUpper();

                                    if (lastWordIsStandsName || lastTwoWordsAreStandsName || lastThreeWordsAreStandsName)
                                    {
                                        nameOfTheStand = lastWordIsStandsName ? arguments[arguments.Length - 1] :
                                            lastTwoWordsAreStandsName ? arguments[arguments.Length - 2] + " " +
                                            arguments[arguments.Length - 1] : stando.ToUpper();

                                        // Then everything except stand name elements is player's name.
                                        // Add spaces between the player's name elements to recover it if necessary.
                                        nameOfThePlayer = string.Join(" ", arguments.Except(nameOfTheStand.Split()));
                                        break;
                                    }
                                }

                                IPlayer searchedPlayer = null;

                                // Searching for the player whose name was specified.
                                foreach (IPlayer plr in Game.GetPlayers())
                                {
                                    if (nameOfThePlayer == plr.Name.ToUpper())
                                    {
                                        searchedPlayer = plr;
                                        break;
                                    }
                                }

                                // Error processing.
                                string errorMessage = null;

                                if (nameOfTheStand == null)
                                    errorMessage = "Stand doesn't exist.";
                                else if (searchedPlayer == null || searchedPlayer.IsDead)
                                    errorMessage = "Player doesn't exist.";

                                if (errorMessage != null)
                                {
                                    Game.ShowChatMessage(errorMessage, Color.Red,
                                        args.User.UserIdentifier);
                                    return;
                                }

                                // Checking for the presence of the same stand already.
                                if (PlayerStandList.ContainsKey(searchedPlayer))
                                {
                                    if (PlayerStandList[searchedPlayer].Name.ToUpper().Contains(nameOfTheStand))
                                    {
                                        Game.ShowChatMessage("This player already has the same stand.", Color.Red,
                                            args.User.UserIdentifier);
                                    }
                                    // Stand will be overwritten.
                                    else
                                    {
                                        PlayerStandList[searchedPlayer].FinalizeTheStand(true);

                                        Stand newStand = Stand.GetStand(searchedPlayer, nameOfTheStand, true);

                                        if (newStand != null)
                                        {
                                            PlayerStandList.Add(searchedPlayer, newStand);
                                        }
                                    }

                                    return;
                                }

                                Stand searchedStand = Stand.GetStand(searchedPlayer, nameOfTheStand, true);

                                if (searchedStand != null)
                                    PlayerStandList.Add(searchedPlayer, searchedStand);
                                else
                                {
                                    Game.ShowChatMessage("Stand is already taken.", Color.Red,
                                        args.User.UserIdentifier);
                                }
                            }
                            else
                                Game.ShowChatMessage("Usage: /STANDGIVE PLAYERNAME STANDNAME", Color.Red, args.User.UserIdentifier);

                            break;
                        #endregion

                        #region STANDREMOVE command
                        case "STANDREMOVE":
                            if (!string.IsNullOrEmpty(args.CommandArguments))
                            {
                                IPlayer foundPlayer = null;

                                // Searching for the player whose name was specified as argument
                                foreach (IPlayer plr in Game.GetPlayers())
                                {
                                    if (argumentUpper == plr.Name.ToUpper())
                                    {
                                        foundPlayer = plr;
                                        break;
                                    }
                                }

                                if (foundPlayer != null)
                                {
                                    // Finding out if the player actually has any stand.
                                    foreach (IPlayer plr in PlayerStandList.Keys)
                                    {
                                        if (foundPlayer == plr)
                                        {
                                            PlayerStandList[foundPlayer].FinalizeTheStand();
                                            return;
                                        }
                                    }

                                    Game.ShowChatMessage(string.Format("{0} has no stand.", foundPlayer.Name),
                                        Color.Red, args.User.UserIdentifier);
                                }
                                else
                                    Game.ShowChatMessage("Player doesn't exist.", Color.Red, args.User.UserIdentifier);
                            }
                            else
                                Game.ShowChatMessage("Usage: /STANDREMOVE PLAYERNAME", Color.Red,
                                    args.User.UserIdentifier);

                            break;
                        #endregion

                        #region STANDBAN command
                        case "STANDBAN":
                            if (!string.IsNullOrEmpty(args.CommandArguments))
                            {
                                bool nameExists = false;
                                int nameIndex = -1;

                                // Finding the argument in the array of stand names.
                                for (int i = 0; i < StandNames.Length; i++)
                                {
                                    if (string.Equals(argumentUpper, StandNames[i], StringComparison.OrdinalIgnoreCase))
                                    {
                                        nameExists = true;
                                        nameIndex = i;
                                        break;
                                    }
                                }

                                if (nameExists)
                                {
                                    if (!BannedStands.Contains(argumentUpper))
                                    {
                                        BannedStands.Add(argumentUpper);
                                        Game.ShowChatMessage(string.Format("{0} is banned from the game!",
                                            StandNames[nameIndex]), Color.Yellow);
                                    }
                                    else
                                    {
                                        Game.ShowChatMessage(string.Format("{0} is already banned.", StandNames[nameIndex]),
                                            Color.Red, args.User.UserIdentifier);
                                    }
                                    break;
                                }
                                else
                                    Game.ShowChatMessage("Stand doesn't exist.", Color.Red,
                                        args.User.UserIdentifier);
                            }
                            else
                                Game.ShowChatMessage("Usage: /STANDBAN STANDNAME", Color.Red,
                                    args.User.UserIdentifier);

                            break;
                        #endregion

                        #region STANDUNBAN command
                        case "STANDUNBAN":
                            if (!string.IsNullOrEmpty(args.CommandArguments))
                            {
                                bool nameExists = false;
                                int nameIndex = -1;

                                // Finding the argument in the array of stand names.
                                for (int i = 0; i < StandNames.Length; i++)
                                {
                                    if (string.Equals(argumentUpper, StandNames[i], StringComparison.OrdinalIgnoreCase))
                                    {
                                        nameExists = true;
                                        nameIndex = i;
                                        break;
                                    }
                                }

                                if (nameExists)
                                {
                                    if (BannedStands.Contains(argumentUpper))
                                    {
                                        BannedStands.Remove(argumentUpper);
                                        Game.ShowChatMessage(string.Format("{0} is unbanned from the game!",
                                            StandNames[nameIndex]), Color.Yellow);
                                    }
                                    else
                                    {
                                        Game.ShowChatMessage(string.Format("{0} is already allowed.", StandNames[nameIndex]),
                                            Color.Red, args.User.UserIdentifier);
                                    }
                                }
                                else
                                    Game.ShowChatMessage("Stand doesn't exist.", Color.Red,
                                        args.User.UserIdentifier);
                            }
                            else
                                Game.ShowChatMessage("Usage: /STANDUNBAN STANDNAME", Color.Red,
                                    args.User.UserIdentifier);

                            break;
                        #endregion

                        #region STANDBOWGIVE command
                        case "STANDBOWGIVE":
                            if (!string.IsNullOrEmpty(args.CommandArguments))
                            {
                                IPlayer[] plrs = Game.GetPlayers();
                                IPlayer searchedPlr = null;

                                // Searching for player...
                                foreach (IPlayer plr in plrs)
                                {
                                    if (plr.Name.ToUpper() == args.CommandArguments.ToUpper())
                                    {
                                        searchedPlr = plr;
                                        break;
                                    }
                                }

                                if (searchedPlr != null)
                                {
                                    // Overwriting the existing bow if the player already has one.
                                    if (PlayerStandBowList.ContainsKey(searchedPlr))
                                    {
                                        // -10 is just a number to tell that the method was called manually.
                                        PlayerStandBowList[searchedPlr].FinalizeTheBow(-10);
                                    }

                                    StandBow standBow = new StandBow(searchedPlr);
                                    PlayerStandBowList.Add(searchedPlr, standBow);
                                }
                                else
                                    Game.ShowChatMessage("Player doesn't exist.", Color.Red,
                                        args.User.UserIdentifier);
                            }
                            else
                                Game.ShowChatMessage("Usage: /STANDBOWGIVE PLAYERNAME", Color.Red,
                                    args.User.UserIdentifier);
                            break;
                        #endregion

                        #region CRATECHANCE command
                        case "CRATECHANCE":
                            int chance = -1;

                            bool argumentIsChance = !string.IsNullOrEmpty(args.CommandArguments) &&
                                int.TryParse(args.CommandArguments, out chance) && chance >= 0 && chance <= 100;

                            if (argumentIsChance)
                            {
                                StandCrate.SpawnCrateChance = chance;
                                Game.ShowChatMessage(string.Format("Stand crate spawn chance set to {0} (SD)!", chance),
                                    Color.Magenta, args.User.UserIdentifier);
                            }
                            else
                                Game.ShowChatMessage("Usage: /CRATECHANCE 0-100", Color.Red,
                                    args.User.UserIdentifier);
                            break;
                        #endregion

                        #region CGS command
                        case "CGS":
                            int standChance = -1;

                            bool argIsChance = !string.IsNullOrEmpty(args.CommandArguments) &&
                                int.TryParse(args.CommandArguments, out standChance) && standChance >= 0 && standChance <= 100;

                            if (argIsChance)
                            {
                                RSSStandStartChance = standChance;
                                Game.ShowChatMessage(string.Format("Start stand chance set to {0} (RSS)!", standChance),
                                    Color.Magenta, args.User.UserIdentifier);
                            }
                            else
                                Game.ShowChatMessage("Usage: /CGS 0-100", Color.Red,
                                    args.User.UserIdentifier);
                            break;
                            #endregion
                    }
                }

                // Common users' commands.
                switch (args.Command)
                {
                    #region JJSDHELP command
                    case "JJSDHELP":
                        string[] commandosos = new string[]
                        {
                                "/JJSDHELP - show a list of commands and their description",
                                "/MODEHELP - show a list of modes and their description",
                                "/MODE - show all existing modes and current mode",
                                "/STAND - show info about your stand",
                                "/ALLSTANDS - show all existing stands"
                        };

                        foreach (string commandoso in commandosos)
                            Game.ShowChatMessage(commandoso, Color.Green, args.User.UserIdentifier);
                        break;
                    #endregion

                    #region MODEHELP command
                    case "MODEHELP":
                        string[] modos = new string[]
                        {
                                "Stand Deluxe (SD) - Boxes with stands fall from the sky! Activate them! Idea and " +
                                    "crates from Bullets Deluxe and Grenades Deluxe by Ebomb09!",
                                "Round Stand Start (RSS) - There is a chance to get a Stand at the start of the round!",
                                "Base - At the beginning of the round, the lucky one gets a stand bow, " +
                                    "the arrows of which contain... stands!",
                                "Off - Stands can only be obtained through the moderator's command..."
                        };

                        foreach (string modo in modos)
                            Game.ShowChatMessage(modo, Color.Magenta, args.User.UserIdentifier);
                        break;
                    #endregion

                    #region MODE command
                    case "MODE":
                        // No need to show this massage to moderators.
                        if (!args.User.IsModerator)
                            Game.ShowChatMessage(string.Format("Modes existing: SD, RSS, Base, Off. Current mode: {0}", modeNames[Mode]),
                                Color.Magenta, args.User.UserIdentifier);

                        break;
                    #endregion

                    #region STAND command
                    case "STAND":
                        // Finding out if the player actually has any stand.
                        IPlayer plr = args.User.GetPlayer();

                        if (plr != null && PlayerStandList.ContainsKey(plr))
                        {
                            foreach (string desc in PlayerStandList[plr].Description)
                                Game.ShowChatMessage(desc, Color.Magenta, args.User.UserIdentifier);
                        }
                        else
                            Game.ShowChatMessage("You have no stand", Color.Magenta, args.User.UserIdentifier);

                        break;
                    #endregion

                    #region ALLSTANDS command
                    case "ALLSTANDS":
                        foreach (string stando in StandNames)
                            Game.ShowChatMessage(stando, Color.Magenta, args.User.UserIdentifier);

                        foreach (string stando in StandReqNames)
                            Game.ShowChatMessage(stando, Color.Magenta, args.User.UserIdentifier);
                        break;
                        #endregion
                }
            }
        }

        // Remembering the stuff to restore it in the next round.
        public static void OnShutdown()
        {
            Game.LocalStorage.SetItem("MODE", Mode.ToString());
            Game.LocalStorage.SetItem("BANNEDSTANDS", BannedStands.ToArray());
            Game.LocalStorage.SetItem("JJSDCRATECHANCE", StandCrate.SpawnCrateChance);
            Game.LocalStorage.SetItem("RSSSTANDCHANCE", RSSStandStartChance);
        }

        // Method used by StandCrate class.
        public static void CrateGiveStand(TriggerArgs args)
        {
            if (args.Sender is IPlayer && !PlayerStandList.ContainsKey((IPlayer)args.Sender))
            {
                Stand stand = Stand.GetStand((IPlayer)args.Sender, "RANDOM");

                if (stand != null && args.Caller is IObject)
                {
                    PlayerStandList.Add((IPlayer)args.Sender, stand);

                    IObject Button = args.Caller as IObject;

                    // Destroy Crate
                    foreach (IObject obj in Game.GetObjectsByCustomID(Button.CustomID))
                    {
                        obj.Destroy();
                    }
                }
            }
        }

        #region -=======================-STANDS-=========================-

        public abstract class Stand
        {
            // Set modifiers, play the effects and block the stand.
            protected Stand(IPlayer plr)
            {
                owner = plr;

                owner.ClearModifiers();
                owner.SetModifiers(Modifiers);

                // Fully heal the player that recieved the stand.
                owner.SetHealth(owner.GetMaxHealth());

                string startInfo = Name.ToUpper() + "\n";

                for (int i = 1; i < Description.Length; i++)
                    startInfo += Description[i] + "\n";

                Game.PlayEffect("CFTXT", owner.GetWorldPosition() + new Vector2(0, 5), startInfo);

                Game.ShowChatMessage(string.Format("{0} recieved {1}!", owner.Name,
                    Name), Color.Cyan);

                int usID = owner.GetUser() == null ? 666 : owner.GetUser().UserIdentifier;
                foreach (string desc in Description)
                    Game.ShowChatMessage(desc, Color.Magenta, usID);

                BlockedStands.Add(Name.ToUpper());
            }

            protected IPlayer owner;

            // Modifiers that players get when they get the stand.
            public abstract PlayerModifiers Modifiers { get; }

            private static Random rnd = new Random();

            public abstract string Name { get; }

            public abstract string[] Description { get; }

            public abstract void ActivateAbilities(IPlayer player, VirtualKeyInfo[] keyEvents);

            // Returns the instance of the stand navigating by the name or randomly.
            public static Stand GetStand(IPlayer ply, string standName = "RANDOM", bool byCommand = false, bool requiem = false)
            {
                bool standIsAvailable = !BlockedStands.Contains(standName) && !BannedStands.Contains(standName);

                if (standName.Contains(" REQ"))
                    return GetStand(ply, standName.Replace(" REQ", ""), byCommand, true);

                if (byCommand || standIsAvailable)
                {
                    switch (standName)
                    {
                        case "MAGICIAN'S RED":
                            return new MagiciansRed(ply);
                        case "HIEROPHANT GREEN":
                            return new HierophantGreen(ply);
                        case "SILVER CHARIOT": // And requiem.
                            {
                                int standReqChance = 20;

                                if (requiem)
                                    standReqChance = 100;

                                if (rnd.Next(1, 101) > standReqChance)
                                    return new SilverChariot(ply);
                                else
                                    return new SilverChariotReq(ply);
                            }
                        case "EBONY DEVIL":
                            return new EbonyDevil(ply);
                        case "EMPEROR":
                            return new Emperor(ply);
                        case "LOVERS": // And requiem.
                            {
                                int standReqChance = 35;

                                if (requiem)
                                    standReqChance = 100;

                                if (rnd.Next(1, 101) > standReqChance)
                                    return new Lovers(ply);
                                else
                                    return new LoversReq(ply);
                            }
                        case "ANUBIS":
                            return new Anubis(ply);
                        case "STAR PLATINUM":
                            return new StarPlatinum(ply);
                        case "THE WORLD":
                            return new TheWorld(ply);
                        case "NOTORIOUS B.I.G":
                            return new NotoriousBIG(ply);
                        case "BAD COMPANY":
                            return new BadCompany(ply);
                        case "CRAZY DIAMOND":
                            return new CrazyDiamond(ply);
                        case "THE HAND":
                            return new TheHand(ply);
                        case "KILLER QUEEN":
                            return new KillerQueen(ply);
                        case "LITTLE FEET":
                            return new LittleFeet(ply);
                        case "PURPLE HAZE":
                            return new PurpleHaze(ply);
                        case "KING CRIMSON":
                            return new KingCrimson(ply);
                        case "STICKY FINGERS":
                            return new StickyFingers(ply);
                        case "GOLD EXPERIENCE": // And requiem.
                            {
                                int standReqChance = 20;

                                if (requiem)
                                    standReqChance = 100;

                                if (rnd.Next(1, 101) > standReqChance)
                                    return new GoldExperience(ply);
                                else
                                    return new GoldExperienceReq(ply);
                            }

                        #region RANDOM
                        case "RANDOM":
                            string[] allStandNames = new string[StandNames.Length];

                            // Making names uppercase to be able to use them and
                            // filter them from the banned and blocked ones.
                            for (int i = 0; i < StandNames.Length; i++)
                                allStandNames[i] = string.Copy(StandNames[i]).ToUpper();

                            if (!byCommand)
                            {
                                // Choose only from the available stands.
                                allStandNames = allStandNames.Except(BannedStands).Except(BlockedStands).ToArray();
                            }

                            // Give stands only if there's free ones left.
                            if (allStandNames.Length != 0)
                            {
                                string randomName = allStandNames[rnd.Next(0, allStandNames.Length)];
                                return GetStand(ply, randomName, byCommand);
                            }
                            else
                                return null;
                        #endregion
                        default:
                            return null;
                    }
                }
                else
                    return null;
            }

            // Remove all the powers from the owner of the stand and the stand from the list.
            public virtual void FinalizeTheStand(bool rewriting = false)
            {
                if (owner != null)
                {
                    if (!owner.IsDead)
                    {
                        float plrHealth = owner.GetHealth();
                        owner.ClearModifiers();

                        // Restoring the health.
                        if (plrHealth > 0f && plrHealth < 100f)
                            owner.SetHealth(plrHealth);

                        if (rewriting == false)
                        {
                            string name = owner.GetUser() == null ? owner.Name : owner.GetUser().Name;
                            Game.ShowChatMessage(string.Format("{0} no longer has a stand!", name), Color.Cyan);
                        }
                    }

                    if (this is SilverChariotReq)
                        BlockedStands.Remove("Silver Chariot".ToUpper());
                    else if (this is LoversReq)
                        BlockedStands.Remove("Lovers".ToUpper());
                    else if (this is GoldExperienceReq)
                        BlockedStands.Remove("Gold Experience".ToUpper());
                }

                PlayerStandList.Remove(owner);
                BlockedStands.Remove(Name.ToUpper());
            }
        }

        class MagiciansRed : Stand
        {
            public MagiciansRed(IPlayer plr) : base(plr) { }

            public override PlayerModifiers Modifiers
            {
                get
                {
                    return new PlayerModifiers()
                    {
                        CanBurn = 0,
                        MaxHealth = 150,
                        CurrentHealth = 150,
                        MeleeDamageDealtModifier = 1.2f
                    };
                }
            }

            public override string Name { get { return "Magician's Red"; } }

            public override string[] Description
            {
                get
                {
                    return new string[] { "Your stand is Magician's Red!",
                "ALT + A - FireStorm - \nReleases a cloud of fire \naround you (COOLDOWN 20S)",
                        "ALT + D - FireBall - \nReleases three flare gun projectiles \nin front of you (COOLDOWN 20S)" };
                }
            }

            private const float fireballCooldown = 20000, firestormCooldown = 20000;

            // -20000 by default to prevent waiting to use abilities after getting the stand.
            private float lastFireballTime = -20000, lastFirestormTime = -20000;

            private bool fireballAllowed, firestormAllowed;

            public override void ActivateAbilities(IPlayer player, VirtualKeyInfo[] keyEvents)
            {
                if (player == owner)
                {
                    for (int i = 0; i < keyEvents.Length; i++)
                    {
                        bool fireballAbilityCasted = keyEvents[i].Event == VirtualKeyEvent.Pressed && keyEvents[i].Key ==
                            VirtualKey.BLOCK && player.KeyPressed(VirtualKey.WALKING);

                        bool firestormAbilityCasted = keyEvents[i].Event == VirtualKeyEvent.Pressed && keyEvents[i].Key ==
                            VirtualKey.ATTACK && player.KeyPressed(VirtualKey.WALKING);

                        fireballAllowed = Game.TotalElapsedGameTime - lastFireballTime >= fireballCooldown;
                        firestormAllowed = Game.TotalElapsedGameTime - lastFirestormTime >= firestormCooldown;

                        if (fireballAbilityCasted && fireballAllowed)
                            PerformFireball();
                        else if (firestormAbilityCasted && firestormAllowed)
                            PerformFirestorm();
                    }
                }
            }

            private void PerformFireball()
            {
                Vector2 plrPos = owner.GetWorldPosition();
                Vector2 projectileSpawnPos = plrPos + new Vector2(owner.FacingDirection == -1 ? -10 : 10, 10);
                Vector2 projectileDirection = new Vector2(owner.FacingDirection, 0f);

                Game.SpawnProjectile(ProjectileItem.FLAREGUN, projectileSpawnPos, projectileDirection);
                Game.PlaySound("Flaregun", plrPos, 1);

                // Shoot two fireballs.
                Events.UpdateCallback.Start((float e) =>
                {
                    // Correction of information.
                    plrPos = owner.GetWorldPosition();
                    projectileSpawnPos = plrPos + new Vector2(owner.FacingDirection == -1 ? -10 : 10, 10);
                    projectileDirection = new Vector2(owner.FacingDirection, 0f);

                    Game.SpawnProjectile(ProjectileItem.FLAREGUN, projectileSpawnPos, projectileDirection);
                    Game.PlaySound("Flaregun", plrPos, 1);
                }, (uint)(200 / Game.SlowmotionModifier), 2);

                Game.PlayEffect("CFTXT", owner.GetWorldPosition() + new Vector2(0, 5), "FIREBALL!\nCooldown 20S!");

                lastFireballTime = Game.TotalElapsedGameTime;

                const int checkForFlagTime = 200;

                // A stub to be able to stop tracking in an anonymous method.
                Events.UpdateCallback notifier = null;
                // Notifying the player about the readiness of an ability.
                notifier = Events.UpdateCallback.Start((float e) =>
                {
                    // Updating the value.
                    fireballAllowed = Game.TotalElapsedGameTime - lastFireballTime >= fireballCooldown;

                    if (PlayerStandList.ContainsKey(owner) && PlayerStandList[owner] is MagiciansRed && fireballAllowed)
                    {
                        int usID = owner.GetUser() == null ? 666 : owner.GetUser().UserIdentifier;

                        Game.ShowChatMessage("FireBall ready!", Color.Magenta, usID);
                        Game.PlayEffect("CFTXT", owner.GetWorldPosition() + new Vector2(0, 5), "FireBall ready!");
                        notifier.Stop();
                    }
                    else if (PlayerStandList.ContainsKey(owner) && !(PlayerStandList[owner] is MagiciansRed))
                        notifier.Stop();
                }, checkForFlagTime);
            }

            private void PerformFirestorm()
            {
                Vector2 plrPos = owner.GetWorldPosition();
                Game.PlaySound("Flamethrower", plrPos, 1);
                Game.SpawnFireNodes(plrPos + new Vector2(0, 10), 22, 6f, FireNodeType.Flamethrower);
                Game.SpawnFireNodes(plrPos + new Vector2(0, 15), 22, 6f, FireNodeType.Flamethrower);
                Game.PlayEffect("CFTXT", owner.GetWorldPosition() + new Vector2(0, 5), "FIRESTORM!\nCooldown 20S!");

                lastFirestormTime = Game.TotalElapsedGameTime;

                const int checkForFlagTime = 200;

                // A stub to be able to stop tracking in an anonymous method.
                Events.UpdateCallback notifier = null;
                // Notifying the player about the readiness of an ability.
                notifier = Events.UpdateCallback.Start((float e) =>
                {
                    // Updating the value.
                    firestormAllowed = Game.TotalElapsedGameTime - lastFirestormTime >= firestormCooldown;

                    if (PlayerStandList.ContainsKey(owner) && PlayerStandList[owner] is MagiciansRed && firestormAllowed)
                    {
                        int usID = owner.GetUser() == null ? 666 : owner.GetUser().UserIdentifier;
                        Game.ShowChatMessage("FireStorm ready!", Color.Magenta, usID);
                        Game.PlayEffect("CFTXT", owner.GetWorldPosition() + new Vector2(0, 5), "FireStorm ready!");
                        notifier.Stop();
                    }
                    else if (PlayerStandList.ContainsKey(owner) && !(PlayerStandList[owner] is MagiciansRed))
                        notifier.Stop();
                }, checkForFlagTime);
            }
        }

        class HierophantGreen : Stand
        {
            public HierophantGreen(IPlayer plr) : base(plr) { }

            public override PlayerModifiers Modifiers
            {
                get
                {
                    return new PlayerModifiers()
                    {
                        RunSpeedModifier = 1.5f,
                        SprintSpeedModifier = 1.5f,
                        MaxHealth = 120,
                        CurrentHealth = 120
                    };
                }
            }

            public override string Name { get { return "Hierophant Green"; } }

            public override string[] Description
            {
                get
                {
                    return new string[] { "Your stand is Hierophant Green!",
                "ALT + A - Emerarudo Supurasshu - \nReleases six wheels around you (COOLDOWN 20S)",
                        "ALT + D - Controlling Puppets - \nON HIT makes an enemy a bot and sets them on \nyour team for some time (COOLDOWN 30S)" };
                }
            }

            private const float emerarudoSupurasshuCooldown = 20000, controllingPuppetsCooldown = 30000;

            // By default for prevent waiting to use abilities after getting the stand.
            private float lastEmerarudoSupurasshuTime = -20000, lastControllingPuppetsTime = -30000;

            private bool emerarudoSupurasshuAllowed, controllingPuppetsAllowed;

            public override void ActivateAbilities(IPlayer player, VirtualKeyInfo[] keyEvents)
            {
                if (player == owner)
                {
                    for (int i = 0; i < keyEvents.Length; i++)
                    {
                        bool emerarudoSupurasshuAbilityCasted = keyEvents[i].Event == VirtualKeyEvent.Pressed &&
                            keyEvents[i].Key == VirtualKey.ATTACK && owner.KeyPressed(VirtualKey.WALKING);

                        bool controllingPuppetsAbilityCasted = keyEvents[i].Event == VirtualKeyEvent.Pressed &&
                            keyEvents[i].Key == VirtualKey.BLOCK && owner.KeyPressed(VirtualKey.WALKING);

                        emerarudoSupurasshuAllowed = Game.TotalElapsedGameTime - lastEmerarudoSupurasshuTime >=
                            emerarudoSupurasshuCooldown;
                        controllingPuppetsAllowed = Game.TotalElapsedGameTime - lastControllingPuppetsTime >=
                            controllingPuppetsCooldown;

                        if (emerarudoSupurasshuAbilityCasted && emerarudoSupurasshuAllowed)
                            PerformEmerarudoSupurasshu();
                        else if (controllingPuppetsAbilityCasted && controllingPuppetsAllowed)
                            PerformControllingPuppets();
                    }
                }
            }

            private void PerformEmerarudoSupurasshu()
            {
                Vector2 plrPos = owner.GetWorldPosition();

                Vector2 wheel1SpawnPos = plrPos + new Vector2(owner.FacingDirection == -1 ? -15 : 15, 10);
                Vector2 wheel2SpawnPos = plrPos + new Vector2(owner.FacingDirection == -1 ? -15 : 15, 19);
                Vector2 wheel3SpawnPos = plrPos + new Vector2(owner.FacingDirection == -1 ? -15 : 15, 28);

                Vector2 wheel4SpawnPos = plrPos + new Vector2(owner.FacingDirection == -1 ? 15 : -15, 10);
                Vector2 wheel5SpawnPos = plrPos + new Vector2(owner.FacingDirection == -1 ? 15 : -15, 19);
                Vector2 wheel6SpawnPos = plrPos + new Vector2(owner.FacingDirection == -1 ? 15 : -15, 28);

                Game.CreateObject("CarWheel01", wheel1SpawnPos, 0, new Vector2(15 * owner.FacingDirection, 0f), 0);
                Game.CreateObject("CarWheel01", wheel2SpawnPos, 0, new Vector2(15 * owner.FacingDirection, 7f), 0);
                Game.CreateObject("CarWheel01", wheel3SpawnPos, 0, new Vector2(15 * owner.FacingDirection, 14f), 0);

                Game.CreateObject("CarWheel01", wheel4SpawnPos, 0, new Vector2(-15 * owner.FacingDirection, 0f), 0);
                Game.CreateObject("CarWheel01", wheel5SpawnPos, 0, new Vector2(-15 * owner.FacingDirection, 7f), 0);
                Game.CreateObject("CarWheel01", wheel6SpawnPos, 0, new Vector2(-15 * owner.FacingDirection, 14f), 0);

                Game.PlaySound("Bazooka", plrPos, 1);

                Game.PlayEffect("CFTXT", owner.GetWorldPosition() + new Vector2(0, 5), "EMERARUDO SUPURASSHU!\nCooldown 20S!");

                lastEmerarudoSupurasshuTime = Game.TotalElapsedGameTime;

                const int checkForFlagTime = 200;

                // A stub to be able to stop tracking in an anonymous method.
                Events.UpdateCallback notifier = null;
                // Notifying the player about the readiness of an ability.
                notifier = Events.UpdateCallback.Start((float e) =>
                {
                    // Updating the value.
                    emerarudoSupurasshuAllowed = Game.TotalElapsedGameTime - lastEmerarudoSupurasshuTime >=
                    emerarudoSupurasshuCooldown;

                    if (PlayerStandList.ContainsKey(owner) && (PlayerStandList[owner] is HierophantGreen) &&
                    emerarudoSupurasshuAllowed)
                    {
                        int usID = owner.GetUser() == null ? 666 : owner.GetUser().UserIdentifier;
                        Game.ShowChatMessage("Emerarudo Supurasshu ready!", Color.Magenta, usID);
                        Game.PlayEffect("CFTXT", owner.GetWorldPosition() + new Vector2(0, 5), "Emerarudo Supurasshu ready!");
                        notifier.Stop();
                    }
                    else if (PlayerStandList.ContainsKey(owner) && !(PlayerStandList[owner] is HierophantGreen))
                        notifier.Stop();
                }, checkForFlagTime);
            }

            private void PerformControllingPuppets()
            {
                Events.PlayerMeleeActionCallback plrDamage = null;

                // Search for the first hit of the player who activated the ability.
                plrDamage = Events.PlayerMeleeActionCallback.Start((plr, args) =>
                {
                    if (PlayerStandList.ContainsKey(plr) && PlayerStandList[plr] is HierophantGreen)
                    {
                        for (int j = 0; j < args.Length; j++)
                        {
                            if (args[j].IsPlayer)
                            {
                                IPlayer puppet = Game.GetPlayer(args[j].ObjectID);
                                IUser user = puppet.GetUser();

                                if (user != null || puppet.IsBot)
                                {
                                    PlayerTeam plrPrevTeam = plr.GetTeam(),
                                    puppetPrevTeam = puppet.GetTeam(),
                                    currTeam = plrPrevTeam == PlayerTeam.Independent ? PlayerTeam.Team1 : plrPrevTeam;

                                    string prevName = puppet.Name, name = puppet.Name + "?";

                                    int puppetId = user == null ? 666 : user.UserIdentifier;
                                    Game.ShowChatMessage("You have become a puppet for 15 seconds!", Color.Magenta,
                                        puppetId);
                                    // Giving the control of the hitted player to a bot.
                                    puppet.SetUser(null);
                                    puppet.SetBotBehavior(new BotBehavior(true, PredefinedAIType.BotA));

                                    puppet.SetBotName(name);

                                    plr.SetTeam(currTeam);
                                    puppet.SetTeam(currTeam);

                                    float puppetTime = Game.TotalElapsedGameTime;

                                    // Checking if it's time to give the control back.
                                    Events.UpdateCallback checkForReleasing = null;
                                    checkForReleasing = Events.UpdateCallback.Start((ms) =>
                                    {
                                        if (Game.TotalElapsedGameTime - puppetTime >= 15000)
                                        {
                                            puppet.SetUser(user, true);
                                            Game.ShowChatMessage("You are free again!", Color.Magenta,
                                                puppetId);
                                            plr.SetTeam(plrPrevTeam);
                                            puppet.SetTeam(puppetPrevTeam);
                                            puppet.SetBotName(prevName);

                                            checkForReleasing.Stop();
                                        }
                                    }, 200);
                                }

                                plrDamage.Stop();
                            }
                        }
                    }
                });

                Game.PlaySound("StrengthBoostStart", owner.GetWorldPosition(), 1);
                Game.PlayEffect("CFTXT", owner.GetWorldPosition() + new Vector2(0, 5), "CONTROLLING PUPPETS!\nCooldown 30S!");

                lastControllingPuppetsTime = Game.TotalElapsedGameTime;

                const int checkForFlagTime = 200;

                // A stub to be able to stop tracking in an anonymous method.
                Events.UpdateCallback notifier = null;
                // Notifying the player about the readiness of an ability.
                notifier = Events.UpdateCallback.Start((float e) =>
                {
                    // Updating the value.
                    controllingPuppetsAllowed = Game.TotalElapsedGameTime - lastControllingPuppetsTime >= controllingPuppetsCooldown;

                    if (PlayerStandList.ContainsKey(owner) && (PlayerStandList[owner] is HierophantGreen) &&
                    controllingPuppetsAllowed)
                    {
                        int usID = owner.GetUser() == null ? 666 : owner.GetUser().UserIdentifier;
                        Game.ShowChatMessage("Controlling Puppets ready!", Color.Magenta, usID);
                        Game.PlayEffect("CFTXT", owner.GetWorldPosition() + new Vector2(0, 5), "Controlling Puppets ready!");
                        notifier.Stop();
                    }
                    else if (PlayerStandList.ContainsKey(owner) && !(PlayerStandList[owner] is HierophantGreen))
                        notifier.Stop();
                }, checkForFlagTime);
            }
        }

        class SilverChariot : Stand
        {
            public SilverChariot(IPlayer plr) : base(plr) { }

            public override PlayerModifiers Modifiers
            {
                get
                {
                    return new PlayerModifiers()
                    {
                        MeleeDamageDealtModifier = 1.5f,
                        MaxHealth = 150,
                        CurrentHealth = 150
                    };
                }
            }

            public override string Name { get { return "Silver Chariot"; } }

            public override string[] Description
            {
                get
                {
                    return new string[] { "Your stand is Silver Chariot!",
                "ALT + A - Silver Chariot - \nGives you a katana (COOLDOWN 20S)",
                        "ALT + D - Drop Armor - \nIncreases attack and speed, but decreases health" };
                }
            }

            private const float silverChariotCooldown = 20000;

            // By default for prevent waiting to use abilities after getting the stand.
            private float lastSilverChariotTime = -20000;

            private bool silverChariotAllowed, dropArmorAllowed = true;

            public override void ActivateAbilities(IPlayer player, VirtualKeyInfo[] keyEvents)
            {
                if (player == owner)
                {
                    for (int i = 0; i < keyEvents.Length; i++)
                    {
                        bool silverChariotAbilityCasted = keyEvents[i].Event == VirtualKeyEvent.Pressed &&
                            keyEvents[i].Key == VirtualKey.ATTACK && owner.KeyPressed(VirtualKey.WALKING);

                        bool dropArmorAbilityCasted = keyEvents[i].Event == VirtualKeyEvent.Pressed &&
                            keyEvents[i].Key == VirtualKey.BLOCK && owner.KeyPressed(VirtualKey.WALKING);

                        silverChariotAllowed = Game.TotalElapsedGameTime - lastSilverChariotTime >=
                            silverChariotCooldown;

                        if (silverChariotAbilityCasted && silverChariotAllowed)
                            PerformSilverChariot();
                        else if (dropArmorAbilityCasted && dropArmorAllowed)
                            PerformDropArmor();
                    }
                }
            }

            private void PerformSilverChariot()
            {
                owner.GiveWeaponItem(WeaponItem.KATANA);

                #region -=====================-COMMON STUFF-=====================-

                // Game.PlaySound("StrengthBoostStop", owner.GetWorldPosition());
                Game.PlayEffect("CFTXT", owner.GetWorldPosition() + new Vector2(0, 5), "SILVER CHARIOT!\nCooldown20S!");

                lastSilverChariotTime = Game.TotalElapsedGameTime;

                // A stub to be able to stop tracking in an anonymous method.
                Events.UpdateCallback notifier = null;
                const int checkForFlagTime = 200;
                // Notifying the player about the readiness of an ability.
                notifier = Events.UpdateCallback.Start((float e) =>
                {
                    // Updating the value.
                    silverChariotAllowed = Game.TotalElapsedGameTime - lastSilverChariotTime >=
                    silverChariotCooldown;

                    if (PlayerStandList.ContainsKey(owner) && (PlayerStandList[owner] is SilverChariot) &&
                    silverChariotAllowed)
                    {
                        int usID = owner.GetUser() == null ? 666 : owner.GetUser().UserIdentifier;
                        Game.ShowChatMessage("Silver Chariot ready!", Color.Magenta, usID);
                        Game.PlayEffect("CFTXT", owner.GetWorldPosition() + new Vector2(0, 5), "Silver Chariot ready!");
                        notifier.Stop();
                    }
                    else if (PlayerStandList.ContainsKey(owner) && !(PlayerStandList[owner] is SilverChariot))
                        notifier.Stop();
                }, checkForFlagTime);

                #endregion -=====================================================-
            }

            private void PerformDropArmor()
            {
                owner.ClearModifiers();

                owner.SetModifiers(new PlayerModifiers
                {
                    MaxHealth = 50,
                    MeleeDamageDealtModifier = 2f,
                    SprintSpeedModifier = 3f,
                    RunSpeedModifier = 3f
                });

                dropArmorAllowed = false;

                Game.PlaySound("StrengthBoostStop", owner.GetWorldPosition(), 1);
                Game.PlayEffect("CFTXT", owner.GetWorldPosition() + new Vector2(0, 5), "DROP ARMOR!");
            }
        }

        class SilverChariotReq : Stand
        {
            public SilverChariotReq(IPlayer plr) : base(plr) { BlockedStands.Add("Silver Chariot".ToUpper()); }

            public override PlayerModifiers Modifiers
            {
                get
                {
                    return new PlayerModifiers()
                    {
                        MeleeDamageDealtModifier = 3.5f,
                        MaxHealth = 450,
                        CurrentHealth = 450
                    };
                }
            }

            public override string Name { get { return "Silver Chariot REQ"; } }

            public override string[] Description
            {
                get
                {
                    return new string[] { "Your stand is Silver Chariot REQUIEM!",
                "ALT + A - Silver Chariot - \nGives you a katana (COOLDOWN 1S)",
                        "ALT + D - Soul Manipulation - \nChanges players' users at random (COOLDOWN 60S)" };
                }
            }

            private const float silverChariotCooldown = 1000, soulManipulationCooldown = 60000;

            // By default for prevent waiting to use abilities after getting the stand.
            private float lastSilverChariotTime = -1000, lastSoulManipulationTime = -60000;

            private bool silverChariotAllowed, soulManipulationAllowed = true;

            public override void ActivateAbilities(IPlayer player, VirtualKeyInfo[] keyEvents)
            {
                if (player == owner)
                {
                    for (int i = 0; i < keyEvents.Length; i++)
                    {
                        bool silverChariotAbilityCasted = keyEvents[i].Event == VirtualKeyEvent.Pressed &&
                            keyEvents[i].Key == VirtualKey.ATTACK && owner.KeyPressed(VirtualKey.WALKING);

                        bool soulManipulationAbilityCasted = keyEvents[i].Event == VirtualKeyEvent.Pressed &&
                            keyEvents[i].Key == VirtualKey.BLOCK && owner.KeyPressed(VirtualKey.WALKING);

                        silverChariotAllowed = Game.TotalElapsedGameTime - lastSilverChariotTime >=
                            silverChariotCooldown;

                        soulManipulationAllowed = Game.TotalElapsedGameTime - lastSoulManipulationTime >=
                            soulManipulationCooldown;

                        if (silverChariotAbilityCasted && silverChariotAllowed)
                            PerformSilverChariot();
                        else if (soulManipulationAbilityCasted && soulManipulationAllowed)
                            PerformSoulManipulation();
                    }
                }
            }

            private void PerformSilverChariot()
            {
                owner.GiveWeaponItem(WeaponItem.KATANA);

                #region -=====================-COMMON STUFF-=====================-

                // Game.PlaySound("StrengthBoostStop", owner.GetWorldPosition());
                Game.PlayEffect("CFTXT", owner.GetWorldPosition() + new Vector2(0, 5), "SILVER CHARIOT!\nCooldown 1S!");

                lastSilverChariotTime = Game.TotalElapsedGameTime;

                // A stub to be able to stop tracking in an anonymous method.
                Events.UpdateCallback notifier = null;
                const int checkForFlagTime = 200;
                // Notifying the player about the readiness of an ability.
                notifier = Events.UpdateCallback.Start((float e) =>
                {
                    // Updating the value.
                    silverChariotAllowed = Game.TotalElapsedGameTime - lastSilverChariotTime >=
                    silverChariotCooldown;

                    if (PlayerStandList.ContainsKey(owner) && (PlayerStandList[owner] is SilverChariotReq) &&
                    silverChariotAllowed)
                    {
                        int usID = owner.GetUser() == null ? 666 : owner.GetUser().UserIdentifier;
                        Game.ShowChatMessage("Silver Chariot ready!", Color.Magenta, usID);
                        Game.PlayEffect("CFTXT", owner.GetWorldPosition() + new Vector2(0, 5), "Silver Chariot ready!");
                        notifier.Stop();
                    }
                    else if (PlayerStandList.ContainsKey(owner) && !(PlayerStandList[owner] is SilverChariotReq))
                        notifier.Stop();
                }, checkForFlagTime);

                #endregion -=====================================================-
            }

            private void PerformSoulManipulation()
            {
                IPlayer[] allPlayers = (Game.GetPlayers().Where(p => p != owner)).ToArray();

                if (allPlayers.Length > 2)
                {
                    // Getting all the players that aren't dead and aren't bots.
                    List<IPlayer> allHumanPlrs = allPlayers.Where(plr => !plr.IsDead && !plr.IsBot).ToList();
                    List<IUser> allHumanUsers = new List<IUser>();

                    // Getting users of these players and removing users from the players.
                    for (int i = 0; i < allHumanPlrs.Count; i++)
                        allHumanUsers.Add(allHumanPlrs[i].GetUser());

                    List<IPlayer> allPlrs = allHumanPlrs, allBotPlrs;
                    List<BotBehavior> allBotUsers = new List<BotBehavior>();

                    bool thereAreBots = allPlayers.Any(p => p.IsBot);
                    if (thereAreBots)
                    {
                        // Getting all the players that aren't dead and are bots.
                        allBotPlrs = allPlayers.Where(plr => !plr.IsDead && plr.IsBot).ToList();
                        allBotUsers = new List<BotBehavior>();

                        // Getting bot behaviours of these bots and removing behaviours from the bots.
                        for (int i = 0; i < allBotPlrs.Count; i++)
                            allBotUsers.Add(allBotPlrs[i].GetBotBehavior());

                        allPlrs = (allHumanPlrs.Union(allBotPlrs)).ToList();
                    }

                    Random rnd = new Random();

                    bool thereIsPlayersLeftUnmixed = allPlrs.Count > 0;
                    // Put random users or bots to the players.
                    while (thereIsPlayersLeftUnmixed)
                    {
                        IPlayer randomPlayer = allPlrs[rnd.Next(0, allPlrs.Count)];
                        string plrsName = randomPlayer.Name;

                        int randomNum = -1;

                        if (thereAreBots)
                            randomNum = rnd.Next(thereAreBots ? 1 : 2, 3);

                        if (allHumanUsers.Count > 0 && (randomNum == -1 || randomNum == 2))
                        {
                            IUser randomPlrUser = allHumanUsers[rnd.Next(0, allHumanUsers.Count)];
                            randomPlayer.SetUser(randomPlrUser, true);
                            allHumanUsers.Remove(randomPlrUser);
                        }
                        else
                        {
                            BotBehavior randomBotUser = allBotUsers[rnd.Next(0, allBotUsers.Count)];
                            randomPlayer.SetBotBehavior(randomBotUser);
                            allBotUsers.Remove(randomBotUser);
                            randomPlayer.SetBotName("Lost Soul");
                        }

                        allPlrs.Remove(randomPlayer);

                        // Update the value.
                        thereIsPlayersLeftUnmixed = allPlrs.Count > 0;
                    }

                    lastSoulManipulationTime = Game.TotalElapsedGameTime;

                    Game.PlaySound("Heartbeat", owner.GetWorldPosition(), 1);
                    Game.PlayEffect("CFTXT", owner.GetWorldPosition() + new Vector2(0, 5), "SOUL MANIPULATION!\nCooldown 60S!");
                    Game.ShowChatMessage("All souls are mixed!", Color.Cyan);

                    // A stub to be able to stop tracking in an anonymous method.
                    Events.UpdateCallback notifier = null;
                    const int checkForFlagTime = 200;
                    // Notifying the player about the readiness of an ability.
                    notifier = Events.UpdateCallback.Start((float e) =>
                    {
                        // Updating the value.
                        soulManipulationAllowed = Game.TotalElapsedGameTime - lastSoulManipulationTime >=
                            soulManipulationCooldown;

                        if (PlayerStandList.ContainsKey(owner) && (PlayerStandList[owner] is SilverChariotReq) &&
                        soulManipulationAllowed)
                        {
                            int usID = owner.GetUser() == null ? 666 : owner.GetUser().UserIdentifier;
                            Game.ShowChatMessage("Soul Manipulation ready!", Color.Magenta, usID);
                            Game.PlayEffect("CFTXT", owner.GetWorldPosition() + new Vector2(0, 5), "Soul Manipulation ready!");
                            notifier.Stop();
                        }
                        else if (PlayerStandList.ContainsKey(owner) && !(PlayerStandList[owner] is SilverChariotReq))
                            notifier.Stop();
                    }, checkForFlagTime);
                }
            }
        }

        class EbonyDevil : Stand
        {
            public EbonyDevil(IPlayer plr) : base(plr)
            {
                onDmgCallback = Events.PlayerDamageCallback.Start(IncreaseAttackAndSpeed);
            }

            public override PlayerModifiers Modifiers
            {
                get
                {
                    return new PlayerModifiers()
                    {
                        ProjectileDamageDealtModifier = 0.1f,
                        MeleeDamageDealtModifier = 0.1f,
                        SprintSpeedModifier = 0.1f,
                        RunSpeedModifier = 0.1f,
                        MaxHealth = 1000,
                        CurrentHealth = 1000
                    };
                }
            }

            public override string Name { get { return "Ebony Devil"; } }

            public override string[] Description
            {
                get
                {
                    return new string[] { "Your stand is Ebony Devil!", "You have a lot of HP!",
                "+0.25 to attack and speed for every 70 damage taken!" };
                }
            }

            public override void ActivateAbilities(IPlayer player, VirtualKeyInfo[] keyEvents) { }

            private float damageTaken = 0;

            Events.PlayerDamageCallback onDmgCallback;

            private void IncreaseAttackAndSpeed(IPlayer plr, PlayerDamageArgs args)
            {
                bool ebonyDevilOwnerTookDamage = plr == owner;

                if (ebonyDevilOwnerTookDamage)
                {
                    damageTaken += args.Damage;

                    while (damageTaken >= 70)
                    {
                        PlayerModifiers newMods = plr.GetModifiers();
                        newMods.ProjectileDamageDealtModifier += 0.25f;
                        newMods.MeleeDamageDealtModifier += 0.25f;
                        newMods.RunSpeedModifier += 0.25f;
                        newMods.SprintSpeedModifier += 0.25f;
                        plr.SetModifiers(newMods);

                        damageTaken -= 70;
                    }
                }
            }

            public override void FinalizeTheStand(bool rewriting = false)
            {
                onDmgCallback.Stop();
                base.FinalizeTheStand(rewriting);
            }
        }

        class Emperor : Stand
        {
            public Emperor(IPlayer plr) : base(plr)
            {
                for (int i = (int)WeaponItemType.NONE; i <= (int)WeaponItemType.InstantPickup; i++)
                    owner.RemoveWeaponItemType((WeaponItemType)i);

                specialWeapon = new CoolMagnum(owner);
                weaponAddedCallback = Events.PlayerWeaponAddedActionCallback.Start(RemoveWrongWpns);
                magnumLostCallback = Events.PlayerWeaponRemovedActionCallback.Start(ReRecieveCoolMagnum);
            }

            public override PlayerModifiers Modifiers
            {
                get
                {
                    return new PlayerModifiers()
                    {
                        ProjectileDamageDealtModifier = 2f,
                        MaxHealth = 75,
                        CurrentHealth = 75
                    };
                }
            }

            public override string Name { get { return "Emperor"; } }

            public override string[] Description
            {
                get
                {
                    return new string[] { "Your stand is Emperor!",
                    "Infinite magnum with homing bullets", "You can't use any other weapon",
                    "Homing by Danger Ross and Ebomb09" };
                }
            }

            public override void ActivateAbilities(IPlayer player, VirtualKeyInfo[] keyEvents) { }

            private CoolMagnum specialWeapon;

            private Events.PlayerWeaponAddedActionCallback weaponAddedCallback;
            private Events.PlayerWeaponRemovedActionCallback magnumLostCallback;

            private void RemoveWrongWpns(IPlayer plr, PlayerWeaponAddedArg args)
            {
                bool emperorRecievedWrongWeapon = plr == owner &&
                    args.WeaponItem != WeaponItem.MAGNUM;

                if (emperorRecievedWrongWeapon)
                {
                    bool pickedThingIsWeapon = args.WeaponItemType != WeaponItemType.InstantPickup &&
                        args.WeaponItemType != WeaponItemType.Powerup;

                    if (pickedThingIsWeapon)
                        plr.Disarm(args.WeaponItemType);
                }
            }

            private void ReRecieveCoolMagnum(IPlayer plr, PlayerWeaponRemovedArg args)
            {
                bool emperorLostMagnum = plr == owner &&
                    args.WeaponItem == WeaponItem.MAGNUM;

                if (emperorLostMagnum)
                {
                    IObject droppedWpn = Game.GetObject(args.TargetObjectID);

                    if (droppedWpn != null)
                        droppedWpn.Destroy();

                    bool standIsNotEmperor = !(PlayerStandList.ContainsKey(owner) &&
                        PlayerStandList[owner] is Emperor);

                    if (owner.IsDead || standIsNotEmperor)
                        magnumLostCallback.Stop();
                }
            }

            public override void FinalizeTheStand(bool rewriting = false)
            {
                weaponAddedCallback.Stop();

                specialWeapon.FinalizeMagnum();

                owner.RemoveWeaponItemType(WeaponItemType.Handgun);

                base.FinalizeTheStand(rewriting);
            }

            // Homing feature by Danger Ross taken from the Ebomb09's Bullets Deluxe script.
            private class CoolMagnum
            {
                // Giving the stand bow and setting tracking.
                public CoolMagnum(IPlayer owner)
                {
                    this.owner = owner;
                    GiveMagnum();
                    magnumProjCreatedCallback = Events.ProjectileCreatedCallback.Start(OnProjCreated);
                    projectileUpdateCallback = Events.UpdateCallback.Start(OnUpdate);
                }

                public Events.ProjectileCreatedCallback magnumProjCreatedCallback;
                public Events.UpdateCallback projectileUpdateCallback;

                private IPlayer owner;
                private HandgunWeaponItem magnumWeapon;
                private List<IProjectile> projList = new List<IProjectile>();

                // Give the gun to an owner;
                private void GiveMagnum()
                {
                    owner.GiveWeaponItem(WeaponItem.MAGNUM);
                    magnumWeapon = owner.CurrentSecondaryWeapon;
                    owner.SetCurrentSecondaryWeaponAmmo(6, 2);
                }

                public void HomingUpdate(IProjectile pp)
                {
                    IPlayer Source = Game.GetPlayer(pp.InitialOwnerPlayerID);

                    if (Source != null)
                    {
                        double Angle = Math.Atan2(pp.Direction.Y, pp.Direction.X);
                        double Minimum = MathHelper.PI / 180 * 10 * Game.SlowmotionModifier;
                        IPlayer Target = null;
                        float Dist = -1;

                        foreach (IPlayer ply in Game.GetPlayers())
                        {
                            float r_Distance = Vector2.Distance(Source.GetWorldPosition(), ply.GetWorldPosition());

                            if (ply != Source && !ply.IsDead && (ply.GetTeam() != Source.GetTeam() || Source.GetTeam() == PlayerTeam.Independent) && ((r_Distance < Dist) || Dist == -1))
                            {
                                Dist = r_Distance;
                                Target = ply;
                            }
                        }

                        if (Target != null)
                        {
                            double d_Angle = Math.Atan2(Target.GetWorldPosition().Y - pp.Position.Y, Target.GetWorldPosition().X - pp.Position.X);

                            double a = Angle - d_Angle;
                            double b = Angle - d_Angle + MathHelper.PI * 2;
                            double c = Angle - d_Angle - MathHelper.PI * 2;

                            double d = 0;
                            if (Math.Abs(a) < Math.Abs(b) && Math.Abs(a) < Math.Abs(c))
                            {
                                d = a;
                            }
                            else if (Math.Abs(b) < Math.Abs(a) && Math.Abs(b) < Math.Abs(c))
                            {
                                d = b;
                            }
                            else
                            {
                                d = c;
                            }

                            if (d > 0)
                            {
                                Angle -= Minimum;
                            }
                            else if (d < 0)
                            {
                                Angle += Minimum;
                            }

                            pp.Direction = new Vector2((float)Math.Cos(Angle), (float)Math.Sin(Angle));
                        }
                    }
                }

                public void OnProjCreated(IProjectile[] pps)
                {
                    foreach (IProjectile pp in pps)
                    {
                        bool emperorShotBullet = owner != null && pp.InitialOwnerPlayerID == owner.UniqueID;

                        if (emperorShotBullet)
                        {
                            HomingUpdate(pp);
                            projList.Add(pp);

                            magnumWeapon = owner.CurrentSecondaryWeapon;

                            if (magnumWeapon.SpareMags == 1)
                                owner.SetCurrentSecondaryWeaponAmmo(5, 2);
                        }
                    }
                }

                public void OnUpdate(float t)
                {
                    if (!owner.IsDead && owner.CurrentSecondaryWeapon.WeaponItem != WeaponItem.MAGNUM)
                        GiveMagnum();

                    for (int i = 0; i < projList.Count; i++)
                    {
                        IProjectile b = projList[i];

                        if (!b.IsRemoved)
                        {
                            HomingUpdate(b);
                        }
                        else
                        {
                            projList.RemoveAt(i);
                        }
                    }
                }

                public void FinalizeMagnum()
                {
                    magnumProjCreatedCallback.Stop();
                    projectileUpdateCallback.Stop();
                }
            }
        }

        class Lovers : Stand
        {
            public Lovers(IPlayer plr) : base(plr) { }

            public override PlayerModifiers Modifiers
            {
                get
                {
                    return new PlayerModifiers()
                    {
                        MaxHealth = 100,
                        CurrentHealth = 100
                    };
                }
            }

            public override string Name { get { return "Lovers"; } }

            public override string[] Description
            {
                get
                {
                    return new string[] { "Your stand is Lovers!",
                "ALT + A - Binding Souls - \nFires a bullet that, when it hits a player, \n" +
                "puts you on both the same team. The death \nof one of the players will result \nthe death of the other. (COOLDOWN 10S)" };
                }
            }

            private const float bindingSoulsCooldown = 10000;

            private float lastBindingSoulsTime = -10000;

            bool bindingSoulsAllowed = true, ownerIsFree = true;

            public override void ActivateAbilities(IPlayer player, VirtualKeyInfo[] keyEvents)
            {
                if (player == owner)
                {
                    for (int i = 0; i < keyEvents.Length; i++)
                    {
                        bool bindingSoulsAbilityCasted = keyEvents[i].Event == VirtualKeyEvent.Pressed &&
                            keyEvents[i].Key == VirtualKey.ATTACK && owner.KeyPressed(VirtualKey.WALKING);

                        bindingSoulsAllowed = (Game.TotalElapsedGameTime - lastBindingSoulsTime >=
                            bindingSoulsCooldown) && ownerIsFree;

                        if (bindingSoulsAbilityCasted && bindingSoulsAllowed)
                            PerformBindingSouls();
                    }
                }
            }

            private void PerformBindingSouls()
            {
                Vector2 plrPos = owner.GetWorldPosition(),
                    bulletSpawnPos = plrPos + new Vector2(owner.FacingDirection == -1 ? -10 : 10, 10),
                    bulletDirection = new Vector2(owner.FacingDirection, 0f);

                IProjectile bullet = Game.SpawnProjectile(ProjectileItem.PISTOL, bulletSpawnPos, bulletDirection);
                bullet.DamageDealtModifier = 0;

                Game.PlaySound("Syringe", plrPos, 1);

                Events.UpdateCallback onBulletRemoval = null;

                onBulletRemoval = Events.UpdateCallback.Start(ms =>
                {
                    if (bullet.IsRemoved && ownerIsFree)
                    {
                        Game.PlayEffect("CFTXT", owner.GetWorldPosition() + new Vector2(0, 5), "BINDING SOULS!\nCooldown 10S!");
                        onBulletRemoval.Stop();
                    }
                });

                Events.ProjectileHitCallback plrBulletHitTracking = null;

                plrBulletHitTracking = Events.ProjectileHitCallback.Start((proj, hitArgs) =>
                {
                    bool bulletIsShotByLovers = proj.InstanceID == bullet.InstanceID;

                    if (bulletIsShotByLovers && hitArgs.IsPlayer)
                    {
                        IPlayer shotPlr = Game.GetPlayer(hitArgs.HitObjectID);

                        PlayerTeam prevTeam = owner.GetTeam(),
                            currTeam = prevTeam == PlayerTeam.Independent ? PlayerTeam.Team4 : owner.GetTeam();
                        owner.SetTeam(currTeam);
                        shotPlr.SetTeam(currTeam);

                        Events.PlayerDeathCallback someonesDeathCallback = null;

                        someonesDeathCallback = Events.PlayerDeathCallback.Start((plr, dthArgs) =>
                        {
                            if (plr == owner || plr == shotPlr)
                            {
                                owner.Kill();
                                shotPlr.Kill();

                                someonesDeathCallback.Stop();
                            }
                        });

                        int loversId = owner.GetUser() == null ? 666 : owner.GetUser().UserIdentifier,
                            shotPlrId = shotPlr.GetUser() == null ? 666 : shotPlr.GetUser().UserIdentifier;

                        int shotUsrId = shotPlr.UserIdentifier;

                        Game.ShowChatMessage(string.Format("You are bound with {0}!", shotPlr.Name), Color.Magenta,
                            loversId);
                        Game.ShowChatMessage(string.Format("You are bound with {0}!", owner.Name), Color.Magenta,
                            shotPlrId);

                        for (int i = 0; i < 3; i++)
                            Game.PlaySound("Heartbeat", plrPos, 1);

                        ownerIsFree = false;
                    }

                    plrBulletHitTracking.Stop();
                });

                #region -=====================-COMMON STUFF-=====================-

                lastBindingSoulsTime = Game.TotalElapsedGameTime;

                Events.UpdateCallback notifier = null;

                const int checkForFlagTime = 200;

                notifier = Events.UpdateCallback.Start((float e) =>
                {
                    // Updating the value.
                    bindingSoulsAllowed = (Game.TotalElapsedGameTime - lastBindingSoulsTime >=
                        bindingSoulsCooldown) && ownerIsFree;

                    bool notifyingMakesSense = PlayerStandList.ContainsKey(owner) &&
                        PlayerStandList[owner] is Lovers && bindingSoulsAllowed;

                    bool ownerHasDifferentStand = PlayerStandList.ContainsKey(owner) &&
                        !(PlayerStandList[owner] is Lovers);

                    if (notifyingMakesSense)
                    {
                        int usID = owner.GetUser() == null ? 666 : owner.GetUser().UserIdentifier;
                        Game.ShowChatMessage("Binding Souls ready!", Color.Magenta, usID);
                        Game.PlayEffect("CFTXT", owner.GetWorldPosition() + new Vector2(0, 5), "Binding Souls ready!");
                        notifier.Stop();
                    }
                    else if (ownerHasDifferentStand || !ownerIsFree)
                        notifier.Stop();
                }, checkForFlagTime);

                #endregion -=====================================================-
            }
        }

        class LoversReq : Stand
        {
            public LoversReq(IPlayer plr) : base(plr) { BlockedStands.Add("Lovers".ToUpper()); }

            public override PlayerModifiers Modifiers
            {
                get
                {
                    return new PlayerModifiers()
                    {
                        MaxHealth = 100,
                        CurrentHealth = 100
                    };
                }
            }

            public override string Name { get { return "Lovers REQ"; } }

            public override string[] Description
            {
                get
                {
                    return new string[] { "Your stand is Lovers REQUIEM!",
                "ALT + A - Binding Souls - \nFires a bullet that, when it hits a player, \nputs you on the same team. " +
                "Your death will result in the \ndeath of your teammate (COOLDOWN 10S)",
                        "ALT + D - Plunder - \nFires a bullet that, when it hits a player, \nsteals their stand for you (COOLDOWN 10S)" };
                }
            }

            private const float bindingSoulsCooldown = 10000, plunderCooldown = 10000;

            private float lastBindingSoulsTime = -10000, lastPlunderTime = -10000;

            bool bindingSoulsAllowed = true, ownerIsFree = true,
                plunderAllowed = true, standIsLovers = true;

            public override void ActivateAbilities(IPlayer player, VirtualKeyInfo[] keyEvents)
            {
                if (player == owner)
                {
                    for (int i = 0; i < keyEvents.Length; i++)
                    {
                        bool bindingSoulsAbilityCasted = keyEvents[i].Event == VirtualKeyEvent.Pressed &&
                            keyEvents[i].Key == VirtualKey.ATTACK && owner.KeyPressed(VirtualKey.WALKING);

                        bool plunderAbilityCasted = keyEvents[i].Event == VirtualKeyEvent.Pressed &&
                            keyEvents[i].Key == VirtualKey.BLOCK && owner.KeyPressed(VirtualKey.WALKING);

                        bindingSoulsAllowed = (Game.TotalElapsedGameTime - lastBindingSoulsTime >=
                            bindingSoulsCooldown) && ownerIsFree;

                        plunderAllowed = (Game.TotalElapsedGameTime - lastPlunderTime >=
                            plunderCooldown) && standIsLovers;

                        if (bindingSoulsAbilityCasted && bindingSoulsAllowed)
                            PerformBindingSouls();
                        else if (plunderAbilityCasted && plunderAllowed)
                            PerformPlunder();
                    }
                }
            }

            private void PerformBindingSouls()
            {
                Vector2 plrPos = owner.GetWorldPosition(),
                    bulletSpawnPos = plrPos + new Vector2(owner.FacingDirection == -1 ? -10 : 10, 10),
                    bulletDirection = new Vector2(owner.FacingDirection, 0f);

                IProjectile bullet = Game.SpawnProjectile(ProjectileItem.PISTOL, bulletSpawnPos, bulletDirection);
                bullet.DamageDealtModifier = 0;

                Game.PlaySound("Syringe", plrPos, 1);

                Events.UpdateCallback onBulletRemoval = null;

                onBulletRemoval = Events.UpdateCallback.Start(ms =>
                {
                    if (bullet.IsRemoved && ownerIsFree && !bindingSoulsAllowed)
                    {
                        bool notifyingMakesSense = PlayerStandList.ContainsKey(owner) &&
                        PlayerStandList[owner] is LoversReq;

                        if (notifyingMakesSense)
                            Game.PlayEffect("CFTXT", owner.GetWorldPosition() + new Vector2(0, 5), "BINDING SOULS!\nCooldown 10S!");

                        onBulletRemoval.Stop();
                    }
                });

                Events.ProjectileHitCallback plrBulletHitTracking = null;

                plrBulletHitTracking = Events.ProjectileHitCallback.Start((proj, hitArgs) =>
                {
                    bool bulletIsShotByLovers = proj.InstanceID == bullet.InstanceID;

                    if (bulletIsShotByLovers && hitArgs.IsPlayer)
                    {
                        IPlayer shotPlr = Game.GetPlayer(hitArgs.HitObjectID);

                        PlayerTeam prevTeam = owner.GetTeam(),
                            currTeam = prevTeam == PlayerTeam.Independent ? PlayerTeam.Team4 : owner.GetTeam();
                        owner.SetTeam(currTeam);
                        shotPlr.SetTeam(currTeam);

                        Events.PlayerDeathCallback someonesDeathCallback = null;

                        someonesDeathCallback = Events.PlayerDeathCallback.Start((plr, dthArgs) =>
                        {
                            if (plr == shotPlr)
                            {
                                shotPlr.Kill();
                                owner.SetTeam(prevTeam);
                                ownerIsFree = true;

                                bool notifyingMakesSense = PlayerStandList.ContainsKey(owner) &&
                                    PlayerStandList[owner] is LoversReq;

                                if (notifyingMakesSense)
                                {
                                    int usID = owner.GetUser() == null ? 666 : owner.GetUser().UserIdentifier;
                                    Game.ShowChatMessage("Binding Souls ready!", Color.Magenta, usID);
                                    Game.PlayEffect("CFTXT", owner.GetWorldPosition() + new Vector2(0, 5), "Binding Souls ready!");
                                }

                                someonesDeathCallback.Stop();
                            }
                            else if (plr == owner)
                            {
                                shotPlr.Kill();
                                owner.Kill();

                                someonesDeathCallback.Stop();
                            }
                        });

                        int loversId = owner.GetUser() == null ? 666 : owner.GetUser().UserIdentifier,
                            shotPlrId = shotPlr.GetUser() == null ? 666 : shotPlr.GetUser().UserIdentifier;

                        int shotUsrId = shotPlr.UserIdentifier;

                        Game.ShowChatMessage(string.Format("You are bound with {0}!", shotPlr.Name), Color.Magenta,
                            loversId);
                        Game.ShowChatMessage(string.Format("You are bound with {0}!", owner.Name), Color.Magenta,
                            shotPlrId);

                        for (int i = 0; i < 3; i++)
                            Game.PlaySound("Heartbeat", plrPos, 1);

                        ownerIsFree = false;
                    }

                    plrBulletHitTracking.Stop();
                });

                #region -=====================-COMMON STUFF-=====================-

                lastBindingSoulsTime = Game.TotalElapsedGameTime;

                Events.UpdateCallback notifier = null;

                const int checkForFlagTime = 200;

                notifier = Events.UpdateCallback.Start((float e) =>
                {
                    // Updating the value.
                    bindingSoulsAllowed = (Game.TotalElapsedGameTime - lastBindingSoulsTime >=
                        bindingSoulsCooldown) && ownerIsFree;

                    bool notifyingMakesSense = PlayerStandList.ContainsKey(owner) &&
                        PlayerStandList[owner] is LoversReq && bindingSoulsAllowed;

                    bool ownerHasDifferentStand = PlayerStandList.ContainsKey(owner) &&
                        !(PlayerStandList[owner] is LoversReq);

                    if (notifyingMakesSense)
                    {
                        int usID = owner.GetUser() == null ? 666 : owner.GetUser().UserIdentifier;
                        Game.ShowChatMessage("Binding Souls ready!", Color.Magenta, usID);
                        Game.PlayEffect("CFTXT", owner.GetWorldPosition() + new Vector2(0, 5), "Binding Souls ready!");
                        notifier.Stop();
                    }
                    else if (ownerHasDifferentStand || !ownerIsFree)
                        notifier.Stop();
                }, checkForFlagTime);

                #endregion -=====================================================-
            }

            private void PerformPlunder()
            {
                Vector2 plrPos = owner.GetWorldPosition(),
                    bulletSpawnPos = plrPos + new Vector2(owner.FacingDirection == -1 ? -10 : 10, 10),
                    bulletDirection = new Vector2(owner.FacingDirection, 0f);

                IProjectile bullet = Game.SpawnProjectile(ProjectileItem.PISTOL45, bulletSpawnPos, bulletDirection);
                bullet.DamageDealtModifier = 0;

                Game.PlaySound("Syringe", plrPos, 1);

                Events.UpdateCallback onBulletRemoval = null;

                onBulletRemoval = Events.UpdateCallback.Start(ms =>
                {
                    if (bullet.IsRemoved && standIsLovers && !plunderAllowed)
                    {
                        bool notifyingMakesSense = PlayerStandList.ContainsKey(owner) &&
                        PlayerStandList[owner] is LoversReq;

                        if (notifyingMakesSense)
                            Game.PlayEffect("CFTXT", owner.GetWorldPosition() + new Vector2(0, 5), "PLUNDER!\nCooldown 10S!");

                        onBulletRemoval.Stop();
                    }
                });

                Events.ProjectileHitCallback plrBulletHitTracking = null;

                plrBulletHitTracking = Events.ProjectileHitCallback.Start((proj, hitArgs) =>
                {
                    bool bulletIsShotByLovers = proj.InstanceID == bullet.InstanceID;

                    if (bulletIsShotByLovers && hitArgs.IsPlayer)
                    {
                        IPlayer shotPlr = Game.GetPlayer(hitArgs.HitObjectID);

                        if (PlayerStandList.ContainsKey(shotPlr))
                        {
                            string nameOfTheStand = PlayerStandList[shotPlr].Name.ToUpper();

                            PlayerStandList[owner].FinalizeTheStand(true);

                            Stand newStand = Stand.GetStand(owner, nameOfTheStand, true);

                            if (newStand != null)
                            {
                                PlayerStandList.Add(owner, newStand);

                                standIsLovers = false;
                            }
                        }
                    }
                    else
                        Game.PlayEffect("CFTXT", owner.GetWorldPosition() + new Vector2(0, 5), "PLUNDER!\nCooldown 10S!");

                    plrBulletHitTracking.Stop();
                });

                #region -=====================-COMMON STUFF-=====================-

                lastPlunderTime = Game.TotalElapsedGameTime;

                Events.UpdateCallback notifier = null;

                const int checkForFlagTime = 200;

                notifier = Events.UpdateCallback.Start((float e) =>
                {
                    // Updating the value.
                    plunderAllowed = (Game.TotalElapsedGameTime - lastPlunderTime >=
                        bindingSoulsCooldown) && standIsLovers;

                    bool notifyingMakesSense = PlayerStandList.ContainsKey(owner) &&
                        PlayerStandList[owner] is LoversReq && plunderAllowed;

                    bool ownerHasDifferentStand = PlayerStandList.ContainsKey(owner) &&
                        !(PlayerStandList[owner] is LoversReq);

                    if (notifyingMakesSense)
                    {
                        int usID = owner.GetUser() == null ? 666 : owner.GetUser().UserIdentifier;
                        Game.ShowChatMessage("Plunder ready!", Color.Magenta, usID);
                        Game.PlayEffect("CFTXT", owner.GetWorldPosition() + new Vector2(0, 5), "Plunder ready!");
                        notifier.Stop();
                    }
                    else if (ownerHasDifferentStand || !standIsLovers)
                        notifier.Stop();
                }, checkForFlagTime);

                #endregion -=====================================================-
            }
        }

        class Anubis : Stand
        {
            public Anubis(IPlayer plr) : base(plr)
            {
                for (int i = (int)WeaponItemType.NONE; i <= (int)WeaponItemType.InstantPickup; i++)
                {
                    if ((int)WeaponItemType.Melee != i)
                        owner.RemoveWeaponItemType((WeaponItemType)i);
                }

                owner.GiveWeaponItem(WeaponItem.KATANA);
                coolKatana = owner.CurrentMeleeWeapon;

                katanaActionCallback = Events.PlayerMeleeActionCallback.Start(InstantlyKillAPerson);
                katanaLostCallback = Events.PlayerWeaponRemovedActionCallback.Start(RemoveControl);
                katanaRecievedCallback = Events.PlayerWeaponAddedActionCallback.Start(OvertakePlayer);

                standFinalizer = Events.UpdateCallback.Start(StandFinalizer);
            }

            MeleeWeaponItem coolKatana;

            Events.PlayerMeleeActionCallback katanaActionCallback;
            Events.PlayerWeaponRemovedActionCallback katanaLostCallback;
            Events.PlayerWeaponAddedActionCallback katanaRecievedCallback;
            Events.UpdateCallback standFinalizer;

            IUser ownerUser, overtakenUser;
            BotBehavior ownerBotUser, overtakenBotUser;
            string ownerName, overtakenPlrName;
            int droppedKatanaId = -1;

            public override PlayerModifiers Modifiers
            {
                get
                {
                    return new PlayerModifiers()
                    {
                        MaxHealth = 50,
                        CurrentHealth = 50
                    };
                }
            }

            public override string Name { get { return "Anubis"; } }

            public override string[] Description
            {
                get
                {
                    return new string[] { "Your stand is Anubis",
                        "You have a katana that instakills", "You control the body that holds it",
                    "Losing the katana will result the lose of the body"};
                }
            }

            public override void ActivateAbilities(IPlayer player, VirtualKeyInfo[] keyEvents) { }

            public void InstantlyKillAPerson(IPlayer plr, PlayerMeleeHitArg[] args)
            {
                if (plr == owner)
                {
                    foreach (PlayerMeleeHitArg arg in args)
                    {
                        bool ownerAttacksWithAKatana = (owner.IsJumpAttacking || owner.IsMeleeAttacking) &&
                            owner.CurrentMeleeWeapon.WeaponItem == WeaponItem.KATANA &&
                                owner.CurrentWeaponDrawn == WeaponItemType.Melee;

                        if (droppedKatanaId == -1 && ownerAttacksWithAKatana)
                        {
                            if (arg.IsPlayer)
                            {
                                IPlayer hitPlayer = Game.GetPlayer(arg.ObjectID);

                                bool hitPlayerIsEnemy = owner.GetTeam() == PlayerTeam.Independent ||
                                    hitPlayer.GetTeam() != owner.GetTeam();

                                if (!hitPlayer.IsDead && hitPlayerIsEnemy)
                                {
                                    for (int i = 0; i <= 6; i++)
                                        Game.PlayEffect("BLD", new Vector2(hitPlayer.GetWorldPosition().X - 10,
                                            hitPlayer.GetWorldPosition().Y + (10 - i)));

                                    owner.SetLinearVelocity(new Vector2((hitPlayer.FacingDirection == -1 ? 0.5f : -0.5f), 5f));
                                    owner.SetWorldPosition(new Vector2(hitPlayer.GetWorldPosition().X +
                                        (hitPlayer.FacingDirection == -1 ? 17 : -17), owner.GetWorldPosition().Y));

                                    for (int i = 0; i <= 6; i++)
                                        Game.PlayEffect("BLD", new Vector2(hitPlayer.GetWorldPosition().X + 10,
                                            hitPlayer.GetWorldPosition().Y + 10 - i));

                                    hitPlayer.SetHealth(hitPlayer.GetMaxHealth());
                                    hitPlayer.SetInputEnabled(false);
                                    hitPlayer.AddCommand(new PlayerCommand(PlayerCommandType.DeathKneelInfinite));

                                    float currentTime = Game.TotalElapsedGameTime, animDelay = 1500f;

                                    Events.UpdateCallback animStart = null;

                                    animStart = Events.UpdateCallback.Start(ms =>
                                    {
                                        if (Game.TotalElapsedGameTime - currentTime >= animDelay)
                                        {
                                            hitPlayer.SetHealth(1);

                                            for (int i = 0; i <= 2; i++)
                                                Game.PlayEffect("BLD", new Vector2(hitPlayer.GetWorldPosition().X,
                                                    hitPlayer.GetWorldPosition().Y + 8));

                                            currentTime = Game.TotalElapsedGameTime;
                                            float gibDelay = 2000f;

                                            Events.UpdateCallback killHitPlayer = null;

                                            killHitPlayer = Events.UpdateCallback.Start(msc =>
                                            {
                                                if (Game.TotalElapsedGameTime - currentTime >= gibDelay)
                                                {
                                                    hitPlayer.Gib();
                                                    killHitPlayer.Stop();
                                                }
                                            });

                                            animStart.Stop();
                                        }
                                    });
                                }
                            }

                            coolKatana = owner.CurrentMeleeWeapon;

                            if (coolKatana.CurrentValue <= 20)
                            {
                                owner.GiveWeaponItem(WeaponItem.KATANA);
                                coolKatana = owner.CurrentMeleeWeapon;
                            }
                        }
                    }
                }
            }

            public void RemoveControl(IPlayer plr, PlayerWeaponRemovedArg args)
            {
                bool ownerLostKatana = plr == owner && args.WeaponItem == WeaponItem.KATANA;

                if (ownerLostKatana)
                {
                    droppedKatanaId = args.TargetObjectID;

                    ownerName = owner.Name;
                    ownerUser = owner.GetUser();

                    if (owner.IsBot)
                        ownerBotUser = owner.GetBotBehavior();

                    owner.SetUser(null);

                    if (overtakenUser == null && overtakenBotUser == null)
                        owner.SetBotBehavior(new BotBehavior(true, PredefinedAIType.BotA));
                    else if (overtakenBotUser != null)
                    {
                        owner.SetBotBehavior(overtakenBotUser);
                        owner.SetBotBehaivorActive(true);
                        owner.SetInputEnabled(true);
                    }
                    else if (overtakenUser != null)
                        owner.SetUser(overtakenUser);

                    owner.SetBotName(overtakenPlrName == null ? "The Vessel" : overtakenPlrName);
                }
            }

            public void OvertakePlayer(IPlayer plr, PlayerWeaponAddedArg args)
            {
                if (args.SourceObjectID == droppedKatanaId)
                {
                    droppedKatanaId = -1;

                    overtakenUser = plr.GetUser();
                    overtakenPlrName = plr.Name;

                    if (plr.IsBot)
                        overtakenBotUser = plr.GetBotBehavior();

                    plr.SetUser(ownerUser);

                    if (ownerBotUser != null)
                    {
                        owner.SetBotBehavior(ownerBotUser);
                        owner.SetBotBehaivorActive(true);
                        owner.SetInputEnabled(true);
                    }

                    plr.SetBotName(ownerName);

                    coolKatana = plr.CurrentMeleeWeapon;

                    PlayerStandList.Remove(owner);

                    if (PlayerStandList.ContainsKey(plr))
                    {
                        PlayerStandList[plr].FinalizeTheStand(true);
                    }

                    owner = plr;

                    PlayerStandList.Add(plr, this);

                    plr.SetModifiers(Modifiers);

                    for (int i = (int)WeaponItemType.NONE; i <= (int)WeaponItemType.InstantPickup; i++)
                    {
                        if ((int)WeaponItemType.Melee != i)
                            owner.RemoveWeaponItemType((WeaponItemType)i);
                    }
                }
                else
                {
                    bool emperorRecievedWrongWeapon = plr == owner &&
                    args.WeaponItemType != WeaponItemType.Melee;

                    if (emperorRecievedWrongWeapon)
                    {
                        bool pickedThingIsWeapon = args.WeaponItemType != WeaponItemType.InstantPickup &&
                            args.WeaponItemType != WeaponItemType.Powerup;

                        if (pickedThingIsWeapon)
                            plr.Disarm(args.WeaponItemType);
                    }
                }
            }

            public void StandFinalizer(float ms)
            {
                if (droppedKatanaId != -1)
                {
                    IObject katana = Game.GetObject(droppedKatanaId);

                    if (katana == null)
                    {
                        FinalizeTheStand(true);
                        Game.ShowChatMessage("Anubis was lost", Color.Magenta, owner.UserIdentifier);
                        standFinalizer.Stop();
                    }
                    else
                        katana = null;
                }
            }

            public override void FinalizeTheStand(bool rewriting = false)
            {
                katanaActionCallback.Stop();
                katanaLostCallback.Stop();
                katanaRecievedCallback.Stop();

                base.FinalizeTheStand(rewriting);
            }
        }

        class StarPlatinum : Stand
        {
            public StarPlatinum(IPlayer plr) : base(plr) { }

            public override PlayerModifiers Modifiers
            {
                get
                {
                    return new PlayerModifiers()
                    {
                        MaxHealth = 300,
                        CurrentHealth = 300,
                        MeleeDamageDealtModifier = 3f
                    };
                }
            }

            public override string Name { get { return "Star Platinum"; } }

            public override string[] Description
            {
                get
                {
                    return new string[] { "Your stand is Star Platinum!",
                "ALT + A - ORA ORA - \nKnocks the player back hard and \ndeals 100 damage (COOLDOWN 20S)", 
                        "ALT + S - Star Platinum Za Warudo - \nStops time for 3 seconds (COOLDOWN 60S)",
                        "ALT + D - Improved Breathing - \nPulls players from the front (COOLDOWN 20S)" };
                }
            }

            private const float improvedBreathingCooldown = 20000, oraOraCooldown = 20000, zaWarudoCooldown = 60000;

            private float lastImprovedBreathingTime = -20000, lastOraOraTime = -20000, lastZaWarudoTime = -60000;

            bool improvedBreathingAllowed = true, oraOraAllowed = true, zaWarudoAllowed = true;

            public override void ActivateAbilities(IPlayer player, VirtualKeyInfo[] keyEvents)
            {
                if (player == owner)
                {
                    for (int i = 0; i < keyEvents.Length; i++)
                    {
                        bool improvedBreathingAbilityCasted = keyEvents[i].Event == VirtualKeyEvent.Pressed &&
                            keyEvents[i].Key == VirtualKey.BLOCK && owner.KeyPressed(VirtualKey.WALKING);

                        bool oraOraAbilityCasted = keyEvents[i].Event == VirtualKeyEvent.Pressed &&
                            keyEvents[i].Key == VirtualKey.ATTACK && owner.KeyPressed(VirtualKey.WALKING);

                        bool zaWarudoAbilityCasted = keyEvents[i].Event == VirtualKeyEvent.Pressed &&
                            keyEvents[i].Key == VirtualKey.KICK && owner.KeyPressed(VirtualKey.WALKING);

                        improvedBreathingAllowed = (Game.TotalElapsedGameTime - lastImprovedBreathingTime >=
                            improvedBreathingCooldown);

                        oraOraAllowed = (Game.TotalElapsedGameTime - lastOraOraTime >=
                            oraOraCooldown);

                        zaWarudoAllowed = (Game.TotalElapsedGameTime - lastZaWarudoTime) >=
                            zaWarudoCooldown;

                        if (improvedBreathingAbilityCasted && improvedBreathingAllowed)
                            PerformImprovedBreathing();
                        else if (oraOraAbilityCasted && oraOraAllowed)
                            PerformOraOra();
                        else if (zaWarudoAbilityCasted && zaWarudoAllowed)
                            PerformZaWarudo();
                    }
                }
            }

            private void PerformImprovedBreathing()
            {
                Vector2 starPlatinumPos = owner.GetWorldPosition();
                int starPlatinumDirection = owner.FacingDirection;

                Func<IPlayer, bool> plrIsInAreaOfMagneting =
                    ((p) =>
                    {
                        PlayerTeam ownerTeam = owner.GetTeam(),
                            pTeam = p.GetTeam();

                        bool playersAreEnemies = ownerTeam == PlayerTeam.Independent ||
                            (ownerTeam != PlayerTeam.Independent && ownerTeam != pTeam);

                        if (!playersAreEnemies || p.IsDead || p.IsRemoved)
                            return false;

                        Vector2 plrWorldPos = p.GetWorldPosition();

                        bool plrYIsInArea = plrWorldPos.Y <= starPlatinumPos.Y + 20 &&
                         plrWorldPos.Y >= starPlatinumPos.Y - 20, plrXIsInArea;

                        if (starPlatinumDirection == -1)
                            plrXIsInArea = plrWorldPos.X < starPlatinumPos.X &&
                                plrWorldPos.X > starPlatinumPos.X - 250;
                        else
                            plrXIsInArea = plrWorldPos.X > starPlatinumPos.X &&
                                plrWorldPos.X < starPlatinumPos.X + 250;

                        return plrYIsInArea && plrXIsInArea;
                    });

                IPlayer[] plrsToMagnet = (Game.GetPlayers().Where(plrIsInAreaOfMagneting)).ToArray();

                bool plrsToMagnetExist = plrsToMagnet != null && plrsToMagnet.Length != 0;

                if (plrsToMagnetExist)
                {
                    IObject stump = Game.CreateObject("InvisibleBlockNoCollision",
                        new Vector2(starPlatinumPos.X + (10 * starPlatinumDirection), starPlatinumPos.Y));
                    stump.SetBodyType(BodyType.Static);

                    Events.UpdateCallback onStarPlatinumMove = null;

                    onStarPlatinumMove = Events.UpdateCallback.Start(ms =>
                    {
                        if (stump != null)
                        {
                            Vector2 spPos = owner.GetWorldPosition();
                            stump.SetWorldPosition(new Vector2(spPos.X + (10 * starPlatinumDirection), spPos.Y));
                        }
                    }, 150);

                    foreach (IPlayer plr in plrsToMagnet)
                    {
                        Vector2 plrPos = plr.GetWorldPosition();

                        IObjectTargetObjectJoint trgJoint = (IObjectTargetObjectJoint)Game.CreateObject("TargetObjectJoint", stump.GetWorldPosition());
                        trgJoint.SetTargetObject(stump);

                        IObjectPullJoint pullJoint = (IObjectPullJoint)Game.CreateObject("PullJoint", plrPos);
                        pullJoint.SetTargetObject(plr);
                        pullJoint.SetTargetObjectJoint(trgJoint);
                        pullJoint.SetForce(7f);
                        pullJoint.SetLineVisual(LineVisual.None);

                        float shitCreationTime = Game.TotalElapsedGameTime;

                        Events.UpdateCallback destroyCreatedShit = null;
                        float destroyShitTime = 1000f;

                        destroyCreatedShit = Events.UpdateCallback.Start(ms =>
                        {
                            if (Game.TotalElapsedGameTime - shitCreationTime >= destroyShitTime)
                            {
                                stump.Remove();
                                trgJoint.Remove();
                                pullJoint.Remove();
                                onStarPlatinumMove.Stop();
                                destroyCreatedShit.Stop();
                            }
                        }, 200);
                    }

                    #region -=====================-COMMON STUFF-=====================-

                    Game.PlayEffect("CFTXT", owner.GetWorldPosition() + new Vector2(0, 5), "IMPROVED BREATHING!\nCooldown 20S!");

                    lastImprovedBreathingTime = Game.TotalElapsedGameTime;

                    Events.UpdateCallback notifier = null;

                    const int checkForFlagTime = 200;

                    notifier = Events.UpdateCallback.Start((float e) =>
                    {
                        // Updating the value.
                        improvedBreathingAllowed = (Game.TotalElapsedGameTime - lastImprovedBreathingTime >=
                                improvedBreathingCooldown);

                        bool notifyingMakesSense = PlayerStandList.ContainsKey(owner) &&
                            PlayerStandList[owner] is StarPlatinum && improvedBreathingAllowed;

                        bool ownerHasDifferentStand = PlayerStandList.ContainsKey(owner) &&
                            !(PlayerStandList[owner] is StarPlatinum);

                        if (notifyingMakesSense)
                        {
                            int usID = owner.GetUser() == null ? 666 : owner.GetUser().UserIdentifier;
                            Game.ShowChatMessage("Improved Breathing ready!", Color.Magenta, usID);
                            Game.PlayEffect("CFTXT", owner.GetWorldPosition() + new Vector2(0, 5), "Improved Breathing ready!");
                            notifier.Stop();
                        }
                        else if (ownerHasDifferentStand)
                            notifier.Stop();
                    }, checkForFlagTime);

                    #endregion -=====================================================-
                }
            }

            private void PerformOraOra()
            {
                Events.PlayerMeleeActionCallback onStarPlatinumPunch = null;

                onStarPlatinumPunch = Events.PlayerMeleeActionCallback.Start((plr, args) =>
                {
                    if (plr == owner)
                    {
                        foreach (PlayerMeleeHitArg arg in args)
                        {
                            if (arg.IsPlayer)
                            {
                                IPlayer hitPlayer = Game.GetPlayer(arg.ObjectID);

                                if (!hitPlayer.IsDead)
                                {
                                    PlayerTeam ownerTeam = plr.GetTeam(),
                                        hitPlrTeam = hitPlayer.GetTeam();

                                    bool playersAreEnemies = ownerTeam == PlayerTeam.Independent ||
                                        (ownerTeam != PlayerTeam.Independent && ownerTeam != hitPlrTeam);

                                    if (playersAreEnemies)
                                    {
                                        int starPlatinumDirection = plr.FacingDirection;

                                        Vector2 newHitPlayerVelocity = new Vector2(starPlatinumDirection * 30, 3);
                                        Vector2 hitPlayerPos = hitPlayer.GetWorldPosition();

                                        hitPlayer.SetWorldPosition(new Vector2(hitPlayerPos.X, hitPlayerPos.Y + 5));
                                        hitPlayer.SetLinearVelocity(newHitPlayerVelocity);

                                        float hitTime = Game.TotalElapsedGameTime,
                                            dealDamageTimer = 500f;

                                        Events.UpdateCallback deal100Damage = null;

                                        deal100Damage = Events.UpdateCallback.Start((ms =>
                                        {
                                            if (Game.TotalElapsedGameTime - hitTime >= dealDamageTimer)
                                            {
                                                hitPlayer.DealDamage(100);
                                                deal100Damage.Stop();
                                            }
                                        }), 100);

                                        Game.PlayEffect("CFTXT", plr.GetWorldPosition() + new Vector2(0, 5), "ORA ORA!");

                                        onStarPlatinumPunch.Stop();
                                    }
                                }
                            }
                        }
                    }
                });

                #region -=====================-COMMON STUFF-=====================-

                Game.PlayEffect("CFTXT", owner.GetWorldPosition() + new Vector2(0, 5), "ORA ORA!\nCooldown 20S!");

                lastOraOraTime = Game.TotalElapsedGameTime;

                Events.UpdateCallback notifier = null;

                const int checkForFlagTime = 200;

                notifier = Events.UpdateCallback.Start((float e) =>
                {
                    // Updating the value.
                    oraOraAllowed = (Game.TotalElapsedGameTime - lastOraOraTime >=
                        oraOraCooldown);

                    bool notifyingMakesSense = PlayerStandList.ContainsKey(owner) &&
                        PlayerStandList[owner] is StarPlatinum && oraOraAllowed;

                    bool ownerHasDifferentStand = PlayerStandList.ContainsKey(owner) &&
                        !(PlayerStandList[owner] is StarPlatinum);

                    if (notifyingMakesSense)
                    {
                        int usID = owner.GetUser() == null ? 666 : owner.GetUser().UserIdentifier;
                        Game.ShowChatMessage("ORA ORA ready!", Color.Magenta, usID);
                        Game.PlayEffect("CFTXT", owner.GetWorldPosition() + new Vector2(0, 5), "ORA ORA ready!");
                        notifier.Stop();
                    }
                    else if (ownerHasDifferentStand)
                        notifier.Stop();
                }, checkForFlagTime);

                #endregion -=====================================================-
            }

            private void PerformZaWarudo()
            {
                IObject[] allObjects = (Game.GetObjects(Game.GetCameraMaxArea()).
                    Where(o => !(o is IPlayer) && (o.GetBodyType() != BodyType.Static)).ToArray());

                Vector2[] objLinearVelocities = new Vector2[allObjects.Length];
                float[] objAngularVelocities = new float[allObjects.Length];

                for (int i = 0; i < allObjects.Length; i++)
                {
                    objLinearVelocities[i] = allObjects[i].GetLinearVelocity();
                    objAngularVelocities[i] = allObjects[i].GetAngularVelocity();

                    allObjects[i].SetBodyType(BodyType.Static);
                }

                Func<IPlayer, bool> plrIsToBeFrozen = (p =>
                {
                    bool standIsTWorSP = PlayerStandList.ContainsKey(p) &&
                        (PlayerStandList[p] is StarPlatinum || PlayerStandList[p] is TheWorld);

                    if (p == owner || standIsTWorSP)
                        return false;
                    else
                        return true;
                });

                IPlayer[] plrs = (Game.GetPlayers().Where(plrIsToBeFrozen)).ToArray();

                Vector2[] plrLinearVelocities = new Vector2[plrs.Length];
                float[] plrAngularVelocities = new float[plrs.Length];

                List<IObject> stumps = new List<IObject>(plrs.Length);

                for (int i = 0; i < plrs.Length; i++)
                {
                    plrLinearVelocities[i] = plrs[i].GetLinearVelocity();
                    plrAngularVelocities[i] = plrs[i].GetAngularVelocity();

                    Vector2 plrPos = plrs[i].GetWorldPosition();

                    IObject stump = Game.CreateObject("InvisibleBlockNoCollision",
                        new Vector2(plrPos.X, plrPos.Y));

                    stump.SetBodyType(BodyType.Static);

                    stumps.Add(stump);

                    IObjectTargetObjectJoint trgJoint = (IObjectTargetObjectJoint)Game.CreateObject("TargetObjectJoint", stump.GetWorldPosition());
                    trgJoint.SetTargetObject(stump);

                    IObjectPullJoint pullJoint = (IObjectPullJoint)Game.CreateObject("PullJoint", plrPos);
                    pullJoint.SetTargetObject(plrs[i]);
                    pullJoint.SetTargetObjectJoint(trgJoint);

                    plrs[i].SetInputEnabled(false);
                }

                float zaWarudoTime = Game.TotalElapsedGameTime,
                    releaseTime = 3000f;

                Events.PlayerMeleeActionCallback increaseHitObjectVelocity = null;

                increaseHitObjectVelocity = Events.PlayerMeleeActionCallback.Start((plr, args) =>
                {
                    if (plr == owner)
                    {
                        foreach (PlayerMeleeHitArg arg in args)
                        {
                            int ownerFaceDirection = plr.FacingDirection;

                            if (arg.IsPlayer)
                            {
                                IPlayer hitPlayer = Game.GetPlayer(arg.ObjectID);

                                PlayerTeam ownerTeam = plr.GetTeam(),
                                    hitPlrTeam = hitPlayer.GetTeam();

                                bool playersAreEnemies = ownerTeam == PlayerTeam.Independent ||
                                    (ownerTeam != PlayerTeam.Independent && ownerTeam != hitPlrTeam);

                                if (playersAreEnemies)
                                {
                                    if (plrs.Contains(hitPlayer))
                                    {
                                        int hitIndex = Array.FindIndex(plrs, p => p == hitPlayer);

                                        plrLinearVelocities[hitIndex].X += 2f * ownerFaceDirection;
                                        plrLinearVelocities[hitIndex].Y += 3f;
                                    }
                                }
                            }
                            else
                            {
                                IObject hitObject = Game.GetObject(arg.ObjectID);

                                if (allObjects.Contains(hitObject))
                                {
                                    int hitIndex = Array.FindIndex(allObjects, o => o == hitObject);

                                    objLinearVelocities[hitIndex].X += 2f * ownerFaceDirection;
                                    objLinearVelocities[hitIndex].Y += 3f;
                                }
                            }
                        }
                    }
                });

                Events.UpdateCallback releaseObjects = null;

                releaseObjects = Events.UpdateCallback.Start((ms) =>
                {
                    if (Game.TotalElapsedGameTime - zaWarudoTime >= releaseTime)
                    {
                        for (int i = 0; i < allObjects.Length; i++)
                        {
                            if (allObjects[i] != null)
                            {
                                allObjects[i].SetBodyType(BodyType.Dynamic);

                                allObjects[i].SetLinearVelocity(objLinearVelocities[i]);
                                allObjects[i].SetAngularVelocity(objAngularVelocities[i]);
                            }
                        }

                        for (int i = 0; i < plrs.Length; i++)
                        {
                            stumps[i].Remove();

                            if (plrs[i] != null)
                            {
                                plrs[i].SetInputEnabled(true);

                                Vector2 plrPos = plrs[i].GetWorldPosition();
                                plrs[i].SetWorldPosition(new Vector2(plrPos.X, plrPos.Y + 5));

                                plrs[i].SetLinearVelocity(plrLinearVelocities[i]);
                                plrs[i].SetAngularVelocity(plrAngularVelocities[i]);
                            }
                        }

                        increaseHitObjectVelocity.Stop();
                        releaseObjects.Stop();
                    }
                }, 500);

                Game.PlaySound("GetSlomo", owner.GetWorldPosition(), 1);
                Game.CreateDialogue("STAR PLATINUM!\nZA WARUDO!", owner);

                #region -=====================-COMMON STUFF-=====================-

                Game.PlayEffect("CFTXT", owner.GetWorldPosition() + new Vector2(0, 5), "Cooldown 60S!");

                lastZaWarudoTime = Game.TotalElapsedGameTime;

                Events.UpdateCallback notifier = null;

                const int checkForFlagTime = 200;

                notifier = Events.UpdateCallback.Start((float e) =>
                {
                    // Updating the value.
                    zaWarudoAllowed = (Game.TotalElapsedGameTime - lastZaWarudoTime >=
                        zaWarudoCooldown);

                    bool notifyingMakesSense = PlayerStandList.ContainsKey(owner) &&
                        PlayerStandList[owner] is StarPlatinum && zaWarudoAllowed;

                    bool ownerHasDifferentStand = PlayerStandList.ContainsKey(owner) &&
                        !(PlayerStandList[owner] is StarPlatinum);

                    if (notifyingMakesSense)
                    {
                        int usID = owner.GetUser() == null ? 666 : owner.GetUser().UserIdentifier;
                        Game.ShowChatMessage("Star Platinum Za Warudo ready!", Color.Magenta, usID);
                        Game.PlayEffect("CFTXT", owner.GetWorldPosition() + new Vector2(0, 5), "Star Platinum Za Warudo ready!");
                        notifier.Stop();
                    }
                    else if (ownerHasDifferentStand)
                        notifier.Stop();
                }, checkForFlagTime);

                #endregion -=====================================================-
            }
        }

        class TheWorld : Stand
        {
            public TheWorld(IPlayer plr) : base(plr) { }

            public override PlayerModifiers Modifiers
            {
                get
                {
                    return new PlayerModifiers()
                    {
                        MaxHealth = 300,
                        CurrentHealth = 300,
                        MeleeDamageDealtModifier = 3f
                    };
                }
            }

            public override string Name { get { return "The World"; } }

            public override string[] Description
            {
                get
                {
                    return new string[] { "Your stand is The World!",
                "ALT + A - MUDA MUDA - \nStrongly knocks the player up and \ndeals 100 damage (COOLDOWN 20S)", 
                        "ALT + S - Za Warudo - \nStops time for 5 seconds (COOLDOWN 30S)",
                        "ALT + D - Knives - \nLaunches many knives from \nthe front (COOLDOWN 20S)" };
                }
            }

            private const float knivesCooldown = 20000, mudaMudaCooldown = 20000, zaWarudoCooldown = 30000;

            private float lastKnivesTime = -20000, lastMudaMudaTime = -20000, lastZaWarudoTime = -30000;

            bool knivesAllowed = true, mudaMudaAllowed = true, zaWarudoAllowed = true;

            public override void ActivateAbilities(IPlayer player, VirtualKeyInfo[] keyEvents)
            {
                if (player == owner)
                {
                    for (int i = 0; i < keyEvents.Length; i++)
                    {
                        bool knivesAbilityCasted = keyEvents[i].Event == VirtualKeyEvent.Pressed &&
                            keyEvents[i].Key == VirtualKey.BLOCK && owner.KeyPressed(VirtualKey.WALKING);

                        bool mudaMudaAbilityCasted = keyEvents[i].Event == VirtualKeyEvent.Pressed &&
                            keyEvents[i].Key == VirtualKey.ATTACK && owner.KeyPressed(VirtualKey.WALKING);

                        bool zaWarudoAbilityCasted = keyEvents[i].Event == VirtualKeyEvent.Pressed &&
                            keyEvents[i].Key == VirtualKey.KICK && owner.KeyPressed(VirtualKey.WALKING);

                        knivesAllowed = (Game.TotalElapsedGameTime - lastKnivesTime >=
                            knivesCooldown);

                        mudaMudaAllowed = (Game.TotalElapsedGameTime - lastMudaMudaTime >=
                            mudaMudaCooldown);

                        zaWarudoAllowed = (Game.TotalElapsedGameTime - lastZaWarudoTime) >=
                            zaWarudoCooldown;

                        if (knivesAbilityCasted && knivesAllowed)
                            PerformKnives();
                        else if (mudaMudaAbilityCasted && mudaMudaAllowed)
                            PerformMudaMuda();
                        else if (zaWarudoAbilityCasted && zaWarudoAllowed)
                            PerformZaWarudo();
                    }
                }
            }

            private void PerformKnives()
            {
                Vector2 theWorldPos = owner.GetWorldPosition();

                IObjectWeaponItem[] thrownKnifes = new IObjectWeaponItem[5];

                for (int i = 0; i < thrownKnifes.Length; i++)
                {
                    Vector2 projectileSpawnPos = theWorldPos + new Vector2(owner.FacingDirection == -1 ? -20 : 20, 3 * i);

                    thrownKnifes[i] = (IObjectWeaponItem)Game.CreateObject("WpnKnife", projectileSpawnPos, 0,
                        new Vector2(25 * owner.FacingDirection, 2), 45 * owner.FacingDirection, owner.FacingDirection);

                    thrownKnifes[i].TrackAsMissile(true);
                }

                float knivesCreateTime = Game.TotalElapsedGameTime,
                    knivesRemoveTime = 1000f;

                Events.UpdateCallback removeKnives = null;

                removeKnives = Events.UpdateCallback.Start(ms =>
                {
                    if (Game.TotalElapsedGameTime - knivesCreateTime >= knivesRemoveTime)
                    {
                        for (int i = 0; i < thrownKnifes.Length; i++)
                        {
                            if (thrownKnifes[i] != null)
                            {
                                thrownKnifes[i].Destroy();
                            }
                        }

                        removeKnives.Stop();
                    }
                }, 250);

                #region -=====================-COMMON STUFF-=====================-

                Game.PlayEffect("CFTXT", owner.GetWorldPosition() + new Vector2(0, 5), "KNIVES!\nCooldown 20S!");

                lastKnivesTime = Game.TotalElapsedGameTime;

                Events.UpdateCallback notifier = null;

                const int checkForFlagTime = 200;

                notifier = Events.UpdateCallback.Start((float e) =>
                {
                    // Updating the value.
                    knivesAllowed = (Game.TotalElapsedGameTime - lastKnivesTime >=
                            knivesCooldown);

                    bool notifyingMakesSense = PlayerStandList.ContainsKey(owner) &&
                        PlayerStandList[owner] is TheWorld && knivesAllowed;

                    bool ownerHasDifferentStand = PlayerStandList.ContainsKey(owner) &&
                        !(PlayerStandList[owner] is TheWorld);

                    if (notifyingMakesSense)
                    {
                        int usID = owner.GetUser() == null ? 666 : owner.GetUser().UserIdentifier;
                        Game.ShowChatMessage("Knives ready!", Color.Magenta, usID);
                        Game.PlayEffect("CFTXT", owner.GetWorldPosition() + new Vector2(0, 5), "Knives ready!");
                        notifier.Stop();
                    }
                    else if (ownerHasDifferentStand)
                        notifier.Stop();
                }, checkForFlagTime);

                #endregion -=====================================================-
            }

            private void PerformMudaMuda()
            {
                Events.PlayerMeleeActionCallback onStarPlatinumPunch = null;

                onStarPlatinumPunch = Events.PlayerMeleeActionCallback.Start((plr, args) =>
                {
                    if (plr == owner)
                    {
                        foreach (PlayerMeleeHitArg arg in args)
                        {
                            if (arg.IsPlayer)
                            {
                                IPlayer hitPlayer = Game.GetPlayer(arg.ObjectID);

                                if (!hitPlayer.IsDead)
                                {
                                    PlayerTeam ownerTeam = plr.GetTeam(),
                                        hitPlrTeam = hitPlayer.GetTeam();

                                    bool playersAreEnemies = ownerTeam == PlayerTeam.Independent ||
                                        (ownerTeam != PlayerTeam.Independent && ownerTeam != hitPlrTeam);

                                    if (playersAreEnemies)
                                    {
                                        int starPlatinumDirection = plr.FacingDirection;

                                        Vector2 newHitPlayerVelocity = new Vector2(3, starPlatinumDirection * 30);

                                        Vector2 hitPlayerPos = hitPlayer.GetWorldPosition();

                                        hitPlayer.SetWorldPosition(new Vector2(hitPlayerPos.X, hitPlayerPos.Y + 5));
                                        hitPlayer.SetLinearVelocity(newHitPlayerVelocity);

                                        float hitTime = Game.TotalElapsedGameTime,
                                            dealDamageTimer = 500f;

                                        Events.UpdateCallback deal100Damage = null;

                                        deal100Damage = Events.UpdateCallback.Start((ms =>
                                        {
                                            if (Game.TotalElapsedGameTime - hitTime >= dealDamageTimer)
                                            {
                                                hitPlayer.DealDamage(100);
                                                deal100Damage.Stop();
                                            }
                                        }), 100);

                                        Game.PlayEffect("CFTXT", plr.GetWorldPosition() + new Vector2(0, 5), "MUDA MUDA!");

                                        onStarPlatinumPunch.Stop();
                                    }
                                }
                            }
                        }
                    }
                });

                #region -=====================-COMMON STUFF-=====================-

                Game.PlayEffect("CFTXT", owner.GetWorldPosition() + new Vector2(0, 5), "MUDA MUDA!\nCooldown 20S!");

                lastMudaMudaTime = Game.TotalElapsedGameTime;

                Events.UpdateCallback notifier = null;

                const int checkForFlagTime = 200;

                notifier = Events.UpdateCallback.Start((float e) =>
                {
                    // Updating the value.
                    mudaMudaAllowed = (Game.TotalElapsedGameTime - lastMudaMudaTime >=
                        mudaMudaCooldown);

                    bool notifyingMakesSense = PlayerStandList.ContainsKey(owner) &&
                        PlayerStandList[owner] is TheWorld && mudaMudaAllowed;

                    bool ownerHasDifferentStand = PlayerStandList.ContainsKey(owner) &&
                        !(PlayerStandList[owner] is TheWorld);

                    if (notifyingMakesSense)
                    {
                        int usID = owner.GetUser() == null ? 666 : owner.GetUser().UserIdentifier;
                        Game.ShowChatMessage("MUDA MUDA ready!", Color.Magenta, usID);
                        Game.PlayEffect("CFTXT", owner.GetWorldPosition() + new Vector2(0, 5), "MUDA MUDA ready!");
                        notifier.Stop();
                    }
                    else if (ownerHasDifferentStand)
                        notifier.Stop();
                }, checkForFlagTime);

                #endregion -=====================================================-
            }

            private void PerformZaWarudo()
            {
                IObject[] allObjects = (Game.GetObjects(Game.GetCameraMaxArea()).
                    Where(o => !(o is IPlayer) && (o.GetBodyType() != BodyType.Static)).ToArray());

                Vector2[] objLinearVelocities = new Vector2[allObjects.Length];
                float[] objAngularVelocities = new float[allObjects.Length];

                for (int i = 0; i < allObjects.Length; i++)
                {
                    objLinearVelocities[i] = allObjects[i].GetLinearVelocity();
                    objAngularVelocities[i] = allObjects[i].GetAngularVelocity();

                    allObjects[i].SetBodyType(BodyType.Static);
                }

                Func<IPlayer, bool> plrIsToBeFrozen = (p =>
                {
                    bool standIsTWorSP = PlayerStandList.ContainsKey(p) &&
                        (PlayerStandList[p] is StarPlatinum || PlayerStandList[p] is TheWorld);

                    if (p == owner || standIsTWorSP)
                        return false;
                    else
                        return true;
                });

                IPlayer[] plrs = (Game.GetPlayers().Where(plrIsToBeFrozen)).ToArray();
                Vector2[] plrLinearVelocities = new Vector2[plrs.Length];
                float[] plrAngularVelocities = new float[plrs.Length];

                List<IObject> stumps = new List<IObject>(plrs.Length);

                for (int i = 0; i < plrs.Length; i++)
                {
                    plrLinearVelocities[i] = plrs[i].GetLinearVelocity();
                    plrAngularVelocities[i] = plrs[i].GetAngularVelocity();

                    Vector2 plrPos = plrs[i].GetWorldPosition();

                    IObject stump = Game.CreateObject("InvisibleBlockNoCollision",
                        new Vector2(plrPos.X, plrPos.Y));

                    stump.SetBodyType(BodyType.Static);

                    stumps.Add(stump);

                    IObjectTargetObjectJoint trgJoint = (IObjectTargetObjectJoint)Game.CreateObject("TargetObjectJoint", stump.GetWorldPosition());
                    trgJoint.SetTargetObject(stump);

                    IObjectPullJoint pullJoint = (IObjectPullJoint)Game.CreateObject("PullJoint", plrPos);
                    pullJoint.SetTargetObject(plrs[i]);
                    pullJoint.SetTargetObjectJoint(trgJoint);

                    plrs[i].SetInputEnabled(false);
                }

                float zaWarudoTime = Game.TotalElapsedGameTime,
                    releaseTime = 5000f;

                Events.PlayerMeleeActionCallback increaseHitObjectVelocity = null;

                increaseHitObjectVelocity = Events.PlayerMeleeActionCallback.Start((plr, args) =>
                {
                    if (plr == owner)
                    {
                        foreach (PlayerMeleeHitArg arg in args)
                        {
                            int ownerFaceDirection = plr.FacingDirection;

                            if (arg.IsPlayer)
                            {
                                IPlayer hitPlayer = Game.GetPlayer(arg.ObjectID);

                                PlayerTeam ownerTeam = plr.GetTeam(),
                                    hitPlrTeam = hitPlayer.GetTeam();

                                bool playersAreEnemies = ownerTeam == PlayerTeam.Independent ||
                                    (ownerTeam != PlayerTeam.Independent && ownerTeam != hitPlrTeam);

                                if (playersAreEnemies)
                                {
                                    if (plrs.Contains(hitPlayer))
                                    {
                                        int hitIndex = Array.FindIndex(plrs, p => p == hitPlayer);

                                        plrLinearVelocities[hitIndex].X += 2f * ownerFaceDirection;
                                        plrLinearVelocities[hitIndex].Y += 3f;
                                    }
                                }
                            }
                            else
                            {
                                IObject hitObject = Game.GetObject(arg.ObjectID);

                                if (allObjects.Contains(hitObject))
                                {
                                    int hitIndex = Array.FindIndex(allObjects, o => o == hitObject);

                                    objLinearVelocities[hitIndex].X += 2f * ownerFaceDirection;
                                    objLinearVelocities[hitIndex].Y += 3f;
                                }
                            }
                        }
                    }
                });

                Events.UpdateCallback releaseObjects = null;

                releaseObjects = Events.UpdateCallback.Start((ms) =>
                {
                    if (Game.TotalElapsedGameTime - zaWarudoTime >= releaseTime)
                    {
                        for (int i = 0; i < allObjects.Length; i++)
                        {
                            if (allObjects[i] != null)
                            {
                                allObjects[i].SetBodyType(BodyType.Dynamic);

                                allObjects[i].SetLinearVelocity(objLinearVelocities[i]);
                                allObjects[i].SetAngularVelocity(objAngularVelocities[i]);
                            }
                        }

                        for (int i = 0; i < plrs.Length; i++)
                        {
                            stumps[i].Remove();

                            if (plrs[i] != null)
                            {
                                plrs[i].SetInputEnabled(true);

                                Vector2 plrPos = plrs[i].GetWorldPosition();
                                plrs[i].SetWorldPosition(new Vector2(plrPos.X, plrPos.Y + 5));

                                plrs[i].SetLinearVelocity(plrLinearVelocities[i]);
                                plrs[i].SetAngularVelocity(plrAngularVelocities[i]);
                            }
                        }

                        increaseHitObjectVelocity.Stop();
                        releaseObjects.Stop();
                    }
                }, 500);

                Game.PlaySound("GetSlomo", owner.GetWorldPosition(), 1);
                Game.CreateDialogue("ZA WARUDO!", owner);

                #region -=====================-COMMON STUFF-=====================-

                Game.PlayEffect("CFTXT", owner.GetWorldPosition() + new Vector2(0, 5), "Cooldown 30S!");

                lastZaWarudoTime = Game.TotalElapsedGameTime;

                Events.UpdateCallback notifier = null;

                const int checkForFlagTime = 200;

                notifier = Events.UpdateCallback.Start((float e) =>
                {
                    // Updating the value.
                    zaWarudoAllowed = (Game.TotalElapsedGameTime - lastZaWarudoTime >=
                        zaWarudoCooldown);

                    bool notifyingMakesSense = PlayerStandList.ContainsKey(owner) &&
                        PlayerStandList[owner] is TheWorld && zaWarudoAllowed;

                    bool ownerHasDifferentStand = PlayerStandList.ContainsKey(owner) &&
                        !(PlayerStandList[owner] is TheWorld);

                    if (notifyingMakesSense)
                    {
                        int usID = owner.GetUser() == null ? 666 : owner.GetUser().UserIdentifier;
                        Game.ShowChatMessage("Za Warudo ready!", Color.Magenta, usID);
                        Game.PlayEffect("CFTXT", owner.GetWorldPosition() + new Vector2(0, 5), "Za Warudo ready!");
                        notifier.Stop();
                    }
                    else if (ownerHasDifferentStand)
                        notifier.Stop();
                }, checkForFlagTime);

                #endregion -=====================================================-
            }
        }

        class NotoriousBIG : Stand
        {
            public NotoriousBIG(IPlayer plr) : base(plr)
            {
                PlayerTeam ownrTm = plr.GetTeam();

                if (ownrTm == PlayerTeam.Independent)
                    ownerTeam = PlayerTeam.Team2;
                else
                    ownerTeam = ownrTm;

                owner.SetTeam(ownerTeam);

                ownerProfile = owner.GetProfile();

                onDeathCallback = Events.PlayerDeathCallback.Start(SpawnBot);
            }

            public override PlayerModifiers Modifiers
            {
                get
                {
                    return new PlayerModifiers()
                    {
                        MaxHealth = 10,
                        CurrentHealth = 10,
                    };
                }
            }

            PlayerTeam ownerTeam;
            IProfile ownerProfile;

            Events.PlayerDeathCallback onDeathCallback;

            public override string Name { get { return "Notorious B.I.G"; } }

            public override string[] Description
            {
                get
                {
                    return new string[] { "Your stand is Notorious B.I.G!",
                "After death, an invulnerable bot will spawn in your team.",
                    "However, it can be killed with a rocket or other stand"};
                }
            }

            public override void ActivateAbilities(IPlayer player, VirtualKeyInfo[] keyEvents) { }

            bool botSpawned = false;

            private void SpawnBot(IPlayer plr)
            {
                if (plr == owner)
                {
                    Vector2 ownerPos = owner.GetWorldPosition();

                    IPlayer bot = Game.CreatePlayer(ownerPos);
                    bot.SetBotBehavior(new BotBehavior(true, PredefinedAIType.BotA));
                    bot.SetInputEnabled(true);
                    bot.SetBotName("Notorious B.I.G");
                    bot.SetTeam(ownerTeam);
                    bot.SetProfile(ownerProfile);
                    bot.SetModifiers(new PlayerModifiers()
                    {
                        RunSpeedModifier = 10,
                        SprintSpeedModifier = 10,
                        MeleeDamageDealtModifier = 10,
                        ProjectileDamageTakenModifier = 0,
                        MeleeDamageTakenModifier = 0,
                        ExplosionDamageTakenModifier = 0,
                        FireDamageTakenModifier = 0,
                        ImpactDamageTakenModifier = 0,
                        ProjectileCritChanceTakenModifier = 0
                    });

                    botSpawned = true;

                    if (onDeathCallback != null)
                        onDeathCallback.Stop();
                }
            }

            public override void FinalizeTheStand(bool rewriting = false)
            {
                if (!rewriting && botSpawned == false)
                    SpawnBot(owner);

                if (onDeathCallback != null)
                    onDeathCallback.Stop();

                base.FinalizeTheStand(rewriting);
            }
        }

        class BadCompany : Stand
        {
            public BadCompany(IPlayer plr) : base(plr)
            {
                PlayerTeam ownrTm = plr.GetTeam();

                if (ownrTm == PlayerTeam.Independent)
                    ownerTeam = PlayerTeam.Team3;
                else
                    ownerTeam = ownrTm;

                owner.SetTeam(ownerTeam);

                ownerProfile = owner.GetProfile();
            }

            public override PlayerModifiers Modifiers
            {
                get
                {
                    return new PlayerModifiers()
                    {
                        MaxHealth = 100,
                        CurrentHealth = 100
                    };
                }
            }

            public override string Name { get { return "Bad Company"; } }

            public override string[] Description
            {
                get
                {
                    return new string[] { "Your stand is Bad Company!",
                "ALT + D - Bad Company - \nSpawns 2 to 7 small bots with an M60 \non your team (COOLDOWN 60S)" };
                }
            }

            PlayerTeam ownerTeam;
            IProfile ownerProfile;

            private const float badCompanyCooldown = 60000;

            private float lastBadCompanyTime = -60000;

            bool badCompanyAllowed = true;

            public override void ActivateAbilities(IPlayer player, VirtualKeyInfo[] keyEvents)
            {
                if (player == owner)
                {
                    for (int i = 0; i < keyEvents.Length; i++)
                    {
                        bool badCompanyAbilityCasted = keyEvents[i].Event == VirtualKeyEvent.Pressed &&
                            keyEvents[i].Key == VirtualKey.BLOCK && owner.KeyPressed(VirtualKey.WALKING);

                        badCompanyAllowed = (Game.TotalElapsedGameTime - lastBadCompanyTime >=
                            badCompanyCooldown);

                        if (badCompanyAbilityCasted && badCompanyAllowed)
                            PerformBadCompany();
                    }
                }
            }

            private void PerformBadCompany()
            {
                Vector2 ownerPos = owner.GetWorldPosition();

                IPlayer[] bcBots = new IPlayer[new Random().Next(2, 8)];

                for (int i = 0; i < bcBots.Length; i++)
                {
                    float botX = i > (bcBots.Length - 1) / 2 ? -1f : 1f;

                    IPlayer bot = Game.CreatePlayer(new Vector2(ownerPos.X + 5 * ((i + 1) * botX), ownerPos.Y));
                    bot.SetBotBehavior(new BotBehavior(true, PredefinedAIType.BotA));
                    bot.SetInputEnabled(true);
                    bot.SetBotName("Bad Company");
                    bot.SetTeam(ownerTeam);
                    bot.SetProfile(ownerProfile);
                    bot.SetModifiers(new PlayerModifiers()
                    {
                        MaxHealth = 50,
                        CurrentHealth = 50,
                        SizeModifier = 0.5f
                    });
                    bot.GiveWeaponItem(WeaponItem.M60);

                    bcBots[i] = bot;
                }

                Events.PlayerWeaponRemovedActionCallback m60RemovedCallback = null;

                m60RemovedCallback = Events.PlayerWeaponRemovedActionCallback.Start((plr, arg) =>
                {
                    if (bcBots.Contains(plr) && arg.WeaponItem == WeaponItem.M60)
                    {
                        Game.GetObject(arg.TargetObjectID).Destroy();
                    }
                });

                #region -=====================-COMMON STUFF-=====================-

                Game.PlayEffect("CFTXT", owner.GetWorldPosition() + new Vector2(0, 5), "BAD COMPANY!\nCooldown 60S!");

                lastBadCompanyTime = Game.TotalElapsedGameTime;

                Events.UpdateCallback notifier = null;

                const int checkForFlagTime = 200;

                notifier = Events.UpdateCallback.Start((float e) =>
                {
                    // Updating the value.
                    badCompanyAllowed = (Game.TotalElapsedGameTime - lastBadCompanyTime >=
                        badCompanyCooldown);

                    bool notifyingMakesSense = PlayerStandList.ContainsKey(owner) &&
                        PlayerStandList[owner] is BadCompany && badCompanyAllowed;

                    bool ownerHasDifferentStand = PlayerStandList.ContainsKey(owner) &&
                        !(PlayerStandList[owner] is BadCompany);

                    if (notifyingMakesSense)
                    {
                        int usID = owner.GetUser() == null ? 666 : owner.GetUser().UserIdentifier;
                        Game.ShowChatMessage("Bad Company ready!", Color.Magenta, usID);
                        Game.PlayEffect("CFTXT", owner.GetWorldPosition() + new Vector2(0, 5), "Bad Company ready!");
                        notifier.Stop();
                    }
                    else if (ownerHasDifferentStand)
                        notifier.Stop();
                }, checkForFlagTime);

                #endregion -=====================================================-
            }
        }

        class CrazyDiamond : Stand
        {
            public CrazyDiamond(IPlayer plr) : base(plr) { }

            public override PlayerModifiers Modifiers
            {
                get
                {
                    return new PlayerModifiers()
                    {
                        MaxHealth = 200,
                        CurrentHealth = 200,
                        MeleeDamageDealtModifier = 1.5f
                    };
                }
            }

            public override string Name { get { return "Crazy Diamond"; } }

            public override string[] Description
            {
                get
                {
                    return new string[] { "Your stand is Crazy Diamond!",
                            "ALT + A - Dynamic Recovery - \nON HIT Makes a static object dynamic (COOLDOWN 5S)",
                        "ALT + S - Recovery - Restores certain types of debris \nback to objects around you (COOLDOWN 15S)",
                        "ALT + D - Static Recovery - \nON HIT Makes a dynamic object static (COOLDOWN 5S)" };
                }
            }

            private const float staticRecoveryCooldown = 5000, dynamicRecoveryCooldown = 5000, recoveryCooldown = 15000;

            private float lastStaticRecoveryTime = -5000, lastDynamicRecoveryTime = -5000, lastRecoveryTime = -15000;

            bool staticRecoveryAllowed = true, dynamicRecoveryAllowed = true, recoveryAllowed = true;

            public override void ActivateAbilities(IPlayer player, VirtualKeyInfo[] keyEvents)
            {
                if (player == owner)
                {
                    for (int i = 0; i < keyEvents.Length; i++)
                    {
                        bool staticRecoveryAbilityCasted = keyEvents[i].Event == VirtualKeyEvent.Pressed &&
                            keyEvents[i].Key == VirtualKey.BLOCK && owner.KeyPressed(VirtualKey.WALKING);

                        bool dynamicRecoveryAbilityCasted = keyEvents[i].Event == VirtualKeyEvent.Pressed &&
                            keyEvents[i].Key == VirtualKey.ATTACK && owner.KeyPressed(VirtualKey.WALKING);

                        bool recoveryAbilityCasted = keyEvents[i].Event == VirtualKeyEvent.Pressed &&
                            keyEvents[i].Key == VirtualKey.KICK && owner.KeyPressed(VirtualKey.WALKING);

                        staticRecoveryAllowed = (Game.TotalElapsedGameTime - lastStaticRecoveryTime >=
                            staticRecoveryCooldown);

                        dynamicRecoveryAllowed = (Game.TotalElapsedGameTime - lastDynamicRecoveryTime >=
                            dynamicRecoveryCooldown);

                        recoveryAllowed = (Game.TotalElapsedGameTime - lastRecoveryTime) >=
                            recoveryCooldown;

                        if (staticRecoveryAbilityCasted && staticRecoveryAllowed)
                            PerformStaticRecovery();
                        else if (dynamicRecoveryAbilityCasted && dynamicRecoveryAllowed)
                            PerformDynamicRecovery();
                        else if (recoveryAbilityCasted && recoveryAllowed)
                            PerformRecovery();
                    }
                }
            }

            Events.PlayerMeleeActionCallback meleeActionCallback = null;

            private void PerformStaticRecovery()
            {
                if (meleeActionCallback != null)
                    meleeActionCallback.Stop();

                meleeActionCallback = Events.PlayerMeleeActionCallback.Start((plr, args) =>
                {
                    if (plr == owner)
                    {
                        foreach (PlayerMeleeHitArg arg in args)
                        {
                            if (!arg.IsPlayer)
                            {
                                IObject hitObject = Game.GetObject(arg.ObjectID);
                                hitObject.SetBodyType(BodyType.Static);

                                meleeActionCallback.Stop();
                            }
                        }
                    }
                });

                #region -=====================-COMMON STUFF-=====================-

                Game.PlayEffect("CFTXT", owner.GetWorldPosition() + new Vector2(0, 5), "STATIC RECOVERY!\nCooldown 5S!");

                lastStaticRecoveryTime = Game.TotalElapsedGameTime;

                Events.UpdateCallback notifier = null;

                const int checkForFlagTime = 200;

                notifier = Events.UpdateCallback.Start((float e) =>
                {
                    // Updating the value.
                    staticRecoveryAllowed = (Game.TotalElapsedGameTime - lastStaticRecoveryTime >=
                        staticRecoveryCooldown);

                    bool notifyingMakesSense = PlayerStandList.ContainsKey(owner) &&
                        PlayerStandList[owner] is CrazyDiamond && staticRecoveryAllowed;

                    bool ownerHasDifferentStand = PlayerStandList.ContainsKey(owner) &&
                        !(PlayerStandList[owner] is CrazyDiamond);

                    if (notifyingMakesSense)
                    {
                        int usID = owner.GetUser() == null ? 666 : owner.GetUser().UserIdentifier;
                        Game.ShowChatMessage("Static Recovery ready!", Color.Magenta, usID);
                        Game.PlayEffect("CFTXT", owner.GetWorldPosition() + new Vector2(0, 5), "Static Recovery ready!");
                        notifier.Stop();
                    }
                    else if (ownerHasDifferentStand)
                        notifier.Stop();
                }, checkForFlagTime);

                #endregion -=====================================================-
            }

            private void PerformDynamicRecovery()
            {
                if (meleeActionCallback != null)
                    meleeActionCallback.Stop();

                meleeActionCallback = Events.PlayerMeleeActionCallback.Start((plr, args) =>
                {
                    if (plr == owner)
                    {
                        foreach (PlayerMeleeHitArg arg in args)
                        {
                            if (!arg.IsPlayer)
                            {
                                IObject hitObject = Game.GetObject(arg.ObjectID);
                                hitObject.SetBodyType(BodyType.Dynamic);

                                hitObject.SetLinearVelocity(new Vector2(owner.FacingDirection *
                                    5, 0));

                                meleeActionCallback.Stop();
                            }
                        }
                    }
                });

                #region -=====================-COMMON STUFF-=====================-

                Game.PlayEffect("CFTXT", owner.GetWorldPosition() + new Vector2(0, 5), "DYNAMIC RECOVERY!\nCooldown 5S!");

                lastDynamicRecoveryTime = Game.TotalElapsedGameTime;

                Events.UpdateCallback notifier = null;

                const int checkForFlagTime = 200;

                notifier = Events.UpdateCallback.Start((float e) =>
                {
                    // Updating the value.
                    dynamicRecoveryAllowed = (Game.TotalElapsedGameTime - lastDynamicRecoveryTime >=
                        dynamicRecoveryCooldown);

                    bool notifyingMakesSense = PlayerStandList.ContainsKey(owner) &&
                        PlayerStandList[owner] is CrazyDiamond && dynamicRecoveryAllowed;

                    bool ownerHasDifferentStand = PlayerStandList.ContainsKey(owner) &&
                        !(PlayerStandList[owner] is CrazyDiamond);

                    if (notifyingMakesSense)
                    {
                        int usID = owner.GetUser() == null ? 666 : owner.GetUser().UserIdentifier;
                        Game.ShowChatMessage("Dynamic Recovery ready!", Color.Magenta, usID);
                        Game.PlayEffect("CFTXT", owner.GetWorldPosition() + new Vector2(0, 5), "Dynamic Recovery ready!");
                        notifier.Stop();
                    }
                    else if (ownerHasDifferentStand)
                        notifier.Stop();
                }, checkForFlagTime);

                #endregion -=====================================================-
            }

            private void PerformRecovery()
            {
                Vector2 ownerPos = owner.GetWorldPosition();
                Area areaToRecover = new Area(ownerPos.Y + 40, ownerPos.X - 40,
                    ownerPos.Y - 40, ownerPos.X + 40);

                List<IObject> allDebris = (Game.GetObjectsByArea(areaToRecover)).
                    Where(o => o.Name.ToUpper().Contains("DEBRIS")).ToList();

                if (allDebris.Count > 0)
                {
                    string[] stoneObjectsList = new string[] { "StoneWeak00A",
                    "StoneWeak00B", "StoneWeak00C" };

                    string[] metalObjectsList = new string[] { "MetalHatch00A",
                    "MetalHatch00B", "MetalDesk00", "MetalRailing00",
                    "MetalRailing00_D", "MetalTable00", "Barrel00", "BarrelExplosive" };

                    string[] woodObjectsList = new string[] { "Crate00",
                    "Crate01", "Crate02", "WoodRailing00", "WoodSupport00A",
                    "WoodBarrel00", "Table00", "Chair00"};

                    Random rnd = new Random();

                    Vector2 objCreatedPos;

                    IObject[] stoneDebris = allDebris.Where(o => o.Name.
                        ToUpper().Contains("STONE")).ToArray();

                    if (stoneDebris != null && stoneDebris.Length > 0)
                    {
                        if (stoneDebris.Length > 1)
                        {
                            for (int j = 0; j < stoneDebris.Length; j += 2)
                            {
                                objCreatedPos = new Vector2(rnd.Next((int)areaToRecover.Left,
                                    (int)areaToRecover.Right + 1), ownerPos.Y);

                                IObject createdObject = Game.CreateObject(stoneObjectsList[rnd.Next
                                    (0, stoneObjectsList.Length)], objCreatedPos);

                                createdObject.SetBodyType(BodyType.Dynamic);

                                allDebris.Remove(stoneDebris[j]);
                                stoneDebris[j].Remove();

                                if (stoneDebris.Length > j + 1)
                                {
                                    allDebris.Remove(stoneDebris[j + 1]);
                                    stoneDebris[j + 1].Remove();
                                }
                            }
                        }
                        else if (stoneDebris.Length == 1)
                        {
                            for (int j = 0; j < stoneDebris.Length; j++)
                            {
                                objCreatedPos = new Vector2(rnd.Next((int)areaToRecover.Left,
                                    (int)areaToRecover.Right + 1), ownerPos.Y);

                                IObject createdObject = Game.CreateObject(stoneObjectsList[rnd.Next
                                    (0, stoneObjectsList.Length)], objCreatedPos);

                                createdObject.SetBodyType(BodyType.Dynamic);

                                allDebris.Remove(stoneDebris[j]);
                                stoneDebris[j].Remove();
                            }
                        }
                    }

                    IObject[] woodDebris = allDebris.Where(o => o.Name.
                        ToUpper().Contains("WOOD")).ToArray();

                    if (woodDebris != null && woodDebris.Length > 0)
                    {
                        if (woodDebris.Length > 1)
                        {
                            for (int j = 0; j < woodDebris.Length; j += 2)
                            {
                                objCreatedPos = new Vector2(rnd.Next((int)areaToRecover.Left,
                                    (int)areaToRecover.Right + 1), ownerPos.Y);

                                IObject createdObject = Game.CreateObject(woodObjectsList[rnd.Next
                                    (0, woodObjectsList.Length)], objCreatedPos);

                                createdObject.SetBodyType(BodyType.Dynamic);

                                allDebris.Remove(woodDebris[j]);
                                woodDebris[j].Remove();

                                if (woodDebris.Length > j + 1)
                                {
                                    allDebris.Remove(woodDebris[j + 1]);
                                    woodDebris[j + 1].Remove();
                                }
                            }
                        }
                        else if (woodDebris.Length == 1)
                        {
                            for (int j = 0; j < woodDebris.Length; j++)
                            {
                                objCreatedPos = new Vector2(rnd.Next((int)areaToRecover.Left,
                                    (int)areaToRecover.Right + 1), ownerPos.Y);

                                IObject createdObject = Game.CreateObject(woodObjectsList[rnd.Next
                                    (0, woodObjectsList.Length)], objCreatedPos);

                                createdObject.SetBodyType(BodyType.Dynamic);

                                allDebris.Remove(woodDebris[j]);
                                woodDebris[j].Remove();
                            }
                        }
                    }

                    IObject[] metalDebris = allDebris.Where(o => o.Name.
                        ToUpper().Contains("METAL")).ToArray();

                    if (metalDebris != null && metalDebris.Length > 0)
                    {
                        if (metalDebris.Length > 1)
                        {
                            for (int j = 0; j < metalDebris.Length; j += 2)
                            {
                                objCreatedPos = new Vector2(rnd.Next((int)areaToRecover.Left,
                                    (int)areaToRecover.Right + 1), ownerPos.Y);

                                IObject createdObject = Game.CreateObject(metalObjectsList[rnd.Next
                                    (0, metalObjectsList.Length)], objCreatedPos);

                                createdObject.SetBodyType(BodyType.Dynamic);

                                allDebris.Remove(metalDebris[j]);
                                metalDebris[j].Remove();

                                if (metalDebris.Length > j + 1)
                                {
                                    allDebris.Remove(metalDebris[j + 1]);
                                    metalDebris[j + 1].Remove();
                                }
                            }
                        }
                        else if (metalDebris.Length == 1)
                        {
                            for (int j = 0; j < metalDebris.Length; j++)
                            {
                                objCreatedPos = new Vector2(rnd.Next((int)areaToRecover.Left,
                                    (int)areaToRecover.Right + 1), ownerPos.Y);

                                IObject createdObject = Game.CreateObject(metalObjectsList[rnd.Next
                                    (0, metalObjectsList.Length)], objCreatedPos);

                                createdObject.SetBodyType(BodyType.Dynamic);

                                allDebris.Remove(metalDebris[j]);
                                metalDebris[j].Remove();
                            }
                        }
                    }

                    #region -=====================-COMMON STUFF-=====================-

                    Game.PlayEffect("CFTXT", owner.GetWorldPosition() + new Vector2(0, 5), "RECOVERY!\nCooldown 15S!");

                    lastRecoveryTime = Game.TotalElapsedGameTime;

                    Events.UpdateCallback notifier = null;

                    const int checkForFlagTime = 200;

                    notifier = Events.UpdateCallback.Start((float e) =>
                    {
                        // Updating the value.
                        recoveryAllowed = (Game.TotalElapsedGameTime - lastRecoveryTime >=
                                recoveryCooldown);

                        bool notifyingMakesSense = PlayerStandList.ContainsKey(owner) &&
                            PlayerStandList[owner] is CrazyDiamond && recoveryAllowed;

                        bool ownerHasDifferentStand = PlayerStandList.ContainsKey(owner) &&
                            !(PlayerStandList[owner] is CrazyDiamond);

                        if (notifyingMakesSense)
                        {
                            int usID = owner.GetUser() == null ? 666 : owner.GetUser().UserIdentifier;
                            Game.ShowChatMessage("Recovery ready!", Color.Magenta, usID);
                            Game.PlayEffect("CFTXT", owner.GetWorldPosition() + new Vector2(0, 5), "Recovery ready!");
                            notifier.Stop();
                        }
                        else if (ownerHasDifferentStand)
                            notifier.Stop();
                    }, checkForFlagTime);

                    #endregion -=====================================================-
                }
            }
        }

        class TheHand : Stand
        {
            public TheHand(IPlayer plr) : base(plr) { }

            public override PlayerModifiers Modifiers
            {
                get
                {
                    return new PlayerModifiers()
                    {
                        MaxHealth = 200,
                        CurrentHealth = 200,
                        MeleeDamageDealtModifier = 1.5f,
                        SprintSpeedModifier = 0.75f,
                        RunSpeedModifier = 0.75f
                    };
                }
            }

            public override string Name { get { return "The Hand"; } }

            public override string[] Description
            {
                get
                {
                    return new string[] { "Your stand is The Hand!",
                            "ALT + A - Liquidation - \nON HIT Erases a player of object (COOLDOWN 25S)",
                        "ALT + D - Space Removing - \nErases space in front of you and moves \nplayers and dynamic objects (COOLDOWN 20S)" };
                }
            }

            private const float liquidationCooldown = 25000, spaceRemovingCooldown = 20000;

            private float liquidationRecoveryTime = -25000, spaceRemovingRecoveryTime = -20000;

            bool liquidationAllowed = true, spaceRemovingAllowed = true;

            public override void ActivateAbilities(IPlayer player, VirtualKeyInfo[] keyEvents)
            {
                if (player == owner)
                {
                    for (int i = 0; i < keyEvents.Length; i++)
                    {
                        bool liquidationAbilityCasted = keyEvents[i].Event == VirtualKeyEvent.Pressed &&
                            keyEvents[i].Key == VirtualKey.ATTACK && owner.KeyPressed(VirtualKey.WALKING);

                        bool spaceRemovingAbilityCasted = keyEvents[i].Event == VirtualKeyEvent.Pressed &&
                            keyEvents[i].Key == VirtualKey.BLOCK && owner.KeyPressed(VirtualKey.WALKING);

                        liquidationAllowed = (Game.TotalElapsedGameTime - liquidationRecoveryTime >=
                            liquidationCooldown);

                        spaceRemovingAllowed = (Game.TotalElapsedGameTime - spaceRemovingRecoveryTime >=
                            spaceRemovingCooldown);

                        if (liquidationAbilityCasted && liquidationAllowed)
                            PerformLiquidation();
                        else if (spaceRemovingAbilityCasted && spaceRemovingAllowed)
                            PerformSpaceRemoving();
                    }
                }
            }

            private void PerformLiquidation()
            {
                Events.PlayerMeleeActionCallback meleeActionCallback = null;

                meleeActionCallback = Events.PlayerMeleeActionCallback.Start((plr, args) =>
                {
                    if (plr == owner)
                    {
                        foreach (PlayerMeleeHitArg arg in args)
                        {
                            IObject hitObject = arg.HitObject;
                            hitObject.Remove();

                            for (int i = 0; i < 5; i++)
                                Game.PlaySound("BalloonPop", owner.GetWorldPosition(), 1);

                            meleeActionCallback.Stop();
                        }
                    }
                });

                #region -=====================-COMMON STUFF-=====================-

                Game.PlayEffect("CFTXT", owner.GetWorldPosition() + new Vector2(0, 5), "LIQUIDATION!\nCooldown 25S!");

                liquidationRecoveryTime = Game.TotalElapsedGameTime;

                Events.UpdateCallback notifier = null;

                const int checkForFlagTime = 200;

                notifier = Events.UpdateCallback.Start((float e) =>
                {
                    // Updating the value.
                    liquidationAllowed = (Game.TotalElapsedGameTime - liquidationRecoveryTime >=
                        liquidationCooldown);

                    bool notifyingMakesSense = PlayerStandList.ContainsKey(owner) &&
                        PlayerStandList[owner] is TheHand && liquidationAllowed;

                    bool ownerHasDifferentStand = PlayerStandList.ContainsKey(owner) &&
                        !(PlayerStandList[owner] is TheHand);

                    if (notifyingMakesSense)
                    {
                        int usID = owner.GetUser() == null ? 666 : owner.GetUser().UserIdentifier;
                        Game.ShowChatMessage("Liquidation ready!", Color.Magenta, usID);
                        Game.PlayEffect("CFTXT", owner.GetWorldPosition() + new Vector2(0, 5), "Liquidation ready!");
                        notifier.Stop();
                    }
                    else if (ownerHasDifferentStand)
                        notifier.Stop();
                }, checkForFlagTime);

                #endregion -=====================================================-
            }

            private void PerformSpaceRemoving()
            {
                Vector2 ownerPos = owner.GetWorldPosition();
                int faceDir = owner.FacingDirection;

                Area areaToRemove, areaToMove;

                float moveObj = 0;

                switch (faceDir)
                {
                    case -1:
                        areaToRemove = new Area(ownerPos.Y + 10, ownerPos.X - 40,
                            ownerPos.Y - 10, ownerPos.X);

                        areaToMove = new Area(ownerPos.Y + 10, ownerPos.X - 1000,
                            ownerPos.Y - 10, ownerPos.X);

                        moveObj = 40;
                        break;
                    case 1:
                        areaToRemove = new Area(ownerPos.Y + 10, ownerPos.X,
                            ownerPos.Y - 10, ownerPos.X + 40);

                        areaToMove = new Area(ownerPos.Y + 10, ownerPos.X,
                            ownerPos.Y - 10, ownerPos.X + 1000);

                        moveObj = -40;
                        break;
                    default:
                        areaToRemove = default(Area);
                        areaToMove = default(Area);
                        break;
                }

                IObject[] objToRemove = Game.GetObjectsByArea(areaToRemove).
                    Where(o => o != owner && (o.Destructable || o.GetBodyType() == BodyType.Dynamic ||
                          o is IPlayer)).ToArray();

                foreach (IObject obj in objToRemove)
                    obj.Remove();

                IObject[] objToMove = Game.GetObjectsByArea(areaToMove).
                    Where(o => o != owner && (o.Destructable || o.GetBodyType() == BodyType.Dynamic ||
                          o is IPlayer)).ToArray();

                foreach (IObject obj in objToMove)
                {
                    Vector2 objCurrPos = obj.GetWorldPosition();
                    obj.SetWorldPosition(new Vector2(objCurrPos.X + moveObj, objCurrPos.Y));
                }

                for (int i = 0; i < 5; i++)
                    Game.PlaySound("DestroyPaper", ownerPos, 1);

                #region -=====================-COMMON STUFF-=====================-

                Game.PlayEffect("CFTXT", owner.GetWorldPosition() + new Vector2(0, 5), "SPACE REMOVING!\nCooldown 20S!");

                spaceRemovingRecoveryTime = Game.TotalElapsedGameTime;

                Events.UpdateCallback notifier = null;

                const int checkForFlagTime = 200;

                notifier = Events.UpdateCallback.Start((float e) =>
                {
                    // Updating the value.
                    spaceRemovingAllowed = (Game.TotalElapsedGameTime - spaceRemovingRecoveryTime >=
                        spaceRemovingCooldown);

                    bool notifyingMakesSense = PlayerStandList.ContainsKey(owner) &&
                        PlayerStandList[owner] is TheHand && spaceRemovingAllowed;

                    bool ownerHasDifferentStand = PlayerStandList.ContainsKey(owner) &&
                        !(PlayerStandList[owner] is TheHand);

                    if (notifyingMakesSense)
                    {
                        int usID = owner.GetUser() == null ? 666 : owner.GetUser().UserIdentifier;
                        Game.ShowChatMessage("Space Removing ready!", Color.Magenta, usID);
                        Game.PlayEffect("CFTXT", owner.GetWorldPosition() + new Vector2(0, 5), "Space Removing ready!");
                        notifier.Stop();
                    }
                    else if (ownerHasDifferentStand)
                        notifier.Stop();
                }, checkForFlagTime);

                #endregion -=====================================================-
            }
        }

        class KillerQueen : Stand
        {
            public KillerQueen(IPlayer plr) : base(plr) { }

            public override PlayerModifiers Modifiers
            {
                get
                {
                    return new PlayerModifiers()
                    {
                        ExplosionDamageTakenModifier = 0,
                    };
                }
            }

            public override string Name { get { return "Killer Queen"; } }

            public override string[] Description
            {
                get
                {
                    return new string[] { "Your stand is Killer Queen!",
                            "ALT + A - First Bomb - \nON HIT Explodes a player or object \nafter 5 seconds (COOLDOWN 30S)",
                        "ALT + S - Bites The Dust - \nON HIT Respawn the player at a spawn \npoint without a stand (COOLDOWN 25S)",
                        "ALT + D - Sheer Heart Attack - \nReleases a bird that explodes when it meets \na player or dynamic object (COOLDOWN 30S)" };
                }
            }

            private const float sheerHeartAttackCooldown = 30000, firstBombCooldown = 30000, bitesTheDustCooldown = 25000;

            private float lastSheerHeartAttackTime = -30000, lastFirstBombTime = -30000, lastBitesTheDustTime = -25000;

            bool sheerHeartAttackAllowed = true, firstBombAllowed = true, bitesTheDustAllowed = true;

            public override void ActivateAbilities(IPlayer player, VirtualKeyInfo[] keyEvents)
            {
                if (player == owner)
                {
                    for (int i = 0; i < keyEvents.Length; i++)
                    {
                        bool sheerHeartAttackAbilityCasted = keyEvents[i].Event == VirtualKeyEvent.Pressed &&
                            keyEvents[i].Key == VirtualKey.BLOCK && owner.KeyPressed(VirtualKey.WALKING);

                        bool firstBombAbilityCasted = keyEvents[i].Event == VirtualKeyEvent.Pressed &&
                            keyEvents[i].Key == VirtualKey.ATTACK && owner.KeyPressed(VirtualKey.WALKING);

                        bool bitesTheDustCasted = keyEvents[i].Event == VirtualKeyEvent.Pressed &&
                            keyEvents[i].Key == VirtualKey.KICK && owner.KeyPressed(VirtualKey.WALKING);

                        sheerHeartAttackAllowed = (Game.TotalElapsedGameTime - lastSheerHeartAttackTime >=
                            sheerHeartAttackCooldown);

                        firstBombAllowed = (Game.TotalElapsedGameTime - lastFirstBombTime >=
                            firstBombCooldown);

                        bitesTheDustAllowed = (Game.TotalElapsedGameTime - lastBitesTheDustTime) >=
                            bitesTheDustCooldown;

                        if (sheerHeartAttackAbilityCasted && sheerHeartAttackAllowed)
                            PerformSheerHeartAttack();
                        else if (firstBombAbilityCasted && firstBombAllowed)
                            PerformFirstBomb();
                        else if (bitesTheDustCasted && bitesTheDustAllowed)
                            PerformBitesTheDust();
                    }
                }
            }

            private void PerformSheerHeartAttack()
            {
                Vector2 ownerPos = owner.GetWorldPosition();

                IObject birdie = Game.CreateObject("Dove00", new Vector2(ownerPos.X + (10 * owner.FacingDirection),
                    ownerPos.Y + 10));

                float creationTime = Game.TotalElapsedGameTime, explosionTime = 10000f;

                Events.UpdateCallback explodeInTime = null, explodeOnCollision = null;

                Vector2 birdiePos = birdie.GetWorldPosition();

                explodeInTime = Events.UpdateCallback.Start((ms) =>
                {
                    if (birdie != null && !birdie.IsRemoved)
                    {
                        if (Game.TotalElapsedGameTime - creationTime >= explosionTime)
                        {
                            Game.TriggerExplosion(birdiePos);

                            if (birdie != null && !birdie.IsRemoved)
                                birdie.Remove();

                            if (explodeOnCollision != null)
                                explodeOnCollision.Stop();

                            explodeInTime.Stop();
                        }
                    }
                    else
                    {
                        Game.TriggerExplosion(birdiePos);

                        if (birdie != null && !birdie.IsRemoved)
                            birdie.Remove();

                        if (explodeOnCollision != null)
                            explodeOnCollision.Stop();

                        explodeInTime.Stop();
                    }
                }, 500);

                explodeOnCollision = Events.UpdateCallback.Start((ms) =>
                {
                    if (birdie != null && !birdie.IsRemoved)
                    {
                        birdiePos = birdie.GetWorldPosition();

                        Area birdieArea = birdie.GetAABB();

                        IObject[] dynamicObjects = Game.GetObjectsByArea(birdieArea).
                            Where(o => o != birdie && o != owner && !(o is IObjectWeaponItem) &&
                            (o is IPlayer || o.GetBodyType() == BodyType.Dynamic)).ToArray();

                        if (dynamicObjects != null && dynamicObjects.Length > 0)
                        {
                            Game.TriggerExplosion(birdiePos);

                            if (birdie != null && !birdie.IsRemoved)
                                birdie.Remove();

                            if (explodeInTime != null)
                                explodeInTime.Stop();

                            explodeOnCollision.Stop();
                        }
                    }
                    else
                    {
                        Game.TriggerExplosion(birdiePos);

                        if (birdie != null && !birdie.IsRemoved)
                            birdie.Remove();

                        if (explodeInTime != null)
                            explodeInTime.Stop();

                        explodeOnCollision.Stop();
                    }
                }, 250);

                #region -=====================-COMMON STUFF-=====================-

                Game.PlayEffect("CFTXT", owner.GetWorldPosition() + new Vector2(0, 5), "SHEER HEART ATTACK!\nCooldown 30S!");

                lastSheerHeartAttackTime = Game.TotalElapsedGameTime;

                Events.UpdateCallback notifier = null;

                const int checkForFlagTime = 200;

                notifier = Events.UpdateCallback.Start((float e) =>
                {
                    // Updating the value.
                    sheerHeartAttackAllowed = (Game.TotalElapsedGameTime - lastSheerHeartAttackTime >=
                        sheerHeartAttackCooldown);

                    bool notifyingMakesSense = PlayerStandList.ContainsKey(owner) &&
                        PlayerStandList[owner] is KillerQueen && sheerHeartAttackAllowed;

                    bool ownerHasDifferentStand = PlayerStandList.ContainsKey(owner) &&
                        !(PlayerStandList[owner] is KillerQueen);

                    if (notifyingMakesSense)
                    {
                        int usID = owner.GetUser() == null ? 666 : owner.GetUser().UserIdentifier;
                        Game.ShowChatMessage("Sheer Heart Attack ready!", Color.Magenta, usID);
                        Game.PlayEffect("CFTXT", owner.GetWorldPosition() + new Vector2(0, 5), "Sheer Heart Attack ready!");
                        notifier.Stop();
                    }
                    else if (ownerHasDifferentStand)
                        notifier.Stop();
                }, checkForFlagTime);

                #endregion -=====================================================-
            }

            private void PerformFirstBomb()
            {
                Events.PlayerMeleeActionCallback meleeActionCallback = null;

                meleeActionCallback = Events.PlayerMeleeActionCallback.Start((plr, args) =>
                {
                    if (plr == owner)
                    {
                        foreach (PlayerMeleeHitArg arg in args)
                        {
                            float activationTime = Game.TotalElapsedGameTime,
                                explosionTime = 5000f;

                            IObject hitObject = arg.HitObject;

                            Events.UpdateCallback onTimerPass = null;

                            onTimerPass = Events.UpdateCallback.Start((ms) =>
                            {
                                if (hitObject != null && !hitObject.IsRemoved)
                                {
                                    if (Game.TotalElapsedGameTime - activationTime >= explosionTime)
                                    {
                                        Vector2 objPos = hitObject.GetWorldPosition();
                                        Game.TriggerExplosion(objPos);

                                        onTimerPass.Stop();
                                    }
                                }
                                else
                                    onTimerPass.Stop();
                            }, 500);

                            meleeActionCallback.Stop();
                        }
                    }
                });

                #region -=====================-COMMON STUFF-=====================-

                Game.PlayEffect("CFTXT", owner.GetWorldPosition() + new Vector2(0, 5), "FIRST BOMB!\nCooldown 30S!");

                lastFirstBombTime = Game.TotalElapsedGameTime;

                Events.UpdateCallback notifier = null;

                const int checkForFlagTime = 200;

                notifier = Events.UpdateCallback.Start((float e) =>
                {
                    // Updating the value.
                    firstBombAllowed = (Game.TotalElapsedGameTime - lastFirstBombTime >=
                        firstBombCooldown);

                    bool notifyingMakesSense = PlayerStandList.ContainsKey(owner) &&
                        PlayerStandList[owner] is KillerQueen && firstBombAllowed;

                    bool ownerHasDifferentStand = PlayerStandList.ContainsKey(owner) &&
                        !(PlayerStandList[owner] is KillerQueen);

                    if (notifyingMakesSense)
                    {
                        int usID = owner.GetUser() == null ? 666 : owner.GetUser().UserIdentifier;
                        Game.ShowChatMessage("First Bomb ready!", Color.Magenta, usID);
                        Game.PlayEffect("CFTXT", owner.GetWorldPosition() + new Vector2(0, 5), "First Bomb ready!");
                        notifier.Stop();
                    }
                    else if (ownerHasDifferentStand)
                        notifier.Stop();
                }, checkForFlagTime);

                #endregion -=====================================================-
            }

            private void PerformBitesTheDust()
            {
                Events.PlayerMeleeActionCallback meleeActionCallback = null;

                meleeActionCallback = Events.PlayerMeleeActionCallback.Start((ply, args) =>
                {
                    if (ply == owner)
                    {
                        foreach (PlayerMeleeHitArg arg in args)
                        {
                            if (arg.IsPlayer)
                            {
                                IPlayer hitPlr = Game.GetPlayer(arg.ObjectID);

                                bool plrWasZeroed = plrIsZeroed.ContainsKey(hitPlr) &&
                                    plrIsZeroed[hitPlr];

                                if (!plrWasZeroed)
                                {
                                    PlayerTeam prevTeam = hitPlr.GetTeam();
                                    IUser hitUser = hitPlr.GetUser();
                                    BotBehavior hitPlrBotBehavior = null;
                                    string hitPlrName = hitPlr.Name;

                                    if (hitUser == null)
                                        hitPlrBotBehavior = hitPlr.GetBotBehavior();

                                    IProfile plrProfile = hitPlr.GetProfile();
                                    IObject[] plrSpawns = Game.GetObjectsByName("SpawnPlayer");

                                    if (plrSpawns != null && plrSpawns.Length > 0)
                                    {
                                        if (hitPlr != null && !hitPlr.IsDead && !hitPlr.IsRemoved)
                                            hitPlr.Remove();

                                        IPlayer newPlr = Game.CreatePlayer(plrSpawns[new Random().
                                            Next(0, plrSpawns.Length)].GetWorldPosition());

                                        newPlr.SetProfile(plrProfile);
                                        newPlr.SetTeam(prevTeam);

                                        if (hitUser != null)
                                        {
                                            newPlr.SetUser(hitUser);
                                        }
                                        else if (hitUser == null && hitPlrBotBehavior != null)
                                        {
                                            newPlr.SetBotBehavior(hitPlrBotBehavior);
                                            newPlr.SetBotName(hitPlrName);
                                            newPlr.SetInputEnabled(true);
                                        }
                                        else
                                        {
                                            newPlr.SetBotBehavior(new BotBehavior(true, PredefinedAIType.BotA));
                                            newPlr.SetBotName(hitPlrName);
                                            newPlr.SetInputEnabled(true);
                                        }
                                    }

                                    meleeActionCallback.Stop();
                                }
                            }
                        }
                    }
                });

                #region -=====================-COMMON STUFF-=====================-

                Game.PlayEffect("CFTXT", owner.GetWorldPosition() + new Vector2(0, 5), "BITES THE DUST!\nCooldown 25S!");

                lastBitesTheDustTime = Game.TotalElapsedGameTime;

                Events.UpdateCallback notifier = null;

                const int checkForFlagTime = 200;

                notifier = Events.UpdateCallback.Start((float e) =>
                {
                    // Updating the value.
                    bitesTheDustAllowed = (Game.TotalElapsedGameTime - lastBitesTheDustTime >=
                        bitesTheDustCooldown);

                    bool notifyingMakesSense = PlayerStandList.ContainsKey(owner) &&
                        PlayerStandList[owner] is KillerQueen && bitesTheDustAllowed;

                    bool ownerHasDifferentStand = PlayerStandList.ContainsKey(owner) &&
                        !(PlayerStandList[owner] is KillerQueen);

                    if (notifyingMakesSense)
                    {
                        int usID = owner.GetUser() == null ? 666 : owner.GetUser().UserIdentifier;
                        Game.ShowChatMessage("Bites The Dust ready!", Color.Magenta, usID);
                        Game.PlayEffect("CFTXT", owner.GetWorldPosition() + new Vector2(0, 5), "Bites The Dust ready!");
                        notifier.Stop();
                    }
                    else if (ownerHasDifferentStand)
                        notifier.Stop();
                }, checkForFlagTime);

                #endregion -=====================================================-
            }
        }

        class LittleFeet : Stand
        {
            public LittleFeet(IPlayer plr) : base(plr) { }

            public override PlayerModifiers Modifiers
            {
                get
                {
                    return new PlayerModifiers()
                    {
                        MaxHealth = 50,
                        CurrentHealth = 50
                    };
                }
            }

            public override string Name { get { return "Little Feet"; } }

            public override string[] Description
            {
                get
                {
                    return new string[] { "Your stand is Little Feet!",
                "ALT + A - Reduce - \nON HIT Shrinks the player temporarily, making \nhim weak and helpless (COOLDOWN 30S)" };
                }
            }

            private const float reduceCooldown = 30000;

            private float lastReduceTime = -30000;

            bool reduceAllowed = true;

            public override void ActivateAbilities(IPlayer player, VirtualKeyInfo[] keyEvents)
            {
                if (player == owner)
                {
                    for (int i = 0; i < keyEvents.Length; i++)
                    {
                        bool badCompanyAbilityCasted = keyEvents[i].Event == VirtualKeyEvent.Pressed &&
                            keyEvents[i].Key == VirtualKey.ATTACK && owner.KeyPressed(VirtualKey.WALKING);

                        reduceAllowed = (Game.TotalElapsedGameTime - lastReduceTime >=
                            reduceCooldown);

                        if (badCompanyAbilityCasted && reduceAllowed)
                            PerformReduce();
                    }
                }
            }

            private void PerformReduce()
            {
                Events.PlayerMeleeActionCallback meleeActionCallback = null;

                meleeActionCallback = Events.PlayerMeleeActionCallback.Start((ply, args) =>
                {
                    if (ply == owner)
                    {
                        foreach (PlayerMeleeHitArg arg in args)
                        {
                            if (arg.IsPlayer)
                            {
                                IPlayer hitPlr = Game.GetPlayer(arg.ObjectID);

                                if (hitPlr != null && !hitPlr.IsDead && !hitPlr.IsRemoved)
                                {
                                    float bigHealth = hitPlr.GetHealth();

                                    PlayerModifiers prevModifiers = hitPlr.GetModifiers(),
                                        newModifiers = new PlayerModifiers()
                                        {
                                            SizeModifier = 0,
                                            MaxHealth = 25,
                                            CurrentHealth = bigHealth < 25 ? bigHealth : 25,
                                            MeleeDamageDealtModifier = 0.1f
                                        };

                                    hitPlr.SetModifiers(newModifiers);

                                    float turningTime = Game.TotalElapsedGameTime,
                                        returnTime = 20000f;

                                    Events.UpdateCallback returnBack = null;

                                    returnBack = Events.UpdateCallback.Start((ms) =>
                                    {
                                        if (hitPlr != null && !hitPlr.IsDead && !hitPlr.IsRemoved)
                                        {
                                            if (Game.TotalElapsedGameTime - turningTime >= returnTime)
                                            {
                                                float currHealth = hitPlr.GetHealth();

                                                hitPlr.SetModifiers(prevModifiers);

                                                if (bigHealth > 25)
                                                    hitPlr.SetHealth(bigHealth - (25 - currHealth));

                                                returnBack.Stop();
                                            }
                                        }
                                        else
                                            returnBack.Stop();
                                    }, 1000);

                                    meleeActionCallback.Stop();
                                }
                            }
                        }
                    }
                });

                #region -=====================-COMMON STUFF-=====================-

                Game.PlayEffect("CFTXT", owner.GetWorldPosition() + new Vector2(0, 5), "REDUCE!\nCooldown 30S!");

                lastReduceTime = Game.TotalElapsedGameTime;

                Events.UpdateCallback notifier = null;

                const int checkForFlagTime = 200;

                notifier = Events.UpdateCallback.Start((float e) =>
                {
                    // Updating the value.
                    reduceAllowed = (Game.TotalElapsedGameTime - lastReduceTime >=
                        reduceCooldown);

                    bool notifyingMakesSense = PlayerStandList.ContainsKey(owner) &&
                        PlayerStandList[owner] is LittleFeet && reduceAllowed;

                    bool ownerHasDifferentStand = PlayerStandList.ContainsKey(owner) &&
                        !(PlayerStandList[owner] is LittleFeet);

                    if (notifyingMakesSense)
                    {
                        int usID = owner.GetUser() == null ? 666 : owner.GetUser().UserIdentifier;
                        Game.ShowChatMessage("Reduce ready!", Color.Magenta, usID);
                        Game.PlayEffect("CFTXT", owner.GetWorldPosition() + new Vector2(0, 5), "Reduce ready!");
                        notifier.Stop();
                    }
                    else if (ownerHasDifferentStand)
                        notifier.Stop();
                }, checkForFlagTime);

                #endregion -=====================================================-
            }
        }

        class PurpleHaze : Stand
        {
            public PurpleHaze(IPlayer plr) : base(plr)
            {
                poisonVirusedPlayers = Events.UpdateCallback.Start(ReduceHealthVirused, 1000);
                poisonNearPlayers = Events.UpdateCallback.Start(ReduceHealthNear, 1000);
                poisonRudePlayers = Events.PlayerDamageCallback.Start(ReduceHealthRude);
            }

            public override PlayerModifiers Modifiers
            {
                get
                {
                    return new PlayerModifiers()
                    {
                        MaxHealth = 120,
                        CurrentHealth = 120
                    };
                }
            }

            public override string Name { get { return "Purple Haze"; } }

            public override string[] Description
            {
                get
                {
                    return new string[] { "Your stand is Purple Haze!",
                "People near you and those who hit you take damage",
                        "ALT + A - Virus - \nON HIT Causes the player to lose \n10 HP every second until the end \nof the round (COOLDOWN 30S)" };
                }
            }

            List<IPlayer> listOfPoisonedPlrs = new List<IPlayer>();

            Events.UpdateCallback poisonVirusedPlayers = null, poisonNearPlayers = null;

            Events.PlayerDamageCallback poisonRudePlayers = null;

            private const float virusCooldown = 30000;

            private float lastVirusTime = -30000;

            bool virusAllowed = true;

            public override void ActivateAbilities(IPlayer player, VirtualKeyInfo[] keyEvents)
            {
                if (player == owner)
                {
                    for (int i = 0; i < keyEvents.Length; i++)
                    {
                        bool virusAbilityCasted = keyEvents[i].Event == VirtualKeyEvent.Pressed &&
                            keyEvents[i].Key == VirtualKey.ATTACK && owner.KeyPressed(VirtualKey.WALKING);

                        virusAllowed = (Game.TotalElapsedGameTime - lastVirusTime >=
                            virusCooldown);

                        if (virusAbilityCasted && virusAllowed)
                            PerformVirus();
                    }
                }
            }

            private void PerformVirus()
            {
                Events.PlayerMeleeActionCallback meleeActionCallback = null;

                meleeActionCallback = Events.PlayerMeleeActionCallback.Start((plr, args) =>
                {
                    if (plr == owner)
                    {
                        foreach (PlayerMeleeHitArg arg in args)
                        {
                            if (arg.IsPlayer)
                            {
                                IPlayer hitPlr = Game.GetPlayer(arg.ObjectID);

                                if (!hitPlr.IsDead && !hitPlr.IsRemoved &&
                                !listOfPoisonedPlrs.Contains(hitPlr))
                                {
                                    listOfPoisonedPlrs.Add(hitPlr);

                                    PlayerModifiers m = hitPlr.GetModifiers();
                                    m.RunSpeedModifier /= 1.75f;
                                    m.SprintSpeedModifier /= 1.75f;
                                    hitPlr.SetModifiers(m);

                                    meleeActionCallback.Stop();
                                }
                            }
                        }
                    }
                });

                #region -=====================-COMMON STUFF-=====================-

                Game.PlayEffect("CFTXT", owner.GetWorldPosition() + new Vector2(0, 5), "VIRUS!\nCooldown 30S!");

                lastVirusTime = Game.TotalElapsedGameTime;

                Events.UpdateCallback notifier = null;

                const int checkForFlagTime = 200;

                notifier = Events.UpdateCallback.Start((float e) =>
                {
                    // Updating the value.
                    virusAllowed = (Game.TotalElapsedGameTime - lastVirusTime >=
                        virusCooldown);

                    bool notifyingMakesSense = PlayerStandList.ContainsKey(owner) &&
                        PlayerStandList[owner] is PurpleHaze && virusAllowed;

                    bool ownerHasDifferentStand = PlayerStandList.ContainsKey(owner) &&
                        !(PlayerStandList[owner] is PurpleHaze);

                    if (notifyingMakesSense)
                    {
                        int usID = owner.GetUser() == null ? 666 : owner.GetUser().UserIdentifier;
                        Game.ShowChatMessage("Virus ready!", Color.Magenta, usID);
                        Game.PlayEffect("CFTXT", owner.GetWorldPosition() + new Vector2(0, 5), "Virus ready!");
                        notifier.Stop();
                    }
                    else if (ownerHasDifferentStand)
                        notifier.Stop();
                }, checkForFlagTime);

                #endregion -=====================================================-
            }

            private void ReduceHealthVirused(float ms)
            {
                foreach (IPlayer plr in listOfPoisonedPlrs)
                {
                    if (plr != null && !plr.IsDead && !plr.IsRemoved)
                        plr.DealDamage(10);
                }
            }

            private void ReduceHealthNear(float ms)
            {
                Vector2 ownerPos = owner.GetWorldPosition();
                Area areaToReduce = new Area(ownerPos.Y + 30, ownerPos.X - 30,
                    ownerPos.Y - 30, ownerPos.X + 30);

                IPlayer[] plrsNear = Game.GetObjectsByArea<IPlayer>(areaToReduce);

                foreach (IPlayer plr in plrsNear)
                {
                    if (plr != null && plr != owner && !plr.IsDead && !plr.IsRemoved)
                        plr.DealDamage(2);
                }
            }

            private void ReduceHealthRude(IPlayer plr, PlayerDamageArgs args)
            {
                if (plr == owner)
                {
                    IPlayer rudePlr = Game.GetPlayer(args.SourceID);

                    if (rudePlr != null)
                    {
                        rudePlr.DealDamage(10);
                    }
                }
            }

            public override void FinalizeTheStand(bool rewriting = false)
            {
                if (poisonNearPlayers != null)
                    poisonNearPlayers.Stop();

                if (poisonRudePlayers != null)
                    poisonRudePlayers.Stop();

                base.FinalizeTheStand(rewriting);
            }
        }

        class KingCrimson : Stand
        {
            public KingCrimson(IPlayer plr) : base(plr)
            {
                onDangerNearCallback = Events.UpdateCallback.Start(NeutralizeDanger);
                damageCallback = Events.PlayerDamageCallback.Start(AllowEpitaph);
            }

            public override PlayerModifiers Modifiers
            {
                get
                {
                    return new PlayerModifiers()
                    {
                        MaxHealth = 200,
                        CurrentHealth = 200,
                        MeleeDamageDealtModifier = 2.5f,
                    };
                }
            }

            Events.UpdateCallback onDangerNearCallback = null;
            Events.PlayerDamageCallback damageCallback = null;
            Events.UpdateCallback forbidEpitath = null;

            IPlayer rudePlr = null;

            public override string Name { get { return "King Crimson"; } }

            public override string[] Description
            {
                get
                {
                    return new string[] { "Your stand is King Crimson!",
                        "Projectiles (except fire) cannot hit you",
                "ALT + S - Timeskip - \nFor 8 seconds, turns you into an entity \nthat moves through any object (COOLDOWN 60S)",
                        "ALT + D - Epitaph - \nUse right after taken melee damage. \nCounterattacks the player, stuns and leaves with 1 HP" };
                }
            }

            private const float timeskipCooldown = 60000;

            private float lastTimeskipTime = -60000;

            bool timeskipAllowed = true, epitaphAllowed = false;

            public override void ActivateAbilities(IPlayer player, VirtualKeyInfo[] keyEvents)
            {
                if (player == owner)
                {
                    for (int i = 0; i < keyEvents.Length; i++)
                    {
                        bool timeskipAbilityCasted = keyEvents[i].Event == VirtualKeyEvent.Pressed &&
                            keyEvents[i].Key == VirtualKey.KICK && owner.KeyPressed(VirtualKey.WALKING);

                        bool epitaphAbilityCasted = keyEvents[i].Event == VirtualKeyEvent.Pressed &&
                            keyEvents[i].Key == VirtualKey.BLOCK && owner.KeyPressed(VirtualKey.WALKING);

                        timeskipAllowed = (Game.TotalElapsedGameTime - lastTimeskipTime >=
                            timeskipCooldown);

                        if (timeskipAbilityCasted && timeskipAllowed)
                            PerformTimeskip();
                        else if (epitaphAbilityCasted && epitaphAllowed &&
                            (rudePlr != null && !rudePlr.IsDead && !rudePlr.IsRemoved))
                            PerformEpitaph(rudePlr);
                    }
                }
            }

            private void PerformTimeskip()
            {
                Area gameCameraArea = Game.GetCameraMaxArea();

                IObject hiddenBlock = Game.CreateObject("InvisibleBlock", gameCameraArea.TopLeft);

                if (hiddenBlock != null && !hiddenBlock.DestructionInitiated)
                {
                    float hiddenBlockSize = 5 * Math.Abs(gameCameraArea.Left)
                        + Math.Abs(gameCameraArea.Right);

                    hiddenBlock.SetSizeFactor(new Point((int)hiddenBlockSize, 1));

                    Vector2 ownerOldPos = owner.GetWorldPosition();

                    owner.SetWorldPosition(new Vector2(gameCameraArea.Center.X - 50, gameCameraArea.Top + 8));
                    owner.SetNametagVisible(false);

                    Vector2 ownerNewPos = owner.GetWorldPosition(),
                        thingPos = ownerOldPos;

                    Area areaForOwner = new Area(ownerNewPos.Y + 30, ownerNewPos.X - 30,
                        ownerNewPos.Y - 20, ownerNewPos.X + 30);

                    float startTimeskipTime = Game.TotalElapsedGameTime,
                        timeskipTime = 10000f;

                    Events.UpdateCallback lookForOwnerMovement = null;

                    lookForOwnerMovement = Events.UpdateCallback.Start(ms =>
                    {
                        if (owner != null && !owner.IsDead && !owner.IsRemoved)
                        {
                            Vector2 ownerCurrPos = owner.GetWorldPosition();

                            if (ownerCurrPos.X < areaForOwner.Left || ownerCurrPos.X > areaForOwner.Right)
                                owner.SetWorldPosition(areaForOwner.Center);
                        }
                    });

                    Game.PlayEffect(EffectName.Electric, thingPos);

                    Events.UpdateCallback createParticles = null;

                    createParticles = Events.UpdateCallback.Start(ms =>
                    {
                        Game.PlayEffect(EffectName.Electric, thingPos);
                    }, 100);

                    Events.PlayerKeyInputCallback controlThing = null;

                    controlThing = Events.PlayerKeyInputCallback.Start((plr, keyEvents) =>
                    {
                        if (plr == owner)
                        {
                            for (int i = 0; i < keyEvents.Length; i++)
                            {
                                if (owner.KeyPressed(VirtualKey.AIM_RUN_LEFT))
                                    thingPos.X -= 10;

                                if (owner.KeyPressed(VirtualKey.AIM_RUN_RIGHT))
                                    thingPos.X += 10;

                                if (owner.KeyPressed(VirtualKey.AIM_CLIMB_UP))
                                    thingPos.Y += 10;

                                if (owner.KeyPressed(VirtualKey.AIM_CLIMB_DOWN))
                                    thingPos.Y -= 10;
                            }
                        }
                    });

                    Events.UpdateCallback partyEnder = null;

                    partyEnder = Events.UpdateCallback.Start(ms =>
                    {
                        if ((Game.TotalElapsedGameTime - startTimeskipTime >= timeskipTime) ||
                            (owner == null || owner.IsDead || owner.IsRemoved))
                        {
                            if (owner != null && !owner.IsDead && !owner.IsRemoved)
                            {
                                owner.SetWorldPosition(thingPos);
                                owner.SetNametagVisible(true);
                            }

                            hiddenBlock.Remove();

                            lookForOwnerMovement.Stop();
                            createParticles.Stop();
                            controlThing.Stop();
                            partyEnder.Stop();
                        }
                    }, 500);

                    #region -=====================-COMMON STUFF-=====================-

                    Game.PlayEffect("CFTXT", owner.GetWorldPosition() + new Vector2(0, 5), "TIMESKIP!\nCooldown 60S!");

                    lastTimeskipTime = Game.TotalElapsedGameTime;

                    Events.UpdateCallback notifier = null;

                    const int checkForFlagTime = 200;

                    notifier = Events.UpdateCallback.Start((float e) =>
                    {
                        // Updating the value.
                        timeskipAllowed = (Game.TotalElapsedGameTime - lastTimeskipTime >=
                            timeskipCooldown);

                        bool notifyingMakesSense = PlayerStandList.ContainsKey(owner) &&
                            PlayerStandList[owner] is KingCrimson && timeskipAllowed;

                        bool ownerHasDifferentStand = PlayerStandList.ContainsKey(owner) &&
                            !(PlayerStandList[owner] is KingCrimson);

                        if (notifyingMakesSense)
                        {
                            int usID = owner.GetUser() == null ? 666 : owner.GetUser().UserIdentifier;
                            Game.ShowChatMessage("Timeskip ready!", Color.Magenta, usID);
                            Game.PlayEffect("CFTXT", owner.GetWorldPosition() + new Vector2(0, 5), "Timeskip ready!");
                            notifier.Stop();
                        }
                        else if (ownerHasDifferentStand)
                            notifier.Stop();
                    }, checkForFlagTime);

                    #endregion -=====================================================-
                }
            }

            private void PerformEpitaph(IPlayer rudePlayer)
            {
                if (forbidEpitath != null)
                    forbidEpitath.Stop();

                Game.PlaySound("Sawblade", owner.GetWorldPosition(), 1);

                for (int i = 0; i <= 6; i++)
                    Game.PlayEffect(EffectName.Electric, new Vector2(rudePlayer.GetWorldPosition().X - 10,
                        rudePlayer.GetWorldPosition().Y + (10 - i)));

                owner.SetWorldPosition(new Vector2(rudePlayer.GetWorldPosition().X +
                    (rudePlayer.FacingDirection == -1 ? 17 : -17), owner.GetWorldPosition().Y));

                rudePlayer.SetHealth(1);
                rudePlayer.SetInputEnabled(false);
                rudePlayer.AddCommand(new PlayerCommand(PlayerCommandType.DeathKneelInfinite));

                float currentTime = Game.TotalElapsedGameTime, animDelay = 5000f;

                Events.UpdateCallback animStart = null;

                animStart = Events.UpdateCallback.Start(ms =>
                {
                    if (Game.TotalElapsedGameTime - currentTime >= animDelay)
                    {
                        rudePlayer.SetInputEnabled(true);
                        rudePlayer.AddCommand(new PlayerCommand(PlayerCommandType.StopDeathKneel));

                        rudePlayer = null;

                        animStart.Stop();
                    }

                    if (rudePlayer == null || rudePlayer.IsDead || rudePlayer.IsRemoved)
                    {
                        animStart.Stop();
                    }
                });

                epitaphAllowed = false;
            }

            private void AllowEpitaph(IPlayer plr, PlayerDamageArgs args)
            {
                if (!epitaphAllowed)
                {
                    if (plr == owner && args.DamageType == PlayerDamageEventType.Melee)
                    {
                        IPlayer rudePlayer = Game.GetPlayer(args.SourceID);

                        if (rudePlayer != null)
                        {
                            bool hitPlayerIsEnemy = owner.GetTeam() == PlayerTeam.Independent ||
                                                rudePlayer.GetTeam() != owner.GetTeam();

                            if (!rudePlayer.IsDead && hitPlayerIsEnemy)
                            {
                                Game.PlayEffect("CFTXT", owner.GetWorldPosition() + new Vector2(0, 5), "EPITAPH AVAILABLE!");

                                int usID = owner.GetUser() == null ? 666 : owner.GetUser().UserIdentifier;
                                Game.ShowChatMessage("EPITAPH AVAILABLE!", Color.Magenta, usID);

                                epitaphAllowed = true;

                                this.rudePlr = rudePlayer;

                                float hitTime = Game.TotalElapsedGameTime,
                                    forbidTime = 2000f;

                                forbidEpitath = Events.UpdateCallback.Start(ms =>
                                {
                                    if (Game.TotalElapsedGameTime - hitTime >= forbidTime)
                                    {
                                        epitaphAllowed = false;
                                        Game.PlayEffect("CFTXT", owner.GetWorldPosition() + new Vector2(0, 5), "EPITATH unavailable...");
                                        Game.ShowChatMessage("Epitath unavailable...", Color.Magenta, usID);

                                        rudePlayer = null;

                                        forbidEpitath.Stop();
                                    }
                                });
                            }
                        }
                    }
                }
            }

            private void NeutralizeDanger(float ms)
            {
                Vector2 ownerPos = owner.GetWorldPosition();
                Area areaToNeutralize = new Area(ownerPos.Y + 25, ownerPos.X - 25,
                    ownerPos.Y - 25, ownerPos.X + 25);

                IProjectile[] nearProjs = Game.GetProjectiles();

                foreach (IProjectile proj in nearProjs)
                {
                    if (proj.OwnerPlayerID != owner.UniqueId)
                    {
                        Vector2 projPos = proj.Position;
                        Vector2 projDir = proj.Direction;

                        if (areaToNeutralize.Contains(projPos))
                        {
                            float newProjX = projPos.X, newProjY = projPos.Y;

                            if (projDir.X < 0)
                            {
                                newProjX = projPos.X - ((areaToNeutralize.Left - projPos.X) * projDir.X) - 1;
                            }
                            else if (projDir.X > 0)
                            {
                                newProjX = projPos.X + ((areaToNeutralize.Right - projPos.X) * projDir.X) + 1;
                            }

                            if (projPos.Y < 0)
                            {
                                newProjY = projPos.Y - ((areaToNeutralize.Bottom - projPos.Y) * projDir.Y) - 1;
                            }
                            else if (projPos.Y > 0)
                            {
                                newProjY = projPos.Y + ((areaToNeutralize.Top - projPos.Y) * projDir.Y) + 1;
                            }

                            proj.Position = new Vector2(newProjX, newProjY);
                        }
                    }
                }

                areaToNeutralize = new Area(ownerPos.Y + 25, ownerPos.X - 15,
                    ownerPos.Y - 25, ownerPos.X + 15);

                IObject[] thrownObjects = Game.GetObjectsByArea(areaToNeutralize)
                    .Where(o => o.IsMissile).ToArray();

                foreach (IObject thrown in thrownObjects)
                {
                    Vector2 objectPos = thrown.GetWorldPosition();
                    thrown.TrackAsMissile(false);

                    Events.UpdateCallback continueMissile = null;

                    continueMissile = Events.UpdateCallback.Start((milSec) =>
                    {
                        objectPos = thrown.GetWorldPosition();

                        if (!areaToNeutralize.Contains(objectPos))
                        {
                            thrown.TrackAsMissile(true);
                            continueMissile.Stop();
                        }
                    });
                }
            }

            public override void FinalizeTheStand(bool rewriting = false)
            {
                if (onDangerNearCallback != null)
                    onDangerNearCallback.Stop();

                if (damageCallback != null)
                    damageCallback.Stop();

                if (forbidEpitath != null)
                    forbidEpitath.Stop();

                base.FinalizeTheStand(rewriting);
            }
        }

        class StickyFingers : Stand
        {
            public StickyFingers(IPlayer plr) : base(plr) { }

            public override PlayerModifiers Modifiers
            {
                get
                {
                    return new PlayerModifiers()
                    {
                        MaxHealth = 120,
                        CurrentHealth = 120,
                        MeleeDamageDealtModifier = 1.2f,
                    };
                }
            }

            public override string Name { get { return "Sticky Fingers"; } }

            public override string[] Description
            {
                get
                {
                    return new string[] { "Your stand is Sticky Fingers!",
                    "ALT + A - Hiden - \nAllows you to hide inside \na nearby player (COOLDOWN 5S)",
                        "ALT + D - Pocket Dimension - \nRemembers the point to which \nyou can teleport (COOLDOWN 15S)" };
                }
            }

            IPlayer playerHid = null;
            Vector2 rememberedPoint = default(Vector2);

            IObject hiddenBlock = null;

            Events.PlayerDeathCallback onHidPlayerDeath = null;

            Events.UpdateCallback lookForOwnerMovement = null;

            private const float hidenCooldown = 5000, pocketDimensionCooldown = 15000;

            private float lastHidenTime = -5000, lastPocketDimensionTime = -15000;

            bool hidenAllowed = true, needToComeOut = false, pocketDimensionAllowed = true, needToTeleport = false;

            public override void ActivateAbilities(IPlayer player, VirtualKeyInfo[] keyEvents)
            {
                if (player == owner)
                {
                    for (int i = 0; i < keyEvents.Length; i++)
                    {
                        bool hidenAbilityCasted = keyEvents[i].Event == VirtualKeyEvent.Pressed &&
                            keyEvents[i].Key == VirtualKey.ATTACK && owner.KeyPressed(VirtualKey.WALKING);

                        bool pocketDimensionAbilityCasted = keyEvents[i].Event == VirtualKeyEvent.Pressed &&
                            keyEvents[i].Key == VirtualKey.BLOCK && owner.KeyPressed(VirtualKey.WALKING);

                        //hidenAllowed = (Game.TotalElapsedGameTime - lastHidenTime >= hidenCooldown);
                        //pocketDimensionAllowed = (Game.TotalElapsedGameTime - lastPocketDimensionTime >= pocketDimensionCooldown);

                        if (hidenAbilityCasted && hidenAllowed)
                            PerformHiden();
                        else if (hidenAbilityCasted && needToComeOut && playerHid != null)
                            ComeOutFromThePlayer(playerHid);
                        else if (pocketDimensionAbilityCasted && pocketDimensionAllowed)
                            PerformPocketDimension();
                        else if (pocketDimensionAbilityCasted && needToTeleport && rememberedPoint != default(Vector2))
                            TeleportToRememberedPoint(rememberedPoint);
                    }
                }
            }

            private void PerformHiden()
            {
                Vector2 ownerPos = owner.GetWorldPosition();

                Area areaToSearch = new Area(ownerPos.Y + 40, ownerPos.X - 40,
                    ownerPos.Y - 40, ownerPos.X + 40);

                IPlayer[] plrsInArea = Game.GetObjectsByArea<IPlayer>(areaToSearch).
                    Where(p => p != owner && !p.IsDead && !p.IsRemoved).ToArray();

                Area gameCameraArea = Game.GetCameraMaxArea();

                hiddenBlock = Game.CreateObject("InvisibleBlock", gameCameraArea.TopLeft);

                if (hiddenBlock != null && !hiddenBlock.DestructionInitiated &&
                    plrsInArea != null && plrsInArea.Length > 0)
                {
                    float hiddenBlockSize = 5 * Math.Abs(gameCameraArea.Left)
                        + Math.Abs(gameCameraArea.Right);

                    hiddenBlock.SetSizeFactor(new Point((int)hiddenBlockSize, 1));

                    playerHid = plrsInArea[new Random().Next(0, plrsInArea.Length)];

                    int usID = owner.GetUser() == null ? 666 : owner.GetUser().UserIdentifier;
                    Game.ShowChatMessage(string.Format("You hid in {0}! ALT + A to come out!",
                        playerHid.Name), Color.Magenta, usID);

                    owner.SetWorldPosition(new Vector2(gameCameraArea.Center.X + 50, gameCameraArea.Top + 8));
                    owner.SetNametagVisible(false);

                    Vector2 ownerNewPos = owner.GetWorldPosition();

                    Area areaForOwner = new Area(ownerNewPos.Y + 30, ownerNewPos.X - 30,
                        ownerNewPos.Y - 20, ownerNewPos.X + 30);

                    lookForOwnerMovement = Events.UpdateCallback.Start(ms =>
                    {
                        if (owner != null && !owner.IsDead && !owner.IsRemoved)
                        {
                            Vector2 ownerCurrPos = owner.GetWorldPosition();

                            if (ownerCurrPos.X < areaForOwner.Left || ownerCurrPos.X > areaForOwner.Right)
                                owner.SetWorldPosition(areaForOwner.Center);
                        }
                    });

                    onHidPlayerDeath = Events.PlayerDeathCallback.Start(ForceToComeOutOnDeath);

                    hidenAllowed = false;
                    needToComeOut = true;
                }
            }

            private void ComeOutFromThePlayer(IPlayer player, bool byPocketDimension = false)
            {
                if (byPocketDimension)
                {
                    playerHid = null;
                    needToComeOut = false;

                    int usID = owner.GetUser() == null ? 666 : owner.GetUser().UserIdentifier;
                    Game.ShowChatMessage("Hiden cancelled. Cooldown 5S!", Color.Magenta, usID);
                }
                else
                {
                    Vector2 plrPos = playerHid.GetWorldPosition();

                    owner.SetWorldPosition(new Vector2(plrPos.X + 10, plrPos.Y));

                    playerHid = null;
                    needToComeOut = false;

                    Game.PlayEffect("CFTXT", owner.GetWorldPosition() + new Vector2(0, 5), "HIDEN!\nCooldown 5S!");
                }

                if (lookForOwnerMovement != null)
                {
                    lookForOwnerMovement.Stop();
                }

                if (onHidPlayerDeath != null)
                    onHidPlayerDeath.Stop();

                if (hiddenBlock != null)
                {
                    hiddenBlock.Remove();
                    hiddenBlock = null;
                }

                owner.SetNametagVisible(true);

                #region -=====================-COMMON STUFF-=====================-

                lastHidenTime = Game.TotalElapsedGameTime;

                Events.UpdateCallback notifier = null;

                const int checkForFlagTime = 200;

                notifier = Events.UpdateCallback.Start((float e) =>
                {
                    // Updating the value.
                    hidenAllowed = (Game.TotalElapsedGameTime - lastHidenTime >=
                        hidenCooldown);

                    bool notifyingMakesSense = PlayerStandList.ContainsKey(owner) &&
                        PlayerStandList[owner] is StickyFingers && hidenAllowed;

                    bool ownerHasDifferentStand = PlayerStandList.ContainsKey(owner) &&
                        !(PlayerStandList[owner] is StickyFingers);

                    if (notifyingMakesSense)
                    {
                        int usID = owner.GetUser() == null ? 666 : owner.GetUser().UserIdentifier;
                        Game.ShowChatMessage("Hiden ready!", Color.Magenta, usID);
                        Game.PlayEffect("CFTXT", owner.GetWorldPosition() + new Vector2(0, 5), "Hiden ready!");

                        notifier.Stop();
                    }
                    else if (ownerHasDifferentStand)
                        notifier.Stop();
                }, checkForFlagTime);

                #endregion -=====================================================-
            }

            private void ForceToComeOutOnDeath(IPlayer plr)
            {
                if (playerHid != null && plr == playerHid)
                {
                    Vector2 plrPos = plr.GetWorldPosition();

                    owner.SetWorldPosition(new Vector2(plrPos.X + 10, plrPos.Y));
                    playerHid = null;
                    needToComeOut = false;

                    Game.PlayEffect("CFTXT", owner.GetWorldPosition() + new Vector2(0, 5), "HIDEN!\nCooldown 5S!");

                    if (lookForOwnerMovement != null)
                    {
                        lookForOwnerMovement.Stop();
                    }

                    if (onHidPlayerDeath != null)
                        onHidPlayerDeath.Stop();

                    if (hiddenBlock != null)
                    {
                        hiddenBlock.Remove();
                        hiddenBlock = null;
                    }

                    owner.SetNametagVisible(true);

                    #region -=====================-COMMON STUFF-=====================-

                    lastHidenTime = Game.TotalElapsedGameTime;

                    Events.UpdateCallback notifier = null;

                    const int checkForFlagTime = 200;

                    notifier = Events.UpdateCallback.Start((float e) =>
                    {
                        // Updating the value.
                        hidenAllowed = (Game.TotalElapsedGameTime - lastHidenTime >=
                            hidenCooldown);

                        bool notifyingMakesSense = PlayerStandList.ContainsKey(owner) &&
                            PlayerStandList[owner] is StickyFingers && hidenAllowed;

                        bool ownerHasDifferentStand = PlayerStandList.ContainsKey(owner) &&
                            !(PlayerStandList[owner] is StickyFingers);

                        if (notifyingMakesSense)
                        {
                            int usID = owner.GetUser() == null ? 666 : owner.GetUser().UserIdentifier;
                            Game.ShowChatMessage("Hiden ready!", Color.Magenta, usID);
                            Game.PlayEffect("CFTXT", owner.GetWorldPosition() + new Vector2(0, 5), "Hiden ready!");

                            notifier.Stop();
                        }
                        else if (ownerHasDifferentStand)
                            notifier.Stop();
                    }, checkForFlagTime);

                    #endregion -=====================================================-
                }
            }

            private void PerformPocketDimension()
            {
                rememberedPoint = playerHid == null ? owner.GetWorldPosition() :
                    playerHid.GetWorldPosition();

                Game.PlayEffect("CFTXT", owner.GetWorldPosition() + new Vector2(0, 5),
                    string.Format("REMEMBERED - X: {0}, Y: {1}\nTELEPORT WITH ALT + D",
                    rememberedPoint.X, rememberedPoint.Y));

                string text = playerHid != null ? string.Format("Point remembered - X: {0}, Y: {1} ({2}). ALT + D to teleport",
                    rememberedPoint.X, rememberedPoint.Y, playerHid.Name) :
                    string.Format("Point remembered - X: {0}, Y: {1}. ALT + D to teleport",
                    rememberedPoint.X, rememberedPoint.Y);

                int usID = owner.GetUser() == null ? 666 : owner.GetUser().UserIdentifier;
                Game.ShowChatMessage(text, Color.Magenta, usID);

                pocketDimensionAllowed = false;
                needToTeleport = true;
            }

            private void TeleportToRememberedPoint(Vector2 whereToTeleport)
            {
                owner.SetWorldPosition(rememberedPoint);
                rememberedPoint = default(Vector2);
                needToTeleport = false;

                if (needToComeOut && !hidenAllowed && playerHid != null)
                    ComeOutFromThePlayer(playerHid, true);

                #region -=====================-COMMON STUFF-=====================-

                Game.PlayEffect("CFTXT", owner.GetWorldPosition() + new Vector2(0, 5), "POCKET DIMENSION!\nCooldown 15S!");

                lastPocketDimensionTime = Game.TotalElapsedGameTime;

                Events.UpdateCallback notifier = null;

                const int checkForFlagTime = 200;

                notifier = Events.UpdateCallback.Start((float e) =>
                {
                    // Updating the value.
                    pocketDimensionAllowed = (Game.TotalElapsedGameTime - lastPocketDimensionTime >=
                        pocketDimensionCooldown);

                    bool notifyingMakesSense = PlayerStandList.ContainsKey(owner) &&
                        PlayerStandList[owner] is StickyFingers && pocketDimensionAllowed;

                    bool ownerHasDifferentStand = PlayerStandList.ContainsKey(owner) &&
                        !(PlayerStandList[owner] is StickyFingers);

                    if (notifyingMakesSense)
                    {
                        int usID = owner.GetUser() == null ? 666 : owner.GetUser().UserIdentifier;
                        Game.ShowChatMessage("Pocket Dimension ready!", Color.Magenta, usID);
                        Game.PlayEffect("CFTXT", owner.GetWorldPosition() + new Vector2(0, 5), "Pocket Dimension ready!");

                        notifier.Stop();
                    }
                    else if (ownerHasDifferentStand)
                        notifier.Stop();
                }, checkForFlagTime);

                #endregion -=====================================================-
            }

            public override void FinalizeTheStand(bool rewriting = false)
            {
                if (lookForOwnerMovement != null)
                    lookForOwnerMovement.Stop();

                if (onHidPlayerDeath != null)
                    onHidPlayerDeath.Stop();

                if (hiddenBlock != null)
                {
                    hiddenBlock.Remove();
                    hiddenBlock = null;
                }

                owner.SetNametagVisible(true);

                base.FinalizeTheStand(rewriting);
            }
        }

        class GoldExperience : Stand
        {
            public GoldExperience(IPlayer plr) : base(plr) { }

            public override PlayerModifiers Modifiers
            {
                get
                {
                    return new PlayerModifiers()
                    {
                        MaxHealth = 120,
                        CurrentHealth = 120,
                    };
                }
            }

            public override string Name { get { return "Gold Experience"; } }

            public override string[] Description
            {
                get
                {
                    return new string[] { "Your stand is Gold Experience!",
                        "ALT + A - Gorudo Ekusuperiensu - \nON HIT Turns a player or object into \na bird for 15 seconds (COOLDOWN 30S)",
                        "ALT + D - Self Heal - \nRestores your health (COOLDOWN 60S)" };
                }
            }

            private const float gorudoEkuseperiensuCooldown = 30000, selfHealCooldown = 60000;

            private float gorudoEkuseperiensuRecoveryTime = -30000, selfHealRecoveryTime = -60000;

            bool gorudoEkuseperiensuAllowed = true, selfHealAllowed = true;

            IObject hiddenBlock = null;

            public override void ActivateAbilities(IPlayer player, VirtualKeyInfo[] keyEvents)
            {
                if (player == owner)
                {
                    for (int i = 0; i < keyEvents.Length; i++)
                    {
                        bool gorudoEkuseperiensuAbilityCasted = keyEvents[i].Event == VirtualKeyEvent.Pressed &&
                            keyEvents[i].Key == VirtualKey.ATTACK && owner.KeyPressed(VirtualKey.WALKING);

                        bool selfHealAbilityCasted = keyEvents[i].Event == VirtualKeyEvent.Pressed &&
                            keyEvents[i].Key == VirtualKey.BLOCK && owner.KeyPressed(VirtualKey.WALKING);

                        gorudoEkuseperiensuAllowed = (Game.TotalElapsedGameTime - gorudoEkuseperiensuRecoveryTime >=
                            gorudoEkuseperiensuCooldown);

                        selfHealAllowed = (Game.TotalElapsedGameTime - selfHealRecoveryTime >=
                            selfHealCooldown);

                        if (gorudoEkuseperiensuAbilityCasted && gorudoEkuseperiensuAllowed)
                            PerformGorudoEkuseperiensu();
                        else if (selfHealAbilityCasted && selfHealAllowed)
                            PerformSelfHeal();
                    }
                }
            }

            private void PerformGorudoEkuseperiensu()
            {
                Area gameCameraArea = Game.GetCameraMaxArea();

                hiddenBlock = Game.CreateObject("InvisibleBlock", gameCameraArea.TopLeft);

                if (hiddenBlock != null && !hiddenBlock.DestructionInitiated)
                {
                    float hiddenBlockSize = 5 * Math.Abs(gameCameraArea.Left)
                        + Math.Abs(gameCameraArea.Right);

                    hiddenBlock.SetSizeFactor(new Point((int)hiddenBlockSize, 1));

                    Events.PlayerMeleeActionCallback onMeleeHit = null;

                    onMeleeHit = Events.PlayerMeleeActionCallback.Start((plr, args) =>
                    {
                        if (plr == owner)
                        {
                            int counter = 1;
                            bool someoneTurned = false;

                            foreach (PlayerMeleeHitArg arg in args)
                            {
                                float hitTime = Game.TotalElapsedGameTime,
                                    returnTime = 15000f;

                                IObject hitObject = arg.HitObject;

                                bool plrWasZeroed = hitObject is IPlayer && plrIsZeroed.ContainsKey((IPlayer)hitObject) &&
                                    plrIsZeroed[(IPlayer)hitObject];

                                if (!plrWasZeroed && hitObject != null && !hitObject.IsRemoved && !hitObject.DestructionInitiated &&
                                !(hitObject.Name.ToUpper().Contains("Dove")) &&
                                ((!(hitObject is IPlayer) && hitObject.GetBodyType() == BodyType.Dynamic) ||
                                (hitObject is IPlayer && !((IPlayer)hitObject).IsDead)))
                                {
                                    Vector2 hitObjectPos = hitObject.GetWorldPosition();
                                    string objectId = hitObject.Name;

                                    hitObject.SetWorldPosition(new Vector2(gameCameraArea.Center.X + 100, gameCameraArea.Top + 8));

                                    if (hitObject is IPlayer)
                                    {
                                        IPlayer hitPlr = (IPlayer)hitObject;
                                        hitPlr.SetNametagVisible(false);
                                        hitPlr.SetInputEnabled(false);

                                        int usID = hitPlr.GetUser() == null ? 666 : hitPlr.GetUser().UserIdentifier;
                                        Game.ShowChatMessage("You've turned into a bird for 15 secodns!", Color.Magenta, usID);
                                    }

                                    Vector2 objNewPos = hitObject.GetWorldPosition();

                                    Area areaForOwner = new Area(objNewPos.Y + 30, objNewPos.X - 30,
                                        objNewPos.Y - 20, objNewPos.X + 30);

                                    IObject birdie = Game.CreateObject("Dove00", hitObjectPos);

                                    someoneTurned = true;

                                    Events.UpdateCallback lookForObjectMovement = null;

                                    bool birdIsAlive = false;

                                    lookForObjectMovement = Events.UpdateCallback.Start(ms =>
                                    {
                                        if (hitObject != null && !hitObject.IsRemoved)
                                        {
                                            Vector2 objectCurrPos = hitObject.GetWorldPosition();

                                            if (objectCurrPos.X < areaForOwner.Left || objectCurrPos.X > areaForOwner.Right)
                                                hitObject.SetWorldPosition(areaForOwner.Center);
                                        }

                                        if (Game.TotalElapsedGameTime - hitTime >= returnTime)
                                        {
                                            if (birdie != null && !birdie.IsRemoved)
                                            {
                                                birdIsAlive = true;

                                                Vector2 birdiePos = birdie.GetWorldPosition();
                                                birdie.Remove();

                                                if (hitObject != null && !hitObject.IsRemoved)
                                                {
                                                    hitObject.SetWorldPosition(birdiePos);

                                                    if (hitObject is IPlayer)
                                                    {
                                                        ((IPlayer)hitObject).SetNametagVisible(true);
                                                        ((IPlayer)hitObject).SetInputEnabled(true);
                                                    }

                                                    hiddenBlock.Remove();
                                                    lookForObjectMovement.Stop();
                                                }
                                            }
                                        }
                                    });

                                    Events.ObjectTerminatedCallback onBirdKill = null;

                                    onBirdKill = Events.ObjectTerminatedCallback.Start(objs =>
                                    {
                                        foreach (IObject obj in objs)
                                        {
                                            if (birdIsAlive == false && obj == birdie)
                                            {
                                                hitObject.Remove();
                                                hiddenBlock.Remove();
                                                lookForObjectMovement.Stop();
                                                onBirdKill.Stop();
                                            }
                                        }
                                    });

                                    counter++;
                                }

                                if (someoneTurned)
                                    onMeleeHit.Stop();
                            }
                        }
                    });

                    #region -=====================-COMMON STUFF-=====================-

                    Game.PlayEffect("CFTXT", owner.GetWorldPosition() + new Vector2(0, 5), "GORUDO EKUSUPERIENSU!\nCooldown 30S!");

                    gorudoEkuseperiensuRecoveryTime = Game.TotalElapsedGameTime;

                    Events.UpdateCallback notifier = null;

                    const int checkForFlagTime = 200;

                    notifier = Events.UpdateCallback.Start((float e) =>
                    {
                        // Updating the value.
                        gorudoEkuseperiensuAllowed = (Game.TotalElapsedGameTime - gorudoEkuseperiensuRecoveryTime >=
                                    gorudoEkuseperiensuCooldown);

                        bool notifyingMakesSense = PlayerStandList.ContainsKey(owner) &&
                            PlayerStandList[owner] is GoldExperience && gorudoEkuseperiensuAllowed;

                        bool ownerHasDifferentStand = PlayerStandList.ContainsKey(owner) &&
                            !(PlayerStandList[owner] is GoldExperience);

                        if (notifyingMakesSense)
                        {
                            int usID = owner.GetUser() == null ? 666 : owner.GetUser().UserIdentifier;
                            Game.ShowChatMessage("Gorudo Ekusuperiensu ready!", Color.Magenta, usID);
                            Game.PlayEffect("CFTXT", owner.GetWorldPosition() + new Vector2(0, 5), "Gorudo Ekusuperiensu ready!");
                            notifier.Stop();
                        }
                        else if (ownerHasDifferentStand)
                            notifier.Stop();
                    }, checkForFlagTime);

                    #endregion -=====================================================-
                }
            }

            private void PerformSelfHeal()
            {
                owner.SetHealth(owner.GetMaxHealth());

                #region -=====================-COMMON STUFF-=====================-

                Game.PlayEffect("CFTXT", owner.GetWorldPosition() + new Vector2(0, 5), "SELF HEAL!\nCooldown 60S!");

                selfHealRecoveryTime = Game.TotalElapsedGameTime;

                Events.UpdateCallback notifier = null;

                const int checkForFlagTime = 200;

                notifier = Events.UpdateCallback.Start((float e) =>
                {
                    // Updating the value.
                    selfHealAllowed = (Game.TotalElapsedGameTime - selfHealRecoveryTime >=
                        selfHealCooldown);

                    bool notifyingMakesSense = PlayerStandList.ContainsKey(owner) &&
                        PlayerStandList[owner] is GoldExperience && selfHealAllowed;

                    bool ownerHasDifferentStand = PlayerStandList.ContainsKey(owner) &&
                        !(PlayerStandList[owner] is GoldExperience);

                    if (notifyingMakesSense)
                    {
                        int usID = owner.GetUser() == null ? 666 : owner.GetUser().UserIdentifier;
                        Game.ShowChatMessage("Self Heal ready!", Color.Magenta, usID);
                        Game.PlayEffect("CFTXT", owner.GetWorldPosition() + new Vector2(0, 5), "Self Heal ready!");
                        notifier.Stop();
                    }
                    else if (ownerHasDifferentStand)
                        notifier.Stop();
                }, checkForFlagTime);

                #endregion -=====================================================-
            }
        }

        public static Dictionary<IPlayer, bool> plrIsZeroed = new Dictionary<IPlayer, bool>();

        class GoldExperienceReq : Stand
        {
            public GoldExperienceReq(IPlayer plr) : base(plr)
            {
                BlockedStands.Add("Gold Experience".ToUpper());

                IPlayer[] plars = Game.GetPlayers();

                foreach (IPlayer p in plars)
                {
                    plrIsZeroed.Add(p, false);
                    plrNameList.Add(p, p.Name);
                    plrUsrList.Add(p, p.GetUser());
                    plrPosList.Add(p, p.GetWorldPosition());
                    plrRektTime.Add(p, float.MaxValue);
                    plrRessTime.Add(p, float.MaxValue);
                    plrModList.Add(p, p.GetModifiers());
                    plrTeamList.Add(p, p.GetTeam());
                    plrProfList.Add(p, p.GetProfile());
                }

                makePlrsSuffer = Events.UpdateCallback.Start(KillAndRessurect);
                controlPlrDeaths = Events.PlayerDeathCallback.Start(RefreshData);
            }

            public override PlayerModifiers Modifiers
            {
                get
                {
                    return new PlayerModifiers()
                    {
                        MaxHealth = 500,
                        CurrentHealth = 500,
                        MeleeDamageDealtModifier = 3.5f
                    };
                }
            }

            public override string Name { get { return "Gold Experience REQ"; } }

            public override string[] Description
            {
                get
                {
                    return new string[] { "Your stand is Gold Experience REQUIEM!",
                        "ALT + A - Gorudo Ekusuperiensu - \nON HIT Turns a player or object into \na bird for 20 seconds (COOLDOWN 1S)",
                        "ALT + D - Self Heal - \nRestores your health (COOLDOWN 1S)",
                        "ALT + S - Zero - \nON HIT Sets the player in an \ninfinite loop of death (COOLDOWN 30S)" };
                }
            }

            private const float gorudoEkuseperiensuCooldown = 1000, selfHealCooldown = 1000, zeroCooldown = 30000;

            float effectTime = 2000f;

            private float gorudoEkuseperiensuRecoveryTime = -1000, selfHealRecoveryTime = -1000, zeroRecoveryTime = -30000;

            bool gorudoEkuseperiensuAllowed = true, selfHealAllowed = true, zeroAllowed = true;

            IObject hiddenBlock = null;

            Events.UpdateCallback makePlrsSuffer = null;
            Events.PlayerDeathCallback controlPlrDeaths = null;

            static Dictionary<IPlayer, IUser> plrUsrList = new Dictionary<IPlayer, IUser>();
            static Dictionary<IPlayer, string> plrNameList = new Dictionary<IPlayer, string>();
            static Dictionary<IPlayer, Vector2> plrPosList = new Dictionary<IPlayer, Vector2>();
            static Dictionary<IPlayer, float> plrRektTime = new Dictionary<IPlayer, float>();
            static Dictionary<IPlayer, float> plrRessTime = new Dictionary<IPlayer, float>();
            static Dictionary<IPlayer, PlayerModifiers> plrModList = new Dictionary<IPlayer, PlayerModifiers>();
            static Dictionary<IPlayer, PlayerTeam> plrTeamList = new Dictionary<IPlayer, PlayerTeam>();
            static Dictionary<IPlayer, IProfile> plrProfList = new Dictionary<IPlayer, IProfile>();

            public override void ActivateAbilities(IPlayer player, VirtualKeyInfo[] keyEvents)
            {
                if (player == owner)
                {
                    for (int i = 0; i < keyEvents.Length; i++)
                    {
                        bool gorudoEkuseperiensuAbilityCasted = keyEvents[i].Event == VirtualKeyEvent.Pressed &&
                            keyEvents[i].Key == VirtualKey.ATTACK && owner.KeyPressed(VirtualKey.WALKING);

                        bool selfHealAbilityCasted = keyEvents[i].Event == VirtualKeyEvent.Pressed &&
                            keyEvents[i].Key == VirtualKey.BLOCK && owner.KeyPressed(VirtualKey.WALKING);

                        bool zeroAbilityCasted = keyEvents[i].Event == VirtualKeyEvent.Pressed &&
                            keyEvents[i].Key == VirtualKey.KICK && owner.KeyPressed(VirtualKey.WALKING);

                        gorudoEkuseperiensuAllowed = (Game.TotalElapsedGameTime - gorudoEkuseperiensuRecoveryTime >=
                            gorudoEkuseperiensuCooldown);

                        selfHealAllowed = (Game.TotalElapsedGameTime - selfHealRecoveryTime >=
                            selfHealCooldown);

                        zeroAllowed = (Game.TotalElapsedGameTime - zeroRecoveryTime >=
                            zeroCooldown);

                        if (gorudoEkuseperiensuAbilityCasted && gorudoEkuseperiensuAllowed)
                            PerformGorudoEkuseperiensu();
                        else if (selfHealAbilityCasted && selfHealAllowed)
                            PerformSelfHeal();
                        else if (zeroAbilityCasted && zeroAllowed)
                            PerformZero();
                    }
                }
            }

            private void PerformGorudoEkuseperiensu()
            {
                Area gameCameraArea = Game.GetCameraMaxArea();

                hiddenBlock = Game.CreateObject("InvisibleBlock", gameCameraArea.TopLeft);

                if (hiddenBlock != null && !hiddenBlock.DestructionInitiated)
                {
                    float hiddenBlockSize = 5 * Math.Abs(gameCameraArea.Left)
                        + Math.Abs(gameCameraArea.Right);

                    hiddenBlock.SetSizeFactor(new Point((int)hiddenBlockSize, 1));

                    Events.PlayerMeleeActionCallback onMeleeHit = null;

                    onMeleeHit = Events.PlayerMeleeActionCallback.Start((plr, args) =>
                    {
                        if (plr == owner)
                        {
                            int counter = 1;
                            bool someoneTurned = false;

                            foreach (PlayerMeleeHitArg arg in args)
                            {
                                float hitTime = Game.TotalElapsedGameTime,
                                    returnTime = 20000f;

                                IObject hitObject = arg.HitObject;

                                bool plrWasZeroed = hitObject is IPlayer && plrIsZeroed.ContainsKey((IPlayer)hitObject) &&
                                    plrIsZeroed[(IPlayer)hitObject];

                                if (!plrWasZeroed && hitObject != null && !hitObject.IsRemoved && !hitObject.DestructionInitiated &&
                                !(hitObject.Name.ToUpper().Contains("Dove")) &&
                                ((!(hitObject is IPlayer) && hitObject.GetBodyType() == BodyType.Dynamic) ||
                                (hitObject is IPlayer && !((IPlayer)hitObject).IsDead)))
                                {
                                    Vector2 hitObjectPos = hitObject.GetWorldPosition();
                                    string objectId = hitObject.Name;

                                    hitObject.SetWorldPosition(new Vector2(gameCameraArea.Center.X + 100, gameCameraArea.Top + 8));

                                    if (hitObject is IPlayer)
                                    {
                                        IPlayer hitPlr = (IPlayer)hitObject;
                                        hitPlr.SetNametagVisible(false);
                                        hitPlr.SetInputEnabled(false);

                                        int usID = hitPlr.GetUser() == null ? 666 : hitPlr.GetUser().UserIdentifier;
                                        Game.ShowChatMessage("You've turned into a bird for 20 secodns!", Color.Magenta, usID);
                                    }

                                    Vector2 objNewPos = hitObject.GetWorldPosition();

                                    Area areaForOwner = new Area(objNewPos.Y + 30, objNewPos.X - 30,
                                        objNewPos.Y - 20, objNewPos.X + 30);

                                    IObject birdie = Game.CreateObject("Dove00", hitObjectPos);

                                    someoneTurned = true;

                                    Events.UpdateCallback lookForObjectMovement = null;

                                    bool birdIsAlive = false;

                                    lookForObjectMovement = Events.UpdateCallback.Start(ms =>
                                    {
                                        if (hitObject != null && !hitObject.IsRemoved)
                                        {
                                            Vector2 objectCurrPos = hitObject.GetWorldPosition();

                                            if (objectCurrPos.X < areaForOwner.Left || objectCurrPos.X > areaForOwner.Right)
                                                hitObject.SetWorldPosition(areaForOwner.Center);
                                        }

                                        if (Game.TotalElapsedGameTime - hitTime >= returnTime)
                                        {
                                            if (birdie != null && !birdie.IsRemoved)
                                            {
                                                birdIsAlive = true;

                                                Vector2 birdiePos = birdie.GetWorldPosition();
                                                birdie.Remove();

                                                if (hitObject != null && !hitObject.IsRemoved)
                                                {
                                                    hitObject.SetWorldPosition(birdiePos);

                                                    if (hitObject is IPlayer)
                                                    {
                                                        ((IPlayer)hitObject).SetNametagVisible(true);
                                                        ((IPlayer)hitObject).SetInputEnabled(true);
                                                    }

                                                    hiddenBlock.Remove();
                                                    lookForObjectMovement.Stop();
                                                }
                                            }
                                        }
                                    });

                                    Events.ObjectTerminatedCallback onBirdKill = null;

                                    onBirdKill = Events.ObjectTerminatedCallback.Start(objs =>
                                    {
                                        foreach (IObject obj in objs)
                                        {
                                            if (birdIsAlive == false && obj == birdie)
                                            {
                                                hitObject.Remove();
                                                hiddenBlock.Remove();
                                                lookForObjectMovement.Stop();
                                                onBirdKill.Stop();
                                            }
                                        }
                                    });

                                    counter++;
                                }

                                if (someoneTurned)
                                    onMeleeHit.Stop();
                            }
                        }
                    });

                    #region -=====================-COMMON STUFF-=====================-

                    Game.PlayEffect("CFTXT", owner.GetWorldPosition() + new Vector2(0, 5), "GORUDO EKUSUPERIENSU!\nCooldown 1S!");

                    gorudoEkuseperiensuRecoveryTime = Game.TotalElapsedGameTime;

                    Events.UpdateCallback notifier = null;

                    const int checkForFlagTime = 200;

                    notifier = Events.UpdateCallback.Start((float e) =>
                    {
                        // Updating the value.
                        gorudoEkuseperiensuAllowed = (Game.TotalElapsedGameTime - gorudoEkuseperiensuRecoveryTime >=
                                    gorudoEkuseperiensuCooldown);

                        bool notifyingMakesSense = PlayerStandList.ContainsKey(owner) &&
                            PlayerStandList[owner] is GoldExperienceReq && gorudoEkuseperiensuAllowed;

                        bool ownerHasDifferentStand = PlayerStandList.ContainsKey(owner) &&
                            !(PlayerStandList[owner] is GoldExperienceReq);

                        if (notifyingMakesSense)
                        {
                            int usID = owner.GetUser() == null ? 666 : owner.GetUser().UserIdentifier;
                            Game.ShowChatMessage("Gorudo Ekusuperiensu ready!", Color.Magenta, usID);
                            Game.PlayEffect("CFTXT", owner.GetWorldPosition() + new Vector2(0, 5), "Gorudo Ekusuperiensu ready!");
                            notifier.Stop();
                        }
                        else if (ownerHasDifferentStand)
                            notifier.Stop();
                    }, checkForFlagTime);

                    #endregion -=====================================================-
                }
            }

            private void PerformSelfHeal()
            {
                owner.SetHealth(owner.GetMaxHealth());

                #region -=====================-COMMON STUFF-=====================-

                Game.PlayEffect("CFTXT", owner.GetWorldPosition() + new Vector2(0, 5), "SELF HEAL!\nCooldown 1S!");

                selfHealRecoveryTime = Game.TotalElapsedGameTime;

                Events.UpdateCallback notifier = null;

                const int checkForFlagTime = 200;

                notifier = Events.UpdateCallback.Start((float e) =>
                {
                    // Updating the value.
                    selfHealAllowed = (Game.TotalElapsedGameTime - selfHealRecoveryTime >=
                        selfHealCooldown);

                    bool notifyingMakesSense = PlayerStandList.ContainsKey(owner) &&
                        PlayerStandList[owner] is GoldExperienceReq && selfHealAllowed;

                    bool ownerHasDifferentStand = PlayerStandList.ContainsKey(owner) &&
                        !(PlayerStandList[owner] is GoldExperienceReq);

                    if (notifyingMakesSense)
                    {
                        int usID = owner.GetUser() == null ? 666 : owner.GetUser().UserIdentifier;
                        Game.ShowChatMessage("Self Heal ready!", Color.Magenta, usID);
                        Game.PlayEffect("CFTXT", owner.GetWorldPosition() + new Vector2(0, 5), "Self Heal ready!");
                        notifier.Stop();
                    }
                    else if (ownerHasDifferentStand)
                        notifier.Stop();
                }, checkForFlagTime);

                #endregion -=====================================================-
            }

            private void PerformZero()
            {
                Events.PlayerMeleeActionCallback onMeleeHit = null;

                onMeleeHit = Events.PlayerMeleeActionCallback.Start((plr, args) =>
                {
                    if (plr == owner)
                    {
                        bool someoneZeroed = false;

                        foreach (PlayerMeleeHitArg arg in args)
                        {
                            if (arg.IsPlayer)
                            {
                                IPlayer hitPlr = Game.GetPlayer(arg.ObjectID);

                                if (!hitPlr.IsDead)
                                {
                                    if (!plrIsZeroed[hitPlr])
                                    {
                                        plrIsZeroed[hitPlr] = true;
                                        hitPlr.Kill();
                                        someoneZeroed = true;
                                    }
                                }
                            }
                        }

                        if (someoneZeroed)
                            onMeleeHit.Stop();
                    }
                });

                #region -=====================-COMMON STUFF-=====================-

                Game.PlayEffect("CFTXT", owner.GetWorldPosition() + new Vector2(0, 5), "ZERO!\nCooldown 30S!");

                zeroRecoveryTime = Game.TotalElapsedGameTime;

                Events.UpdateCallback notifier = null;

                const int checkForFlagTime = 200;

                notifier = Events.UpdateCallback.Start((float e) =>
                {
                    // Updating the value.
                    zeroAllowed = (Game.TotalElapsedGameTime - zeroRecoveryTime >=
                        zeroCooldown);

                    bool notifyingMakesSense = PlayerStandList.ContainsKey(owner) &&
                        PlayerStandList[owner] is GoldExperienceReq && zeroAllowed;

                    bool ownerHasDifferentStand = PlayerStandList.ContainsKey(owner) &&
                        !(PlayerStandList[owner] is GoldExperienceReq);

                    if (notifyingMakesSense)
                    {
                        int usID = owner.GetUser() == null ? 666 : owner.GetUser().UserIdentifier;
                        Game.ShowChatMessage("Zero ready!", Color.Magenta, usID);
                        Game.PlayEffect("CFTXT", owner.GetWorldPosition() + new Vector2(0, 5), "Zero ready!");
                        notifier.Stop();
                    }
                    else if (ownerHasDifferentStand)
                        notifier.Stop();
                }, checkForFlagTime);

                #endregion -=====================================================-
            }

            private void KillAndRessurect(float ms)
            {
                for (int i = 0; i < plrIsZeroed.Keys.Count; i++)
                {
                    IPlayer plr = plrIsZeroed.Keys.ElementAt(i);

                    if (plr != null && plrIsZeroed[plr])
                    {
                        if (plr.IsDead || plr.IsRemoved)
                        {
                            if (Game.TotalElapsedGameTime - plrRektTime[plr] >= 2000f)
                            {
                                IPlayer newPlr = Game.CreatePlayer(plrPosList[plr]);

                                if (plrUsrList[plr] != null)
                                    newPlr.SetUser(plrUsrList[plr]);
                                else
                                {
                                    newPlr.SetBotBehavior(new BotBehavior(true, PredefinedAIType.BotA));
                                    newPlr.SetInputEnabled(true);
                                }

                                newPlr.SetProfile(plrProfList[plr]);
                                newPlr.SetModifiers(plrModList[plr]);
                                newPlr.SetBotName(plrNameList[plr]);
                                newPlr.SetTeam(plrTeamList[plr]);

                                newPlr.SetHealth(15);

                                plrIsZeroed.Remove(plr);
                                plrNameList.Remove(plr);
                                plrUsrList.Remove(plr);
                                plrPosList.Remove(plr);
                                plrRektTime.Remove(plr);
                                plrRessTime.Remove(plr);
                                plrModList.Remove(plr);
                                plrTeamList.Remove(plr);
                                plrProfList.Remove(plr);

                                plr.Remove();

                                plrIsZeroed.Add(newPlr, true);
                                plrNameList.Add(newPlr, newPlr.Name);
                                plrUsrList.Add(newPlr, newPlr.GetUser());
                                plrPosList.Add(newPlr, newPlr.GetWorldPosition());
                                plrRektTime.Add(newPlr, float.MaxValue);
                                plrRessTime.Add(newPlr, Game.TotalElapsedGameTime);
                                plrModList.Add(newPlr, newPlr.GetModifiers());
                                plrTeamList.Add(newPlr, newPlr.GetTeam());
                                plrProfList.Add(newPlr, newPlr.GetProfile());
                            }
                        }
                        else
                        {
                            if (Game.TotalElapsedGameTime - plrRessTime[plr] >= 2000f)
                            {
                                plr.Kill();
                            }
                        }
                    }
                }
            }

            private void RefreshData(IPlayer plr)
            {
                if (plrIsZeroed.Keys.Contains(plr) && plrIsZeroed[plr] == true)
                {
                    plrNameList[plr] = plr.Name;
                    plrUsrList[plr] = plr.GetUser();
                    plrPosList[plr] = plr.GetWorldPosition();
                    plrRektTime[plr] = Game.TotalElapsedGameTime;
                    plrModList[plr] = plr.GetModifiers();
                    plrTeamList[plr] = plr.GetTeam();
                    plrRessTime[plr] = float.MaxValue;
                    plrProfList[plr] = plr.GetProfile();
                }
            }
        }

        #endregion -=====================================================-

        #region -====================-OTHER TYPES-=======================-

        public class StandBow
        {
            // Giving the stand bow and setting tracking.
            public StandBow(IPlayer owner)
            {
                this.owner = owner;

                GiveBow();

                standBowRecievedCallback = Events.PlayerWeaponAddedActionCallback.Start(CheckOwner);
                standArrowCreatedCallBack = Events.ProjectileCreatedCallback.Start(TrackProjectiles);
                standArrowHitCallBack = Events.ProjectileHitCallback.Start(GiveStandToPlr);
                standBowLostCallback = Events.PlayerWeaponRemovedActionCallback.Start(RemoveOwnerStatus);

                onStandArrowsEndCallback = Events.UpdateCallback.Start(FinalizeTheBow);
            }

            private Events.PlayerWeaponAddedActionCallback standBowRecievedCallback;
            private Events.ProjectileCreatedCallback standArrowCreatedCallBack;
            private Events.ProjectileHitCallback standArrowHitCallBack;
            private Events.PlayerWeaponRemovedActionCallback standBowLostCallback;

            private Events.UpdateCallback onStandArrowsEndCallback;

            private IPlayer owner;
            private RifleWeaponItem bowWeapon;
            private int weaponDroppedID = -1;
            private List<int> projectileIds = new List<int>();
            private int lastProjID = -1;

            // Give the bow to an owner;
            private void GiveBow()
            {
                owner.GiveWeaponItem(WeaponItem.BOW);
                bowWeapon = owner.CurrentPrimaryWeapon;
                Game.ShowChatMessage(string.Format("{0} got the stand bow!", owner.Name), Color.Cyan);
            }

            // Set the owner to a player who equipped previously dropped bow.
            private void CheckOwner(IPlayer ply, PlayerWeaponAddedArg wpn)
            {
                if (wpn.SourceObjectID == weaponDroppedID)
                {
                    owner = ply;
                    weaponDroppedID = -1;
                    Game.ShowChatMessage(string.Format("{0} got the stand bow!", owner.Name), Color.Cyan);
                }
            }

            // Track projectiles shot from the stand bow and updates the bowWeapon field.
            private void TrackProjectiles(IProjectile[] projs)
            {
                for (int i = 0; i < projs.Length; i++)
                {
                    bool ownerIsShootingFromTheBow = owner != null && projs[i].InitialOwnerPlayerID == owner.UniqueID &&
                        projs[i].ProjectileItem == ProjectileItem.BOW;

                    if (ownerIsShootingFromTheBow)
                    {
                        RifleWeaponItem currWeapon = owner.CurrentPrimaryWeapon;

                        bool plrIsShootingFromTheStandBow = currWeapon.CurrentAmmo == bowWeapon.CurrentAmmo - 1 &&
                            currWeapon.MagSize == bowWeapon.MagSize && currWeapon.WeaponItem == bowWeapon.WeaponItem &&
                            currWeapon.PowerupBouncingRounds == bowWeapon.PowerupBouncingRounds &&
                            currWeapon.TotalAmmo == bowWeapon.TotalAmmo - 1;

                        bool bowIsLost = !plrIsShootingFromTheStandBow && owner != null && weaponDroppedID == -1;

                        if (plrIsShootingFromTheStandBow)
                        {
                            bowWeapon = currWeapon;
                            projs[i].DamageDealtModifier = 0;
                            projectileIds.Add(projs[i].InstanceID);
                            lastProjID = projs[i].InstanceID;
                        }
                        // If the stand bow was overwritten with a regular bow or the player picked up ammo.
                        else if (bowIsLost)
                            FinalizeTheBow(-10);
                    }
                }
            }

            // Give a random stand to a player who was hit by the stand arrow. 
            private void GiveStandToPlr(IProjectile proj, ProjectileHitArgs args)
            {
                bool plrIsShotByOwner = owner != null && proj.InitialOwnerPlayerID == owner.UniqueID && args.IsPlayer;

                if (plrIsShotByOwner)
                {
                    IPlayer shotPlr = Game.GetPlayer(args.HitObjectID);

                    bool plrCanHaveStand = !PlayerStandList.ContainsKey(shotPlr) && !shotPlr.IsDead;
                    bool isBowProj = (projectileIds.Contains(proj.InstanceID) || lastProjID == proj.InstanceID);

                    if (plrCanHaveStand && isBowProj)
                    {
                        Stand stand = Stand.GetStand(shotPlr);

                        if (stand != null)
                            PlayerStandList.Add(shotPlr, stand);
                    }
                }
            }

            // Remove the owner status from the player who had lost the bow.
            private void RemoveOwnerStatus(IPlayer ply, PlayerWeaponRemovedArg args)
            {
                bool ownerLostStandBow = owner != null && ply == owner && args.TargetObjectID != 0 &&
                    args.WeaponItem == WeaponItem.BOW;

                if (ownerLostStandBow)
                {
                    owner = null;

                    if (bowWeapon.CurrentAmmo != 0)
                        weaponDroppedID = args.TargetObjectID;
                }
            }

            // Stop all the tracking and remove the stand bow if there's no stand arrows left OR
            // if this method was called manually (ms value equals -10).
            public void FinalizeTheBow(float ms)
            {
                string message = "Stand arrows are over!";

                bool calledManually = ms == -10;

                if (bowWeapon.CurrentAmmo == 0 || calledManually)
                {
                    if (!calledManually)
                    {
                        Game.ShowChatMessage(message, Color.Cyan);
                        owner.RemoveWeaponItemType(WeaponItemType.Rifle);
                    }

                    PlayerStandBowList.Remove(owner);

                    standBowRecievedCallback.Stop();
                    standArrowCreatedCallBack.Stop();
                    standArrowHitCallBack.Stop();
                    standBowLostCallback.Stop();
                    onStandArrowsEndCallback.Stop();
                }
            }
        }

        // Crates from Ebomb09's Bullets Deluxe/Grenades Deluxe script
        // taken from there and adapted for this script.
        public class StandCrate
        {
            // 75% by default.
            public static int SpawnCrateChance = SDCrateSpawnChance;

            private static Random random = new Random();

            private static string cID;

            public static void OnObjectCreated(IObject[] objects)
            {
                foreach (IObject obj in objects)
                {
                    if (obj is IObjectSupplyCrate)
                    {
                        if (!obj.IsRemoved && !obj.RemovalInitiated)
                        {
                            WeaponItem item = (obj as IObjectSupplyCrate).GetWeaponItem();

                            if (item == WeaponItem.BOUNCINGAMMO ||
                                item == WeaponItem.FIREAMMO ||
                                item == WeaponItem.STRENGTHBOOST ||
                                item == WeaponItem.SPEEDBOOST ||
                                item == WeaponItem.SLOWMO_5 ||
                                item == WeaponItem.SLOWMO_10 ||
                                item == WeaponItem.C4 ||
                                item == WeaponItem.GRENADES ||
                                item == WeaponItem.SHURIKEN ||
                                item == WeaponItem.MOLOTOVS ||
                                item == WeaponItem.MINES ||
                                item == WeaponItem.PILLS ||
                                item == WeaponItem.MEDKIT)

                            {
                                if (random.Next(1, 101) <= SpawnCrateChance)
                                {
                                    CreateAmmoCrate(obj.GetWorldPosition());
                                    obj.Remove();
                                }
                            }
                        }
                    }
                }
            }

            public static void CreateAmmoCrate(Vector2 pos)
            {
                cID = "DISABLED" + Game.TotalElapsedGameTime;

                IObjectWeldJoint Welder = (IObjectWeldJoint)Game.CreateObject("WeldJoint", pos);

                IObject Grid = Game.CreateObject("InvisibleBlockNoCollision", pos + new Vector2(-4, 4));
                Grid.SetSizeFactor(new Point(2, 2));

                CollisionFilter col = new CollisionFilter();
                col.CategoryBits = 32;
                col.MaskBits = 3;
                col.AboveBits = 0;
                col.BlockExplosions = false;
                col.BlockFire = false;
                col.BlockMelee = false;
                col.ProjectileHit = false;
                Grid.SetCollisionFilter(col);

                IObject BG1 = Game.CreateObject("BgShadow00A", pos);
                IObject BG2 = Game.CreateObject("BgSymbol00Arrow", pos, MathHelper.PI * 2.25f);
                BG2.SetColors(new string[3] { "BgLightYellow", "BgLightYellow", "BgLightYellow" });

                IObjectAlterCollisionTile Alter = (IObjectAlterCollisionTile)Game.CreateObject("AlterCollisionTile", pos);
                Alter.SetDisablePlayerMelee(true);
                Alter.SetDisableProjectileHit(true);
                Alter.SetDisabledCategoryBits(0xFFFF);
                Alter.SetDisabledMaskBits(0xFFFF);
                Alter.SetDisabledAboveBits(0xFFFF);

                for (int i = 0; i < 4; i++)
                {
                    Vector2 Shift = Vector2.Zero;
                    Shift.X = (float)Math.Cos((MathHelper.PI / 2) * i + MathHelper.PI / 4) * 5.65f;
                    Shift.Y = (float)Math.Sin((MathHelper.PI / 2) * i + MathHelper.PI / 4) * 5.65f;

                    IObject Box = Game.CreateObject("StreetsweeperCratePart", pos + Shift, MathHelper.PI / 2 * i - MathHelper.PI / 2);
                    Box.SetBodyType(BodyType.Static);
                    Box.CustomID = cID;

                    Welder.AddTargetObject(Box);
                    Alter.AddTargetObject(Box);
                }

                IObjectActivateTrigger Button = (IObjectActivateTrigger)Game.CreateObject("ActivateTrigger", pos);
                Button.SetHighlightObject(Grid);
                Button.SetScriptMethod("CrateGiveStand");

                Welder.AddTargetObject(Button);
                Welder.AddTargetObject(Grid);
                Welder.AddTargetObject(BG1);
                Welder.AddTargetObject(BG2);
                Welder.AddTargetObject(Alter);

                Button.CustomID = cID;
                Grid.CustomID = cID;
                BG1.CustomID = cID;
                BG2.CustomID = cID;
                Welder.CustomID = cID;
                Alter.CustomID = cID;

                Events.UpdateCallback.Start(ValidateStandCrate, 500, 1);
            }

            public static void ValidateStandCrate(float ms)
            {
                foreach (IObject obj in Game.GetObjectsByCustomID(cID))
                {
                    obj.SetBodyType(BodyType.Dynamic);
                }
            }
        }

        private enum Modes
        {
            StandDeluxe,
            RoundStartStand,
            Base,
            Off
        }

        #endregion -=====================================================-

        /* -=======================-SCRIPT END-=======================- */
    }
}