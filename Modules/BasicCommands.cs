using Discord;
using Discord.Net;
using Discord.WebSocket;
using Discord.Rest;
using Discord.Commands;
using System;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
using System.Xml.Linq;
using System.Net;
using System.Threading;
using System.Text.RegularExpressions;
using System.IO;

namespace raidbot.Modules
{
    // for commands to be available, and have the Context passed to them, we must inherit ModuleBase
    public class BasicCommands : ModuleBase
    {
        private CommandService _service;
        //need _services to access config file
        private readonly IServiceProvider _services;
        private readonly IConfiguration _config;

        public BasicCommands(CommandService service, IServiceProvider services)
        {
            _config = services.GetRequiredService<IConfiguration>();
            _service = service;
            
        }

        [Command("help")]
        [Summary("Shows what a specific command does and what parameters it takes.")]
        public async Task HelpAsync([Remainder, Summary("Command to retrieve help for")] string command = null)
        {
            char prefix = Char.Parse(_config["Prefix"]);
            
            if (command == null)
            {
                var builder = new EmbedBuilder()
                {
                    Color = new Color(114, 137, 218),
                    Title = $"**Command Help for {Context.Client.CurrentUser.Username}**",
                    Description = "Use !help (command) for more information on how to use specific commands.\n"

                };

                foreach (var module in _service.Modules) //loop through modules from _service
                {
                    string description = null;
                    foreach (var cmd in module.Commands) //loop through commands in modules
                    {
                        var result = await cmd.CheckPreconditionsAsync(Context);
                        if (result.IsSuccess)
                            description += $"{prefix}{cmd.Aliases.First()}\n"; //if command passes, first alias AKA command name is added
                    }

                    if (!string.IsNullOrWhiteSpace(description)) //if the module wasn't empty, add info to field
                    {
                        builder.AddField(x =>
                        {
                            x.Name = module.Name;
                            x.Value = description;
                            x.IsInline = false;
                        });
                    }
                }
                //send help msg in DM to user
                //var dmchannel = await Context.User.GetOrCreateDMChannelAsync();
                //await dmchannel.SendMessageAsync("", false, builder.Build());

                //or post in channel where used
                await ReplyAsync("", false, builder.Build());
            }
            else //user asks for help for specific command
            {
                var result = _service.Search(Context, command);

                if (!result.IsSuccess)//command not found in search
                {
                    await ReplyAsync($"Sorry, **{command}** command doesn't exist.");
                    return;
                }

                var builder = new EmbedBuilder()
                {
                    Color = new Color(114, 137, 218),
                    
                };

                foreach (var match in result.Commands)
                {
                    var cmd = match.Command;
                    builder.Title = $"Help for command {command}";
                    builder.Description = $"**Usage:** {prefix}{command} ({string.Join(") (", cmd.Parameters.Select(p => p.Name))})";
                    builder.AddField(x =>
                    {
                        x.Name = "**Aliases:** " + string.Join(", ", cmd.Aliases) + System.Environment.NewLine;
                        x.Value =
                            $"**Summary:** \n{cmd.Summary}\n" +
                            $"**Parameters:** \n{string.Join("\n", cmd.Parameters.Select(p => "**" + p.Name + ":** " + p.Summary ))}";
                        x.IsInline = false;
                    });
                }
                await ReplyAsync("", false, builder.Build());
            }
        }

        //more commands under here
        [Command("test")]
        [Summary("Dev purposes only. Tests options in config file.")]
        public async Task ServerTest()
        {
            char Prefix = Char.Parse(_config["Prefix"]);
            var SignupsID =Convert.ToUInt64(_config["SignupsID"]);
            var BotchannelID = Convert.ToUInt64(_config["BotchannelID"]) ;
            var suchannel = await Context.Guild.GetChannelAsync(SignupsID) as SocketTextChannel;
            var botchannel = await Context.Guild.GetChannelAsync(BotchannelID) as SocketTextChannel;
            await ReplyAsync($"Prefix: '{Prefix}' \nRaid signups channel: {suchannel.Mention} \nBot channel: {botchannel.Mention}");
        }

        [Command("meow")]
        [Alias("cat")]
        [Summary("Random cats, nuff said.")]
        public async Task CatCmd()
        {
            var embed = new EmbedBuilder();
            //grab the xml
            //Side note: I recommend making a static instance of HttpClient through out your application instead of making a new instance each time
            //but for this example I went with this
            var response = await new HttpClient().GetAsync("http://thecatapi.com/api/images/get?format=xml&results_per_page=20");
            //if it fails to get the xml, then we return
            if (!response.IsSuccessStatusCode) return;

            //read the xml as a string
            string xml = await response.Content.ReadAsStringAsync();
            var xdoc = XDocument.Parse(xml); //parse xml
            //get each image element
            var elems = xdoc.Elements("response").Elements("data").Elements("images").Elements("image");

            if (elems == null) return; //if its null, then we return

            List<string> urls = new List<string>(); //new list to store the urls
            urls.AddRange(elems.Select(x => x.Element("url")?.Value)); //add the image urls to the list
            int max = urls.Count; //set the max page
            max--; //reduce it by 1 cause list index starts from 0
            //embed.WithTitle($"Cat Image [0/{max}]"); //set title
            //embed.WithDescription("_use reaction to navigate (Expires around 20 seconds after changing pictures)_"); //set description
            embed.WithColor(Color.Green); //set color
            embed.WithImageUrl(urls.First()); //set the first image
            var msg = await ReplyAsync("", false, embed.Build()); //send msg
            var nextArrow = new Emoji("➡");
            var backArrow = new Emoji("⬅");
            //add arrow reactions to the msg
            //await msg.AddReactionAsync(backArrow, new RequestOptions());
            //await Task.Delay(500);
            //await msg.AddReactionAsync(nextArrow, new RequestOptions());
            //start a new thread (prevents the messagehandler event from getting blocked)
            
            //emoji counting not working

            /*
            var task = Task.Run(async () =>
            {
                int i = 0; int page = 0; int PreviousNext = 0; int PreviousBack = 0;
                
                try
                {
                    while (i <= 40)
                    {
                        i++; //add to i
                        if (i == 1) //if its the first run or someone has pushed one of the arrow reactions
                        {
                            //get the reaction arrows and sets them to the variables
                            var userlist = msg.GetReactionUsersAsync(nextArrow, 100).FlattenAsync();
                            PreviousNext = msg.GetReactionUsersAsync(nextArrow).GetAwaiter().GetResult().Count;
                            PreviousBack = msg.GetReactionUsersAsync(backArrow).GetAwaiter().GetResult().Count;
                        }

                        await Task.Delay(500); //sleep the thread for 500ms
                        //set the currentback variable
                        var currentBack = msg.GetReactionUsersAsync(backArrow).GetAwaiter().GetResult().Count;
                        //if a user has removed or added a back arrow reaction, and the page is greater than 0
                        if (PreviousBack < currentBack || PreviousBack > currentBack && page > 0)
                        {
                            page--; //reduce the page by 1
                            PreviousBack = currentBack; //set the previous back to the current
                            i = 0; //reset i to 0
                            embed.WithTitle($"Cat Image [{page}/{max}]"); //set title
                            embed.WithImageUrl(urls[page]); //grab the image by page index and set it
                            //update msg
                            msg.ModifyAsync(x => x.Embed = embed.Build()).GetAwaiter();
                        }
                        //set the currentback variable
                        var currentNext = msg.GetReactionUsersAsync(nextArrow).GetAwaiter().GetResult().Count;
                        //if a user has removed or added a forward arrow, and the page is less than max
                        if (PreviousNext < currentNext || PreviousNext > currentNext && page < max)
                        {
                            page++; //add a page
                            PreviousNext = currentNext; //set previousnext to current
                            i = 0; //set i to 0
                            embed.WithTitle($"Cat Image [{page}/{max}]"); //set title
                            embed.WithImageUrl(urls[page]); //grab the image by page index and set it
                            //update msg
                            msg.ModifyAsync(x => x.Embed = embed.Build()).GetAwaiter();
                        }
                    }
                    //if loop is over (timed out), then we notify the user by embed
                    embed.WithTitle("Cat Command [EXPIRED]");
                    embed.WithColor(Color.Red);
                    embed.WithDescription("This command has expired.");
                    msg.ModifyAsync(x => x.Embed = embed.Build()).GetAwaiter();
                    //if the bot has permission, then we try to remove all of the reactions on the embed
                    if (((SocketGuildUser)Context.Guild.GetCurrentUserAsync().GetAwaiter().GetResult()).GuildPermissions.ManageMessages)
                        msg.RemoveAllReactionsAsync().GetAwaiter();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            });
            */
            //embed.WithImageUrl(img);
            //await ReplyAsync("", false, embed.Build());
        }
    }
}