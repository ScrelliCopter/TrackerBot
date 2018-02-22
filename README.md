Run away while you still can.

Some notes if your eally wanna build this thing:
- You need some libraries
 - Either clone Discord.Net into your Git folder, if you don't plan on debugging then you can just add the Nuget packages.
 - On Windows you need libsodium.dll opus.dll for Discord.Net.
 - On Windows you also need to go get the OpenMPT library.
 - On Windows make sure you get the 64 bit libraries if you have a 64 bit machine.
 - On Linux the above 3 points don't matter because in the future package managers exist.
 - On MacOS idk because I don't have a Mac, don't ask.
 - If you wish to subject yourself to the highly experimental libgme support you'll need that too, I needed to cross compile mine and the dll is huge so I'm not sharing it.
