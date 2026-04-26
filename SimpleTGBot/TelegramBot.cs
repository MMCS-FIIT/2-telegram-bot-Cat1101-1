using Telegram.Bot.Types.ReplyMarkups;

namespace SimpleTGBot;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

enum InputStates {
    Idle,
    WaitForWord,
    WaitForIngredient
}

public class TelegramBot
{
    private const string BotToken = "8617299209:AAHSeEF3gqrRz-xDAGHbZ8KlepNex-2ollw";

    private Dictionary<long, InputStates> _userStates;
    
    /// <summary>
    /// Инициализирует и обеспечивает работу бота до нажатия клавиши Esc
    /// </summary>
    public async Task Run()
    {
        _userStates = new Dictionary<long, InputStates>();   
        
        // Инициализируем наш клиент, передавая ему токен.
        var botClient = new TelegramBotClient(BotToken);
        
        // Служебные вещи для организации правильной работы с потоками
        using CancellationTokenSource cts = new CancellationTokenSource();
        
        // Разрешённые события, которые будет получать и обрабатывать наш бот.
        // Будем получать только сообщения. При желании можно поработать с другими событиями.
        ReceiverOptions receiverOptions = new ReceiverOptions()
        {
            AllowedUpdates = new [] { UpdateType.Message, UpdateType.CallbackQuery }
        };

        // Привязываем все обработчики и начинаем принимать сообщения для бота
        botClient.StartReceiving(
            updateHandler: OnMessageReceived,
            pollingErrorHandler: OnErrorOccured,
            receiverOptions: receiverOptions,
            cancellationToken: cts.Token
        );

        // Проверяем что токен верный и получаем информацию о боте
        var me = await botClient.GetMeAsync(cancellationToken: cts.Token);
        Console.WriteLine($"Бот @{me.Username} запущен.\nДля остановки нажмите клавишу Esc...");
        
        // Ждём, пока будет нажата клавиша Esc, тогда завершаем работу бота
        while (Console.ReadKey().Key != ConsoleKey.Escape){}

        // Отправляем запрос для остановки работы клиента.
        cts.Cancel();
    }
    
    /// <summary>
    /// Обработчик события получения сообщения.
    /// </summary>
    /// <param name="botClient">Клиент, который получил сообщение</param>
    /// <param name="update">Событие, произошедшее в чате. Новое сообщение, голос в опросе, исключение из чата и т. д.</param>
    /// <param name="cancellationToken">Служебный токен для работы с многопоточностью</param>
    async Task OnMessageReceived(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        Console.WriteLine(update.Type);
        
        if (update.Type == UpdateType.CallbackQuery)
        {
            await HandleCallback(update.CallbackQuery, botClient, cancellationToken);
        }
        
        var message = update.Message;
        if (message is null)
        {
            return;
        }

        if (message.Text is not { } messageText)
        {
            return;
        }
        
        var chatId = message.Chat.Id;
        var userId = message.From?.Id ?? chatId;
        
        if (_userStates.ContainsKey(userId))
        {
            if (_userStates[userId] == InputStates.WaitForWord)
            {
                await SendRecipesByWords(message, botClient, cancellationToken);
            }

            if (_userStates[userId] == InputStates.WaitForIngredient)
            {
                await SendRecipesByIngredient(message, botClient, cancellationToken);
            }
        }


        if (messageText == "/start" || messageText == "/menu")
        {
            try
            {
                await SendMenu(chatId, botClient, cancellationToken);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }

    /// <summary>
    /// Обрабатывает коллбеки от клавиатуры
    /// </summary>
    /// <param name="callback"></param>
    /// <param name="botClient"></param>
    /// <param name="cancellationToken"></param>
    async Task HandleCallback(CallbackQuery? callback, ITelegramBotClient botClient,
        CancellationToken cancellationToken)
    {
        try
        {
            if (callback is null) return;
            if (callback.Message is null) return;

            if (callback.Data is not { } callbackData) return;

            await botClient.AnswerCallbackQueryAsync(callback.Id, cancellationToken: cancellationToken);
                
            switch (callbackData)
            {
                case "random_recipe":
                    await SendRandomRecipe(callback, botClient, cancellationToken);
                    return;
                case "word_recipes":
                    await SendRecipeWordRequest(callback, botClient, cancellationToken);
                    return;
                case "ingredient_recipes":
                    await SendRecipeIngredientRequest(callback, botClient, cancellationToken);
                    return;
                case "cat_recipes":
                    await SendCategories(callback, botClient, cancellationToken);
                    return;
            }
            
            string[] query = callbackData.Split("_");
            if (query[0] == "ingr" && int.TryParse(query[1], out var result))
            {
                await SendRecipesByIngredient(callback.Message, botClient, cancellationToken, result);
                return;
            }
                
            if (query[0] == "word" && int.TryParse(query[1], out result))
            {
                await SendRecipesByWords(callback.Message, botClient, cancellationToken, result);
                return;
            }
                
            if (query[0] == "cat" && int.TryParse(query[2], out result))
            {
                await SendRecipesByCategory(callback.Message, botClient, cancellationToken, result, query[1]);
                return;
            }

            if (int.TryParse(callbackData, out var id))
            {
                await SendRecipeById(callback, botClient, cancellationToken, id);
                return;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    /// <summary>
    /// Отправляет меню с действиями
    /// </summary>
    async Task SendMenu(long chatId, ITelegramBotClient botClient, CancellationToken cancellationToken)
    {
        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new [] {InlineKeyboardButton.WithCallbackData("Случайное блюдо", 
                "random_recipe")},
            new [] {InlineKeyboardButton.WithCallbackData("Блюдо по первому слову", 
                "word_recipes")},
            new [] {InlineKeyboardButton.WithCallbackData("Блюдо по вашему ингредиенту", 
                "ingredient_recipes")},
            new [] {InlineKeyboardButton.WithCallbackData("Блюда по категориям", 
                "cat_recipes")}
        });
            
        await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Здесь вы найдёте рецепты еды на любой вкус и с любыми ингридиентами"
                  + "\n\nВыберите пункт меню:",
            replyMarkup: keyboard,
            cancellationToken: cancellationToken);
    }
    
    /// <summary>
    /// Отправляет список рецептов с заданным словом
    /// В виде интерактивной InlineKeyboard
    /// </summary>
    /// <param name="offset">Сколько рецептов пропустить</param>
    async Task SendRecipesByWords(Message message, ITelegramBotClient botClient,
        CancellationToken cancellationToken, int offset=0)
    {
        if (message.Text is null || message.From is null) return;
        
        _userStates[message.From.Id] = InputStates.Idle;
        
        var buttons = new List<InlineKeyboardButton[]>();

        try
        {
            var word = await Translator.Translate(message.Text ?? "chicken");

            var meals = await MealDBApi.GetRecipesByFirstWord(word.Replace("\n", ""), offset);

            foreach (var meal in meals)
            {
                string name = await Translator.Translate(meal.strMeal, "en", "ru");
                int id = meal.idMeal;
            
                buttons.Add(new []{InlineKeyboardButton.WithCallbackData(name, id.ToString())});
            }
        
            buttons.Add(new []
            {
                InlineKeyboardButton.WithCallbackData("<--", "word_"+Math.Max(offset-5, 0)),
                InlineKeyboardButton.WithCallbackData("-->", "word_"+Math.Max(offset+5, 0))
            });

            if (message.ReplyMarkup is null)
            {
                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Рецепты:\t\t",
                    cancellationToken: cancellationToken);
        
                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: message.Text + "                      &#x200D;",
                    replyMarkup: new InlineKeyboardMarkup(buttons),
                    parseMode: ParseMode.Html,
                    cancellationToken: cancellationToken);
            }
            else
            {
                await botClient.EditMessageReplyMarkupAsync(message.Chat.Id,
                    message.MessageId,
                    replyMarkup: new InlineKeyboardMarkup(buttons),
                    cancellationToken: cancellationToken);
            }
        }
        catch (Exception e)
        {
            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "Рецепт не найден",
                cancellationToken: cancellationToken);
        }
    }
    
    /// <summary>
    /// Отправляет список рецептов с заданным ингредиентом
    /// В виде интерактивной InlineKeyboard
    /// </summary>
    /// <param name="offset">Сколько рецептов пропустить</param>
    async Task SendRecipesByIngredient(Message message, ITelegramBotClient botClient,
        CancellationToken cancellationToken, int offset=0)
    {
        if (message.Text is null || message.From is null) return;
        
        _userStates[message.From.Id] = InputStates.Idle;
        
        var buttons = new List<InlineKeyboardButton[]>();

        try
        {
            var word = await Translator.Translate(message.Text ?? "chicken");
            word = word.Replace("\n", "").Replace(" ", "_").Trim().ToLower();
            if (word == "potato") word = "potatoes";

            var meals = await MealDBApi.GetRecipesByIngredient(word, offset);

            foreach (var meal in meals)
            {
                string name = await Translator.Translate(meal.strMeal, "en", "ru");
                int id = meal.idMeal;
            
                buttons.Add(new []{InlineKeyboardButton.WithCallbackData(name, id.ToString())});
            }
            
            buttons.Add(new []
            {
                InlineKeyboardButton.WithCallbackData("<--", "ingr_"+Math.Max(offset-5, 0)),
                InlineKeyboardButton.WithCallbackData("-->", "ingr_"+Math.Max(offset+5, 0))
            });

            if (message.ReplyMarkup is null)
            {
                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Рецепты:",
                    cancellationToken: cancellationToken);
        
                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: message.Text + "                      &#x200D;",
                    replyMarkup: new InlineKeyboardMarkup(buttons),
                    parseMode: ParseMode.Html,
                    cancellationToken: cancellationToken);
            }
            else
            {
                await botClient.EditMessageReplyMarkupAsync(message.Chat.Id,
                    message.MessageId,
                    replyMarkup: new InlineKeyboardMarkup(buttons),
                    cancellationToken: cancellationToken);
            }
        }
        catch (Exception e)
        {
            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "Ингредиент не найден",
                cancellationToken: cancellationToken);
        }
    }
    
    /// <summary>
    /// Отправляет список рецептов с заданной категорией
    /// В виде интерактивной InlineKeyboard
    /// </summary>
    /// <param name="offset">Сколько рецептов пропустить</param>
    /// <param name="cat">Категория из meal_db</param>
    async Task SendRecipesByCategory(Message message, ITelegramBotClient botClient,
        CancellationToken cancellationToken, int offset=0, string cat="")
    {
        if (message.Text is null || message.From is null) return;
        
        _userStates[message.From.Id] = InputStates.Idle;
        
        var buttons = new List<InlineKeyboardButton[]>();

        try
        {
            var meals = await MealDBApi.GetRecipesByCategory(cat.Replace("\n", ""), offset);

            foreach (var meal in meals)
            {
                string name = await Translator.Translate(meal.strMeal, "en", "ru");
                int id = meal.idMeal;
            
                buttons.Add(new []{InlineKeyboardButton.WithCallbackData(name, id.ToString())});
            }
        
            buttons.Add(new []
            {
                InlineKeyboardButton.WithCallbackData("<--", $"cat_{cat}_{Math.Max(offset-5, 0)}"),
                InlineKeyboardButton.WithCallbackData("-->", $"cat_{cat}_{Math.Max(offset+5, 0)}")
            });

            if (message.ReplyMarkup is null)
            {
                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Рецепты:",
                    replyMarkup: new InlineKeyboardMarkup(buttons),
                    cancellationToken: cancellationToken);
            }
            else
            {
                await botClient.EditMessageReplyMarkupAsync(message.Chat.Id,
                    message.MessageId,
                    replyMarkup: new InlineKeyboardMarkup(buttons),
                    cancellationToken: cancellationToken);
            }
        }
        catch (Exception e)
        {
            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "Рецепт не найден",
                cancellationToken: cancellationToken);
        }
    }
    
    /// <summary>
    /// Отправляет запрос пользователю на ввод слова из названия блюда
    /// Сохраняет этот запрос в состояние
    /// </summary>
    async Task SendRecipeWordRequest(CallbackQuery callbackQuery, ITelegramBotClient botClient, CancellationToken cancellationToken)
    {
        var userId = callbackQuery.From.Id;
        var chatId = callbackQuery.Message.Chat.Id;

        _userStates[userId] = InputStates.WaitForWord;
        
        await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Отправьте первое слово в названии блюда",
            cancellationToken: cancellationToken);
    }
    
    /// <summary>
    /// Отправляет запрос пользователю на ввод ингредиента
    /// Сохраняет этот запрос в состояние
    /// </summary>
    async Task SendRecipeIngredientRequest(CallbackQuery callbackQuery, ITelegramBotClient botClient, CancellationToken cancellationToken)
    {
        var userId = callbackQuery.From.Id;
        var chatId = callbackQuery.Message.Chat.Id;

        _userStates[userId] = InputStates.WaitForIngredient;
        
        await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Отправьте главный ингредиент",
            cancellationToken: cancellationToken);
    }

    
    /// <summary>
    /// Отправляет рецепт с нужным id
    /// </summary>
    async Task SendRecipeById(CallbackQuery callbackQuery, ITelegramBotClient botClient,
        CancellationToken cancellationToken, int id)
    {
        Meal meal = await MealDBApi.GetRecipeByID(id);
        await SendRecipe(meal, callbackQuery, botClient, cancellationToken);
    }
    
    /// <summary>
    /// Отправляет случайный рецепт
    /// </summary>
    async Task SendRandomRecipe(CallbackQuery callbackQuery, ITelegramBotClient botClient, CancellationToken cancellationToken)
    {
        Meal meal = await MealDBApi.GetRandomRecipe();
        await SendRecipe(meal, callbackQuery, botClient, cancellationToken);
    }

    /// <summary>
    /// Отправляет инструкцию к рецепту и ингредиенты
    /// </summary>
    async Task SendRecipe(Meal meal, CallbackQuery callbackQuery, ITelegramBotClient botClient,
        CancellationToken cancellationToken)
    {
        string name = await Translator.Translate(meal.strMeal, "en", "ru");
        string instruction = await Translator.Translate(meal.strInstructions, "en", "ru");;

        await botClient.SendTextMessageAsync(
            chatId: callbackQuery.Message.Chat.Id,
            text: $"<b>{name}</b>\n\n" + instruction,
            parseMode: ParseMode.Html,
            cancellationToken: cancellationToken);

        string ingredients = "";
        for (var i = 0; i < 10; i++)
        {
            var ingredient = meal.GetIngredients().ToList()[i] + " " + meal.GetMeasures().ToList()[i];
            
            if (string.IsNullOrWhiteSpace(ingredient)) break;
            var ingr = await Translator.Translate(ingredient, "en", "ru");

            ingredients += ingr + "\n";
        }
        
        await botClient.SendTextMessageAsync(
            chatId: callbackQuery.Message.Chat.Id,
            text: $"<b>Ингредиенты</b>\n\n" + ingredients,
            parseMode: ParseMode.Html,
            cancellationToken: cancellationToken);
    }
    
    /// <summary>
    /// Отправляет категории рецептов
    /// </summary>
    async Task SendCategories(CallbackQuery callbackQuery, ITelegramBotClient botClient, 
        CancellationToken cancellationToken)
    {
        var buttons = new List<InlineKeyboardButton[]>();

        try
        {
            var meals = await MealDBApi.GetCategories();

            for (int i = 0; i < meals.Length; i++)
            {
                string name = await Translator.Translate(meals[i].strCategory, "en", "ru");
                if (name.Replace("\n", "").Trim().ToLower() == "сторона") name = "Закуски";
                
                buttons.Add(new []{InlineKeyboardButton.WithCallbackData(name, 
                    $"cat_{meals[i].strCategory}_{0}")});
            }
        
            await botClient.SendTextMessageAsync(
                chatId: callbackQuery.Message.Chat.Id,
                text: "Категории:          &#x200D;",
                parseMode: ParseMode.Html,
                replyMarkup: new InlineKeyboardMarkup(buttons),
                cancellationToken: cancellationToken);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }

    /// <summary>
    /// Обработчик исключений, возникших при работе бота
    /// </summary>
    /// <param name="botClient">Клиент, для которого возникло исключение</param>
    /// <param name="exception">Возникшее исключение</param>
    /// <param name="cancellationToken">Служебный токен для работы с многопоточностью</param>
    /// <returns></returns>
    Task OnErrorOccured(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        // В зависимости от типа исключения печатаем различные сообщения об ошибке
        var errorMessage = exception switch
        {
            ApiRequestException apiRequestException
                => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            
            _ => exception.ToString()
        };

        Console.WriteLine(errorMessage);
        
        // Завершаем работу
        return Task.CompletedTask;
    }
}