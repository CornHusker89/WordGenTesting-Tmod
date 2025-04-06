using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.IO;
using Terraria.ModLoader;
using Terraria.Utilities;
using WorldGenTesting.Types;

namespace WorldGenTesting.Helpers;

public static class TestingHelper {
    /// <summary>
    ///     takes a screenshot of a specific section of a world. Can be called during world gen, but not without a loaded world
    /// </summary>
    /// <param name="target">section of the world (in tile coordinates) to screenshot</param>
    /// <param name="filename">
    ///     the filename for the resulting screenshot. path will be
    ///     Terraria/tModLoader/SavedGenerationScreenshots/(filename).png
    /// </param>
    public static void TakeScreenshot(Rectangle target, string filename) {
        // graphics stuff needs to be run on main thread
        Main.RunOnMainThread(() => {
            var graphics = Main.graphics.GraphicsDevice;
            var spriteBatch = new SpriteBatch(graphics);
            var renderTarget = new RenderTarget2D(graphics, target.Width * 16, target.Height * 16);

            graphics.SetRenderTarget(renderTarget);
            graphics.Clear(Color.SkyBlue);
            spriteBatch.Begin();

            // render walls
            for (var x = target.Left; x < target.Left + target.Width; x++)
            for (var y = target.Top; y < target.Top + target.Height; y++) {
                var tile = Main.tile[x, y];
                if (tile != null && tile.WallType != WallID.None) {
                    Asset<Texture2D> wallTexture;
                    var textureFilepath = WallLoader.GetWall(tile.WallType)?.Texture;
                    if (textureFilepath != null) {
                        wallTexture = ModContent.Request<Texture2D>(textureFilepath, AssetRequestMode.ImmediateLoad);
                    }
                    else {
                        // manually load wall, because its a vanilla wall
                        if (TextureAssets.Wall[tile.WallType].State == AssetState.NotLoaded)
                            Main.instance.LoadWall(tile.WallType);
                        wallTexture = TextureAssets.Wall[tile.WallType];
                    }

                    var sourceRect = new Rectangle(tile.WallFrameX, tile.WallFrameY, 32, 32);
                    var position = new Vector2((x - target.Left) * 16 - 8, (y - target.Top) * 16 - 8);
                    spriteBatch.Draw(wallTexture.Value, position, sourceRect, Color.White);
                }
            }

            // render tiles
            for (var x = target.Left; x < target.Left + target.Width; x++)
            for (var y = target.Top; y < target.Top + target.Height; y++) {
                var tile = Main.tile[x, y];
                if (tile != null && tile.HasTile) {
                    Asset<Texture2D> tileTexture;
                    var textureFilepath = TileLoader.GetTile(tile.TileType)?.Texture;
                    if (textureFilepath != null) {
                        tileTexture =
                            ModContent.Request<Texture2D>(textureFilepath, AssetRequestMode.ImmediateLoad);
                    }
                    else {
                        // manually load tile, because its a vanilla tile
                        if (TextureAssets.Tile[tile.TileType].State == AssetState.NotLoaded)
                            Main.instance.LoadTiles(tile.TileType);
                        tileTexture = TextureAssets.Tile[tile.TileType];
                    }

                    var sourceRect = new Rectangle(tile.TileFrameX, tile.TileFrameY, 16, 16);
                    var position = new Vector2((x - target.Left) * 16, (y - target.Top) * 16);
                    spriteBatch.Draw(tileTexture.Value, position, sourceRect, Color.White);
                }
            }

            spriteBatch.End();
            graphics.SetRenderTarget(null);

            // determine filepath
            var path = ModLoader.ModPath.Replace("Mods", "SavedGenerationScreenshots");

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            var thisPath = Path.Combine(path, filename);

            // add number suffix if necessary
            var counter = 2;
            while (File.Exists(thisPath + ".png")) {
                thisPath = Path.Combine(path, filename) + $"({counter})";
                counter++;
            }

            using (var stream = new FileStream(thisPath + ".png", FileMode.Create)) {
                renderTarget.SaveAsPng(stream, renderTarget.Width, renderTarget.Height);
            }

            spriteBatch.Dispose();
            renderTarget.Dispose();
        });
    }

    /// <summary>
    ///     creates a new world. resulting file can be found in Main.ActiveWorldFileData.
    ///     Blocking function, use either in separate thread or in command callback
    /// </summary>
    /// <param name="name">name of the world</param>
    /// <param name="size">size of the world. defaults to medium</param>
    /// <param name="evil">the world evil. defaults to random</param>
    /// <param name="seed">seed of the world. defaults to a random seed</param>
    /// <param name="save">if true, the world will be saved. otherwise, will be discarded once generation finishes</param>
    public static void MakeWorld(string name, WorldSize size = WorldSize.Medium, WorldEvil evil = WorldEvil.Random,
        string seed = null, bool save = false) {
        if (Main.selectedPlayer == 0) {
            Main.LoadPlayers();
            Main.SelectPlayer(Main.PlayerList[0]);
        }

        switch (size) {
            case WorldSize.Small:
                Main.maxTilesX = 4200;
                Main.maxTilesY = 1200;
                break;
            case WorldSize.Medium:
                Main.maxTilesX = 6400;
                Main.maxTilesY = 1800;
                break;
            case WorldSize.Large:
                Main.maxTilesX = 8400;
                Main.maxTilesY = 2400;
                break;
            case WorldSize.RandomSmallMediumLarge:
                var rand = Random.Shared.Next(0, 3);
                if (rand == 0) {
                    Main.maxTilesX = 4200;
                    Main.maxTilesY = 1200;
                }
                else if (rand == 1) {
                    Main.maxTilesX = 6400;
                    Main.maxTilesY = 1800;
                }
                else {
                    Main.maxTilesX = 8400;
                    Main.maxTilesY = 2400;
                }

                break;
            case WorldSize.RandomSmallMedium:
                if (Random.Shared.Next(0, 2) == 0) {
                    Main.maxTilesX = 4200;
                    Main.maxTilesY = 1200;
                }
                else {
                    Main.maxTilesX = 6400;
                    Main.maxTilesY = 1800;
                }

                break;
            case WorldSize.RandomSmallLarge:
                if (Random.Shared.Next(0, 2) == 0) {
                    Main.maxTilesX = 4200;
                    Main.maxTilesY = 1200;
                }
                else {
                    Main.maxTilesX = 8400;
                    Main.maxTilesY = 2400;
                }

                break;
            case WorldSize.RandomMediumLarge:
                if (Random.Shared.Next(0, 2) == 0) {
                    Main.maxTilesX = 6400;
                    Main.maxTilesY = 1800;
                }
                else {
                    Main.maxTilesX = 8400;
                    Main.maxTilesY = 2400;
                }

                break;
            default:
                throw new ArgumentException("Invalid size parameter given. Must be a valid WorldSize enum");
        }

        WorldGen.setWorldSize();

        Main.GameMode = 0;
        WorldGen.WorldGenParam_Evil = (int)evil;
        Main.worldName = name;
        Main.ActiveWorldFileData = WorldFile.CreateMetadata(Main.worldName, false, Main.GameMode);

        if (seed is null)
            Main.ActiveWorldFileData.SetSeedToRandom();
        else
            Main.ActiveWorldFileData.SetSeed(seed);

        WorldGen.generatingWorld = true;
        Main.rand = new UnifiedRandom(Main.ActiveWorldFileData.Seed);
        WorldGen.gen = true;

        var mod = ModContent.GetInstance<WorldGenTesting>();
        try {
            WorldGen.clearWorld();
            Main.LoadWorlds();
            WorldGen.GenerateWorld(Main.ActiveWorldFileData.Seed);

            WorldGen.generatingWorld = false;
            mod.SendToOutput($"World generation on seed {Main.ActiveWorldFileData.Seed} complete.");
            ModContent.GetInstance<WorldGenTesting>().Logger
                .Info($"World generation on seed {Main.ActiveWorldFileData.Seed} complete.");

            if (save) {
                WorldFile.SaveWorld(Main.ActiveWorldFileData.IsCloudSave, true);
                mod.SendToOutput("World saving complete.");
                ModContent.GetInstance<WorldGenTesting>().Logger.Info("World saving complete.");
            }

            Main.ActiveWorldFileData = null;
        }
        catch (Exception e) {
            mod.SendToOutput("World generation or saving failed with exception.");
            ModContent.GetInstance<WorldGenTesting>().Logger
                .Error($"World generation or saving failed with exception: {e}");
        }

        // to prevent duplicate worlds (???) on the world list
        Main.LoadWorlds();
    }

    /// <returns>
    ///     full world string used to generate new worlds, based on the currently loaded file or the given one. assumes
    ///     classic difficulty. ex. 1.1.1.1723613162
    /// </returns>
    public static string GetWorldSeedString(WorldFileData worldFileData = null) {
        WorldFileData file;
        if (worldFileData == null)
            file = Main.ActiveWorldFileData;
        else
            file = worldFileData;

        var seed = string.Empty;
        switch (file.WorldSizeName) {
            case "Small":
                seed += "1.";
                break;
            case "Medium":
                seed += "2.";
                break;
            case "Large":
                seed += "3.";
                break;
            default:
                throw new Exception($"World size of \"{seed}\" not recognized as Small, Medium, or Large");
        }

        // assumes classic difficulty
        seed += "1.";

        if (!file.HasCrimson)
            seed += "1.";
        else
            seed += "2.";

        seed += file.Seed;
        return seed;
    }
}