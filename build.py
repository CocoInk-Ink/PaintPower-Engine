import os
import json
import sys
import platform

Platform:str = platform.system()

is_windows:bool = Platform == "Windows"
is_linux:bool = Platform == "Linux"
is_osx:bool = Platform == "Darwin" # MacOS for those who don't know

mydir:str = __file__.replace("build.py", "")

# Read the original build file
with open("build.iss", "r") as f:
    content = f.read()

# Replace the text
updated = content.replace("old text", "new text")

# Save to a new file
with open("your-build.iss", "w") as f:
    f.write(updated)
