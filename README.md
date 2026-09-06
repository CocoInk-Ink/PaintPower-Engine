# PaintPower Engine
# Copyright (c) 2026 CocoInk Software.
#
# The xPaint Project, PaintPower Engine.

# What is PaintPower?
The PaintPower Engine is just the underlying application of xPaint.
xPaint extends PaintPower to add branding and website connectivity.

But PaintPower is the app itself.

# Code styles

The PaintPower Engine is made with C#. It is also made to run on many devices,
so it uses .NET 6, Avalonia 11.0 and C# 10.0 for compatibility.

Avalonia looks modern by default, but I want xPaint to look a bit older,
or at least unique. So for now I have a classic theme installed.

# Goal
The Goal is to make an interactive multimedia creation application that can be used by many,
mostly by users who have use a block based programming language like Scratch (scratch.org).

It includes many types of creation: Scripting, Painting, Animating, Video editing (low priority for now), but absolutely no audio editing.
Scratch 1.x did not have any audio editor, so we won't have that or a vector graphics editor either.

Later getting a Scratch project to convert to PaintScript would be cool.

# PaintScript

You can find PaintScript related repos here:

The Compiler, and Definition of the language here: https://github.com/CocoInk-Ink/PaintScript-Standard

The language server here: https://github.com/CocoInk-Ink/Paintscript-Language-Support

The runtime: https://github.com/CocoInk-Ink/PaintScript-Engine

# Project Structure
The PaintPower Engine has two main projects:
PaintPower    (Full editor)
PaintPower-VM (Will use the PaintScript Engine, but includes the viewport (stage))

These two projects use another two projects:

Assets        (Stores assets like images, sounds, themes, default projects, etc...)
Toolbox       (Tools that both the PaintPower and PaintPower-VM projects use)

# ResourceKit
For assets, we use a class called "ResourceKit". It extracts all assets from inside the app and links them for immediate use.
This is the system I have been working on and it's killing me to work on this all by myself (I have no freinds and my family is not intreisted). 
