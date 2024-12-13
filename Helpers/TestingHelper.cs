using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.IO;
using Terraria.ModLoader;
using Terraria.ObjectData;
using Terraria.Social;
using Terraria.UI;
using Terraria.WorldBuilding;

namespace WorldGenTesting.Helpers;

public static class TestingHelper
{   
    public enum WorldEvil : int
    {
        Crimson = 1,
        Random = 0,
        Corruption = 1
    }
    
    public enum WorldSize : int
    {
        Small = 0,
        Medium = 1,
        Large = 2
    }
    
    
    /// <summary>
    /// takes a screenshot of a specific section of a world. Can be called during world gen, but not without a loaded world
    /// </summary>
    /// <param name="target">section of the world (in tile coordinates) to screenshot</param>
    /// <param name="filename">the filename for the resulting screenshot. path will be Terraria/tModLoader/SavedGenerationScreenshots/(filename).png</param>
    public static void TakeScreenshot(Rectangle target, string filename)
    {
        GraphicsDevice graphics = Main.graphics.GraphicsDevice;
        SpriteBatch spriteBatch = new SpriteBatch(graphics);
        RenderTarget2D renderTarget = new RenderTarget2D(graphics, target.Width * 16, target.Height * 16);

        graphics.SetRenderTarget(renderTarget);
        graphics.Clear(Color.SkyBlue);
        spriteBatch.Begin();

        // render walls
        for (int x = target.Left; x < target.Left + target.Width; x++)
        {
            for (int y = target.Top; y < target.Top + target.Height; y++)
            {
                Tile tile = Main.tile[x, y];
                if (tile != null && tile.WallType != WallID.None)
                {
                    Asset<Texture2D> wallTexture;
                    string textureFilepath = WallLoader.GetWall(tile.WallType)?.Texture;
                    if (textureFilepath != null)
                        wallTexture = ModContent.Request<Texture2D>(textureFilepath, AssetRequestMode.ImmediateLoad);
                    else
                        wallTexture = TextureAssets.Wall[tile.WallType];
                    if (wallTexture == null)
                        continue;
                    
                    Rectangle sourceRect = new Rectangle(tile.WallFrameX, tile.WallFrameY, 32, 32);
                    Vector2 position = new Vector2((x - target.Left) * 16 - 8, (y - target.Top) * 16 - 8);
                    spriteBatch.Draw(wallTexture.Value, position, sourceRect, Color.White);
                }
            }
        }
        
        // render tiles
        for (int x = target.Left; x < target.Left + target.Width; x++)
        {
            for (int y = target.Top; y < target.Top + target.Height; y++)
            {
                Tile tile = Main.tile[x, y];
                if (tile != null && tile.HasTile)
                {
                    // int tileStyle = TileObjectData.GetTileStyle(tile);
                    // if (tileStyle != -1)
                    // {
                    //     var tileData = TileObjectData.GetTileData(tile.TileType, tileStyle);
                    //     Console.WriteLine(tileData.CoordinateFullHeight + ", " + tileData.CoordinateFullWidth);
                    // }
                    
                    Asset<Texture2D> tileTexture;
                    string textureFilepath = TileLoader.GetTile(tile.TileType)?.Texture;
                    if (textureFilepath != null)
                        tileTexture = ModContent.Request<Texture2D>(textureFilepath, AssetRequestMode.ImmediateLoad);
                    else
                        tileTexture = TextureAssets.Tile[tile.TileType];
                    if (tileTexture == null)
                        continue;
                    
                    Rectangle sourceRect = new Rectangle(tile.TileFrameX, tile.TileFrameY, 16, 16);
                    Vector2 position = new Vector2((x - target.Left) * 16, (y - target.Top) * 16);
                    spriteBatch.Draw(tileTexture.Value, position, sourceRect, Color.White);
                }
            }
        }
        
        spriteBatch.End();
        graphics.SetRenderTarget(null);

        // determine filepath
        string path = ModLoader.ModPath.Replace("Mods", "SavedGenerationScreenshots");

        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);

        string thisPath = Path.Combine(path, filename);

        int counter = 2;
        while (File.Exists(thisPath + ".png"))
        {
            thisPath = Path.Combine(path, filename) + $"({counter})";
            counter++;
        }

        using (FileStream stream = new FileStream(thisPath + ".png", FileMode.Create))
            renderTarget.SaveAsPng(stream, renderTarget.Width, renderTarget.Height);

        spriteBatch.Dispose();
        renderTarget.Dispose();
    }
    
    /// <summary>
    /// creates a new world. resulting file can be found in Main.ActiveWorldFileData
    /// </summary>
    /// <param name="name">name of the world</param>
    /// <param name="size">size of the world</param>
    /// <param name="evil">the world evil (corruption or crimson</param>
    /// <param name="seed">seed of the world. defaults to a random seed</param>
    /// <param name="save">if true, the world will be saved. otherwise, will be discarded once generation finishes</param>
    public static void MakeWorld(string name, WorldSize size = WorldSize.Small, WorldEvil evil = WorldEvil.Random,
        string seed = null, bool save = false)
    {
        if (Main.selectedPlayer == 0)
        {
            Main.LoadPlayers();
            Main.SelectPlayer(Main.PlayerList[0]);
            
            Main.LoadWorlds();
        }    
        
        if ((int)size == 0) {
            Main.maxTilesX = 4200;
            Main.maxTilesY = 1200;
        }
        else if ((int)size == 1) {
            Main.maxTilesX = 6400;
            Main.maxTilesY = 1800;
        }
        else {
            Main.maxTilesX = 8400;
            Main.maxTilesY = 8400;
        }
        WorldGen.setWorldSize();
        
        Main.GameMode = 0;
        WorldGen.WorldGenParam_Evil = (int)evil;
        Main.worldName = name;
        Main.ActiveWorldFileData = WorldFile.CreateMetadata(Main.worldName, 
            SocialAPI.Cloud != null && SocialAPI.Cloud.EnabledByDefault, Main.GameMode);
                
        if (seed is null)
            Main.ActiveWorldFileData.SetSeedToRandom();
        else
            Main.ActiveWorldFileData.SetSeed(seed);

        WorldGen.GenerateWorld(Main.ActiveWorldFileData.Seed);
        WorldGen.generatingWorld = false;
        
        if (save)
            WorldFile.SaveWorld(Main.ActiveWorldFileData.IsCloudSave, resetTime: true);
        
        return;
    }
    
    public static void DeleteWorld(WorldFileData worldFileData)
    {
        throw new NotImplementedException();
        //string filename = worldFileData.GetFileName();
    }
}