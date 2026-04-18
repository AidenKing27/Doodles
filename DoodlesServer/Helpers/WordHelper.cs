using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DoodlesServer.Helpers;

public static class WordHelper
{
    public static List<string> GetThreeRandomWords(List<string> allUsedWords)
    {
        List<string> words = new();

        var lines = File.ReadLines(@"../../../Files/wordlist.txt");
        Random rnd = new();
        while (words.Count < 3)
        {
            string word = lines.ElementAt(rnd.Next(lines.Count()));
            if (!allUsedWords.Contains(word))
            {
                words.Add(word);
                allUsedWords.Add(word);
            }
        }

        return words;
    }
}
