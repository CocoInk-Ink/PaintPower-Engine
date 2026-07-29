# WXA Format Specification

The **WXA** format is used by the PaintPower Animation Editor.  
It is stored as a **.rar archive** containing several files and folders.

But it will currently be a .zip instead for simplicity.

---

## File Structure

A WXA file contains:

```
settings.xml
settings.json
items/
    Scenes/
        scene1.was
        scene2.was
timeline/
    ...
```

---

## Description

### **settings.xml / settings.json**
Contains metadata such as:
- Project name  
- Resolution  
- Frame rate  
- Scene list  

### **items/Scenes**
Each `.was` file represents a scene in the animation.

### **timeline**
Defines:
- Keyframes  
- Layer order  
- Animation curves  
- Scene transitions  

---

## Notes

- The format is still evolving  
- Contributors should expect changes  
- Backward compatibility is not guaranteed during Pre‑Alpha  

---

## Future Plans

- Full schema documentation  
- Example WXA files  
- Validation tools  
- Import/export utilities  
