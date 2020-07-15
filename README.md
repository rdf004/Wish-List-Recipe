## Wish List
### Description:
Players can save in-game items that they want to purchase or receive as a gift in the future to a wish list. Wish lists in this example use PlayFab entity groups. The player to whom the wish list belongs to is the administrator of the entity group, and players who can view the wishlist are members. The wish list is stored as an itemized CSV in the entity group's data.

This technique is designed for durable items, but can easily be extended to consumable items.

### Ingredients (Building Blocks):
  * [Accounts](https://api.playfab.com/docs/building-blocks#Accounts)
  * [Player Inventory](https://api.playfab.com/docs/building-blocks#Player_Inventory)
  * [Catalog & CatalogItems (Bundle / Container)](https://api.playfab.com/docs/building-blocks#Catalog)
  * [Entity Groups](https://docs.microsoft.com/en-us/gaming/playfab/features/social/groups/quickstart)

### Preparation ###

  1. Under the **Economy &gt; Catalogs** section of the Game Manager create a catalog called **game_items** and add the following items to the catalog:

| Item ID | Consumable |
| :-----: | :--------: |
|   cat   |  Durable   |
|   dog   |  Durable   |
|  mango  |  Durable   |

  2. Next, under the **Automation > Cloud Script**, navigate to the **Revisions** tab. Copy and paste the [Cloud Script file](./CloudScriptFile.js). Then, click **SAVE AS REVISION 2** and check the box for **Deploy this revision after save**.

​    

### Mechanic Walkthrough:
#### Login ####

1. Client obtains a valid session ticket via LoginWithPlayFabRequest (required to make Client API Calls)
2. After logging in, the client lists all groups the player belongs to as a member. The nomenclature for wishlist groups is [player's title Player ID] + "wishlist". Therefore, the client iterates through the list of groups until the wishlist is found. If the wishlist is found, the client uses a GetObjects request to get the wishlist and update the store buttons appropriately. If the wishlist is not found, a wishlist is created.
3. To create a wish list, a Cloud Script function is called to create the entity group.
4. The buttons corresponding to items already on the wishlist change to green. The button text indicates that the item is on the wish list.

#### Update Wishlist

1. UpdateWishlist takes in the ItemId as a string.
2. Client calls GetObjects on the entity group and reads the wish list, which is a CSV of ItemId's.
3. If the ItemId shows up in the CSV, then it is removed from the CSV. If the ItemId does not show up in the CSV, it is added to the CSV.
4. Client calls a Cloud Script function to update the entity group object with the updated CSV. The entity group object with the key "wishlist" has its value set to the updated CSV.
5. If successful, the change is reflected in the Unity game. The color and text of the button corresponding to the item that was either added or removed from the wish list is changed.

#### Add Player to wish list entity group

1. Client calls a Cloud Script function, which does the following.
2. The Cloud Script functions gets the title's entity token, and the account information corresponding to the player to be added to the entity group.
3. The entity group calls AddMembers with the entity key belonging to the player to be added.

----

#### Unity 3d Example Setup Instructions:
Import the following asset packages into a new or existing Unity project:

  * Ensure you have [the latest SDK](https://github.com/PlayFab/UnitySDK/raw/versioned/Packages/UnitySDK.unitypackage).
  * Ensure you have the [recipe files](https://github.com/PlayFab/PlayFab-Samples/raw/master/Recipes/PrizeWheel/Example-Unity3d/PrizeWheelRecipe.unitypackage).

1. Add assets to your project.
2. Open the SampleScene scene
3. Change TitleId's in LoginClass.cs to your TitleId.
4. Add your Username, TitleId, and Password to the LoginWithPlayFabRequest in LoginClass.cs
5. Run the scene and observe the console for call-by-call status updates
   1. Click login to authenticate your user.
   2. Click the three buttons in the top left corresponding to the game items (cat, dog, and mango). See how the buttons change in Unity, and how the Entity Group data changes in Game Manager.
   3. To add other players to the Entity Group as members, so these players can see the wish list, change the **id** in the **FunctionParameter** object in the **AddPlayFabIdToGroup** function in Wishlist.cs to the PlayFabId belonging to the player you want to add to the Entity Group as a member.