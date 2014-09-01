nsisbuildrunner
===============

A simple TeamCity Build Runner for Nullsoft Scriptable Install System (NSIS). The code was originally from http://www.tandisoft.com/2011/11/built-teamcity-makensisexe-output.html (All credit where it's due) and was converted from VB.Net to C# by me.

How to build
===============

1. Clone the repo
2. Build with Visual Studio 2013
3. Copy the output to your build server (for example c:\untils\NSISTeamCityBuildRunner)
4. Add a build step to your TeamCity project. Set the runner type to "Command Line", Run option to "Executable with parameters", Command executable to the path from step 2 above and finally "Command Parameters" to the path of your NSIS script relative to the root of your build agent checkout folder.

License & Copyright
===============

This software is released under the MIT License in 2014 by Matt Cantillon. I may be contacted via GitHub at https://github.com/mcantillon.

How to Contribute
===============

Pull requests including bug fixes, new features and improved test coverage are welcomed. Please do your best, where possible, to follow the style of code found in the existing codebase.

Credits
===============

The following people have contributed ideas, documentation, or code to nsisbuildrunner:

 * Matt Cantillon
 * Tom Adams (Original VB.Net code (http://www.tandisoft.com/2011/11/built-teamcity-makensisexe-output.html))