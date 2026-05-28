namespace PaintPower.Templates.FileTemplates;

public class Txt : FileTemplate
{
    public Txt()
    {
        filetype = "txt";
        name = "Text File";
        description = "A text file used for storing plain text data.";
        category = "Text Formatting";
    }
}