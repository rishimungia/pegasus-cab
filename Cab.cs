using GTA;
using GTA.Math;
using GTA.Native;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace OnlineCab
{
    public class Cab : Script
    {
        internal static Vehicle cab;
        internal static Ped driver;

        internal static CabAction cabAction;
        internal static int currentCabType;
        internal static bool cabActive;
        internal static bool isRushed, canRush, rideSkipped;
        private static bool hideControls, phoneActive;
        private static int cabTimeOut, rushEndTime;

        private static readonly float spawnDistance = 75.0f;
        private static readonly int spawnDriveStyle = 1076631591;
        private static readonly int normalDriveStyle = 1076369579;
        private static readonly float normalDriveSpeed = 20.0f;
        private static readonly int rushedDriveStyle = 1076631591;
        private static readonly float rushedDriveSpeed = 25.0f;

        internal static Vector3 pickupLocation;
        internal static Vector3 rideDestination;
        private static Blip destinationBlip;

        internal static List<Utils.CabTypeData> cabTypes = new List<Utils.CabTypeData>();

        private static readonly Vector2[] cabSpawn =
        {
            new Vector2(1, 1),
            new Vector2(1, -1),
            new Vector2(-1, 1),
            new Vector2(-1, -1)
        };

        public Cab()
        {
            Utils.LoadXml();
            Menu.SetupMenu();
            Phone.SetupPhone();

            Tick += OnTick;
            KeyUp += OnKeyUp;
            KeyDown += OnKeyDown;

            GTA.UI.Notification.Show(
                GTA.UI.NotificationIcon.PegasusDelivery,
                "Pegasus Cab",
                "Contact Added",
                "Pegasus Cabs now available through your phone."
            );
        }

        void OnTick(object sender, EventArgs e)
        {
            Phone._iFruit.Update();
            Menu.menuPool.Process();

            if (cabActive)
            {
                ProcessCab();

                HandleCleanup();

                if (Game.IsControlJustReleased(GTA.Control.VehicleHeadlight) && cabAction == CabAction.DropPlayer) hideControls = !hideControls;

                phoneActive = Function.Call<bool>(Hash.IS_PED_RUNNING_MOBILE_PHONE_TASK, Game.Player.Character);

                if (!hideControls && !phoneActive)
                {
                    if (Game.IsControlJustReleased(GTA.Control.Duck) && cabAction == CabAction.DropPlayer) PauseRide();

                    if (Game.IsControlJustReleased(GTA.Control.Jump) && canRush) RushCab();

                    if (Game.IsControlJustReleased(GTA.Control.MoveRight)) UpdateDestination();

                    if (Game.IsControlJustReleased(GTA.Control.Sprint) && cabAction == CabAction.DropPlayer) SkipRide();
                }
            }

            if (cabActive && !hideControls && !phoneActive && Game.Player.Character.IsInVehicle(cab)) Menu.DrawScalformMenu();  
        }

        void OnKeyUp(object sender, KeyEventArgs e)
        {
            
        }

        void OnKeyDown(object sender, KeyEventArgs e)
        {
            
        }

        internal static void OrderCab (int type)
        {
            if (!cabActive)
            {
                currentCabType = type;

                Fare.ResetFare();

                cabAction = CabAction.SpwanCab;
                rideSkipped = false;
                isRushed = false;

                cabActive = true;
                Menu.cancelOption.Enabled = true;
                hideControls = false;
            }
            else
            {
                GTA.UI.Notification.Show(
                    GTA.UI.NotificationIcon.PegasusDelivery,
                    "Pegasus Cab",
                    "Ride Active",
                    "A trip is already active."
                );
            }
        }

        internal static void CancelCab ()
        {
            if (cabActive && Game.Player.Character.IsInVehicle(cab))
            {
                if (cabAction == CabAction.AskLocation)
                {
                    cabActive = false;

                    Game.Player.Character.Task.LeaveVehicle();

                    GTA.UI.Notification.Show(
                        GTA.UI.NotificationIcon.PegasusDelivery,
                        "Pegasus Cab",
                        "Ride Cancelled",
                        "Your ride was canceled."
                    );

                    Wait(2000);

                    driver.Task.ClearAll();
                    driver.Task.CruiseWithVehicle(cab, 15.0f, DrivingStyle.Normal);

                    driver.MarkAsNoLongerNeeded();
                    cab.MarkAsNoLongerNeeded();
                }

                else if (cabAction == CabAction.ReachLocation || cabAction == CabAction.DropPlayer || cabAction == CabAction.PauseRide)
                {
                    PauseRide();
                }

                else
                {
                    Game.Player.Character.Task.LeaveVehicle();
                    GTA.UI.Notification.Show(
                        GTA.UI.NotificationIcon.PegasusDelivery,
                        "Pegasus Cab",
                        "Invalid Request",
                        "No active ride found!"
                    );
                }
            }
            else
            {
                cabActive = false;

                GTA.UI.Notification.Show(
                    GTA.UI.NotificationIcon.PegasusDelivery,
                    "Pegasus Cab",
                    "Ride Cancelled",
                    "Your ride was canceled."
                );

                driver.Task.ClearAll();
                driver.Task.CruiseWithVehicle(cab, 15.0f, DrivingStyle.Normal);

                cab.AttachedBlip?.Delete();

                driver.MarkAsNoLongerNeeded();
                cab.MarkAsNoLongerNeeded();
            }

            Menu.cancelOption.Enabled = false;
        }

        internal static void DeleteCab ()
        {
            cab?.Delete();
            driver?.Delete();

            cabActive = false;
        }

        internal static void HandleCleanup ()
        {
            if (cabAction == CabAction.GetPlayer && (Game.GameTime >= cabTimeOut || Game.Player.Character.Position.DistanceTo(pickupLocation) > 100.0f))
            {
                cabActive = false;
                canRush = false;
                isRushed = false;
                cab.AttachedBlip.Delete();

                driver.Task.ClearAll();
                driver.Task.CruiseWithVehicle(cab, 15f, DrivingStyle.Normal);

                cab.MarkAsNoLongerNeeded();
                driver.MarkAsNoLongerNeeded();

                GTA.UI.Notification.Show(
                    GTA.UI.NotificationIcon.PegasusDelivery,
                    "Pegasus Cab",
                    "Ride Update",
                    "Your ride had to be canceled. You won't be charged for the ride."
                );
            }
            if ((cabAction == CabAction.DropPlayer) && (Game.IsControlJustReleased(GTA.Control.VehicleExit) || !Game.Player.Character.IsInVehicle(cab)))
            {
                GTA.UI.Notification.Show(
                    GTA.UI.NotificationIcon.PegasusDelivery,
                    "Pegasus Cab",
                    "Ride Update",
                    "Your ride has ended." +
                    "~n~Ride Fare: ~g~" + "$" + Fare.GetTotalFare() +
                    "~n~~w~Thank you for riding with us!"
                );
                Game.Player.Money -= Fare.GetTotalFare();


                cabActive = false;
                canRush = false;
                isRushed = false;
                destinationBlip?.Delete();

                driver.Task.ClearAll();
                driver.Task.CruiseWithVehicle(cab, 15f, DrivingStyle.Normal);

                cab.MarkAsNoLongerNeeded();
                driver.MarkAsNoLongerNeeded();
            }
            if (cab != null && driver != null)
            {
                if (cab.IsConsideredDestroyed || cab.EngineHealth == 0)
                {
                    GTA.UI.Notification.Show(
                        GTA.UI.NotificationIcon.PegasusDelivery,
                        "Pegasus Cab",
                        "Ride Update",
                        "Your ride had to be canceled. You won't be charged for the ride."
                    );

                    cabActive = false;
                    canRush = false;
                    isRushed = false;
                    destinationBlip?.Delete();

                    if (driver.IsAlive)
                    {
                        driver.Task.ClearAll();
                        driver.Task.WanderAround();
                    }

                    cab.MarkAsNoLongerNeeded();
                    driver.MarkAsNoLongerNeeded();
                }
                if (driver.IsDead || !driver.IsInVehicle(cab))
                {
                    GTA.UI.Notification.Show(
                        GTA.UI.NotificationIcon.PegasusDelivery,
                        "Pegasus Cab",
                        "Ride Update",
                        "Your ride had to be canceled. You won't be charged for the ride."
                    );

                    cabActive = false;
                    canRush = false;
                    isRushed = false;
                    destinationBlip?.Delete();

                    if (driver.IsAlive) driver.Task.ClearAll();

                    cab.MarkAsNoLongerNeeded();
                    driver.MarkAsNoLongerNeeded();
                }
            }

        }

        internal static void RushCab (bool endRush = false)
        {
            if (cabActive && cabAction == CabAction.DropPlayer)
            {
                if (endRush)
                {
                    Function.Call(Hash.TASK_VEHICLE_DRIVE_TO_COORD_LONGRANGE,
                        driver, cab,
                        rideDestination.X,
                        rideDestination.Y,
                        rideDestination.Z,
                        normalDriveSpeed,
                        normalDriveStyle,
                        10f
                    );

                    isRushed = false;
                    canRush = true;
                }
                else if (canRush)
                {
                    Utils.DriverSpeech("TAXID_SPEED_UP");

                    Function.Call(Hash.TASK_VEHICLE_DRIVE_TO_COORD_LONGRANGE,
                        driver, cab,
                        rideDestination.X,
                        rideDestination.Y,
                        rideDestination.Z,
                        rushedDriveSpeed,
                        rushedDriveStyle,
                        10f
                    );

                    isRushed = true;
                    canRush = false;
                    rushEndTime = Game.GameTime + 60000;
                }
            }
        }

        internal static void PauseRide ()
        {
            if (cabActive && cabAction == CabAction.DropPlayer)
            {
                driver.Task.ClearAll();
                Function.Call(Hash.TASK_VEHICLE_TEMP_ACTION,
                    driver,
                    cab,
                    27, 999999
                );

                isRushed = false;
                canRush = false;
                hideControls = false;

                cabAction = CabAction.PauseRide;

                Utils.DriverSpeech("TAXID_GET_OUT_EARLY");
                Wait(250);
            }
        }

        internal static void ResumeRide ()
        {
            if (cabActive && cabAction == CabAction.PauseRide)
            {
                Function.Call(Hash.TASK_VEHICLE_DRIVE_TO_COORD_LONGRANGE,
                    driver, cab,
                    rideDestination.X,
                    rideDestination.Y,
                    rideDestination.Z,
                    normalDriveSpeed,
                    normalDriveStyle,
                    10f
                );

                canRush = true;

                cabAction = CabAction.DropPlayer;

                Utils.DriverSpeech("TAXID_BEGIN_JOURNEY");
                Wait(250);
            }
        }

        internal static void UpdateDestination ()
        {
            if (Game.IsWaypointActive && (cabAction == CabAction.DropPlayer || cabAction == CabAction.PauseRide))
            {
                driver.Task.ClearAll();
                cab.StopBringingToHalt();

                rideDestination = World.GetNextPositionOnStreet(World.WaypointPosition);
                Function.Call(Hash.TASK_VEHICLE_DRIVE_TO_COORD_LONGRANGE,
                    driver, cab,
                    rideDestination.X,
                    rideDestination.Y,
                    rideDestination.Z,
                    normalDriveSpeed,
                    normalDriveStyle,
                    10f
                );

                destinationBlip?.Delete();
                destinationBlip = World.CreateBlip(rideDestination);
                destinationBlip.Name = "Destination";
                destinationBlip.Sprite = BlipSprite.PointOfInterest;
                destinationBlip.Scale = 1.25f;
                destinationBlip.Color = BlipColor.Blue;
                destinationBlip.ShowRoute = true;

                World.RemoveWaypoint();

                isRushed = false;
                canRush = true;

                Utils.DriverSpeech("TAXID_CHANGE_DEST");

                cabAction = CabAction.DropPlayer;

                GTA.UI.Notification.Show(
                    GTA.UI.NotificationIcon.PegasusDelivery,
                    "Pegasus Cab",
                    "Ride Update",
                    "Your ride destination has been updated." +
                    "~n~New Destination: ~HUD_COLOUR_PURPLELIGHT~" + World.GetStreetName(rideDestination) +
                    "~n~~w~Estimated Fare: ~g~" + "$" + Fare.EstimateFare(true) +
                    "~n~~w~Enjoy your ride."
                );
            }
        }

        internal static void SkipRide ()
        {
            cabAction = CabAction.SkipRideFadeOut;
            rideSkipped = true;
        }

        internal static void ProcessCab ()
        {
            switch (cabAction)
            {
                case CabAction.SpwanCab:
                    {
                        // Get Cab Spawn Position
                        Vector2 randomSpawn = cabSpawn[Utils.GetRandomInt(0, 4)];
                        Vector3 vehicleSpawn = new Vector3(
                            Game.Player.Character.Position.X + (randomSpawn.X * spawnDistance), 
                            Game.Player.Character.Position.Y + (randomSpawn.Y * spawnDistance), 
                            Game.Player.Character.Position.Z
                        );
                        
                        // Spawn Vehicle
                        Model cabModel = new Model(Game.GenerateHash(cabTypes[currentCabType].vehicles[Utils.GetRandomInt(0, cabTypes[currentCabType].vehicles.Count)]));
                        cab = World.CreateVehicle(cabModel, vehicleSpawn);
                        cabModel.MarkAsNoLongerNeeded();

                        cab.PlaceOnNextStreet();
                        if (cabTypes[currentCabType].black)
                        {
                            cab.Mods.PrimaryColor = VehicleColor.MetallicBlack;
                            cab.Mods.SecondaryColor = VehicleColor.MetallicBlack;
                            cab.Mods.PearlescentColor = VehicleColor.MetallicBlack;
                        }

                        cab.AddBlip();
                        cab.AttachedBlip.Sprite = BlipSprite.Cab;
                        cab.AttachedBlip.ShowsHeadingIndicator = true;
                        cab.AttachedBlip.Color = BlipColor.Blue;
                        cab.AttachedBlip.Name = "Pegasus Cab";

                        // Spawn Driver Ped
                        Model driverModel = new Model(Game.GenerateHash(cabTypes[currentCabType].drivers[Utils.GetRandomInt(0, cabTypes[currentCabType].drivers.Count)]));
                        driver = World.CreatePed(driverModel, vehicleSpawn);
                        driverModel.MarkAsNoLongerNeeded();

                        driver.SetIntoVehicle(cab, VehicleSeat.Driver);
                        driver.CanBeDraggedOutOfVehicle = false;
                        driver.CanBeKnockedOffBike = false;

                        Function.Call(Hash.SET_DRIVER_ABILITY, driver, 0.75f);

                        // Drive Task
                        pickupLocation = World.GetNextPositionOnStreet(Game.Player.Character.Position);
                        Function.Call(Hash.TASK_VEHICLE_DRIVE_TO_COORD_LONGRANGE,
                            driver, cab,
                            pickupLocation.X,
                            pickupLocation.Y,
                            pickupLocation.Z,
                            16.0f,
                            spawnDriveStyle,
                            7.5f
                        );

                        if (Game.IsWaypointActive) rideDestination = World.GetNextPositionOnStreet(World.WaypointPosition);
                        GTA.UI.Notification.Show(
                            GTA.UI.NotificationIcon.PegasusDelivery, 
                            "Pegasus Cab", 
                            "Cab Booked",
                            "Vehicle: ~b~" + Game.GetLocalizedString(cab.DisplayName) +
                            "~n~~w~Plate: ~y~" + Function.Call<string>(Hash.GET_VEHICLE_NUMBER_PLATE_TEXT, cab) +
                            "~n~~w~Type: ~b~" + cabTypes[currentCabType].name +
                            "~n~~w~Your driver will reach your location soon."
                        );

                        cabAction = CabAction.ReachPlayer;
                    }; break;
                case CabAction.ReachPlayer:
                    {
                        if (cab.Position.DistanceTo(pickupLocation) <= 10.0f)
                        {
                            cab.AttachedBlip.IsFlashing = true;

                            driver.Task.ClearAll();
                            Function.Call(Hash.TASK_VEHICLE_TEMP_ACTION,
                                driver,
                                cab,
                                27, 999999
                            );

                            cab.SoundHorn(200);
                            Wait(500);
                            cab.SoundHorn(200);

                            GTA.UI.Notification.Show(
                                GTA.UI.NotificationIcon.PegasusDelivery,
                                "Pegasus Cab",
                                "Ride Update",
                                "Your driver is here to pick you up."
                            );

                            cabTimeOut = 300000 + Game.GameTime;

                            cabAction = CabAction.GetPlayer;
                        }
                        if (cab.Position.DistanceTo(Game.Player.Character.Position) <= 5.0f && cabAction == CabAction.ReachPlayer)
                        {
                            driver.Task.ClearAll();
                            Function.Call(Hash.TASK_VEHICLE_TEMP_ACTION,
                                driver,
                                cab,
                                27, 999999
                            );

                            cab.AttachedBlip.IsFlashing = true;

                            cab.SoundHorn(200);
                            Wait(500);
                            cab.SoundHorn(200);

                            GTA.UI.Notification.Show(
                                GTA.UI.NotificationIcon.PegasusDelivery,
                                "Pegasus Cab",
                                "Ride Update",
                                "Your driver is here to pick you up."
                            );

                            cabTimeOut = 300000 + Game.GameTime;

                            cabAction = CabAction.GetPlayer;
                        }
                    }; break;
                case CabAction.GetPlayer:
                    {
                        if (Game.Player.Character.Position.DistanceTo(cab.Position) <= 3.5f)
                        {
                            Game.DisableControlThisFrame(GTA.Control.VehicleExit);
                            Game.DisableControlThisFrame(GTA.Control.Enter);
                            GTA.UI.Screen.ShowHelpTextThisFrame("Press ~INPUT_ENTER~ to enter the ~b~cab");
                        }
                        if (Game.IsControlJustReleased(GTA.Control.Enter) && Game.Player.Character.Position.DistanceTo(cab.Position) <= 3.5f)
                        {
                            bool backSeat = Function.Call<int>(Hash.GET_VEHICLE_MODEL_NUMBER_OF_SEATS, cab.Model.Hash) >= 4;
                            Game.Player.Character.Task.EnterVehicle(cab, backSeat ? VehicleSeat.RightRear : VehicleSeat.Any, -1, 1, EnterVehicleFlags.DontJackAnyone);
                        }
                        if (Game.Player.Character.IsInVehicle(cab))
                        {
                            if (Game.IsWaypointActive) cabAction = CabAction.ReachLocation;
                            else
                            {
                                cabAction = CabAction.AskLocation;
                                Utils.DriverSpeech("TAXID_WHERE_TO");
                            }
                            cab.AttachedBlip.Delete();
                        }
                    }; break;
                case CabAction.AskLocation:
                    {
                        if (!Game.IsWaypointActive)
                        {
                            GTA.UI.Screen.ShowHelpTextThisFrame("Set a ~q~Waypoint~w~ to start your ride");
                        }
                        else
                        {
                            cabAction = CabAction.ReachLocation;
                        }
                        if (!Game.Player.Character.IsInVehicle(cab))
                        {
                            cab.AddBlip();
                            cab.AttachedBlip.Sprite = BlipSprite.Cab;
                            cab.AttachedBlip.ShowsHeadingIndicator = true;
                            cab.AttachedBlip.Name = "Pegasus Cab";
                            cab.AttachedBlip.Color = BlipColor.Blue;
                            cab.AttachedBlip.IsFlashing = true;

                            cabAction = CabAction.GetPlayer;
                            cabTimeOut = 300000 + Game.GameTime;
                        }
                    }; break;
                case CabAction.ReachLocation:
                    {
                        Menu.cancelOption.Enabled = false;

                        rideDestination = World.GetNextPositionOnStreet(World.WaypointPosition);
                        Function.Call(Hash.TASK_VEHICLE_DRIVE_TO_COORD_LONGRANGE,
                            driver, cab,
                            rideDestination.X,
                            rideDestination.Y,
                            rideDestination.Z,
                            normalDriveSpeed,
                            normalDriveStyle,
                            10f
                        );

                        destinationBlip?.Delete();
                        destinationBlip = World.CreateBlip(rideDestination);
                        destinationBlip.Name = "Destination";
                        destinationBlip.Sprite = BlipSprite.PointOfInterest;
                        destinationBlip.Scale = 1.25f;
                        destinationBlip.Color = BlipColor.Blue;
                        destinationBlip.ShowRoute = true;

                        World.RemoveWaypoint();

                        Utils.DriverSpeech("TAXID_BEGIN_JOURNEY");

                        GTA.UI.Notification.Show(
                            GTA.UI.NotificationIcon.PegasusDelivery,
                            "Pegasus Cab",
                            "Ride Update",
                            "Your ride has started." +
                            "~n~Destination: ~HUD_COLOUR_PURPLELIGHT~" + World.GetStreetName(rideDestination) +
                            "~n~~w~Estimated Fare: ~g~" + "$" + Fare.EstimateFare() +
                            "~n~~w~Enjoy your ride."
                        );

                    canRush = true;
                        cabAction = CabAction.DropPlayer;
                    }; break;
                case CabAction.DropPlayer:
                    {
                        float remainingDistance = cab.Position.DistanceTo(rideDestination);
                        if (cab.HasCollided && remainingDistance <= 30.0f)
                        {
                            canRush = false;
                            isRushed = false;
                            driver.Task.ClearAll();
                            cab.StopBringingToHalt();

                            Utils.DriverSpeech("TAXID_ARRIVE_AT_DEST");

                            GTA.UI.Notification.Show(
                                GTA.UI.NotificationIcon.PegasusDelivery,
                                "Pegasus Cab",
                                "Ride Update",
                                "You have arrived at your destination." +
                                "~n~Ride Fare: ~g~" + "$" + Fare.GetTotalFare() +
                                "~n~~w~Thank you for riding with us!"
                            );
                            Game.Player.Money -= Fare.GetTotalFare();

                            destinationBlip?.Delete();

                            cabAction = CabAction.ClearCab;
                        }
                        if (canRush && remainingDistance <= 50.0f)
                        {
                            RushCab(true);
                            canRush = false;
                        }
                        if (remainingDistance <= 30.0f && cab.IsStopped)
                        {
                            Utils.DriverSpeech("TAXID_ARRIVE_AT_DEST");

                            GTA.UI.Notification.Show(
                                GTA.UI.NotificationIcon.PegasusDelivery,
                                "Pegasus Cab",
                                "Ride Update",
                                "You have arrived at your destination." +
                                "~n~Ride Fare: ~g~" + "$" + Fare.GetTotalFare() +
                                "~n~~w~Thank you for riding with us!"
                            );
                            Game.Player.Money -= Fare.GetTotalFare();

                            destinationBlip?.Delete();

                            cabAction = CabAction.ClearCab;
                        }
                    }; break;
                case CabAction.PauseRide:
                    {
                        if (Game.IsControlJustReleased(GTA.Control.Duck))
                        {
                            ResumeRide();
                        }
                        if (Game.IsControlJustReleased(GTA.Control.VehicleExit))
                        {
                            GTA.UI.Notification.Show(
                                GTA.UI.NotificationIcon.PegasusDelivery,
                                "Pegasus Cab",
                                "Ride Update",
                                "Your ride has ended." +
                                "~n~Ride Fare: ~g~" + "$" + Fare.GetTotalFare() +
                                "~n~~w~Thank you for riding with us!"
                            );
                            Game.Player.Money -= Fare.GetTotalFare();
                            cabAction = CabAction.ClearCab;
                            destinationBlip?.Delete();
                        }
                    }; break;
                case CabAction.SkipRideFadeOut:
                    {
                        GTA.UI.Screen.FadeOut(500);
                        Game.DisableAllControlsThisFrame();
                        Wait(1000);
                        cabAction = CabAction.SkipRideTeleport;
                    }; break;
                case CabAction.SkipRideTeleport:
                    {
                        Game.DisableAllControlsThisFrame();

                        driver.Task.ClearAllImmediately();
                        cab.StopBringingToHalt();
                        canRush = false;

                        cab.Position = rideDestination;
                        cab.PlaceOnNextStreet();
                        driver.SetIntoVehicle(cab, VehicleSeat.Driver);

                        Wait(4000);

                        Function.Call(Hash.TASK_VEHICLE_TEMP_ACTION,
                            driver,
                            cab,
                            9,
                            1000
                        );

                        GTA.UI.Screen.FadeIn(500);
                        Wait(1500);

                        cabAction = CabAction.SkipRideFadeIn;
                    }; break;
                case CabAction.SkipRideFadeIn:
                    {
                        Game.DisableAllControlsThisFrame();

                        driver.Task.ClearAll();
                        Function.Call(Hash.TASK_VEHICLE_TEMP_ACTION,
                            driver,
                            cab,
                            27,
                            999999
                        );

                        GTA.UI.Notification.Show(
                            GTA.UI.NotificationIcon.PegasusDelivery,
                            "Pegasus Cab",
                            "Ride Update",
                            "You have arrived at your destination." +
                            "~n~Ride Fare: ~g~" + "$" + Fare.GetTotalFare() +
                            "~n~~w~Thank you for riding with us!"
                        );

                        Game.Player.Money -= Fare.GetTotalFare();

                        destinationBlip?.Delete();

                        Game.Player.Character.Task.LeaveVehicle();

                        cabAction = CabAction.ClearCab;
                    }; break;
                case CabAction.ClearCab:
                    {
                        if (!Game.Player.Character.IsInVehicle(cab))
                        {
                            Wait(2000);

                            driver.Task.CruiseWithVehicle(cab, 35, DrivingStyle.Normal);

                            driver.MarkAsNoLongerNeeded();
                            cab.MarkAsNoLongerNeeded();

                            cabActive = false;
                        }
                    }; break;
            }
        }

        internal enum CabAction
        {
            SpwanCab,
            ReachPlayer,
            GetPlayer,
            AskLocation,
            ReachLocation,
            DropPlayer,
            PauseRide,
            SkipRideFadeOut,
            SkipRideTeleport,
            SkipRideFadeIn,
            ClearCab
        };

    }
}
