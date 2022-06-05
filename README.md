Thrive
======

This is Thim's fork of the Thrive repository. For more information on Thrive, visit
[Revolutionary Games' Website](https://revolutionarygamesstudio.com/).



Overview
--------

Repository structure:
- assets: This folder contains all the assets such as models and other binaries. The big files in this folder use [Git LFS](https://git-lfs.github.com/) in order to keep this repository from bloating. You need to have Git LFS installed to get the files. Some better editable versions of the assets are stored in a separate [repository](https://github.com/Revolutionary-Games/Thrive-Raw-Assets).
- [doc: Documentation files.](/doc) Contains style guide, engine overview and other useful documentation.
- simulation_parameters: Contains JSON files as well as C# constants for tweaking the game.
- scripts: Utility scripts for Thrive development
- src: The core of the game written in C# as well as Godot scenes.
- test: Contains tests that will ensure that core parts work correctly. These don't currently exist for the Godot version.

Getting Involved
----------------
If you want to contribute code, all you have to do is make a PR. For more details read the [contribution guidelines](CONTRIBUTING.md). The [styleguide][styleguide] is quite short, so be sure to give that a look as well.

If you want to contribute some other variety of asset, consider contributing to (or joining) Thrive's main team. Assets from the main repo are good to trasfer over to this one, but because of permissions it does NOT work the other way, and I sure wouldn't want to steal taleneted people away from Thrive!


[setupguide]: doc/setup_instructions.md
[lfs]: https://wiki.revolutionarygamesstudio.com/wiki/Git_LFS
[learninggodot]: doc/learning_godot.md
