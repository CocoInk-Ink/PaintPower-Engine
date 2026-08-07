# Upload Plan — Project Linking & Upload Workflow

This document describes how PaintPower handles project uploads to the server.

---

## Project Linking

When loading a project from disk, the user should be asked:

**“Do you want to attach this project to your account?”**

If yes:
- Option 1: Create a new project on the server  
- Option 2: Select an existing project to overwrite  

If an existing project is selected:
- Retrieve its ID  
- Assign that ID to the project in C#  

---

## Uploading a Project

When the user clicks **Upload**:

### If the project is linked:
Ask:
- “Upload as new project?”  
- “Overwrite existing project?”  
- “Unlink project?”  

### If the project is unlinked:
Ask:
- “Do you want to link this project first?”  

### If the user is not signed in:
Show:
- “You must be signed in to upload projects.”  

---

## Notes

- Upload logic must check authentication  
- Unlinked projects cannot overwrite server data  
- Linked projects must store their server ID  
