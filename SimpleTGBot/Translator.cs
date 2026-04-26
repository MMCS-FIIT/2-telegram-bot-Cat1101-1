using System.Text.Json;

namespace SimpleTGBot;

public class Translator
{
    /// <summary>
    /// Переводить текст через Google Translate
    /// </summary>
    /// <param name="msg">Сообщение</param>
    /// <param name="from">Язык оригинала</param>
    /// <param name="to">Язык перевода</param>
    public static async Task<string> Translate(string msg, string from="ru", string to="en")
    {
        using var client = new HttpClient();
        var result = "";
        
        foreach (var ss in msg.Split(new []{'\n', '.', '\r'},
                     StringSplitOptions.RemoveEmptyEntries))
        {
            var url = $"https://translate.googleapis.com/translate_a/single" +
                      $"?client=gtx&sl={from}&tl={to}&dt=t&q={Uri.EscapeDataString(ss)}";
            var response = await client.GetStringAsync(url);
            var json = JsonDocument.Parse(response);
            result += "\n" + json.RootElement[0][0][0].GetString();
        }

        return result;
    }
}