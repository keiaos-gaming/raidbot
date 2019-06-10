using Discord;
using Discord.Net;
using Discord.WebSocket;
using Discord.Commands;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

/*
    changed file name syntax for use on pi, for testing on pc use: fileName = fileName + @"\raids\" + raid + ".txt";
 */
namespace raidbot.Modules
{
    public class Admin : ModuleBase
    {
        //need _services to access config file
        private readonly IServiceProvider _services;
        private readonly IConfiguration _config;

        ulong signupsID;
        ulong botChannel;
        public Admin(IServiceProvider services)
        {
            _config = services.GetRequiredService<IConfiguration>();
            signupsID = Convert.ToUInt64(_config["SignupsID"]);
            botChannel = Convert.ToUInt64(_config["BotchannelID"]);
           
        }
        
        //TO DO: add some way to get role limits, null if not provided
        
        [Command("openraid")]
        [Alias("open", "or")]
        [Summary("Creates text file to hold names and roles for sign ups")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task OpenRaidCmd([Summary("Name for text file / name of raid, one word only.")] string raid,[Summary("Number of tanks for raid.")] int tankLimit = 0, [Summary("Number of healers for raid.")]int healLimit = 0,[Summary("Number of melee DPS for raid.")] int mdpsLimit = 0,[Summary("Number of ranged DPS for raid.")] int rdpsLimit = 0, [Remainder, Summary("Custom message to send to signups channel, include information such as date/time, trials, normal/vet, etc.")] string message = null)
        {
            
            string fileName = raid + ".txt";
            fileName = Path.GetFullPath(fileName).Replace(fileName, "");
            fileName = fileName + "//raids//" + raid.ToLower() + ".txt";
            if (Context.Channel.Id != botChannel)
            {
                await ReplyAsync($"Please use this command in {(await Context.Guild.GetChannelAsync(botChannel) as SocketTextChannel).Mention}");
            }
            else if (File.Exists(fileName))
            {
                await ReplyAsync($"File Name {fileName} already exists, use another name or clear the raid.");
            }
            else if (message == null)
            {
                await ReplyAsync($"Please include more information about the raid to be saved and announced, such as time, date, specific raids, normal/veteran, etc.");
            }
            else if (tankLimit == 0 || healLimit == 0 || mdpsLimit == 0 || rdpsLimit == 0)
            {
                await ReplyAsync("Please include role limits with command. Use '!help openraid' for help on how to use this command.");
            }
            else
            {
                File.Create(fileName).Close();
                
                //get leader defaults
                bool defaultFound = false;
                string line = "", roles = "";
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

                string roleLimits = "";
                //add role limits if all are specified
                if (tankLimit != 0 && healLimit != 0 && mdpsLimit != 0 && rdpsLimit != 0)
                {
                    roleLimits = $"{tankLimit.ToString()} {healLimit.ToString()} {mdpsLimit.ToString()} {rdpsLimit.ToString()}";
                }

                //write summary to file, write first signup as person who opened raid
                string[] lines = {message, roleLimits, Context.Message.Author.Username, roles};
                File.WriteAllLines(fileName,lines);

                var channel = await Context.Guild.GetChannelAsync(signupsID) as SocketTextChannel;
                await channel.SendMessageAsync($"Raid signups now open for {raid}! \n{message}");
                await ReplyAsync($"Successfully created raid. Role Limits: {tankLimit} tanks, {healLimit} healers, {mdpsLimit} mdps, {rdpsLimit} rdps.");

            }
        }

        [Command("closeraid")]
        [Alias("clearraid", "clear", "close", "cr")]
        [Summary("Deletes signups for specified raid.")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task CloseRaidCmd([Remainder, Summary("Name of the text file to be deleted.")] string name = null)
        {
            string sendmsg = "";

            if (Context.Channel.Id != botChannel)
            {
                sendmsg = $"Please use this command in {(await Context.Guild.GetChannelAsync(botChannel) as SocketTextChannel).Mention}";
            }
            else if (name == null)
            {
                sendmsg = "Please enter the file name with the command.";
            }
            else
            {
                try
                {

                    string fileName = name + ".txt";
                    fileName = Path.GetFullPath(fileName).Replace(fileName, "");
                    fileName = fileName + "//raids//" + name.ToLower() + ".txt";
                    if (!File.Exists(fileName))
                    {
                        sendmsg = "Error: " + fileName + " does not exist.";
                    }
                    else
                    {
                        File.Delete(fileName);
                        sendmsg = "Deleted " + name + ".txt successfully.";
                    }
                }
                catch (Exception e)
                {
                    await ReplyAsync("Exception: " + e.Message);
                }
            }

            await ReplyAsync(sendmsg);
        }

        //roll call updated for role limit
        [Command("rollcall")]
        [Alias("rc")]
        [RequireUserPermission(GuildPermission.Administrator)]
        [Summary("Mentions users signed up for specified raid with a message that raid is forming.")]
        public async Task RollCallCmd([Summary("Name of raid to call roll call for.")] string raid = null)
        {

            if (Context.Channel.Id != botChannel)
            {
                await ReplyAsync($"Please use this command in {(await Context.Guild.GetChannelAsync(botChannel) as SocketTextChannel).Mention}");
            }
            else if (raid == null)
            {
                await ReplyAsync("Please enter the raid name with the command.");
            }
            else
            {
                List<string> tankList = new List<string>(), healerList= new List<string>(), mdpsList= new List<string>(), rdpsList= new List<string>(), overflow = new List<string>();
                string fileName = raid + ".txt";
                fileName = Path.GetFullPath(fileName).Replace(fileName, "");
                fileName = fileName + "//raids//" + raid.ToLower() + ".txt";
                int tankLimit = 2, healLimit = 2, mLimit = 4, rLimit = 4;
                if (!File.Exists(fileName))
                {
                    await ReplyAsync("Error: " + fileName + " does not exist.");
                }
                else
                {
                    string line = "";
                    string sendmsg = "";
                    try
                    {
                        StreamReader sr = new StreamReader(fileName);
                        //skip first line (summary)
                        line = sr.ReadLine();
                        //skip role limits
                        string roleLimits;
                        line = sr.ReadLine();
                        roleLimits = line;
                        
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

                        line = sr.ReadLine();
                        if (line == null) //no signups in file
                        {
                            await ReplyAsync("No players signed up for raid.");
                        }
                        else
                        {
                            SocketUser plyr = null;
                            var guild = Context.Guild as SocketGuild;
                            var users = guild.Users;
                            while (line != null)
                            {
                                string player = line;
                                line = sr.ReadLine();
                                if (line.Contains("tank") && tankList.Count() < tankLimit)
                                {
                                    tankList.Add(player);
                                }
                                else if (line.Contains("healer")&& healerList.Count() < healLimit)
                                {
                                    healerList.Add(player);
                                }
                                else if (line.Contains("mdps")&& mdpsList.Count() < mLimit)
                                {
                                    mdpsList.Add(player);
                                }
                                else if (line.Contains("rdps")&&  rdpsList.Count() < rLimit)
                                {
                                    rdpsList.Add(player);
                                }
                                else
                                {
                                    overflow.Add($"{player}: {line}");
                                }
                            
                                line = sr.ReadLine();
                            }
                            try
                            {
                                foreach (string tank in tankList)
                                {
                                    plyr = users.Where(x => x.Username == tank).First() as SocketUser;
                                    if (plyr != null)
                                    {
                                        sendmsg = sendmsg  + plyr.Mention + " (tank), ";
                                        plyr = null;
                                    }
                                }
                                foreach (string heals in healerList)
                                {
                                    plyr = users.Where(x => x.Username == heals).First() as SocketUser;
                                    if (plyr != null)
                                    {
                                        sendmsg = sendmsg  + plyr.Mention + " (healer), ";
                                        plyr = null;
                                    }
                                }
                                foreach (string melee in mdpsList)
                                {
                                    plyr = users.Where(x => x.Username == melee).First() as SocketUser;
                                    if (plyr != null)
                                    {
                                        sendmsg = sendmsg  + plyr.Mention + " (mdps), ";
                                        plyr = null;
                                    }
                                }
                                foreach (string ranged in rdpsList)
                                {
                                    plyr = users.Where(x => x.Username == ranged).First() as SocketUser;
                                    if (plyr != null)
                                    {
                                        sendmsg = sendmsg  + plyr.Mention + " (rdps), ";
                                        plyr = null;
                                    }
                                }
                            }
                            catch(Exception )
                            {
                                Console.WriteLine($"Player {line} in {raid}.txt not found in server.");
                            }
                            var channel = await Context.Guild.GetChannelAsync(signupsID) as SocketTextChannel;
                            await channel.SendMessageAsync ($"{sendmsg}forming up for {raid}, time to log in!");
                        }
                        sr.Close();
                    }
                    catch (Exception e)
                    {
                        await ReplyAsync("Exception: " + e.Message);
                    }
                    
                }
            }
            
        }
        [Command("edit")]
        [RequireUserPermission(GuildPermission.Administrator)]
        [Summary("Edits a specified user's sign up within a raid file.")]
        public async Task EditCmd([Summary("Name of raid to edit.")] string raid = null, [Summary("Username or Mention of user to edit in file.")] SocketGuildUser user = null, [Remainder, Summary("Edit function, available options are: remove, add (roles optional), edit (new roles optional).")]string function = null)
        {
            if (raid == null)
            {
                await ReplyAsync("Please include the raid name with this command.");
            }
            else if (function == null)
            {
                await ReplyAsync("Please include a function with this command.");
            }
            else 
            {
                string line = "", sendmsg = "";
                List<string> names = new List<string>();
                List<string> roles = new List<string>();
                bool playerFound = false;

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
                        string raidSum = sr.ReadLine();
                        string roleLimits = sr.ReadLine();
                        line = sr.ReadLine();

                        //loop through file
                        while (line != null)
                        {
                            if (user.Username == line)
                            {
                                playerFound = true;
                                if (function.ToLower().Contains("remove"))
                                {
                                    //skip adding names and roles
                                    line = sr.ReadLine();
                                    sendmsg = $"{user.Username} removed from {raid} sign ups.";
                                }
                                else if (function.ToLower().Contains("edit"))
                                {
                                    //edit roles
                                    string newRoles = function.ToLower().Replace("edit", "");
                                    string updatedRoles = "";
                                    if (newRoles.ToUpper().Contains("MDPS") || newRoles.ToUpper().Contains("MELEE")|| newRoles.ToUpper().Contains("MELE")|| newRoles.ToUpper().Contains("MELLE"))
                                    {
                                        updatedRoles += "mdps ";
                                    }
                                    if (newRoles.ToUpper().Contains("RDPS") || newRoles.ToUpper().Contains("RANGE")|| newRoles.ToUpper().Contains("RANGED"))
                                    {
                                        updatedRoles += "rdps ";
                                    }
                                    if (newRoles.ToUpper().Contains("HEALER") || newRoles.ToUpper().Contains("HEALS") || newRoles.ToUpper().Contains("HEAL"))
                                    {
                                        updatedRoles += "healer ";
                                    }
                                    if (newRoles.ToUpper().Contains("TANK"))
                                    {
                                        updatedRoles += "tank ";
                                    }
                                    if (updatedRoles == "")
                                    {
                                        updatedRoles = "mdps ";
                                    }
                                    names.Add(line);
                                    roles.Add(updatedRoles);
                                    line = sr.ReadLine();
                                    sendmsg = $"{user.Username} roles updated to {updatedRoles}";
                                }
                            }
                            else //line is not user
                            {
                                names.Add(line);
                                line = sr.ReadLine();
                                roles.Add(line);

                            }
                            line = sr.ReadLine();
                        }
                        sr.Close();
                        //rewrite file if changes were made
                        if (playerFound == true)
                        {
                            StreamWriter sw = new StreamWriter(fileName);
                            sw.WriteLine(raidSum);
                            sw.WriteLine(roleLimits);
                            for (int x = 0; x < names.Count(); x++)
                            {
                                sw.WriteLine(names[x]);
                                sw.WriteLine(roles[x]);
                            }
                            sw.Close();
                        }
                        if (playerFound == false)
                        {
                            if (function.ToLower().Contains("add"))
                            {
                                string newRoles = function.ToLower().Replace("add", "");
                                string updatedRoles = "";
                                //get roles
                                if (newRoles.ToUpper().Contains("MDPS") || newRoles.ToUpper().Contains("MELEE")|| newRoles.ToUpper().Contains("MELE")|| newRoles.ToUpper().Contains("MELLE"))
                                {
                                    updatedRoles += "mdps ";
                                }
                                if (newRoles.ToUpper().Contains("RDPS") || newRoles.ToUpper().Contains("RANGE")|| newRoles.ToUpper().Contains("RANGED"))
                                {
                                    updatedRoles += "rdps ";
                                }
                                if (newRoles.ToUpper().Contains("HEALER") || newRoles.ToUpper().Contains("HEALS") || newRoles.ToUpper().Contains("HEAL"))
                                {
                                    updatedRoles += "healer ";
                                }
                                if (newRoles.ToUpper().Contains("TANK"))
                                {
                                    updatedRoles += "tank ";
                                }
                                if (updatedRoles == "")
                                {
                                    updatedRoles = "mdps ";
                                }
                                StreamWriter writer = new StreamWriter(@fileName, true);

                                writer.WriteLine(user.Username);
                                writer.WriteLine(updatedRoles);
                                writer.Close();
                                sendmsg = $"{user.Username} added to sign ups as {updatedRoles}";
                            }
                            else
                            {
                                sendmsg = $"{user.Username} not found in {raid} sign ups.";
                            }
                        }
                        //rewrite names and roles to file                        
                        await ReplyAsync(sendmsg);
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
