# Machinations UP - Unity Example: Tanks

This repository integrates Machinations UP within Tanks, an example used in a [popular talk](https://www.youtube.com/watch?v=6vmRwLYWNRo) on Unity Scriptable Objects.

# Running this example

1. Install the latest version of the Unity 3D Engine by downloading Unity Hub from [here](https://store.unity.com/#plans-individual). Once you installed Unity Hub, you will need to add a Unity 3D install. Stuck? Check our [Detailed Unity Installation Guide](README-unity.md) here.
2. Open Unity 3D and navigate to where you cloned this repo. Upon opening the folder, your Unity Editor should look something like this:  
   ![Image of Unity Editor](./readme.md.resources/StartupScene.jpg)
3. In Machinations, create a copy of Ruby's Adventure [Machinations diagram](https://www.machinations.io).
4. In Unity, in the Scene Hierarchy tab, configure the `MachinationsGameLayer` with the correct `User Key` & `Diagram Token`. Here's how to find these:
   1. MachinationsGameLayer inside Unity:  
      ![Image of MachinationsGameLayer Configuration](./readme.md.resources/MGLConfig.jpg)
   2. User Key in your Machinations account:  
      ![Image of Machinations User Account](./readme.md.resources/MachinationsUserAccount.jpg)
   3. Diagram Token in the Machinations Diagram:  
      ![Image of Machinations Diagram Details](./readme.md.resources/MachinationsDiagramDetails.jpg)
5. Run the game in Unity by pressing the "Play" arrow in the center-top, above the stage.
6. Change the `Player HP` Pool in the Ruby's Adventure diagram. If everything works, Ruby's in-game health should also change.

# Useful Links

Head over to our [Developer Portal](developer.machinations.io) for more Machinations tinkering adventures.

Machinations product documentation can be found [here](docs.machinations.io).

If you want to learn some Unity, why not see, step by step, how this very game was built: [Unity's Ruby's Adventure Tutorial](https://learn.unity.com/project/ruby-s-2d-rpg).
