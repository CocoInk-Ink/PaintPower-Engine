using System.Collections.Generic;
using PaintPower.Templates.FileTemplates;

namespace PaintPower.Templates;

public class FileList
{
    public List<FileTemplate> templates = new List<FileTemplate>();

    public FileList()
    {
        // Alphabetical order
        templates.Add(new As());
        templates.Add(new Asm());
        templates.Add(new Axaml());
        templates.Add(new Bmp());
        templates.Add(new C());
        templates.Add(new Coco());
        templates.Add(new CocoScript());
        templates.Add(new CoffeeScript());
        templates.Add(new CSharp());
        templates.Add(new Cpp());
        templates.Add(new Css());
        templates.Add(new Custom());
        templates.Add(new Flv());
        templates.Add(new Gif());
        templates.Add(new H());
        templates.Add(new Hpp());
        templates.Add(new Htm());
        templates.Add(new Html());
        templates.Add(new Java());
        templates.Add(new Jpeg());
        templates.Add(new Jpg());
        templates.Add(new Js());
        templates.Add(new Json());
        templates.Add(new Jsonc());
        templates.Add(new Jsx());
        templates.Add(new Lua());
        templates.Add(new M());
        templates.Add(new Md());
        templates.Add(new Mov());
        templates.Add(new Mp4());
        templates.Add(new Png());
        templates.Add(new Psf());
        templates.Add(new Pss());
        templates.Add(new Pxml());
        templates.Add(new Pxs());
        templates.Add(new Py());
        templates.Add(new S());
        templates.Add(new Sasm());
        templates.Add(new Script());
        templates.Add(new Spk());
        templates.Add(new Sxml());
        templates.Add(new Ts());
        templates.Add(new Txt());
        templates.Add(new Webp());
        templates.Add(new Wxa());
        templates.Add(new Wxc());
        templates.Add(new Wxv());
        templates.Add(new Xaml());
        templates.Add(new Xml());
        // templates.Add(new XPaint());
        templates.Add(new Xs());
        templates.Add(new Xss());
        templates.Add(new Yaml());
        templates.Add(new Yml());
    }
}