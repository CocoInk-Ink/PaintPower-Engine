# Publishing PaintPower

This document explains how to build PaintPower for deployment.

---

## Publishing Commands

### Windows PowerShell
```
.\publish-all.ps1
```

### Linux / macOS
```
./publish-all.sh
```

These scripts:
- Build the project  
- Package the output  
- Prepare files for release  

---

## Release Builds

Release builds are uploaded to:
https://github.com/CocoBox84/PaintPower-Engine/releases/

---

## Notes

- Scripts may require execution permissions  
- Publishing steps may change as the engine grows  
- Automated CI/CD may be added in the future  
