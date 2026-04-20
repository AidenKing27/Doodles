using System;
using System.Collections.Generic;
using System.Text;

namespace DoodlesServer.Models;

public class RevealedLetters(int index, char letter)
{
    public int Index { get; set; } = index;
    public char Letter { get; set; } = letter;
}
