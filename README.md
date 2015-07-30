Sm4sh-Tools
===========

Miscellaneous tools for dealing with Smash 4 files.

- AnimCmd
![Screenshot](http://i.imgur.com/jERDutV.png)
  - Aims to be a full fledged moveset editor for the smash4 ACMD filetypes, commonly refered to as "Moveset Files". The application supports full text editor based code writing with syntax highlighting and code completion. The application is still under active developement and some features may be unstable / incomplete.
  - Usable features.
    - Syntax highlighting of integer types.
    - Basic code completion
    - Expandable event dictionary (Events.txt in startup directory)
    - Opens either full characters via selecting the character folder, or single files.
    - Displays unknown commands as well as defined commands, with full rebuild capability.
    - Marking of changed actions in the tree view.
    - Exporting event lists as plaintext.
    - Customizable event syntax and descriptions. (events.cfg)
    - Tooltips when hovering over commands.
    - Exporing full character dumps as .txt.
    
  - Planned updates
    - Exporting / importing individual event lists as raw data.
    - Realtime error checking of the code box.
    - Adding new event lists to files.
    - Creating entirely new ACMD files.
    - Creating entirely new MTable files.
    - Better user interface, including application icon.
    - Undo and redo support.

  - Known Bugs
    - Does not warn on exiting before saving.
    - Saving with incorrect parameters in a command will crash the tool.

[![Build status](https://ci.appveyor.com/api/projects/status/e6q6vbdgjs4eoop5?svg=true)](https://ci.appveyor.com/project/Sammi-Husky/sm4sh-tools)
