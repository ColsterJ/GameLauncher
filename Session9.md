# Academy Session 9

## Local Encryption Fix

Team members:

- Colin Jones
- Andrew Horner
- Joshua Storrs
- Jesse Landis-Eigsti
- Liz Kimbell

## The problem

Recently, the LauncherClient application that is used to open Steam and run games stopped being able to save and load the settings sent to it from the server. The secret key was being stored in LauncherClient.exe.config, and it was encrypted using the ProtectSection() method. If the program is run as Administrator, it works again, but that isn't ideal. The program produced an error that took some digging to fully understand.

It took time to discover how to reproduce the error outside of LFG, but it turned out to be possible by creating an additional Windows account (which would have lesser privileges) and running LauncherClient and the React interface on that.

It turns out that ProtectSection() is mainly intended to be used on server applications with administrator access, for i.e. storing database connection strings. Previously, it still worked to use it client-side, even though it wasn't the intended use. It seems that with some recent Windows update(s) this approach doesn't work anymore on most machines.

## The solution

The app config file can still be used without administrator privileges as long as ProtectSection() isn't used. However, we still needed to encrypt the data somehow for security.

Luckily, we were able to encrypt and decrypt it using the machine key instead of ProtectSection(), simply storing the pre-encrypted string in the config file and decrypting it when it's loaded in. The machine key is something every Windows installation will have available. The code in [this link](https://stackoverflow.com/questions/36812592/encrypt-and-decrypt-with-machinekey-in-c-sharp) demonstrates how to use the machine key to encrypt and decrypt data.

The problem has been solved in [this commit](https://github.com/ColsterJ/GameLauncher/commit/2ca4249fc4ace5627b43b580ae5dd43e5e05b9e0). Even though that post seems to be discussing an ASP.NET application, the strategy worked for us in our client-side app. John has tested this on a few computers at LFG, and the application now works without running as administrator!

## Further work

- There should be checks in place if decrypting the secret with the machine key causes some kind of error.
- There are plenty of other areas to work on, such as:
- Documenting the process to setup the server and client
- Explaining where the different pieces are located in the repository
- Improving the React frontend both visually and in terms of code
- Improving the GUI of the LauncherClient