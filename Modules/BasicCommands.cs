using Discord;
using Discord.Net;
using Discord.WebSocket;
using Discord.Commands;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace raidbot.Modules
{
    // for commands to be available, and have the Context passed to them, we must inherit ModuleBase
    public class BasicCommands : ModuleBase
    {
        private readonly IConfiguration _config;
        private CommandService _service;

        public BasicCommands(CommandService service)
        {
            _service = service;
        }

        [Command("help")]
        [Summary("Shows what a specific command does and what parameters it takes.")]
        public async Task HelpAsync([Remainder, Summary("Command to retrieve help for")] string command = null)
        {
            char prefix = '!';//Char.Parse(_config["Prefix"]);
            
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
    }
}