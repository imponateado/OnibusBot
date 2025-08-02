namespace OnibusBot.Utils;

public class CoordConverter
{
    private const double PI = Math.PI;
    private const double SM_A = 6378137.0;
    private const double SM_B = 6356752.314;
    private const double SM_ECC_SQUARED = 6.69437999013e-03;
    private const double UTM_SCALE_FACTOR = 0.9996;

    private static double DegToRad(double deg)
    {
        return deg / 180.0 * PI;
    }

    private static double RadToDeg(double rad)
    {
        return rad / PI * 180.0;
    }

    private static double ArcLengthOfMeridian(double phi)
    {
        double n = (SM_A - SM_B) / (SM_A + SM_B);
        double alpha = ((SM_A + SM_B) / 2.0) * (1.0 + (Math.Pow(n, 2.0) / 4.0) + (Math.Pow(n, 4.0) / 64.0));
        double beta = (-3.0 * n / 2.0) + (9.0 * Math.Pow(n, 3.0) / 16.0) + (-3.0 * Math.Pow(n, 5.0) / 32.0);
        double gamma = (15.0 * Math.Pow(n, 2.0) / 16.0) + (-15.0 * Math.Pow(n, 4.0) / 32.0);
        double delta = (-35.0 * Math.Pow(n, 3.0) / 48.0) + (105.0 * Math.Pow(n, 5.0) / 256.0);
        double epsilon = (315.0 * Math.Pow(n, 4.0) / 512.0);
        double result = alpha * (phi + (beta * Math.Sin(2.0 * phi)) + (gamma * Math.Sin(4.0 * phi)) +
                                 (delta * Math.Sin(6.0 * phi)) + (epsilon * Math.Sin(8.0 * phi)));

        return result;
    }

    private static double UTMCentralMeridian(int zone)
    {
        return DegToRad(-183.0 + (zone * 6.0));
    }

    private static double FootpointLatitude(double y)
    {
        double n = (SM_A - SM_B) / (SM_A + SM_B);

        double alpha_ = ((SM_A + SM_B) / 2.0) *
                        (1 + (Math.Pow(n, 2.0) / 4) + (Math.Pow(n, 4.0) / 64));

        double y_ = y / alpha_;

        double beta_ = (3.0 * n / 2.0) + (-27.0 * Math.Pow(n, 3.0) / 32.0) +
                       (269.0 * Math.Pow(n, 5.0) / 512.0);

        double gamma_ = (21.0 * Math.Pow(n, 2.0) / 16.0) +
                        (-55.0 * Math.Pow(n, 4.0) / 32.0);

        double delta_ = (151.0 * Math.Pow(n, 3.0) / 96.0) +
                        (-417.0 * Math.Pow(n, 5.0) / 128.0);

        double epsilon_ = (1097.0 * Math.Pow(n, 4.0) / 512.0);

        double result = y_ + (beta_ * Math.Sin(2.0 * y_)) +
                        (gamma_ * Math.Sin(4.0 * y_)) +
                        (delta_ * Math.Sin(6.0 * y_)) +
                        (epsilon_ * Math.Sin(8.0 * y_));

        return result;
    }

    private static void MapXYToLatLon(double x, double y, double lambda0, out double phi, out double lambda)
    {
        double phif = FootpointLatitude(y);

        double ep2 = (Math.Pow(SM_A, 2.0) - Math.Pow(SM_B, 2.0)) / Math.Pow(SM_B, 2.0);
        double cf = Math.Cos(phif);
        double nuf2 = ep2 * Math.Pow(cf, 2.0);
        double Nf = Math.Pow(SM_A, 2.0) / (SM_B * Math.Sqrt(1 + nuf2));
        double Nfpow = Nf;

        double tf = Math.Tan(phif);
        double tf2 = tf * tf;
        double tf4 = tf2 * tf2;

        // Coeficientes fracionários para x**n
        double x1frac = 1.0 / (Nfpow * cf);

        Nfpow *= Nf;
        double x2frac = tf / (2.0 * Nfpow);

        Nfpow *= Nf;
        double x3frac = 1.0 / (6.0 * Nfpow * cf);

        Nfpow *= Nf;
        double x4frac = tf / (24.0 * Nfpow);

        Nfpow *= Nf;
        double x5frac = 1.0 / (120.0 * Nfpow * cf);

        Nfpow *= Nf;
        double x6frac = tf / (720.0 * Nfpow);

        Nfpow *= Nf;
        double x7frac = 1.0 / (5040.0 * Nfpow * cf);

        Nfpow *= Nf;
        double x8frac = tf / (40320.0 * Nfpow);

        // Coeficientes polinomiais para x**n
        double x2poly = -1.0 - nuf2;
        double x3poly = -1.0 - 2 * tf2 - nuf2;
        double x4poly = 5.0 + 3.0 * tf2 + 6.0 * nuf2 - 6.0 * tf2 * nuf2 -
                        3.0 * (nuf2 * nuf2) - 9.0 * tf2 * (nuf2 * nuf2);
        double x5poly = 5.0 + 28.0 * tf2 + 24.0 * tf4 + 6.0 * nuf2 + 8.0 * tf2 * nuf2;
        double x6poly = -61.0 - 90.0 * tf2 - 45.0 * tf4 - 107.0 * nuf2 + 162.0 * tf2 * nuf2;
        double x7poly = -61.0 - 662.0 * tf2 - 1320.0 * tf4 - 720.0 * (tf4 * tf2);
        double x8poly = 1385.0 + 3633.0 * tf2 + 4095.0 * tf4 + 1575 * (tf4 * tf2);

        // Calcular latitude
        phi = phif + x2frac * x2poly * (x * x) +
              x4frac * x4poly * Math.Pow(x, 4.0) +
              x6frac * x6poly * Math.Pow(x, 6.0) +
              x8frac * x8poly * Math.Pow(x, 8.0);

        // Calcular longitude
        lambda = lambda0 + x1frac * x +
                 x3frac * x3poly * Math.Pow(x, 3.0) +
                 x5frac * x5poly * Math.Pow(x, 5.0) +
                 x7frac * x7poly * Math.Pow(x, 7.0);
    }

    public static List<double> UTMToLatLon(double x, double y, int zone, bool southHemi)
    {
        x -= 500000.0;
        x /= UTM_SCALE_FACTOR;

        if (southHemi)
            y -= 10000000.0;

        y /= UTM_SCALE_FACTOR;

        double cmeridian = UTMCentralMeridian(zone);
        MapXYToLatLon(x, y, cmeridian, out double phi, out double lambda);

        var latitude = RadToDeg(phi);
        var longitude = RadToDeg(lambda);
        
        var coords = new List<double> { latitude, longitude };
        return coords;
        ;
    }
}
