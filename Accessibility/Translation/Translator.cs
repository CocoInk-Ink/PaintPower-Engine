using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using PaintPower.Logging;

namespace PaintPower.Accessibility.Translation;

public static class Translator
{
    // <Language, short_name> Example: <"English", "en">
    private static Dictionary<string, string> langList = new();
    // <Word_to_translate, translated_word> Example: <"Hello!", "Hola!">
    private static Dictionary<string, string> langDict = new();
    public static string lang = "en";

    public static bool refreshNeeded = false;

    public static event Action? LanguageChanged;

    public static void load(string preferredLanguage)
    {
        if (!string.IsNullOrWhiteSpace(preferredLanguage))
            lang = preferredLanguage;

        langList.Clear();
        langDict.Clear();

        try
        {
            Log.QuickLog("Getting Lang-list");
            LoadLangList("Assets/lang/lang-list.txt");
            Log.QuickLog("Got lang-list");
        }
        catch
        {
            Log.QuickLog("Failed to get lang-list.");

            // Clear list and add english as fallback.
            langList.Clear();
            langDict.Clear();
            langList.Add("English", "en");
        }

        try
        {
            Log.QuickLog("Getting lang file.");
            LoadGettextFile($"Assets/lang/{lang}.po");
            Log.QuickLog("Got lang file.");
        }
        catch
        {
            // fallback: no translations
            langDict.Clear();
            Log.QuickLog("No translation.");
        }

        refreshNeeded = true;

        refresh();
    }

    public static void refresh()
    {
        LanguageChanged?.Invoke();
        Log.QuickLog($"Refreshing layout... Language: {lang}");
        // ListLanguage();
    }

    public static Dictionary<string, string> GetAvailableLanguages()
    {
        return new Dictionary<string, string>(langList);
    }

    public static void changeLang(string newLang)
    {
        lang = newLang;
        LanguageChanged?.Invoke();
    }

    // Print the language's dictionary
    public static void ListLanguage()
    {
        foreach (var language in langDict)
        {
            Log.QuickLog($"English word: {language.Key}\t\t|\t\t Translated Word: {language.Value}");
        }
    }

    public static void AddTranslation(string word, string translation)
    {
        langList[word] = translation;
    }

    public static string Translate(string s)
    {
        return Map(s);
    }

    public static string Map(string s)
    {
        return langDict.TryGetValue(s, out var t) ? t : s;
    }

    public static void SetLangList(Dictionary<string, string> list)
    {
        langDict = list;
    }

    // ---------------------------------------------------------
    // GETTEXT PARSER (.po format)
    // ---------------------------------------------------------
    public static void LoadGettextFile(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("Language file not found", path);

        var lines = File.ReadAllLines(path);
        ParseGettext(lines);
    }

    public static void LoadGettextString(string content)
    {
        var lines = content.Split('\n');
        ParseGettext(lines);
    }

    private static void ParseGettext(string[] lines)
    {
        langDict.Clear();

        string? currentId = null;
        string? currentStr = null;
        bool readingId = false;
        bool readingStr = false;

        void Commit()
        {
            if (!string.IsNullOrEmpty(currentId) && currentStr != null)
            {
                langDict[currentId] = currentStr;
            }

            currentId = null;
            currentStr = null;
            readingId = false;
            readingStr = false;
        }

        foreach (var raw in lines)
        {
            string line = raw.Trim();

            // Blank line → commit previous entry
            if (string.IsNullOrWhiteSpace(line))
            {
                Commit();
                continue;
            }

            if (line.StartsWith("#"))
                continue;

            if (line.StartsWith("msgid"))
            {
                Commit(); // commit previous before starting new one
                currentId = ExtractString(line);
                readingId = true;
                readingStr = false;
                continue;
            }

            if (line.StartsWith("msgstr"))
            {
                currentStr = ExtractString(line);
                readingId = false;
                readingStr = true;
                continue;
            }

            if (line.StartsWith("\""))
            {
                if (readingId && currentId != null)
                    currentId += ExtractString(line);
                else if (readingStr && currentStr != null)
                    currentStr += ExtractString(line);

                continue;
            }
        }

        // Commit last entry
        Commit();
    }

    private static string ExtractString(string line)
    {
        int first = line.IndexOf('"');
        int last = line.LastIndexOf('"');

        if (first >= 0 && last > first)
            return line.Substring(first + 1, last - first - 1);

        return "";
    }

    // Load list of languages
    public static void LoadLangList(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("Language file not found", path);

        langList.Clear();
        langDict.Clear();

        var lines = File.ReadAllLines(path);

        foreach (var raw in lines)
        {
            string line = raw.Trim();

            if (string.IsNullOrWhiteSpace(line))
                continue;

            if (line.StartsWith("#"))
                continue;

            // Expected format: "en, English"
            var parts = line.Split(',', 2, StringSplitOptions.TrimEntries);

            if (parts.Length != 2)
                continue; // skip malformed lines

            string shortCode = parts[0];
            string fullName = parts[1];

            // Add to dictionary
            langList[fullName] = shortCode;
        }
    }
}