# What is this for?
Whenever an Rpc is invoked, you won't know who invoked the Rpc unless it contains an info parameter. This preloader patcher inserts the info parameter into every Rpc in the base game, for other modders to utilize.

# Notice for Modders
While this is installed, you cannot (and shouldn't) invoke Rpcs directly. For example, instead of using `character.RPCA_Die()` you should use `character.view.RPC(nameof(Character.RPCA_Die), PhotonNetwork.LocalPlayer)`.

Reason being, you'll be invoking the Rpc methods improperly, missing the info parameter, leading to exceptions.

You *can* get around these exceptions with things like reflection, or depending on a modified Assembly-CSharp, but it's easier (and better practice) to not directly invoke Rpc methods.

## Potential issues for Transpilers
Due to the base game directly invoking some Rpc methods, this patcher has to modify the IL of some methods to account for the extra parameter.

These changes in IL, and some changes caused by simplifying and re-optimizing maros (IL) with `Mono.Cecil.Rocks` may cause some issues for transpilers.
- The patcher logs all modified methods on the Debug channel, if you're encountering strange issues, I recommend checking these logs to see which methods are being modified.
- To go a step further, you can enable `Preloader.DumpAssemblies` in your `BepInEx.cfg`, and check the modified IL for any changes that may be breaking your transpilers.

# Installation (Manual)
1. [Download BepInEx](https://github.com/BepInEx/BepInEx/releases/download/v5.4.23.3/BepInEx_win_x64_5.4.23.3.zip) if you don't already have it installed. If you do, skip to step 4.
2. Extract the contents of `BepInEx_win_x64_5.4.23.3.zip` into your game's root directory.
   - You may navigate there directly by: opening your Steam library, right clicking them game, selecting Manage, and then Browse local files.
3. Run the game once, wait for the game's window to appear, then you may close the game.
4. Download the latest release of this mod, or whichever one you'd prefer.
5. Move the `PEAKERRpcInfo.dll` contained within the .zip to your `BepInEx/patchers` folder.
6. Enjoy!