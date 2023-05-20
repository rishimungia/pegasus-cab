using GTA;
using GTA.Native;
using System.Collections.Generic;
using System.Xml;
using System.Globalization;
using System;

namespace OnlineCab
{
    public class Utils : Script
    {
        private static XmlDocument _xml = new XmlDocument();

        internal static void LoadXml()
        {
            // Load the XML file from a specified path
            _xml.Load(@"scripts\OnlineCab\OnlineCabData.xml");

            // Get the root node of the XML document
            XmlNode rootNode = _xml.DocumentElement;

            XmlNodeList cabTypeNodes = rootNode.SelectNodes("CabType");

            foreach (XmlNode node in cabTypeNodes)
            {
                CabTypeData cabTypeData = new CabTypeData
                {
                    name = node.Attributes["name"].Value,
                    description = node.Attributes["description"].Value,
                    baseFare = int.Parse(node.Attributes["baseFare"].Value, CultureInfo.InvariantCulture),
                    fareMultiplier = int.Parse(node.Attributes["fareMultiplier"].Value, CultureInfo.InvariantCulture),
                    black = Convert.ToBoolean(node.Attributes["black"].Value)
                };

                XmlNodeList vehicleNodes = node.SelectNodes("Vehicles/Vehicle");
                XmlNodeList driverNodes = node.SelectNodes("Drivers/Ped");

                foreach (XmlNode vNode in vehicleNodes)
                {
                    cabTypeData.vehicles.Add(vNode.InnerText);
                }

                foreach (XmlNode dNode in driverNodes)
                {
                    cabTypeData.drivers.Add(dNode.InnerText);
                }

                Cab.cabTypes.Add(cabTypeData);
            }
        }

        internal static int GetRandomInt(int min, int max)
        {
            return Function.Call<int>(Hash.GET_RANDOM_INT_IN_RANGE, min, max);
        }

        internal static void DriverSpeech(string speech)
        {
            Cab.driver.PlayAmbientSpeech(speech, "A_M_M_EASTSA_02_LATINO_FULL_01", SpeechModifier.Force);
        }

        internal class CabTypeData
        {
            public string name;
            public string description;
            public int baseFare;
            public int fareMultiplier;
            public bool black;
            public List<string> vehicles = new List<string>();
            public List<string> drivers = new List<string>();
        }

    }
}
