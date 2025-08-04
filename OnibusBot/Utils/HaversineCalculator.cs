using System;
using System.Collections.Generic;
using System.Linq;
using OnibusBot.Interfaces;

namespace OnibusBot.Utils;

public static class HaversineCalculator
{
    public static double HaversiniAlgorithm(double lat1, double lon1, double lat2, double lon2)
    {
        
        // Convert degrees to radians
        double lat1Rad = lat1 * Math.PI / 180;
        double lon1Rad = lon1 * Math.PI / 180;
        double lat2Rad = lat2 * Math.PI / 180;
        double lon2Rad = lon2 * Math.PI / 180;

        // Calculate differences
        double latDiff = lat2Rad - lat1Rad;
        double lonDiff = lon2Rad - lon1Rad;

        // Haversine formula
        double a = Math.Sin(latDiff / 2) * Math.Sin(latDiff / 2) +
                   Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                   Math.Sin(lonDiff / 2) * Math.Sin(lonDiff / 2);

        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        // Earth's radius in kilometers
        double earthRadius = 6371;
        return earthRadius * c;
    }
}