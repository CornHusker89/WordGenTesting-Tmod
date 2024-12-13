using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WorldGenTesting.Helpers;

namespace WorldGenTesting.Items
{
	public class SpawnTest : ModItem	
	{
		
		public override void SetDefaults()
		{
			Item.useStyle = ItemUseStyleID.Swing;
			Item.useTime = 20;
			Item.useAnimation = 20;
			Item.rare = ItemRarityID.Blue;
		}
		
		public override bool AltFunctionUse(Terraria.Player player)
		{
			return true;
		}
		
		public override bool? UseItem(Terraria.Player player)
		{
			int x = (Main.MouseWorld / 16).ToPoint16().X;
			int y = (Main.MouseWorld / 16).ToPoint16().Y;			

			TestingHelper.TakeScreenshot(new Rectangle(x - 10, y - 10, 20, 20), 
				Main.ActiveWorldFileData.Seed.ToString());
			
			return true;
		}

	}
}