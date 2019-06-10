using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Discord.Commands;
using Discord;

/*
    Text File Format: Raid_name.txt
    raid summary
    role limits: tank heal mdps rdps
    player name
    player roles
 */

namespace raidbot
{
    public class SignUp : ModuleBase
    {
        int maxSignUps = 12;
        int tankLimit = 2;
        int healLimit = 2;
        int mLimit = 4;
        int rLimit = 4;

        [Command("signup")]
        [Alias("su")]
        [Summary("Signs user up for specified raid with specified roles.")]
        public async Task SignUpCmd([Summary("Raid to sign up for")]string raid, [Summary("Roles you wish to sign up with for raid."),Remainder]string roles = null)
        {
            int signUps = 0;
            bool playerAllowed = true;
            string line = "", sendmsg = "";

            //define filepath
            string fileName = raid + ".txt";
            fileName = Path.GetFullPath(fileName).Replace(fileName, "");
            fileName = fileName + "//raids//" + raid.ToLower() + ".txt";

            if (!File.Exists(fileName)) //file doesnt exist
            {
                sendmsg = ($"Raid for {raid} doesn't exist!");
            }
            else
            {
                try
                {
                    //check if player is already signed up
                    StreamReader sr = new StreamReader(fileName);
                    line = sr.ReadLine();
                    //skip first line (summary)
                    line = sr.ReadLine();
                    //skip role limits
                    line = sr.ReadLine();

                    //loop through file
                    while (line != null)
                    {
                        if (Context.Message.Author.Username == line)
                        {
                            //if player is found, msg sends and skips sign up process
                            sendmsg = "Only one signup allowed per person. You have already signed up.";
                            playerAllowed = false;
                        }
                        signUps++;
                        line = sr.ReadLine();
                    }
                    sr.Close();
                    signUps = (signUps) / 2;
                }
                catch (Exception e)
                {
                    await ReplyAsync("Exception: " + e.Message);
                }

                if (playerAllowed) // player not found in signup
                {
                    if (roles == null) // roles omitted, use defaults
                    {
                        bool defaultFound = false;
                        try
                        {
                            //search defaults for user
                            StreamReader sr = new StreamReader("defaults.txt");
                            line = sr.ReadLine();
                            while (line != null)
                            {
                                if (Context.Message.Author.Username == line)
                                {
                                    //user found roles are saved
                                    defaultFound = true;
                                    roles = sr.ReadLine();
                                }
                                line = sr.ReadLine();
                            }

                            if (!defaultFound) // not found in defaults.txt, uses dps as default
                            {
                                roles = "mdps ";
                            }
                            sr.Close();
                        }
                        catch (Exception e)
                        {
                            await ReplyAsync("Exception: " + e.Message);
                        }

                        //adds names to sign up file
                        try
                        {
                            StreamWriter sw = new StreamWriter(@fileName, true);

                            //Write a line of text
                            sw.WriteLine(Context.Message.Author.Username);
                            sw.WriteLine(roles);
                            //close the file
                            sw.Close();
                        }
                        catch (Exception e)
                        {
                            await ReplyAsync("Exception: " + e.Message);
                        }

                        sendmsg = Context.Message.Author.Username + " has signed up as " + roles;

                    }

                    //user gives roles
                    else
                    {
                        //format roles for writing
                        string updatedRoles = "";
                        if (roles.ToUpper().Contains("MDPS") || roles.ToUpper().Contains("MELEE")|| roles.ToUpper().Contains("MELE")|| roles.ToUpper().Contains("MELLE"))
                        {
                            updatedRoles += "mdps ";
                        }
                        if (roles.ToUpper().Contains("RDPS") || roles.ToUpper().Contains("RANGE")|| roles.ToUpper().Contains("RANGED"))
                        {
                            updatedRoles += "rdps ";
                        }
                        if (roles.ToUpper().Contains("HEALER") || roles.ToUpper().Contains("HEALS") || roles.ToUpper().Contains("HEAL"))
                        {
                            updatedRoles += "healer ";
                        }
                        if (roles.ToUpper().Contains("TANK"))
                        {
                            updatedRoles += "tank ";
                        }
                        if (updatedRoles == "")
                        {
                            updatedRoles = "mdps ";
                        }

                        //adds name and roles to file
                        try
                        {
                            StreamWriter sw = new StreamWriter(@fileName, true);

                            sw.WriteLine(Context.Message.Author.Username);
                            sw.WriteLine(updatedRoles);
                            sw.Close();
                        }
                        catch (Exception e)
                        {
                            await ReplyAsync("Exception: " + e.Message);
                        }

                        sendmsg = Context.Message.Author.Username + " has signed up as " + updatedRoles;

                    }

                    //if raid is full 
                    if (signUps >= (tankLimit + healLimit + mLimit + rLimit))
                    {
                        sendmsg += "\nRaid is full! Signed up as overflow.";
                    }
                }
            }
            await ReplyAsync(sendmsg);
        }

        [Command("withdraw")]
        [Summary("Withdraws user from specified raid if signed up.")]
        public async Task WithdrawCmd([Summary("Raid to withdraw from")] string raid = null)
        {
            String line;
            string sendmsg = "";
            string raidSum, roleLimits;

            //define file path
            string fileName = raid + ".txt";
            fileName = Path.GetFullPath(fileName).Replace(fileName, "");
            fileName = fileName + "//raids//" + raid.ToLower() + ".txt";

            int i = 0;
            List<string> names = new List<string>();
            List<string> roles = new List<string>();
            bool playerFound = false;

            if (raid != null && File.Exists(fileName)) //command is correct and file exists
            {
                //read sign up list to see if player has already registered
                try
                {
                    StreamReader sr = new StreamReader(fileName);
                    raidSum = sr.ReadLine();
                    roleLimits = sr.ReadLine();
                    line = sr.ReadLine();

                    //loop through file
                    while (line != null)
                    {
                        if (Context.Message.Author.Username == line)
                        {
                            //if player is found, msg sends, skips saving name and roles for rewrite
                            sendmsg = Context.Message.Author.Username + " removed from " + raid + " signups.";
                            line = sr.ReadLine();
                            playerFound = true;
                        }
                        else
                        {
                            //if not user, adds lines to names and roles for rewrite
                            names.Add(line);
                            line = sr.ReadLine();
                            roles.Add(line);
                            i++;
                        }

                        line = sr.ReadLine();
                    }
                    sr.Close();

                    //rewrite names and roles to file
                    StreamWriter sw = new StreamWriter(fileName);
                    sw.WriteLine(raidSum);
                    sw.WriteLine(roleLimits);
                    for (int x = 0; x < i; x++)
                    {
                        sw.WriteLine(names[x]);
                        sw.WriteLine(roles[x]);
                    }
                    sw.Close();
                }
                catch (Exception e)
                {
                    await ReplyAsync("Exception: " + e.Message);
                }
                if (!playerFound) //user not in file
                {
                    sendmsg = "Player not found in signup list.";
                }
            }
            else if (raid == null) //parameter not given
            {
                sendmsg = "Please include a raid with the command.";
            }
            else if (!File.Exists(fileName)) //file doesnt exist, raid with specified name for file doesnt exist
            {
                sendmsg = "Error: " + raid + " raid doesn't exist";
            }

            await ReplyAsync(sendmsg);
        }

        //updated status for role limits
        [Command("status")]
        [Summary("Lists players signed up for specified raid")]
        public async Task StatusCmd([Summary("Name of raid for status.")] string raid = null)
        {
            if (raid == null) //no parameter given
            {
                await ReplyAsync("Please include the name of the raid with the command.");
            }
            else
            {
                //define file path
                string fileName = raid + ".txt";
                fileName = Path.GetFullPath(fileName).Replace(fileName, "");
                fileName = fileName + "//raids//" + raid.ToLower() + ".txt";
                if (!File.Exists(fileName))
                {
                    await ReplyAsync($"Raid for {raid} does not exist.");
                }
                else
                {
                    int number = 0;
                    int mdps = 0, rdps = 0, tanks = 0, heals = 0;
                    string line;
                    string raidSum, roleLimits;
                    //lists for player names
                    List<string> tankList = new List<string>(), healerList= new List<string>(), mdpsList= new List<string>(), rdpsList= new List<string>(), overflow = new List<string>();
                    //create embed
                    var builder = new EmbedBuilder()
	                    .WithColor(Color.Green)
	                    .WithAuthor(author => {
		                    author.WithName(raid);
	                    });

                    try
                    {
                        //read file, if not empty add names and roles to message
                        StreamReader sr = new StreamReader(fileName);
                        //read raid summary and add to embed
                        raidSum = sr.ReadLine();
                        roleLimits = sr.ReadLine();

                        //get role limits from file
                        if (roleLimits != null)
                        {

                            try
                            {
                                string[] splitstring = roleLimits.Split(' ');
                                tankLimit = Convert.ToInt32(splitstring[0]);
                                healLimit = Convert.ToInt32(splitstring[1]);
                                mLimit = Convert.ToInt32(splitstring[2]);
                                rLimit = Convert.ToInt32(splitstring[3]);
                            }
                            catch(Exception e)
                            {
                                await ReplyAsync("Exception: " + e.Message);
                            }
                        }

                        builder.WithTitle(raidSum);
                        //get first name
                        line = sr.ReadLine();                        
                        while (line != null)
                        {
                            string player = line;
                            number++;

                            line = sr.ReadLine();
                            if (line.Contains("tank") && tankList.Count() < tankLimit)
                            {
                                tankList.Add(player);
                                tanks++;
                            }
                            else if (line.Contains("healer")&& healerList.Count() < healLimit)
                            {
                                healerList.Add(player);
                                heals++;
                            }
                            else if (line.Contains("mdps")&& mdpsList.Count() < mLimit)
                            {
                                mdpsList.Add(player);
                                mdps++;
                            }
                            else if (line.Contains("rdps")&&  rdpsList.Count() < rLimit)
                            {
                                rdpsList.Add(player);
                                rdps++;
                            }
                            else
                            {
                                overflow.Add($"{player}: {line}");
                            }
                            
                            line = sr.ReadLine();
                        }

                        sr.Close();                        
                        if (number == 0) //no signups in file
                        {
                            builder.WithDescription("No players signed up for raid.");
                            await Context.Channel.SendMessageAsync("", false, builder.Build()).ConfigureAwait(false);
                        }
                        else //file read successfully, add to embed, build, and send
                        {
                            string players = "";
                            //add tanks to embed
                            foreach(string tank in tankList)
                            {
                                players += tank + "\n";
                            }
                            if (players != "") 
                                builder.AddField("Tanks:", players);
                            //add healers to embed
                            players = "";
                            foreach(string heal in healerList)
                            {
                                players += heal + "\n";
                            }
                            if (players != "")
                                builder.AddField("Healers:", players);
                            //add mdps to embed
                            players = "";
                            foreach(string m in mdpsList)
                            {
                                players += m + "\n";
                            }
                            if (players != "")
                                builder.AddField("Melee DPS:", players);
                            //add rdps to embed
                            players = "";
                            foreach(string r in rdpsList)
                            {
                                players += r + "\n";
                            }
                            if (players != "")
                                builder.AddField("Ranged DPS:", players);
                            //add overflow to embed
                            players = "";
                            foreach(string o in overflow)
                            {
                                players += o + "\n";
                            }
                            if (players != "")
                                builder.AddField("Overflow:", players);
                            //add counts to footer
                            builder.WithFooter(footer => {
                                footer.WithText(mdps + " mdps, "+ rdps + " rdps, " + tanks + " tanks, " + heals + " heals, " + number + $"/{tankLimit + healLimit + mLimit + rLimit} signed up");
                                });
                            await Context.Channel.SendMessageAsync("", false, builder.Build()).ConfigureAwait(false);
                        }

                    }
                    catch (Exception e)
                    {
                        await ReplyAsync("Exception: " + e.Message);
                    }
                    
                }

            }
        }

        [Command("raidlist")]
        [Alias("list")]
        [Summary("Lists raids availble for signups.")]
        public async Task RaidListCmd()
        {
            //define file path
            string path = Path.GetFullPath("config.txt").Replace("config.txt", @"raids/");
            string[] folder = Directory.GetFiles(path);
            string sendmsg = "";
            //loop through array and get names of files
            foreach (string file in folder)
            {
                string raid = file.Replace(".txt", "");
                raid = raid.Replace(path , "");
                sendmsg += raid + "\n";
            }
            var builder = new EmbedBuilder()
	            .WithDescription(sendmsg)
	            .WithAuthor(author => {
		            author
			            .WithName("Available Raids:");
	        });
            if (folder.Count() == 0)//no files in folder
            {
                await ReplyAsync("No raids currently available.");
            }
            else
            {
                await Context.Channel.SendMessageAsync("", false, builder.Build()).ConfigureAwait(false);
            }
        }

        [Command("default")]
        [Summary("Sets default roles to be used for raids when roles not specified.")]
        public async Task DefaultCmd([Remainder, Summary("Roles for defaults.")]string roles = null)
        {
            if (roles == null) // no parameter given with command
            {
                await ReplyAsync("Please include roles with the command.");
            }
            else
            {
                string line;
                string newRoles = "";
                int i = 0;
                List<string> names = new List<string>();
                List<string> defaults = new List<string>();
                bool playerFound = false;


                //process roles and format
                if (roles.ToUpper().Contains("MDPS") || roles.ToUpper().Contains("MELEE")|| roles.ToUpper().Contains("MELE")|| roles.ToUpper().Contains("MELLE"))
                {
                    newRoles = newRoles + "mdps ";
                }
                if (roles.ToUpper().Contains("RDPS") || roles.ToUpper().Contains("RANGED")|| roles.ToUpper().Contains("RANGE"))
                {
                    newRoles = newRoles + "rdps ";
                }
                if (roles.ToUpper().Contains("TANK"))
                {
                    newRoles = newRoles + "tank ";
                }
                if (roles.ToUpper().Contains("HEAL") || roles.ToUpper().Contains("HEALER") || roles.ToUpper().Contains("HEALS"))
                {
                    newRoles += "healer ";
                }
                
                if (newRoles == "") //roles given did not contain dps tank or heal
                {
                    await ReplyAsync($"Error, {roles} is not a valid role.");
                }
                else
                {
                    try
                    {
                        //read defaults to see if default was already given
                        StreamReader sr = new StreamReader("defaults.txt");
                        line = sr.ReadLine();

                        while (line != null)
                        {
                            if (Context.Message.Author.Username == line)
                            {
                                //if player is found, msg sends, skips saving name and roles for rewrite
                                names.Add(line);
                                defaults.Add(newRoles);
                                line = sr.ReadLine();
                                playerFound = true;
                                i++;

                            }
                            else
                            {
                                //if not user, adds lines to names and roles for rewrite
                                names.Add(line);
                                line = sr.ReadLine();
                                defaults.Add(line);
                                i++;
                            }

                            line = sr.ReadLine();
                        }
                        sr.Close();

                        if (!playerFound) // user not already in defaults file
                        {
                            names.Add(Context.Message.Author.Username);
                            defaults.Add(newRoles);
                            i++;
                        }

                        //write names back into file
                        StreamWriter sw = new StreamWriter("defaults.txt");
                        for (int x = 0; x < i; x++)
                        {
                            sw.WriteLine(names[x]);
                            sw.WriteLine(defaults[x]);
                        }
                        sw.Close();
                        await ReplyAsync(Context.Message.Author.Username + " registered " + newRoles + "as default.");
                    }
                    catch (Exception e)
                    {
                        await ReplyAsync("Exception: " + e.Message);
                    }
                }
            }
        }

    }
}