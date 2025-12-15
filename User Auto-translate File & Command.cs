using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

public class CPHInline
{
    private static readonly string dictionaryFilePath = @".\rez1\excludeDictionary.txt";
    private static HashSet<string> excludeDictionary = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public bool Execute()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(dictionaryFilePath));

            if (File.Exists(dictionaryFilePath))
            {
                foreach (var line in File.ReadAllLines(dictionaryFilePath))
                {
                    excludeDictionary.Add(line);
                }
            }
            else
            {
                File.WriteAllText(dictionaryFilePath, string.Empty);
            }
        }
        catch (Exception ex)
        {
            CPH.LogError($"Initialization error: {ex.Message}");
            return false;
        }

        CPH.TryGetArg("rawInput", out string rawInput);
        CPH.TryGetArg("command", out string command);

        if (args["broadcastUser"].ToString() == "True" || args["isModerator"].ToString() == "True")
        {
            if ((command == "!add" || command == "!remove") && string.IsNullOrEmpty(rawInput))
            {
                CPH.SendMessage("The input cannot be empty.");
                return false;
            }

            switch (command)
            {
                case "!add":
                    AddToExcludeDictionary(rawInput);
                    break;
                case "!remove":
                    RemoveFromExcludeDictionary(rawInput);
                    break;
                case "!list":
                    ShowExcludeDictionary();
                    break;
                case "!clear":
                    ClearExcludeDictionary();
                    break;
            }
        }

        return true;
    }

    private void SaveExcludeDictionary()
    {
        try
        {
            File.WriteAllLines(dictionaryFilePath, excludeDictionary);
        }
        catch (Exception ex)
        {
            CPH.LogError($"Error saving exclude dictionary: {ex.Message}");
        }
    }

    private void AddToExcludeDictionary(string rawInput)
    {
        List<string> addedWords = new List<string>();
        var words = rawInput.Split(',').Select(word => word.Trim()).Where(word => !string.IsNullOrEmpty(word));

        foreach (var word in words)
        {
            if (excludeDictionary.Add(word))
            {
                addedWords.Add(word);
            }
        }

        if (addedWords.Count > 0)
        {
            SaveExcludeDictionary();
            CPH.SendMessage($"Added \"{string.Join("\", \"", addedWords)}\" to the exclude dictionary.");
    }
    else
    {
        CPH.SendMessage("No new words were added to the exclude dictionary.");
    }
}

private void RemoveFromExcludeDictionary(string rawInput)
{
    List<string> removedWords = new List<string>();
    var words = rawInput.Split(',').Select(word => word.Trim()).Where(word => !string.IsNullOrEmpty(word));

    foreach (var word in words)
    {
        if (excludeDictionary.Remove(word))
        {
            removedWords.Add(word);
        }
    }

    if (removedWords.Count > 0)
    {
        SaveExcludeDictionary();
        CPH.SendMessage($"Removed \"{string.Join("\", \"", removedWords)}\" from the exclude dictionary.");
}
else
{
    CPH.SendMessage("No words were removed from the exclude dictionary.");
}
}

private void ClearExcludeDictionary()
{
    excludeDictionary.Clear();
    SaveExcludeDictionary();
    CPH.SendMessage("The exclude dictionary has been cleared.");
}

private void ShowExcludeDictionary()
{
    if (excludeDictionary.Count > 0)
    {
        string excludeText = string.Join(", ", excludeDictionary);
        string prefix = "Excluded words/phrases:";
        SplitAndSendMessage(prefix, excludeText);
    }
    else
    {
        CPH.SendMessage("The exclude dictionary is empty.");
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