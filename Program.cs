public static class Program {
    private static Discord.WebSocket.DiscordSocketClient _client = new();

    public static async Task Main() {
        _client.Log += Log;

        var token = "";

        await _client.LoginAsync(Discord.TokenType.Bot, token);
        await _client.StartAsync();

        _client.Ready += ClientReady;
        _client.SlashCommandExecuted += SlashCommandHandler;

        await Task.Delay(-1);
    }

    public static async Task ClientReady() {
        var register = new Discord.SlashCommandBuilder()
            .WithName("register")
            .WithDescription("Register Discord Account To Bank Account")
            .AddOption("password", Discord.ApplicationCommandOptionType.String, "Password", isRequired: true);

        var balance = new Discord.SlashCommandBuilder()
            .WithName("balance")
            .WithDescription("Get Account Balance")
            .AddOption("id", Discord.ApplicationCommandOptionType.String, "Balance of account", isRequired: false);

        var send = new Discord.SlashCommandBuilder()
            .WithName("transfer")
            .WithDescription("Send Money To Someone")
            .AddOption("user", Discord.ApplicationCommandOptionType.User, "User to send to", isRequired: true)
            .AddOption("amount", Discord.ApplicationCommandOptionType.Number, "Amount to send", isRequired: true);

        var shop = new Discord.SlashCommandBuilder()
            .WithName("shop")
            .WithDescription("Open the shop");


        var sell_item_builder = new Discord.SlashCommandOptionBuilder()
                    .WithName("item")
                    .WithDescription("Item to sell")
                    .WithType(Discord.ApplicationCommandOptionType.String);

        Enum.GetValues<Item>()
            .ToList()
            .ForEach(x => sell_item_builder = sell_item_builder.AddChoice(Enum.GetName(typeof(Item), x), (int)x));

        var sell = new Discord.SlashCommandBuilder()
            .WithName("sell")
            .WithDescription("Put an item up for auction")
            .AddOption(sell_item_builder)
            .AddOption("amount", Discord.ApplicationCommandOptionType.Number, "Amount to sell", isRequired: true)
            .AddOption("price", Discord.ApplicationCommandOptionType.Number, "How much each item should sell for", isRequired: true);

        try {
            await _client.CreateGlobalApplicationCommandAsync(register.Build());
            await _client.CreateGlobalApplicationCommandAsync(balance.Build());
            await _client.CreateGlobalApplicationCommandAsync(send.Build());
            await _client.CreateGlobalApplicationCommandAsync(sell.Build());
        } catch(ApplicationException ex) {
            _ = ex;
            Console.WriteLine("error");
        }
    }

    private static async Task SlashCommandHandler(Discord.WebSocket.SocketSlashCommand cmd) {
        switch (cmd.Data.Name) {
            case "register":
                await RegisterHandler(cmd);
                break;
            case "balance":
                await BalanceHandler(cmd);
                break;
            case "transfer":
                await SendHandler(cmd);
                break;
            case "sell":
                await SellHandler(cmd);
                break;
        };
    }

    public static User Get(string id) {
        var bitches = DatabaseLayer.Query<User>();
        var b = bitches.FirstOrDefault(x => x.Id == id);
        if(b is null) { return new User{ Id = "" }; }

        return b;
    }

    public static bool CorrectPassword(string id, string inp) {
        var b = Get(id);
        var base64 = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(inp));
        return base64 == b.Password;
    }


    private static async Task RegisterHandler(Discord.WebSocket.SocketSlashCommand cmd) {
        var pw = (string)cmd.Data.Options.First(x => x.Name == "password").Value;

        var b = DatabaseLayer.Query<User>().FirstOrDefault(x => CorrectPassword(x.Id, pw));

        if(b is null) {
            var error_embed = new Discord.EmbedBuilder()
                .WithTitle($"Password Is Incorrect")
                .WithColor(Discord.Color.Red)
                .WithCurrentTimestamp();

            await cmd.RespondAsync(embed: error_embed.Build(), ephemeral: true);
            return;
        }

        var user_id = cmd.User.Id;

        var db = new DiscordUser() {
            DiscordId = user_id.ToString(),
            BitchId = b.Id
        };

        if(DatabaseLayer.Query<DiscordUser>().FirstOrDefault(x => x.BitchId == b.Id) != null) {
            var error_embed = new Discord.EmbedBuilder()
                .WithTitle($"Account Already Registered")
                .WithColor(Discord.Color.Red)
                .WithCurrentTimestamp();

            await cmd.RespondAsync(embed: error_embed.Build(), ephemeral: true);
            return;
        }

        DatabaseLayer.Create<DiscordUser>(db);

        var embed = new Discord.EmbedBuilder()
            .WithTitle($"Registered Account")
            .WithColor(Discord.Color.Blue)
            .WithCurrentTimestamp();

        await cmd.RespondAsync(embed: embed.Build(), ephemeral: true);
    }

    private static async Task BalanceHandler(Discord.WebSocket.SocketSlashCommand cmd) {
        var option = cmd.Data.Options.FirstOrDefault();

        string id = "";

        if(option is not null) {
            id = (string)option.Value;
        } else {
            var db = DatabaseLayer.Query<DiscordUser>().FirstOrDefault(x => x.DiscordId == cmd.User.Id.ToString());
            if(db is null) {
                var error_embed = new Discord.EmbedBuilder()
                    .WithTitle($"Please Specify an id")
                    .WithColor(Discord.Color.Green)
                    .WithCurrentTimestamp();

                await cmd.RespondAsync(embed: error_embed.Build());
                return;
            }

            id = db.BitchId;
        }

        var username = cmd.User.GlobalName;

        var bs = DatabaseLayer.Query<User>();
        var b = bs.First(x => x.Id == id);

        Console.WriteLine($"{b.Id}");

        var embed = new Discord.EmbedBuilder()
            .WithTitle($"{username}'s Balance")
            .WithDescription($"£{b.Money}")
            .WithColor(Discord.Color.Green)
            .WithCurrentTimestamp();

        await cmd.RespondAsync(embed: embed.Build(), ephemeral: true);
    }

    private static async Task SendHandler(Discord.WebSocket.SocketSlashCommand cmd) {
        var user = (Discord.WebSocket.SocketGuildUser)cmd.Data.Options.First(x => x.Name == "user").Value;
        var amount = (double)cmd.Data.Options.First(x => x.Name == "amount").Value;

        var u1 = DatabaseLayer.Query<DiscordUser>().FirstOrDefault(x => x.DiscordId == cmd.User.Id.ToString());

        if(u1 is null) {
            var error_embed = new Discord.EmbedBuilder()
                .WithTitle($"Please register to send money")
                .WithColor(Discord.Color.Red)
                .WithCurrentTimestamp();

            await cmd.RespondAsync(embed: error_embed.Build(), ephemeral: true);
            return;
        }

        var u1b = DatabaseLayer.Query<User>().FirstOrDefault(x => x.Id == u1.BitchId);

        var u2 = DatabaseLayer.Query<DiscordUser>().FirstOrDefault(x => x.DiscordId == user.Id.ToString());

        if(u2 is null) {
            var error_embed = new Discord.EmbedBuilder()
                .WithTitle($"User {user.GlobalName} needs to register")
                .WithColor(Discord.Color.Red)
                .WithCurrentTimestamp();

            await cmd.RespondAsync(embed: error_embed.Build(), ephemeral: true);
            return;
        }

        var u2b = DatabaseLayer.Query<User>().FirstOrDefault(x => x.Id == u2.BitchId);

        if(u1b!.Money < amount) {
            var error_embed = new Discord.EmbedBuilder()
                .WithTitle($"You do not have enough money in your account")
                .WithDescription($"Balance: {u1b.Money}, U a brokie")
                .WithColor(Discord.Color.Red)
                .WithCurrentTimestamp();

            await cmd.RespondAsync(embed: error_embed.Build(), ephemeral: true);
            return;
        }

        u1b!.Money -= amount;
        DatabaseLayer.Update(u1b);

        u2b!.Money += amount;
        DatabaseLayer.Update(u2b);

        var embed = new Discord.EmbedBuilder()
            .WithTitle($"Send {amount} to account {u2.BitchId}")
            .WithDescription($"New balance: {u1b.Money}")
            .WithColor(Discord.Color.Blue)
            .WithCurrentTimestamp();

        await cmd.RespondAsync(embed: embed.Build(), ephemeral: true);
    }

    public static async Task SellHandler(Discord.WebSocket.SocketSlashCommand cmd) {
        var item = cmd.Data.Options.FirstOrDefault(x => x.Name == "item");
        var embed = new Discord.EmbedBuilder()
            .WithTitle($"Selling Item: {item!.Value}")
            .WithDescription($"Now go to the in-game bank and deposit your items")
            .WithColor(Discord.Color.Blue)
            .WithCurrentTimestamp();

        await cmd.RespondAsync(embed: embed.Build(), ephemeral: true);
    }

    public static async Task ItemsHandler(Discord.WebSocket.SocketSlashCommand cmd) {


        var embed = new Discord.EmbedBuilder()
            .WithTitle($"Tweaky Shop")
            .WithDescription($"")
            .WithColor(Discord.Color.Blue)
            .WithCurrentTimestamp();

        await cmd.RespondAsync(embed: embed.Build(), ephemeral: true);

    }

    private static Task Log(Discord.LogMessage msg) {
        Console.WriteLine(msg.ToString());
        return Task.CompletedTask;
    }
}
