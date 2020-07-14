using System;

using PlayFab;
using PlayFab.GroupsModels;
using PlayFab.ClientModels;
using PlayFab.DataModels;
using PlayFab.Json;

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.UI;

public class WishList : MonoBehaviour
{

    /* The following variables are to identify the group entity which holds the wish list. */
    /* They are first set in the WishList.FindOrCreateWishList function */

    public static string group_entityKeyId;
    public static string group_entityKeyType;

    /* 
        Find the entity group for the player's wish list, or create one if does not exist

        @param player_entityKeyId: the entity ID of the player; for a title entity the ID should be;
            in most cases, this can be found in LoginResult.EntityToken.Entity.Id 
        @param player_entityKeyType: the entity type of the player whose wish list we are searching
            for; should be title_player_account entity in most cases
        
        Upon login, this function examines all entity groups that the player belongs to. For each group, 
        the group name is compared to the nomenclature for wish list groups. If the group is not found, 
        then one is created 
    */

    public static void FindOrCreateWishList(string player_entityKeyId, string player_entityKeyType) {

        /* Create entity key for the ListMembership request */

        PlayFab.GroupsModels.EntityKey entity = new PlayFab.GroupsModels.EntityKey {Id = player_entityKeyId, Type = player_entityKeyType };

        var request = new ListMembershipRequest { Entity = entity };

        PlayFabGroupsAPI.ListMembership( request, membershipResult => {

            bool found = false; // Will tell us whether the wish list entity group exists

            /* 
                Iterate through all groups the player belongs to. If the wish list entity group exists, 
                it should be one of these groups
            */

            for (int i = 0; i < membershipResult.Groups.Count; i++ ) {
                string group_name = LoginClass.getPlayerEntityKeyId() + "wishlist";

                /* Compare the name of the group to the nomenclature the wish list entity group name will follow */
                
                if( membershipResult.Groups[i].GroupName.Equals( group_name ) ) {

                    found = true; // If the name matches, we found the wish list entity group

                    /* Set the wish list group's entity ID and entity type so we can access the group in other functions */
                    WishList.group_entityKeyId = membershipResult.Groups[i].Group.Id;
                    WishList.group_entityKeyType = membershipResult.Groups[i].Group.Type;

                    PlayFab.DataModels.EntityKey group_ek = new PlayFab.DataModels.EntityKey { Id = membershipResult.Groups[i].Group.Id, Type = membershipResult.Groups[i].Group.Type };
                    GetObjectsRequest getObjectsRequest = new GetObjectsRequest { Entity = group_ek };

                    /*  This is the wish list entity group. To get the wish list CSV, we need to get the object in that entity
                        group with the "wishlist" key 
                    */

                    PlayFabDataAPI.GetObjects(getObjectsRequest, objectResult => {
                        
                        if( !string.IsNullOrEmpty( (string) objectResult.Objects["wishlist"].DataObject ) ) {
                            string wl = (string) objectResult.Objects["wishlist"].DataObject;
                            /* Set up the Unity game store. Specifically, change colors and button text if an item is on the wishlist */
                            StoreSetup.SetUpStore(wl, false);
                        }

                    }, error => { Debug.LogError(error.GenerateErrorReport()); });

                }
            }

            // AddPlayFabIdToGroup(); // Where should this go?


            /* Wish list entity group does not exist, so create one */
            if( !found ) {
                /* 
                    Wish list entity groups should follow the following nomenclature:
                    [PlayFab title ID] + "wishlist.

                    This nomenclature allows us to find the group by name in the future.
                */
                string group_name = LoginClass.getPlayerEntityKeyId() + "wishlist";
                CreateWishlist(group_name);

            }

        }, error => { Debug.LogError(error.GenerateErrorReport()); });

    }

    /* 

        If the item is on the wish list, remove it. If the item is not on the wish list, add it.

        @param item_id: ItemID of the item to be added to or remove from the wishlist
        
        This function gets the "wishlist" object from the entity group data. If the item is on the wish list,
        this function updates the CSV by removing it. If the item is not on the wish list, this function
        updates the CSV by adding it. It then calls UpdateGroupObject, which updates the actual entity group data.

    */

    public void UpdateWishlist(string item_id) {

        /* Create entity key and request to get object data from group. */
        PlayFab.DataModels.EntityKey group_ek = new PlayFab.DataModels.EntityKey { Id = WishList.group_entityKeyId, Type = WishList.group_entityKeyType };
        GetObjectsRequest getObjectsRequest = new GetObjectsRequest { Entity = group_ek };

        /* GetObjects to get the wish list in CSV form. */
        PlayFabDataAPI.GetObjects(getObjectsRequest, objectResult => {

            string wl;
            bool adding_item; // This tells us whether we are adding or removing an item from the wish list


            if( !string.IsNullOrEmpty( (string) objectResult.Objects["wishlist"].DataObject ) ) {
                                    
                wl = (string) objectResult.Objects["wishlist"].DataObject; // string of the CSV of items on the wish list
                
                if( !WishlistContainsItem(wl, item_id) ) {
                    
                    /* Wish list does not contain the item, so we must add it. */
                    wl = AddItemToCSV(wl, item_id);
                    adding_item = true;

                } else {
                    /* Wish list contains item, so we must remove it. */
                    wl = RemoveItemFromCSV(wl, item_id);
                    adding_item = false;

                }
            } else {

                wl = item_id;
                adding_item = true;
            }

            /* UpdateGroupObject is where the entity group data is actually updated */
            UpdateGroupObject(wl, adding_item, item_id);
            
        }, error => {
            Debug.LogError(error.GenerateErrorReport());
        });               

    }

    /* 

        Remove an ItemID from the CSV.

        @param csv: the CSV list of ItemID's on the wish list
        @param item_id: ItemID of the item to be added to be removed from the wishlist

        @return the updated CSV

    */

    private static string RemoveItemFromCSV(string csv, string item_id) {
        /* Split CSV into an array of ItemIDs  */
        string[] items = csv.Split(',');
        int ind = Array.IndexOf(items, item_id); // item at which ItemID exists
        List<string> items_list = new List<string>(items); // Convert array to list for easy removal

        /* Only try to remove if the ItemID shows up in the CSV */
        if( ind != -1 ) {
            items_list.RemoveAt(ind);
        }

        string updated_csv = string.Join(",", items_list);
        return updated_csv;
    }

    /* 

        Add an ItemID to the CSV.

        @param csv: the CSV list of ItemID's on the wish list
        @param item_id: ItemID of the item to be added to the wishlist

        @return the updated CSV

    */

    private static string AddItemToCSV(string csv, string item_id) {
        return csv + "," + item_id;
    }

    /* 

        See if a wish list CSV contains a specific ItemID.

        @param csv: the CSV list of ItemID's on the wish list
        @param item_id: ItemID of the item

        @return true if the wish list contains the item, false otherwise

    */

    private static bool WishlistContainsItem(string csv, string item_id) {
        /* Split CSV into an array of ItemIDs  */
        string[] items = csv.Split(',');
        int ind = Array.IndexOf(items, item_id); // item at which ItemID exists
        List<string> items_list = new List<string>(items); // Convert array to list for easy removal

        if( ind != -1 ) {
            return true;
        } else {
            return false;
        }

    }

    /* 

        Execute a PlayFab Cloud Script function to update the group entity object data to the
        updated CSV. Title-level data should not be changed directly from the client.

        @param dataobj: the updated CSV; the Cloud Script function sets the entity group object data to
            this value.
        @param item_id: ItemID of the item that was either added or removed

    */

    private void UpdateGroupObject(string dataobj, bool adding_item, string item_id) {

        /* Call a Cloud Script function to update the group entity object data */
        PlayFabCloudScriptAPI.ExecuteEntityCloudScript(new PlayFab.CloudScriptModels.ExecuteEntityCloudScriptRequest() {
            
            // Group entity on which we call Cloud Script function
            Entity = new PlayFab.CloudScriptModels.EntityKey { Id = WishList.group_entityKeyId, Type = WishList.group_entityKeyType },
            // Cloud Script function name
            FunctionName = "addItemtoWishlist",
            // Function parameters for Cloud Script function; prop1 is the updated CSV
            FunctionParameter = new { prop1 = dataobj }, 
            // Create a Playstream event, which can be found in Game Manager; helpful for debugging and logging
            GeneratePlayStreamEvent = true

        }, result => {
            /* The Cloud Script function returned successfully, so we must update the store in our Unity game. */
            if( adding_item ) {
                /* The item with ItemID item_id was added, so update store accordingly. */
                StoreSetup.SetUpStore(item_id, false);

            } else {
                /* The item with ItemID item_id was removed, so update store accordingly. */
                StoreSetup.SetUpStore(item_id, true);

            }

        }, error => { Debug.LogError(error.GenerateErrorReport()); });

    }

    /* 

        Execute a PlayFab Cloud Script function to create an entity group for the player's wish list. We use
        Cloud Script because title-level data should not be changed directly from the client.

        @param group_name: the name of the entity group

    */

    private static void CreateWishlist(string group_name) {

        /* Execute Cloud Script function to create the entity group for the wishlist */
        PlayFabCloudScriptAPI.ExecuteEntityCloudScript(new PlayFab.CloudScriptModels.ExecuteEntityCloudScriptRequest() {
            /*  The entity is the player who should be the administrator and first member of the entity group; in our case, this is the player who
                owns the wish list */
            Entity = new PlayFab.CloudScriptModels.EntityKey { Id = LoginClass.getPlayerEntityKeyId(), Type = LoginClass.getPlayerEntityKeyType() },
            // The name of the Cloud Script function we are calling
            FunctionName = "createUserWishList",
            // The parameter provided to your function
            FunctionParameter = new { groupName = group_name },
            // Optional - Shows this event in PlayStream; helpful for logging and debugging
            GeneratePlayStreamEvent = true

        }, result => {

            JsonObject jsonResult = (JsonObject) result.FunctionResult;

            WishList.group_entityKeyId = (string) jsonResult["ek_id"];
            WishList.group_entityKeyType = (string) jsonResult["ek_type"];

        }, error => { Debug.LogError(error.GenerateErrorReport()); });

    }

    /*

        Execute a PlayFab Cloud Script function to add another PlayFab player to the entity group as a member. This allows the added
        player to view the owner's wishlist.

        @param playfabid: the PlayFab ID of the player who we want to add to the wish list entity group

    */

    public static void AddPlayFabIdToGroup(string playfabid) {
        /*  The Cloud Script function adds the member with the corresponding PlayFab ID to the entity group, so they
            can view the wish list. */
        PlayFabCloudScriptAPI.ExecuteEntityCloudScript(new PlayFab.CloudScriptModels.ExecuteEntityCloudScriptRequest() {
            // The entity key for the group to whom we want to add a player
            Entity = new PlayFab.CloudScriptModels.EntityKey { Id = group_entityKeyId, Type = group_entityKeyType },
            // The name of the Cloud Script function we are calling
            FunctionName = "addPlayFabIdToGroup",
            // The parameter provided to your function
            FunctionParameter = new {id = playfabid},
            // Optional - Shows this event in PlayStream; helpful for logging and debugging
            GeneratePlayStreamEvent = true

        }, result => {


        }, error => { Debug.LogError(error.GenerateErrorReport()); });
    }


}
