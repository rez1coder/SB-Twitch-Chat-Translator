using System;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Web;

public class CPHInline
{
    private static readonly HttpClient client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

    public bool Execute()
    {
        CPH.TryGetArg("user", out string user);
        CPH.TryGetArg("rawInput", out string text);
        CPH.TryGetArg("fromLanguage", out string fromLanguage);
        CPH.TryGetArg("toLanguage", out string toLanguage);

        if (CPH.TryGetArg("isReply", out bool isReply) && isReply)
        {
            CPH.TryGetArg("reply.userName", out user);
            CPH.TryGetArg("reply.msgBody", out text);
        }

        if (string.IsNullOrEmpty(text))
        {
            CPH.SendMessage($"@{user}, type \"!en [Text]\" or reply to a message with \"!en\" to translate.");
            return false;
        }

        string translation = Translate(text, fromLanguage, toLanguage);
        SplitAndSendMessage($"{user}: ({toLanguage})", translation);
        return true;
    }

    private string Translate(string text, string fromLanguage, string toLanguage)
    {
        try
        {
            string url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl={fromLanguage}&tl={toLanguage}&dt=t&q={HttpUtility.UrlEncode(text, Encoding.UTF8)}";
            string result = client.GetStringAsync(url).Result;
            JArray jsonArray = JArray.Parse(result);
            var translatedText = new StringBuilder();
            foreach (var sentenceArray in jsonArray[0])
            {
                translatedText.Append(sentenceArray[0]?.ToString());
            }

            return translatedText.ToString();
        }
        catch (Exception ex)
        {
            CPH.LogError($"Error during translation: {ex.Message}");
            return "Failed to translate.";
        }
    }

    private void SplitAndSendMessage(string prefix, string message)
    {
        int maxChars = 500;
        string[] words = message.Split(' ');
        string output = prefix;

        foreach (string word in words)
        {
            if ((output + " " + word).Length > maxChars)
            {
                CPH.SendMessage(output.Trim());
                output = word;
                CPH.Wait(100);
            }
            else
            {
                output += (string.IsNullOrEmpty(output) ? "" : " ") + word;
            }
        }

        if (!string.IsNullOrEmpty(output))
        {
            CPH.SendMessage(output.Trim());
        }
    }
}