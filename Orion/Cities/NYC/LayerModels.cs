using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Orion.Models;

namespace Orion.Cities.NYC
{
    public class LandUseModel : ShapeModel
    {
        public enum LandType
        {
            Residence,
            Commercial,
            Manufacturing,
            Mixed,
            PARK,
            Unknown
        }
        public enum LandTier
        {
            ResidenceLarge,
            ResidenceMedium,
            ResidenceSmall,
            ResidenceXSmall,
            CommercialLarge,
            CommercialMedium,
            CommercialSmall,
            MixedLarge,
            MixedMedium,
            MixedSmall,
            Mixed,
            Manufacturing,
            PARK,
            Airport,
            Unknown
        }

        Dictionary<string, LandType> ZoneTypes = new Dictionary<string, LandType>()
        {
            {"R", LandType.Residence },
            {"PC", LandType.Residence },
            {"C", LandType.Commercial },
            {"EC", LandType.Commercial },
            {"M", LandType.Manufacturing },
            {"MX",  LandType.Mixed},
            {"PARK", LandType.PARK },
            {"SV", LandType.PARK },
            {"SB", LandType.PARK },
            {"NA", LandType.PARK },
            {"PLAYGROUND", LandType.PARK },
            {"Other", LandType.Mixed }
        };

        public string ZoneDist;
        public LandType ZoneType
        { get
            {
                try
                {
                    return ZoneTypes[Regex.Match(ZoneDist, "[a-zA-Z]+").Value];
                }
                catch
                {
                    return ZoneTypes["Other"];
                }
            }
        }
        public string ZoneDensity
        {
            get
            {
                return Regex.Match(ZoneDist, @"\d").Value ?? "";
            }
        }

        public static LandType AssessLandType(LandUseModel[] landuses, int population)
        {
            int Rx = 0, Rs = 0, Rm = 0, Rl = 0, Cs = 0, Cm = 0, Cl = 0, Mix = 0, M = 0, P = 0;
            float margin = 0.3f;

            foreach (var use in landuses)
            {
                if (use.ZoneType == LandType.Residence && (use.ZoneDensity == "" || use.ZoneDensity == "1" || use.ZoneDensity == "2" || use.ZoneDensity == "3"))
                    Rx++;
                else if (use.ZoneType == LandType.Residence && (use.ZoneDensity == "4" || use.ZoneDensity == "5"))
                    Rs++;
                else if (use.ZoneType == LandType.Residence && (use.ZoneDensity == "6" || use.ZoneDensity == "7" || use.ZoneDensity == "8"))
                    Rm++;
                else if (use.ZoneType == LandType.Residence && (use.ZoneDensity == "9" || use.ZoneDensity == "10"))
                    Rl++;
                else if (use.ZoneType == LandType.Commercial && (use.ZoneDensity == "" || use.ZoneDensity == "1" || use.ZoneDensity == "2" || use.ZoneDensity == "3"))
                    Cs++;
                else if (use.ZoneType == LandType.Commercial && (use.ZoneDensity == "4" || use.ZoneDensity == "8"))
                    Cm++;
                else if (use.ZoneType == LandType.Commercial && (use.ZoneDensity == "5" || use.ZoneDensity == "6" || use.ZoneDensity == "7"))
                    Cl++;
                else if (use.ZoneType == LandType.Mixed)
                    Mix++;
                else if (use.ZoneType == LandType.Manufacturing)
                    M++;
                else if (use.ZoneType == LandType.PARK)
                    P++;
            }

            if (Cl != 0 && Rl != 0 && (Cl == Rl || Math.Abs(Cl - Rl) <= Math.Max(Cl, Rl) * margin))
                return LandType.Mixed;// LandTier.MixedLarge;
            else if (Cl > Rl)
                return LandType.Commercial;// LandTier.CommercialLarge;
            else if (Rl > Cl && population != 0)
                return LandType.Residence;// LandTier.ResidenceLarge;
            else if (Cm != 0 && Rm != 0 && (Cm == Rm || Math.Abs(Cm - Rm) <= Math.Max(Cm, Rm) * margin))
                return LandType.Mixed;// LandTier.MixedMedium;
            else if (Cm > Rm)
                return LandType.Commercial;// LandTier.CommercialMedium;
            else if (Rm > Cm && population != 0)
                return LandType.Residence;// LandTier.ResidenceMedium;
            else if (Mix > 0)
                return LandType.Mixed;// LandTier.MixedMedium;
            else if (Cs != 0 && Rs != 0 && (Cs == Rs || Math.Abs(Cs - Rs) <= Math.Max(Cs, Rs) * margin))
                return LandType.Mixed;// LandTier.MixedSmall;
            else if (Cs > Rs)
                return LandType.Commercial;// LandTier.CommercialSmall;
            else if (Rs > Cs && population != 0)
                return LandType.Residence;// LandTier.ResidenceSmall;
            else if (Rx > 0 && population != 0)
                return LandType.Residence;// LandTier.ResidenceXSmall;
            else if (P > 0)
                return LandType.PARK;
            else if (M > 0)
                return LandType.Manufacturing;
            else
                return LandType.Unknown;
        }

        public static int AssessLandDensity(LandUseModel[] landuses, int population)
        {
            int Rx = 0, Rs = 0, Rm = 0, Rl = 0, Cs = 0, Cm = 0, Cl = 0, Mix = 0, M = 0, P = 0;
            float margin = 0.3f;

            foreach (var use in landuses)
            {
                if (use.ZoneType == LandType.Residence && (use.ZoneDensity == "" || use.ZoneDensity == "1" || use.ZoneDensity == "2" || use.ZoneDensity == "3"))
                    Rx++;
                else if (use.ZoneType == LandType.Residence && (use.ZoneDensity == "4" || use.ZoneDensity == "5"))
                    Rs++;
                else if (use.ZoneType == LandType.Residence && (use.ZoneDensity == "6" || use.ZoneDensity == "7" || use.ZoneDensity == "8"))
                    Rm++;
                else if (use.ZoneType == LandType.Residence && (use.ZoneDensity == "9" || use.ZoneDensity == "10"))
                    Rl++;
                else if (use.ZoneType == LandType.Commercial && (use.ZoneDensity == "" || use.ZoneDensity == "1" || use.ZoneDensity == "2" || use.ZoneDensity == "3"))
                    Cs++;
                else if (use.ZoneType == LandType.Commercial && (use.ZoneDensity == "4" || use.ZoneDensity == "8"))
                    Cm++;
                else if (use.ZoneType == LandType.Commercial && (use.ZoneDensity == "5" || use.ZoneDensity == "6" || use.ZoneDensity == "7"))
                    Cl++;
                else if (use.ZoneType == LandType.Mixed)
                    Mix++;
                else if (use.ZoneType == LandType.Manufacturing)
                    M++;
                else if (use.ZoneType == LandType.PARK)
                    P++;
            }

            if (Cl != 0 && Rl != 0 && (Cl == Rl || Math.Abs(Cl - Rl) <= Math.Max(Cl, Rl) * margin))
                return 4;
            else if (Cl > Rl)
                return 4;
            else if (Rl > Cl && population != 0)
                return 4;
            else if (Cm != 0 && Rm != 0 && (Cm == Rm || Math.Abs(Cm - Rm) <= Math.Max(Cm, Rm) * margin))
                return 3;
            else if (Cm > Rm)
                return 3;
            else if (Rm > Cm && population != 0)
                return 3;
            else if (Mix > 0)
                return 3;
            else if (Cs != 0 && Rs != 0 && (Cs == Rs || Math.Abs(Cs - Rs) <= Math.Max(Cs, Rs) * margin))
                return 2;
            else if (Cs > Rs)
                return 2;
            else if (Rs > Cs && population != 0)
                return 2;
            else if (Rx > 0 && population != 0)
                return 1;
            else if (P > 0)
                return 1;
            else if (M > 0)
                return 1;
            else
                return 0;
        }

        public static LandTier AssessLandTier(LandUseModel[] landuses, int population)
        {
            int Rx = 0, Rs = 0, Rm = 0, Rl = 0, Cs = 0, Cm = 0, Cl = 0, Mix = 0, M = 0, P = 0;
            float margin = 0.5f;

            foreach (var use in landuses)
            {
                if (use.ZoneType == LandType.Residence && (use.ZoneDensity == "" || use.ZoneDensity == "1" || use.ZoneDensity == "2" || use.ZoneDensity == "3"))
                    Rx++;
                else if (use.ZoneType == LandType.Residence && (use.ZoneDensity == "4" || use.ZoneDensity == "5"))
                    Rs++;
                else if (use.ZoneType == LandType.Residence && (use.ZoneDensity == "6" || use.ZoneDensity == "7" || use.ZoneDensity == "8"))
                    Rm++;
                else if (use.ZoneType == LandType.Residence && (use.ZoneDensity == "9" || use.ZoneDensity == "10"))
                    Rl++;
                else if (use.ZoneType == LandType.Commercial && (use.ZoneDensity == "" || use.ZoneDensity == "1" || use.ZoneDensity == "2" || use.ZoneDensity == "3"))
                    Cs++;
                else if (use.ZoneType == LandType.Commercial && (use.ZoneDensity == "4" || use.ZoneDensity == "8"))
                    Cm++;
                else if (use.ZoneType == LandType.Commercial && (use.ZoneDensity == "5" || use.ZoneDensity == "6" || use.ZoneDensity == "7"))
                    Cl++;
                else if (use.ZoneType == LandType.Mixed)
                    Mix++;
                else if (use.ZoneType == LandType.Manufacturing)
                    M++;
                else if (use.ZoneType == LandType.PARK)
                    P++;
            }

            if (Cl != 0 && Rl != 0 && (Cl == Rl || Math.Abs(Cl - Rl) <= Math.Max(Cl, Rl) * margin))
                return LandTier.MixedLarge;
            else if (Cl > Rl)
                return LandTier.CommercialLarge;
            else if (Rl > Cl && population != 0)
                return LandTier.ResidenceLarge;
            else if (Cm != 0 && Rm != 0 && (Cm == Rm || Math.Abs(Cm - Rm) <= Math.Max(Cm, Rm) * margin))
                return LandTier.MixedMedium;
            else if (Cm > Rm)
                return LandTier.CommercialMedium;
            else if (Rm > Cm && population != 0)
                return LandTier.ResidenceMedium;
            else if (Mix > 0)
                return LandTier.MixedMedium;
            else if (Cs != 0 && Rs != 0 && (Cs == Rs || Math.Abs(Cs - Rs) <= Math.Max(Cs, Rs) * margin))
                return LandTier.MixedSmall;
            else if (Cs > Rs)
                return LandTier.CommercialSmall;
            else if (Rs > Cs && population != 0)
                return LandTier.ResidenceSmall;
            else if (Rx > 0 && population != 0)
                return LandTier.ResidenceXSmall;
            else if (M > 0)
                return LandTier.Manufacturing;
            else if (P > 0)
                return LandTier.PARK;
            else
                return LandTier.Unknown;
        }

        public static LandTier AssessLandTierV2(LandUseModel[] landuses, int population, string ntaname)
        {
            int Rx = 0, Rs = 0, Rm = 0, Rl = 0, Cs = 0, Cm = 0, Cl = 0, Mix = 0, M = 0, P = 0;

            if (ntaname.ToLower().Contains("park-cemetery-etc"))
                return LandTier.PARK;
            if (ntaname.ToLower().Contains("airport"))
                return LandTier.Airport;

            foreach (var use in landuses)
            {
                if (use.ZoneType == LandType.Residence && (use.ZoneDensity == "" || use.ZoneDensity == "1" || use.ZoneDensity == "2" || use.ZoneDensity == "3"))
                    Rx++;
                else if (use.ZoneType == LandType.Residence && (use.ZoneDensity == "4" || use.ZoneDensity == "5"))
                    Rs++;
                else if (use.ZoneType == LandType.Residence && (use.ZoneDensity == "6" || use.ZoneDensity == "7" || use.ZoneDensity == "8"))
                    Rm++;
                else if (use.ZoneType == LandType.Residence && (use.ZoneDensity == "9" || use.ZoneDensity == "10"))
                    Rl++;
                else if (use.ZoneType == LandType.Commercial && (use.ZoneDensity == "" || use.ZoneDensity == "1" || use.ZoneDensity == "2" || use.ZoneDensity == "3"))
                    Cs++;
                else if (use.ZoneType == LandType.Commercial && (use.ZoneDensity == "4" || use.ZoneDensity == "8"))
                    Cm++;
                else if (use.ZoneType == LandType.Commercial && (use.ZoneDensity == "5" || use.ZoneDensity == "6" || use.ZoneDensity == "7"))
                    Cl++;
                else if (use.ZoneType == LandType.Mixed)
                    Mix++;
                else if (use.ZoneType == LandType.Manufacturing)
                    M++;
                else if (use.ZoneType == LandType.PARK)
                    P++;
            }

            if (Cl > 0 && Rl > 0 && population > 0)
                return LandTier.MixedLarge;
            else if (Rl > 0 && population > 0)
                return LandTier.ResidenceLarge;
            else if (Cl > 0)
                return LandTier.CommercialLarge;
           
            else if (((Cm > 0 && Rm > 0)  || Mix > 0) && population > 0)
                return LandTier.MixedMedium;
            else if (Rm > 0 && population > 0)
                return LandTier.ResidenceMedium;
            else if (Cm > 0)
                return LandTier.CommercialMedium;
           
            else if (Cs > 0 && Rs > 0 && population > 0)
                return LandTier.MixedSmall;
            else if (Rs > 0 && population > 0)
                return LandTier.ResidenceSmall;
            else if (Cs > 0)
                return LandTier.CommercialSmall;

            else if (Rx > 0 && population > 0)
                return LandTier.ResidenceXSmall;
            else if (M > 0)
                return LandTier.Manufacturing;
            else if (P > 0)
                return LandTier.PARK;
            else
                return LandTier.Unknown;
        }

        public static LandTier AssessLandTierV3(LandUseModel[] landuses, int population, string ntaname)
        {
            int Rs = 0, Rm = 0, Rl = 0, Cm = 0, Cl = 0, Mix = 0, M = 0, P = 0;

            if (ntaname.ToLower().Contains("park-cemetery-etc"))
                return LandTier.PARK;
            if (ntaname.ToLower().Contains("airport"))
                return LandTier.Airport;

            foreach (var use in landuses)
            {
                if (use.ZoneType == LandType.Residence && 
                    (use.ZoneDensity == "" || use.ZoneDensity == "1" 
                    || use.ZoneDensity == "2" || use.ZoneDensity == "3"
                    || use.ZoneDensity == "4" || use.ZoneDensity == "5"))
                    Rs++;
                else if (use.ZoneType == LandType.Residence && (use.ZoneDensity == "6" || use.ZoneDensity == "7"))
                    Rm++;
                else if (use.ZoneType == LandType.Residence && (use.ZoneDensity == "8" || use.ZoneDensity == "9" || use.ZoneDensity == "10"))
                    Rl++;
                else if (use.ZoneType == LandType.Commercial && (use.ZoneDensity == "" || use.ZoneDensity == "1" || use.ZoneDensity == "2" || use.ZoneDensity == "3"))
                    Mix++;
                else if (use.ZoneType == LandType.Commercial && (use.ZoneDensity == "4" || use.ZoneDensity == "7" || use.ZoneDensity == "8"))
                    Cm++;
                else if (use.ZoneType == LandType.Commercial && (use.ZoneDensity == "5" || use.ZoneDensity == "6"))
                    Cl++;
                else if (use.ZoneType == LandType.Mixed)
                    Mix++;
                else if (use.ZoneType == LandType.Manufacturing)
                    M++;
                else if (use.ZoneType == LandType.PARK)
                    P++;
            }

            if (Cl > 0 && Rl > 0 && population > 0)
                return LandTier.Mixed;
            else if (Rl > 0 && population > 0)
                return LandTier.ResidenceLarge;
            else if (Cl > 0)
                return LandTier.CommercialLarge;

            else if (Mix > 0 && population > 0)
                return LandTier.Mixed;

            else if (Cm > 0 && Rm > 0 && population > 0)
                return LandTier.Mixed;
            else if (Rm > 0 && population > 0)
                return LandTier.ResidenceMedium;
            else if (Cm > 0)
                return LandTier.CommercialMedium;

            else if (Rs > 0 && population > 0)
                return LandTier.ResidenceSmall;     

            else if (M > 0)
                return LandTier.Manufacturing;
            else if (P > 0)
                return LandTier.PARK;
            else
                return LandTier.Unknown;
        }
    }

    public class CensusBlockModel : ShapeModel
    {
        public string bctcb2010;
        public string Boro_Name;

    }

    public class CensusTractModel : ShapeModel
    {
        public string boro_ct201;
        public string ntaname;
    }

    public class TaxiZoneModel: ShapeModel
    {
        public string Zone;
        public double LocationID;
        public string Borough;

        public static readonly double[] CityCenters =
        {
            /*CoOpCity*/ 51,
            /*DowntownBrooklyn*/ 65,
            /*Elmhurst*/ 82,
            /*EastNewYork*/ 76,
            /*HeartlandVillage*/ 118,
            /*Jamaica*/ 130,
            /*MelroseSouth*/ 159,
            /*MidtownSouth*/ 164,
            /*WorldTradeCenter*/ 261
        };
    }
}
