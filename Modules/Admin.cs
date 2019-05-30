using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;

namespace raidbot.Modules
{
    public class Admin : ModuleBase
    {
        //dev server ids
        ulong signupsID = 365715381035466764;
        ulong botChannel = 354072212971585536;
/*
        //washed up ids
        ulong signupsID = 583131333623021569;
        ulong botChannel = 583137667693281280;
*/
        [Command("openraid")]
        [Alias("open", "or")]
        [Summary("Creates text file to hold names and roles for sign ups")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task OpenRaidCmd([Summary("Name for text file / name of raid, one word only.")] string raid, [Remainder, Summary("Custom message to send to signups channel, include information such as date/time, trials, normal/vet, etc.")] string message = null)
        {
            
            string fileName = raid + ".txt";
            fileName = Path.GetFullPath(fileName).Replace(fileName, "");
            fileName = fileName + @"\raids\" + raid + ".txt";
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
                //write summary to file, write first signup as person who opened raid
                string[] lines = {message, Context.Message.Author.Username, roles};
                File.WriteAllLines(fileName,lines);

                var channel = await Context.Guild.GetChannelAsync(signupsID) as SocketTextChannel;
                if (message != null)
                {
                    await channel.SendMessageAsync($"Raid signups now open for {raid}! \n{message}");
                }
                else
                {
                    await channel.SendMessageAsync($"Raid signups now open for {raid}!");
                }
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
                    fileName = fileName + @"\raids\" + name + ".txt";
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
                string fileName = raid + ".txt";
                fileName = Path.GetFullPath(fileName).Replace(fileName, "");
                fileName = fileName + @"\raids\" + raid + ".txt";
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

                        line = sr.ReadLine();
                        if (line == null)
                        {
                            await ReplyAsync("No users signed up for " + raid + " raid.");
                        }
                        else
                        {
                            var guild = Context.Guild as SocketGuild;
                            var users = guild.Users;
                            int count = 0;
                            while (line != null && count <= 11)
                            {
                                SocketUser player = null;
                                try
                                {
                                    player = users.Where(x => x.Username == line).First() as SocketUser;
                                }
                                catch(Exception e)
                                {
                                    Console.WriteLine($"Player {line} in {raid}.txt not found in server.");
                                }
                                if (player != null)
                                {
                                    sendmsg = sendmsg  + player.Mention + " ";
                                    count++;
                                }
                                line = sr.ReadLine();
                                line = sr.ReadLine();
                            }
                            sendmsg = sendmsg + "forming up for " + raid + "! Time to log in.";
                            var channel = await Context.Guild.GetChannelAsync(signupsID) as SocketTextChannel;
                            await channel.SendMessageAsync(sendmsg);
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
    }
}
