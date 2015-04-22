Sm4sh-Tools
===========

Miscellaneous tools for dealing with Smash 4 files.

- AnimCmd
  - Aims to be a full fledged moveset editor for the smash4 ACMD filetypes, commonly refered to as "Moveset Files". The application supports full text editor based code writing with syntax highlighting and code completion. The application is still under active developement and some features may be unstable / incomplete.
  - Usable features.
    - Syntax highlighting of integer types.
    - Basic code completion
    - Expandable event dictionary (Events.txt in startup directory)
    - Opens either full characters via selecting the character folder, or single files.
    - Displays unknown commands as well as defined commands, with full rebuild capability.
  
  - Planned updates
    - Customizable event syntax and descriptions.
    - Tooltips when hovering over commands.
    - Exporting / importing individual event lists as raw data.
    - Exporting event lists as plaintext.
    - Realtime error checking of the code box.
    - Marking of changed actions in the tree view.
    - Adding new event lists to files.
    - Creating entirely new ACMD files.
    - Creating entirely new MTable files.
    - Better user interface, including application icon.
    - Undo and redo support.

  - Known Bugs
    - Leaving the code box blank and saving will remove that event list from the file entirely.
    - Trying to open the code for an removed event list (see bug 1) will crash the application.
    - Saving an event list without an Script_End() command will cause the eventlist to merge with the next, corrupting the file.
    - Does not warn on exiting before saving.
    - Saving with incorrect parameters in a command will crash the tool.

[![Build status](https://ci.appveyor.com/api/projects/status/e6q6vbdgjs4eoop5?svg=true)](https://ci.appveyor.com/project/Sammi-Husky/sm4sh-tools)
