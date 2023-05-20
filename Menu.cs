using GTA;
using GTA.Native;
using LemonUI;
using LemonUI.Menus;
using System;

namespace OnlineCab
{
    public class Menu : Script
    {
        internal static ObjectPool menuPool;

        internal static NativeMenu menu;
        internal static NativeItem cancelOption;

        internal static void SetupMenu ()
        {
            menuPool = new ObjectPool();
            menu = new NativeMenu("Pegasus Cab", "By PIXELBIT");
            menuPool.Add(menu);

            for (int i = 0; i < Cab.cabTypes.Count; i++)
            {
                NativeItem cabItem = new NativeItem(Cab.cabTypes[i].name, Cab.cabTypes[i].description);
                menu.Add(cabItem);

                int type = i;
                
                cabItem.Activated += (object sender, EventArgs e) => {
                    Cab.OrderCab(type);
                    menu.Visible = false;
                };
            }
            
            NativeSeparatorItem separatorItem = new NativeSeparatorItem();
            menu.Add(separatorItem);

            cancelOption = new NativeItem("Cancel Ride");
            menu.Add(cancelOption);
            cancelOption.Enabled = false;
            cancelOption.Activated += (object sender, EventArgs e) => { 
                Cab.CancelCab(); 
                menu.Visible = false; 
            };

            //NativeItem deleteCab = new NativeItem("[Debug] Delete Cab");
            //menu.Add(deleteCab);
            //deleteCab.Activated += (object sender, EventArgs e) => { Cab.DeleteCab(); };
        }

        internal static void DrawScalformMenu ()
        {
            Scaleform scaleform = new Scaleform("instructional_buttons");
            scaleform.CallFunction("CLEAR_ALL");
            scaleform.CallFunction("TOGGLE_MOUSE_BUTTONS", 0);
            scaleform.CallFunction("CREATE_CONTAINER");

            if (Cab.canRush) scaleform.CallFunction(
                "SET_DATA_SLOT", 
                Game.IsWaypointActive ? 4 : 3,
                Function.Call<string>(Hash.GET_CONTROL_INSTRUCTIONAL_BUTTONS_STRING, 2, 22, 1),
                "Rush"
            );
            if (Cab.cabAction == Cab.CabAction.DropPlayer)
            {
                scaleform.CallFunction(
                    "SET_DATA_SLOT",
                    0,
                    Function.Call<string>(Hash.GET_CONTROL_INSTRUCTIONAL_BUTTONS_STRING, 2, 74, 1),
                    "Hide Controls"
                );
                scaleform.CallFunction(
                    "SET_DATA_SLOT",
                    1,
                    Function.Call<string>(Hash.GET_CONTROL_INSTRUCTIONAL_BUTTONS_STRING, 2, 36, 1),
                    "Stop Here"
                );
                if (Game.IsWaypointActive) scaleform.CallFunction(
                    "SET_DATA_SLOT",
                    2,
                    Function.Call<string>(Hash.GET_CONTROL_INSTRUCTIONAL_BUTTONS_STRING, 2, 35, 1),
                    "Change Destination"
                );
                if (!Cab.rideSkipped) scaleform.CallFunction(
                    "SET_DATA_SLOT",
                    Game.IsWaypointActive ? 3 : 2,
                    Function.Call<string>(Hash.GET_CONTROL_INSTRUCTIONAL_BUTTONS_STRING, 2, 21, 1),
                    "Skip (Extra Cost)"
                );
            }
            else if (Cab.cabAction == Cab.CabAction.PauseRide)
            {
                scaleform.CallFunction(
                    "SET_DATA_SLOT",
                    0,
                    Function.Call<string>(Hash.GET_CONTROL_INSTRUCTIONAL_BUTTONS_STRING, 2, 75, 1),
                    "Exit"
                );
                scaleform.CallFunction(
                    "SET_DATA_SLOT",
                    1,
                    Function.Call<string>(Hash.GET_CONTROL_INSTRUCTIONAL_BUTTONS_STRING, 2, 36, 1),
                    "Continue"
                );
                if (Game.IsWaypointActive) scaleform.CallFunction(
                    "SET_DATA_SLOT",
                    2,
                    Function.Call<string>(Hash.GET_CONTROL_INSTRUCTIONAL_BUTTONS_STRING, 2, 35, 1),
                    "Change Destination"
                );
            }

            scaleform.CallFunction("DRAW_INSTRUCTIONAL_BUTTONS", -1);
            scaleform.Render2D();
        }
    }
}
