# Goals!

# Welcome! And thank you for contributing to this project!

  

## There are many goals we want to accomplish before the first release of xPaint.

## There is a lot to do, and it's all gotta be written in C#, oh boy...

## Here are some goals we are still working on:

- ### Editors

    - Fix paint editor

    - Better script editor

    - Create video editor

    - Create animation editor

  #### For the VM and runtime, it runs bytecode compiled from KiteScript, and all sorts of languages get compiled to KiteScript instead of direct bytecode.

- ### VM

    - [x] Create VM

    - [x] Make it extendable, allow it to run multiple programming languages on multiple threads all at once and make it easy to recreate in other languages like Java, ActionScript and JavaScript so that a web player version can exist
	- [] Make it great
	- [] Run CLI languages at once and allow them to talk to each other all at once
	- [] VM messages
	- [] Allow the languages to access the Sprites, media, and Display Objects. This should allow videos, sounds, and other media to work
	- [] Also to have a bit of an interpreter for live code.

- ### Compiler
	- [] Compile code to KiteScript, all complex languages get compiled to code in the KiteScript language.
	Then the the KiteScript can then be compiled to bytecode.

	- [] Make the compiler(s)
	- [x] Make a KiteScript Compiler
	- Find a solution for interpreted languages
	- Make it work with multiple popular programming languages, like:
		- ActionScript (1.0-2.0)
		- ActionScript (3.0 flex, air, royale)
		- Assembly x86 (CLI version)
		- Assembly Arm (CLI version)
		- C
		- C#
		- C++
		- Coco (Custom language)
		- CocoScript (Custom language)
		- CoffeeScript
		- Go
		- Java
		- JavaScript (Interpreted)
		- TypeScript
		- KiteScript
		- Python
		- Dart
		- Prolog
		- FriedRice
		- SQL
		- Visual Basic
		- Ruby
		- Fortran
		- R
		- Special Compiler for Scratch blocks
		- PHP
		- MATLAB
		- HTML
		- EJS
		- CSS
		- JSX
        - Rust
		- Paint (Language used for building)
		- PXML (Custom language) (like HTML)
		- PSS (Custom language) (like CSS)
		- PaintScript (Custom language)
        - Adobe PostScript
		- Sparkle (Custom language)
		- SXML (Custom language) (like HTML)
		- XSS (Custom language) (like CSS)
		- XS (Custom language) (like JS (JavaScript))
		- Zig
		- And more!
	- Each supported language should be able to talk to each other through: messages, functions, and other methods. By either exposing a function, class or object to the VM, then calling it though another language, it all gets compiled to KiteScript so it's easier, then KiteScript to bytecode.
	- [ ] Compile all to KiteScript
	- Compile messages
	- Compile and export Sprites (contains: scripts, inner sprites, images, sounds, videos, other media)
- ### xPaint Website and the internet
	- [x] Implement login, get CSRF security tokens
    - [x] Project upload to user's mystuff within the editor
    - [ ] Download projects from user's mystuff within the editor

    - [ ] See views, comments, and reply to comments within the editor
    - [ ] Make it extendable and easy for contributors to create their own custom server
	- [ ] Easy project collaboration

- ### General features:
	- [x] Import/Export assets
	- [ ] WASM Support
	- [x] Paint Editor. Is Finished?: [ ]
	- [ ] Video Editor. Is Finished?: [ ]
	- [x] Sound Player. Is Finished?: [ ]