using GTA;
using GTA.Math;
using System;

namespace OnlineCab
{
    public class Fare : Script
    {
        private static float baseDistance = 100.0f;
        private static float distancePointUnit = 100.0f;

        private static float remainingBaseDistance = 100.0f;
        private static int savedDistancePoints = 0;
        private static Vector3 lastRerouteLocation;
        private static bool didReroute = false;

        private static int baseFare = 5;
        private static int fareMultiplier = 1;
        private static int skipCost = 5;

        internal static int EstimateFare (bool reroute = false)
        {
            if (reroute)
            {
                float distanceTravelled = didReroute ? 
                    Math.Max(Vector3.Distance(lastRerouteLocation, Cab.cab.Position) - remainingBaseDistance, 0.0f): 
                    Math.Max(Vector3.Distance(Cab.pickupLocation, Cab.cab.Position) - remainingBaseDistance, 0.0f);

                if (remainingBaseDistance > 0) remainingBaseDistance -= (didReroute ?
                    Vector3.Distance(lastRerouteLocation, Cab.cab.Position) : Vector3.Distance(Cab.pickupLocation, Cab.cab.Position));

                savedDistancePoints += (int)Math.Round(distanceTravelled / distancePointUnit);

                float distance = Vector3.Distance(Cab.cab.Position, Cab.rideDestination);

                int estimateDistancePoints = (int)Math.Round(distance / distancePointUnit);

                lastRerouteLocation = Cab.cab.Position;
                didReroute = true;

                return (baseFare + (savedDistancePoints * fareMultiplier) + (estimateDistancePoints * fareMultiplier));
            }
            else
            {
                float distance = Math.Max(Vector3.Distance(Cab.pickupLocation, Cab.rideDestination) - baseDistance, 0.0f);
                int estimateDistancePoints = (int)Math.Round(distance / distancePointUnit);

                return (baseFare + (estimateDistancePoints * fareMultiplier));
            }
        }

        internal static int GetTotalFare ()
        {
            float distanceTravelled = didReroute ?
                Math.Max(Vector3.Distance(lastRerouteLocation, Cab.cab.Position) - remainingBaseDistance, 0.0f):
                Math.Max(Vector3.Distance(Cab.pickupLocation, Cab.cab.Position) - remainingBaseDistance, 0.0f);

            int distancePoints = (int)Math.Round(distanceTravelled / distancePointUnit);

            return (baseFare + (savedDistancePoints * fareMultiplier) + (distancePoints * fareMultiplier) + (Cab.rideSkipped ? skipCost : 0));
        }

        internal static void ResetFare ()
        {
            baseFare = Cab.cabTypes[Cab.currentCabType].baseFare;
            fareMultiplier = Cab.cabTypes[Cab.currentCabType].fareMultiplier;

            savedDistancePoints = 0;
            remainingBaseDistance = baseDistance;
            didReroute = false;
        }

    }
}
