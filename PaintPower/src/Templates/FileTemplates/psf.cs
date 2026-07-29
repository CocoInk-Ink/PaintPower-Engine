namespace PaintPower.Templates.FileTemplates;

public class Psf : FileTemplate
{
    public Psf()
    {
        filetype = "psf";
        name = "Paint Style File";
        description = "A style file format used by xPaint. Like how CSS is for HTML, but for xPaint's style system.";
        category = "Style Programming (xPaint)";
    }
}