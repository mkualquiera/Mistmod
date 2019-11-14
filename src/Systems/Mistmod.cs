using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

[assembly: ModInfo("Mistmod",
	Description = "Mistborn mod",
	Website     = "https://github.com/1macho/",
	Authors     = new []{ "1Macho" })]

namespace MistMod
{
	/// <summary> Main system for the MistMod </summary>
	public class MistModSystem : ModSystem
	{
		/// <summary> The names of the metals that mistings and mistborns can burn. </summary>
        public static readonly string[] METALS = new string[] {"copper", "zinc", "tinbronze", "brass", "electrum",
             "bendalloy", "gold", "cadmium", "aluminium", "nicrosil", "duraluminium", "chromium", "pewter", "steel", "tin", "iron"};

		/// <summary> Id of the mod for networking channels and such. </summary>
		public static readonly string MOD_ID = "mistmod";

		/// <summary> The handler for client-side mod functionality. </summary>
		public ClientAllomancyHandler ClientHandler {get; private set;}

		/// <summary> The handler for server-side mod functionality. </summary>
		public ServerAllomancyHandler ServerHandler {get; private set;}
	
		/// <summary> The start point for the core api. </summary>
		public override void Start(ICoreAPI api)
		{
			// Initialize item classes.
			api.RegisterItemClass("ItemAllomanticMetalProvider",typeof(ItemAllomanticMetalProvider));
			api.RegisterItemClass("ItemLerasium", typeof(ItemLerasium));
		}
		
		/// <summary> Start point for the client api. </summary>
		public override void StartClientSide(ICoreClientAPI api)
		{
			// Create and initialize the client handler.
			ClientHandler = new ClientAllomancyHandler(api, this);
			ClientHandler.Initialize();
		}
		
		/// <summary> Start point for the server api. </summary>
		public override void StartServerSide(ICoreServerAPI api)
		{
			// Create and initialize the server handler.
			ServerHandler = new ServerAllomancyHandler(api, this);
			ServerHandler.Initialize();
		}
	}
}