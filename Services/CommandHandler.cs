using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;

namespace raidbot.Services
{
    public class CommandHandler
    {
        // setup fields to be set later in the constructor
        private readonly IConfiguration _config;
        private readonly CommandService _commands;
        private readonly DiscordSocketClient _client;
        private readonly IServiceProvider _services;

        public CommandHandler(IServiceProvider services)
        {
            // juice up the fields with these services
            // since we passed the services in, we can use GetRequiredService to pass them into the fields set earlier
            _config = services.GetRequiredService<IConfiguration>();
            _commands = services.GetRequiredService<CommandService>();
            _client = services.GetRequiredService<DiscordSocketClient>();
            _services = services;
            
            // take action when we execute a command
            _commands.CommandExecuted += CommandExecutedAsync;

            // take action when we receive a message (so we can process it, and see if it is a valid command)
            _client.MessageReceived += MessageReceivedAsync;
        }

        public async Task InitializeAsync()
        {
            // register modules that are public and inherit ModuleBase<T>.
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
        }

        // this class is where the magic starts, and takes actions upon receiving messages
        public async Task MessageReceivedAsync(SocketMessage rawMessage)
        {
            // ensures we don't process system/other bot messages
            if (!(rawMessage is SocketUserMessage message)) 
            {
                return;
            }
            
            if (message.Source != MessageSource.User) 
            {
                return;
            }

            // sets the argument position away from the prefix we set
            var argPos = 0;

            // get prefix from the configuration file
            char prefix = Char.Parse(_config["Prefix"]);
            ulong botChannel = Convert.ToUInt64(_config["BotchannelID"]);

            if (message.Channel.Id != botChannel) //doesnt sass the admin channel
            {
                //add some sass to the bot
                if (message.ToString().ToLower().Contains("bot sucks") && message.Author.Username.ToLower() == "vishy")
                {
                    await message.Channel.SendMessageAsync("Get a life vishy.");
                }
                else if (message.ToString().ToLower().Contains("sucks")|| message.ToString().ToLower().Contains("suck"))
                {
                    await message.Channel.SendMessageAsync("I mean, you can think that.");
                }
                else if (message.ToString().ToLower().Contains("no u ") || message.ToString().ToLower() == "no u")
                {
                    await message.Channel.SendMessageAsync("no u");
                }
                else if (message.ToString().ToLower().Contains("fuck you") || message.ToString().ToLower().Contains("fuck u"))
                {
                    await message.Channel.SendMessageAsync("u wot m8");
                }
                else if (message.ToString().ToLower().Contains("bad bot"))
                {
                    await message.Channel.SendMessageAsync("Sorry.");
                }
                else if (message.ToString().ToLower().Contains("real bot"))
                {
                    await message.Channel.SendMessageAsync("*I'm a real boy... I mean bot.*");
                }
                else if (message.ToString().ToLower().Contains("fight me") || message.ToString().ToLower().Contains("fite me"))
                {
                    await message.Channel.SendMessageAsync("Cash me outside, how bou dah");
                }
                else if (message.ToString().ToLower().StartsWith("im ") || message.ToString().ToLower().StartsWith("i'm "))
                {
                    char[] charArray = message.ToString().ToCharArray();
                    if (charArray.Length < 35)
                    {
                        string dadJoke = "";
                        if (message.ToString().ToLower().StartsWith("im "))
                            dadJoke = message.ToString().ToLower().Replace("im", "");
                        if (message.ToString().ToLower().StartsWith("i'm "))
                            dadJoke = message.ToString().ToLower().Replace("i'm", "");
                        string msg = $"Hi{dadJoke}, I'm Dad.";
                        await message.Channel.SendMessageAsync(msg);                
                    }
                }
            }

            // determine if the message has a valid prefix, and adjust argPos based on prefix
            if (!(message.HasMentionPrefix(_client.CurrentUser, ref argPos) || message.HasCharPrefix(prefix, ref argPos))) 
            {
                return;
            }
           
            var context = new SocketCommandContext(_client, message);

            // execute command if one is found that matches
            await _commands.ExecuteAsync(context, argPos, _services); 
        }

        public async Task CommandExecutedAsync(Optional<CommandInfo> command, ICommandContext context, IResult result)
        {
            // if a command isn't found, log that info to console and exit this method
            if (!command.IsSpecified)
            {
                System.Console.WriteLine($"{DateTime.Now,0:t} Command failed to execute for [{context.User.Username}] <-> [{result.ErrorReason}]!");
                return;
            }
                

            // log success to the console and exit this method
            if (result.IsSuccess)
            {
                System.Console.WriteLine($"{DateTime.Now,0:t} Command [{command.Value.Name}] executed for -> [{context.User.Username}]");
                return;
            }
            
            // failure scenario, let's let the user know
            await context.Channel.SendMessageAsync($"Sorry, {context.User.Username}... something went wrong -> [{result}]!");
        }        
    }
}