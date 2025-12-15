using System;
using System.Collections.Generic;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using System.Web;
using System.IO;
using System.Text;

public class CPHInline
{
    private static readonly HttpClient client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
    private static readonly string dictionaryFilePath = @".\rez1\excludeDictionary.txt";
    private static HashSet<string> excludeDictionary = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private static DateTime? lastModifiedTime = null;

    public bool Execute()
    {
        if (HasDictionaryFileChanged())
        {
            LoadExcludeDictionary();
        }

        CPH.TryGetArg("user", out string user);
        CPH.TryGetArg("rawInput", out string text);
        CPH.TryGetArg("fromLanguage", out string fromLanguage);
        CPH.TryGetArg("toLanguage", out string toLanguage);
        CPH.TryGetArg("messageStripped", out string messageStripped);
        CPH.TryGetArg("emoteCount", out int emoteCount);

        // Prefix for translated messages
        string messagePrefix = $"{user}: ({toLanguage}) ";

        if (string.Equals(user, CPH.TwitchGetBot().UserName, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (emoteCount > 0)
        {
            text = messageStripped.Trim();
        }

        if (text.Contains("@") && text.Trim().IndexOf(' ') == -1)
        {
            return false;
        }

        if (string.IsNullOrEmpty(text) || text.StartsWith("!") || text.StartsWith(messagePrefix) || ContainsExcludedWord(text))
        {
            return false;
        }

        string translatedText = Translate(text, fromLanguage, toLanguage);

        if (translatedText == null || string.Equals(text, translatedText, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        SplitAndSendMessage(messagePrefix, translatedText);
        return true;
    }

    private bool HasDictionaryFileChanged()
    {
        try
        {
            DateTime fileLastModified = File.GetLastWriteTime(dictionaryFilePath);
            if (lastModifiedTime == null || fileLastModified != lastModifiedTime)
            {
                lastModifiedTime = fileLastModified;
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            CPH.LogError($"Error checking file modification time: {ex.Message}");
            return false;
        }
    }

    private void LoadExcludeDictionary()
    {
        try
        {
            excludeDictionary.Clear();
            excludeDictionary.UnionWith(File.ReadLines(dictionaryFilePath));
        }
        catch (Exception ex)
        {
            CPH.LogError($"Error loading exclude dictionary: {ex.Message}");
        }
    }

    private bool ContainsExcludedWord(string text)
    {
        if (text.StartsWith("@"))
        {
            int spaceIndex = text.IndexOf(' ');
            if (spaceIndex > -1)
            {
                text = text.Substring(spaceIndex + 1).Trim();
            }
        }

        return excludeDictionary.Contains(text);
    }

    public string Translate(string text, string fromLanguage, string toLanguage)
    {
        try
        {
            string url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl={fromLanguage}&tl={toLanguage}&dt=t&q={HttpUtility.UrlEncode(text)}";
            string result = client.GetStringAsync(url).Result;
            JArray jsonArray = JArray.Parse(result);

            if (jsonArray[0] == null)
            {
                return "Failed to translate.";
            }

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
        StringBuilder output = new StringBuilder(prefix);

        foreach (string word in words)
        {
            if ((output.Length + word.Length + 1) > maxChars)
            {
                CPH.SendMessage(output.ToString());
                output.Clear();
                output.Append(word);
                CPH.Wait(100);
            }
            else
            {
                output.Append(" " + word);
            }
        }

        if (output.Length > 0)
        {
            CPH.SendMessage(output.ToString());
        }
    }
}