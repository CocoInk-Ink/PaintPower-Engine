AI wrote this, it's is pretty wrong in some places.


Overview
This document is the final, authoritative plain‑text specification for KiteScript (KS) — the high‑level language used throughout the xPaint toolchain. KS is the human‑readable intermediate language that all supported source languages lower into. KS compiles to KiteScript Assembly (KSA) and executes on the Sprite VM, which is DIItem‑based and supports sprite operations, GUI rendering, and DIItem interaction.

KS is:

Object‑oriented

Typed by default

Supports generics

Supports generic variables without explicit types

Supports per‑language quirk modes (JavaScript, C#, C, ActionScript, etc.)

Supports GUI mode (HTML/XKML)

Fully tag‑based

Strictly sectioned

Designed to unify scripting, markup, and assembly‑level control under one syntax

This specification explains both the old parts of KS (variables, control flow, syscalls, foreach, blocks) and the new parts (OOP, generics, type system, quirks, language modes, GUI mode, and enforcement).

File structure and sections
A KS file is composed of sections, each beginning with a tag:

[#script] — main code section

[#gui] — GUI section

[#kxml] — XKML markup section

[#data] — structured data section

[#diplay : #html] — HTML GUI section

Additional sections may be added in future versions

Rules for sections

Only code sections ([#script]) may contain [#lang] and [#quirk] tags.

A [#lang] tag applies only to the code section it appears in, not the entire file.

A code section may contain at most one [#lang] tag.

[#quirk] tags may appear multiple times inside a code section or inside blocks.

GUI sections do not accept [#lang] unless nested inside a code block.

Core syntax and tags
KS uses a strict bracket‑tag syntax:

Code
[#keyword modifiers](arguments);
Core tags (existing features)

[#define var id](value); — define variable

[#set id](value); — assign

[#get id] — retrieve

[#call sys_id](args); — system call

[#if expr], [#else if expr], [#else]

[#block] ... [/#block]

[#foreach item in collection]

[#handler name] — event handler

[#export var id](value); — export variable

These remain unchanged and are fully supported.

Types and type system
KS is statically typed by default.

Primitive types

Int

Float

Bool

Char

String

Void

Reference types

class types

interface types

object

Composite types

Array<T>

Tuple<T1,T2>

Struct

Special types

Any

Nullable<T>

Type rules

Fields, method parameters, and return types must be annotated.

Local variables may omit types for inference.

Widening conversions allowed; narrowing conversions require explicit cast.

Non‑nullable by default; use Nullable<T> or T?.

Object‑oriented programming
KS supports full OOP:

Class declaration

Code
[#define class Name extends Base implements I1,I2] [#block]
    [#field private id]: Int;
    [#property public Name]: String { get; set; }
    [#ctor](id: Int, name: String) [#block]
        [#call base.ctor]();
        [#set this.id](id);
        [#set this.Name](name);
    [/#block]
    [#method public Greet](): Void [#block]
        [#call PopularIds.functions.print](join("Hello, ", [#get this.Name]));
    [/#block]
[/#block]
Interfaces

Code
[#define interface IUpdatable] [#block]
    [#method Update](): Void;
[/#block]
Structs

Code
[#define struct Point] [#block]
    [#field public x]: Int;
    [#field public y]: Int;
[/#block]
Enums

Code
[#define enum Color] [#block]
    Red = 0;
    Green = 1;
    Blue = 2;
[/#block]
DIItem and Sprite

Classes may extend DIItem:

DIItem provides x, y, Skin, Say, Center, GlidePos, etc.

Instances map to DIItem handles in the VM.

Event handlers compile to C# Actions in the runtime model.

Generics and generic variables
KS supports generics:

Code
[#define class Box<T>] [#block]
    [#field private value]: T;
    [#method public Get](): T [#block]
        [#return]([#get this.value]);
    [/#block]
[/#block]
Generic variables without explicit type

Code
[#define var box]<T>();
[#set box]<Int>(42);
Rules:

A generic variable declared with <T> is a generic slot.

First binding sets the concrete type for the scope.

Rebinding to a different type is an error unless [#quirk allow-rebind] is active.

Variables declared without type default to Any.

Variable kinds and language quirks
KS supports var, let, and const.

Default KS semantics

let — block‑scoped, typed or inferred

const — block‑scoped, immutable

var — block‑scoped, inferred type

Language quirk modes

Inside a code section:

Code
[#lang js]
Activates JavaScript semantics:

var is function‑scoped and hoisted

let is block‑scoped

const is block‑scoped and immutable

undefined exists

JS truthiness rules apply

Other modes:

[#lang csharp] — C# semantics

[#lang c] — C semantics

[#quirk c-pointer] — pointer arithmetic

[#quirk allow-rebind] — allow type rebinding for var

Events and handlers
KS event handlers compile to runtime Actions:

Code
[#handler Start] [#block]
    ...
[/#block]

[#handler receive:message1] [#block]
    ...
[/#block]

[#handler KeyPress:W,UpArrow] [#block]
    ...
[/#block]
Handlers may be attached or removed at runtime.

GUI mode and HTML compilation
GUI sections compile to GUI syscalls:

Code
[#gui]

[#diplay : #html]
    [#head]
        [#title]KiteScript GUI[/#title]
        [#description]This is a GUI...[/#description]
    [/#head]
    [#body]
        [#h1]KiteScript GUI[/#h1]
        [#button id "runButton"]Run[/#button]
    [/#body]
[/#diplay]
XKML sections ([#kxml]) compile to structured UI and data tables.

Full example demonstrating all features
Below is a complete KS example using:

Variables

Control flow

Foreach

Syscalls

OOP

Generics

Generic variables

Language quirks

Events

GUI mode

XKML

Code
[#script] @!! KiteScript Test Script

[#lang js]   ; JS semantics for this code section

[#define var "x" "x"]
[#define var "y" "y"]
[#define var "status" "status"]

[#set "x"](15);
[#set "y"]("Outer");
[#set "status"]("unknown");

[#call PopularIds.functions.print]("Starting system check...");

[#if x < 10]
[#block]
    [#call PopularIds.functions.print]("x is less than 10");
    [#set "status"]("low");
[/#block]

[#else if x == 10]
[#block]
    [#call PopularIds.functions.print]("x is exactly 10");
    [#set "status"]("equal");
[/#block]

[#else if x > 10]
[#block]
    [#call PopularIds.functions.print]("x is greater than 10");
    [#set "status"]("high");
[/#block]

[#else]
[#block]
    [#call PopularIds.functions.print]("Unexpected value for x");
[/#block]

[#call PopularIds.functions.print]("Status after x-check:");
[#call PopularIds.functions.print]([#get "status"]);

[#if y == "Outer"]
    [#call PopularIds.functions.print]("y is Outer");

[#block]
    [#define var "y" "y"]
    [#set "y"]("Inner");

    [#call PopularIds.functions.print]("Inside nested block:");
    [#call PopularIds.functions.print]([#get "y"]);
[/#block]

[#call PopularIds.functions.print]("Outside nested block:");
[#call PopularIds.functions.print]([#get "y"]);

[#if status != "unknown"]
    [#call PopularIds.functions.print]("Status is valid");

[#call PopularIds.functions.print]("System check complete.");

[#define var 0x9837]("Hello, World!");

[#foreach char in [#get 0x9837]] [#call 0x000](char);

[#foreach char in Message] [#block]
    [#call 0x000](char);
[/#block]

[#define class Player extends DIItem implements IUpdatable] [#block]
    [#field private id]: Int;
    [#field public Name]: String;
    [#field private health]: Int;

    [#ctor](id: Int, name: String) [#block]
        [#call base.ctor]();
        [#set this.id](id);
        [#set this.Name](name);
        [#set this.health](100);
    [/#block]

    [#method public SayName](): Void [#block]
        [#call PopularIds.functions.print](join("Player: ", [#get this.Name]));
    [/#block]

    [#method public Damage](amount: Int): Void [#block]
        [#set this.health]([#get this.health] - amount);
        [#if [#get this.health] <= 0]
        [#block]
            [#call PopularIds.functions.print](join([#get this.Name], " is dead"));
        [/#block]
        [/#if]
    [/#block]
[/#block]

[#define var box]<T>();
[#set box]<Int>(42);
[#call PopularIds.functions.print]([#get box]);

[#define func Identity]<T>(v: T): T [#block]
    [#return](v);
[/#block]

[#define var anyHolder] : Any;
[#set anyHolder]("a string");
[#set anyHolder](123);

[#handler Start] [#block]
    [#call PopularIds.functions.print]("Start handler running");
    [#call 0x210]();
    [#call 0x211](1, 40, 40);
    [#call PopularIds.functions.print]([#call Identity]<String>("Hello from Identity"));
[/#block]

[#handler receive:message1] [#block]
    [#set "Is done waiting?"](true);
    [#call 0x300](3000);
    [#call PopularIds.functions.print]("Waited 3 seconds!");
[/#block]

[#handler KeyPress:W,UpArrow,Space] [#block] [/#block]
[#handler KeyPress:A,LeftArrow] [#block] [/#block]
[#handler KeyPress:S,DownArrow] [#block] [/#block]
[#handler KeyPress:D,RightArrow] [#block] [/#block]

[#define var Name]("Nino");
[#define var Message]("");
[#define var "Is done waiting?"](false);

[#export var SpecialThing]([#get myFunction]);

[/#script]

[#gui]

[#diplay : #html]
    [#head]
        [#title]KiteScript GUI[/#title]
        [#description]This is a GUI for KiteScript.[/#description]
    [/#head]
    [#body]
        [#h1]KiteScript GUI[/#h1]
        [#button id "runButton"]Run[/#button]
        [#button id "stopButton"]Stop[/#button]
        [#textarea id "codeArea" rows "20" cols "80"][/#textarea]
    [/#body]
[/#diplay]

[#kxml]
[#data]
    [#table id "Users"]
        [#pattern id "Header"]
            [#cell]Name:String[/#cell]
            [#cell]Age:Integer[/#cell]
            [#cell]Email:String[/#cell]
        [/#pattern]
        [#row id "User1"]
            [#cell]"John Doe"[/#cell]
            [#cell]30[/#cell]
            [#cell]"john.doe@example.com"[/#cell]
        [/#row]
        [#row id "User2"]
            [#cell]"Jane Smith"[/#cell]
            [#cell]25[/#cell]
            [#cell]"jane.smith@example.com"[/#cell]
        [/#row]
    [/#table]
[/#data]
Final notes
KS is fully object‑oriented, typed, generic, and quirk‑aware.

All quirks and language modes are expressed in KS syntax.

GUI mode and XKML are first‑class citizens.

Generic variables without explicit types are supported.

The specification above is complete and final for KS.

Write it to a general user who has never heard of this before, not me.
Jose, here’s the version written for a general reader—someone who has never heard of KiteScript, xPaint, DIItems, or any of this before.
It explains the language clearly, from the ground up, and then shows a full example using everything.

I’m keeping it plain‑text, clean, and approachable.

What KiteScript Is
KiteScript is the main programming language used inside the xPaint environment.
It acts as a universal middle language: many different languages (C, C++, C#, JavaScript, HTML, XML, XAML, CSS, ActionScript, and even assembly languages) can be translated into KiteScript. Then KiteScript is compiled into a lower‑level form called KiteScript Assembly, which runs on the xPaint Sprite Virtual Machine.

KiteScript is:

Tag‑based (everything is written using bracketed tags)

Object‑oriented (supports classes, methods, fields, inheritance)

Strongly typed by default

Able to use generic types

Able to use generic variables without a type

Able to switch “quirk modes” so it can behave like JavaScript, C#, C, etc.

Able to express GUI and markup (HTML and XKML)

Able to express sprite behavior (movement, skins, DIItem interaction)

KiteScript is designed so that any language can be lowered into it, and the VM always knows how to run it.

How KiteScript Files Are Structured
A KiteScript file is divided into sections, each introduced by a tag:

[#script] — normal code

[#gui] — graphical user interface

[#kxml] — markup language (XKML)

[#diplay : #html] — HTML GUI section

Only the script section can contain language modes and quirks.

Core Syntax
KiteScript uses bracket‑tags:

Code
[#keyword](arguments);
Examples:

Code
[#define var x](15);
[#set x](20);
[#get x]
[#call 0x000]("A");
Blocks:

Code
[#block]
    ...
[/#block]
Control flow:

Code
[#if x < 10]
[#else if x == 10]
[#else]
Loops:

Code
[#foreach char in [#get message]]
Variables and Types
KiteScript supports:

var — behavior depends on language mode

let — typed or inferred, block‑scoped

const — typed or inferred, block‑scoped, immutable

Any — dynamic type

Full type annotations: Int, String, Bool, Float, Array<T>, etc.

Generic Variables Without a Type
You can declare a variable without specifying its type:

Code
[#define var box]<T>();
[#set box]<Int>(42);
Or leave it completely untyped:

Code
```
[#define var something];
[#set something]("hello");
[#set something](123);   ; allowed only if Any or quirk mode permits
```
Language Modes and Quirks
Because different languages behave differently, KiteScript allows quirk modes inside a script section.

Example:

Code
```
[#lang js]
This makes the entire script section behave like JavaScript:

var is function‑scoped

var is hoisted

undefined exists

JS truthiness rules apply

let and const behave like JS

Other modes:

Code
[#lang csharp]
[#lang c]
[#quirk allow-rebind]
[#quirk c-pointer]
These modes only affect the current script section, not the whole file.

Object‑Oriented Programming
KiteScript supports:

Classes

Interfaces

Structs

Enums

Methods

Constructors

Inheritance

Properties

Access modifiers (public, private, protected)

Example:

Code
```
[#define class Player extends DIItem implements IUpdatable] [#block]
    [#field private health]: Int;
    [#field public Name]: String;

    [#ctor](name: String) [#block]
        [#call base.ctor]();
        [#set this.Name](name);
        [#set this.health](100);
    [/#block]

    [#method public Greet](): Void [#block]
        [#call PopularIds.functions.print](join("Hello, ", [#get this.Name]));
    [/#block]
[/#block]
```
DIItem is the base type for all sprite objects in xPaint.

GUI and Markup
KiteScript can express GUI layouts using HTML‑style tags:

Code
[#diplay : #html]
    [#head]
        [#title]KiteScript GUI[/#title]
    [/#head]
    [#body]
        [#button id "runButton"]Run[/#button]
    [/#body]
[/#diplay]
And XKML for structured data:

Code
[#kxml]
[#data]
    [#table id "Users"]
        [#row id "User1"]
            [#cell]"John Doe"[/#cell]
            [#cell]30[/#cell]
        [/#row]
    [/#table]
[/#data]
Full Example Using Everything
Below is a complete KiteScript example demonstrating:

Variables

Control flow

Foreach

Syscalls

Classes

Generic variables

Language quirks

Events

GUI mode

XKML

Code
```
[#script] @!! KiteScript Test Script

[#lang js]   ; JavaScript semantics for this section

[#define var "x" "x"]
[#define var "y" "y"]
[#define var "status" "status"]

[#set "x"](15);
[#set "y"]("Outer");
[#set "status"]("unknown");

[#call PopularIds.functions.print]("Starting system check...");

[#if x < 10]
[#block]
    [#call PopularIds.functions.print]("x is less than 10");
    [#set "status"]("low");
[/#block]

[#else if x == 10]
[#block]
    [#call PopularIds.functions.print]("x is exactly 10");
    [#set "status"]("equal");
[/#block]

[#else if x > 10]
[#block]
    [#call PopularIds.functions.print]("x is greater than 10");
    [#set "status"]("high");
[/#block]

[#else]
[#block]
    [#call PopularIds.functions.print]("Unexpected value for x");
[/#block]

[#call PopularIds.functions.print]("Status after x-check:");
[#call PopularIds.functions.print]([#get "status"]);

[#if y == "Outer"]
    [#call PopularIds.functions.print]("y is Outer");

[#block]
    [#define var "y" "y"]
    [#set "y"]("Inner");

    [#call PopularIds.functions.print]("Inside nested block:");
    [#call PopularIds.functions.print]([#get "y"]);
[/#block]

[#call PopularIds.functions.print]("Outside nested block:");
[#call PopularIds.functions.print]([#get "y"]);

[#if status != "unknown"]
    [#call PopularIds.functions.print]("Status is valid");

[#call PopularIds.functions.print]("System check complete.");

[#define var 0x9837]("Hello, World!");

[#foreach char in [#get 0x9837]] [#call 0x000](char);

[#foreach char in Message] [#block]
    [#call 0x000](char);
[/#block]

[#define class Player extends DIItem implements IUpdatable] [#block]
    [#field private id]: Int;
    [#field public Name]: String;
    [#field private health]: Int;

    [#ctor](id: Int, name: String) [#block]
        [#call base.ctor]();
        [#set this.id](id);
        [#set this.Name](name);
        [#set this.health](100);
    [/#block]

    [#method public SayName](): Void [#block]
        [#call PopularIds.functions.print](join("Player: ", [#get this.Name]));
    [/#block]

    [#method public Damage](amount: Int): Void [#block]
        [#set this.health]([#get this.health] - amount);
        [#if [#get this.health] <= 0]
        [#block]
            [#call PopularIds.functions.print](join([#get this.Name], " is dead"));
        [/#block]
        [/#if]
    [/#block]
[/#block]

[#define var box]<T>();
[#set box]<Int>(42);
[#call PopularIds.functions.print]([#get box]);

[#define func Identity]<T>(v: T): T [#block]
    [#return](v);
[/#block]

[#define var anyHolder] : Any;
[#set anyHolder]("a string");
[#set anyHolder](123);

[#handler Start] [#block]
    [#call PopularIds.functions.print]("Start handler running");
    [#call 0x210]();
    [#call 0x211](1, 40, 40);
    [#call PopularIds.functions.print]([#call Identity]<String>("Hello from Identity"));
[/#block]

[#handler receive:message1] [#block]
    [#set "Is done waiting?"](true);
    [#call 0x300](3000);
    [#call PopularIds.functions.print]("Waited 3 seconds!");
[/#block]

[#handler KeyPress:W,UpArrow,Space] [#block] [/#block]
[#handler KeyPress:A,LeftArrow] [#block] [/#block]
[#handler KeyPress:S,DownArrow] [#block] [/#block]
[#handler KeyPress:D,RightArrow] [#block] [/#block]

[#define var Name]("Nino");
[#define var Message]("");
[#define var "Is done waiting?"](false);

[#export var SpecialThing]([#get myFunction]);

[/#script]

[#gui]

[#diplay : #html]
    [#head]
        [#title]KiteScript GUI[/#title]
        [#description]This is a GUI for KiteScript.[/#description]
    [/#head]
    [#body]
        [#h1]KiteScript GUI[/#h1]
        [#button id "runButton"]Run[/#button]
        [#button id "stopButton"]Stop[/#button]
        [#textarea id "codeArea" rows "20" cols "80"][/#textarea]
    [/#body]
[/#diplay]

[#kxml]
[#data]
    [#table id "Users"]
        [#pattern id "Header"]
            [#cell]Name:String[/#cell]
            [#cell]Age:Integer[/#cell]
            [#cell]Email:String[/#cell]
        [/#pattern]
        [#row id "User1"]
            [#cell]"John Doe"[/#cell]
            [#cell]30[/#cell]
            [#cell]"john.doe@example.com"[/#cell]
        [/#row]
        [#row id "User2"]
            [#cell]"Jane Smith"[/#cell]
            [#cell]25[/#cell]
            [#cell]"jane.smith@example.com"[/#cell]
        [/#row]
    [/#table]
[/#data]
```