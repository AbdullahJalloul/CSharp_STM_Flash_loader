using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace Flashing
{
    class STProductdescription
    {
        static public Product prod;

        static public List<Sector> Sectors;
        string[] infofile;

        public STProductdescription(string FileName)
        {
            prod = new Product();
            Sectors = new List<Sector>();
            infofile = File.ReadAllLines(FileName);
            for (int i = 0; i < infofile.Length; i++)
            {
                infofile[i] = Regex.Replace(infofile[i], "(;;).+", String.Empty);
                infofile[i] = Regex.Replace(infofile[i], @"(\s)+|(\t)+", String.Empty);
                infofile[i] = Regex.Replace(infofile[i], "(;)", String.Empty);
            }

            productFilling();
            sectorsFilling();
        }


        private int productFilling()
        {
            Match match;
            int i;
            int indexstart = 0;
            int lenghttoget = 0;

            for (i = 0; i < infofile.Length; i++)
            {
                match = Regex.Match(infofile[i], @"^\[Product]");
                if (match.Success)
                {
                    i++;
                    break;
                }

            }

            for (int j = i; j < infofile.Length; j++)
            {
                indexstart = 0;

                if ((match = Regex.Match(infofile[j], @"^Name=")).Success)
                {
                    indexstart = match.Index + match.Length;
                    lenghttoget = infofile[j].Length - indexstart;
                    prod.name = infofile[j].Substring(indexstart, lenghttoget);

                }
                else if ((match = Regex.Match(infofile[j], @"^PID=")).Success)
                {
                    indexstart = match.Index + match.Length;
                    lenghttoget = infofile[j].Length - indexstart;
                    uint.TryParse(infofile[j].Substring(indexstart, lenghttoget), System.Globalization.NumberStyles.HexNumber, null, out prod.PID);

                }
                else if ((match = Regex.Match(infofile[j], @"^BID=")).Success)
                {
                    indexstart = match.Index + match.Length;
                    lenghttoget = infofile[j].Length - indexstart;
                    uint.TryParse(infofile[j].Substring(indexstart, lenghttoget), System.Globalization.NumberStyles.HexNumber, null, out prod.BID);

                }
                else if ((match = Regex.Match(infofile[j], @"^FlashSize=")).Success)
                {
                    indexstart = match.Index + match.Length;
                    lenghttoget = infofile[j].Length - indexstart;
                    uint.TryParse(infofile[j].Substring(indexstart, lenghttoget), System.Globalization.NumberStyles.HexNumber, null, out prod.FlashSize);
                }
                else if ((match = Regex.Match(infofile[j], @"^PacketSize=")).Success)
                {
                    indexstart = match.Index + match.Length;
                    lenghttoget = infofile[j].Length - indexstart;
                    uint.TryParse(infofile[j].Substring(indexstart, lenghttoget), System.Globalization.NumberStyles.HexNumber, null, out prod.PacketSize);
                }
                else if ((match = Regex.Match(infofile[j], @"^ACKVAL=")).Success)
                {
                    indexstart = match.Index + match.Length;
                    lenghttoget = infofile[j].Length - indexstart;
                    uint.TryParse(infofile[j].Substring(indexstart, lenghttoget), System.Globalization.NumberStyles.HexNumber, null, out prod.AckVal);
                }
                else if ((match = Regex.Match(infofile[j], @"^MAPNAME=")).Success)
                {
                    indexstart = match.Index + match.Length;
                    lenghttoget = infofile[j].Length - indexstart;
                    prod.mapname = infofile[j].Substring(indexstart, lenghttoget);
                }
                else if ((match = Regex.Match(infofile[j], @"^PagesPerSector=")).Success)
                {
                    indexstart = match.Index + match.Length;
                    lenghttoget = infofile[j].Length - indexstart;
                    uint.TryParse(infofile[j].Substring(indexstart, lenghttoget), System.Globalization.NumberStyles.HexNumber, null, out prod.PagesPerSector);
                }
                else if ((match = Regex.Match(infofile[j], @"^family=")).Success)
                {
                    indexstart = match.Index + match.Length;
                    lenghttoget = infofile[j].Length - indexstart;
                    uint.TryParse(infofile[j].Substring(indexstart, lenghttoget), out prod.family);
                }
                else if ((match = Regex.Match(infofile[j], @"^\[.+]")).Success)
                {
                    i = j;
                    break;
                }
            }

            return i;
        }

        private void sectorsFilling()
        {
            int i;
            int j;
            int indexstart = 0;
            int lenghttoget = 0;
            bool isinsector = false;
            Match match;
            for (i = 0; i < infofile.Length; i++)
            {

                if ((match = Regex.Match(infofile[i], @"^\[Sector")).Success)
                {
                    Sectors.Add(new Sector());
                    isinsector = true;
                }
                else if (Regex.Match(infofile[i], @"^\[").Success && !Regex.Match(infofile[i], @"^\[Sector").Success)
                {
                    isinsector = false;
                }

                if (isinsector)
                {
                    if ((match = Regex.Match(infofile[i], @"^Name=")).Success)
                    {
                        indexstart = match.Index + match.Length;
                        lenghttoget = infofile[i].Length - indexstart;
                        Sectors[Sectors.Count - 1].Name = infofile[i].Substring(indexstart, lenghttoget);

                    }
                    else if ((match = Regex.Match(infofile[i], @"^Index=")).Success)
                    {
                        indexstart = match.Index + match.Length;
                        lenghttoget = infofile[i].Length - indexstart;
                        uint.TryParse(infofile[i].Substring(indexstart, lenghttoget), System.Globalization.NumberStyles.HexNumber, null, out   Sectors[Sectors.Count - 1].dwSectorIndex);

                    }
                    else if ((match = Regex.Match(infofile[i], @"^Address=")).Success)
                    {
                        indexstart = match.Index + match.Length;
                        lenghttoget = infofile[i].Length - indexstart;
                        uint.TryParse(infofile[i].Substring(indexstart, lenghttoget), System.Globalization.NumberStyles.HexNumber, null, out   Sectors[Sectors.Count - 1].dwStartAddress);

                    }
                    else if ((match = Regex.Match(infofile[i], @"^Size=")).Success)
                    {
                        indexstart = match.Index + match.Length;
                        lenghttoget = infofile[i].Length - indexstart;
                        uint.TryParse(infofile[i].Substring(indexstart, lenghttoget), System.Globalization.NumberStyles.HexNumber, null, out   Sectors[Sectors.Count - 1].dwSectorSize);
                    }
                    else if ((match = Regex.Match(infofile[i], @"^Type=")).Success)
                    {
                        indexstart = match.Index + match.Length;
                        lenghttoget = infofile[i].Length - indexstart;
                        uint.TryParse(infofile[i].Substring(indexstart, lenghttoget), System.Globalization.NumberStyles.HexNumber, null, out   Sectors[Sectors.Count - 1].bSectorType);
                    }
                    else if ((match = Regex.Match(infofile[i], @"^UFO=")).Success)
                    {
                        Sectors[Sectors.Count - 1].UseForOperation = true;
                        Sectors[Sectors.Count - 1].UseForErase = true;
                        Sectors[Sectors.Count - 1].UseForUpload = true;
                        Sectors[Sectors.Count - 1].UseForWriteProtect = true;
                    }
                }
            }

        }
    }

    class Product
    {
        public uint PagesPerSector;
        public uint family;
        public uint PacketSize;
        public uint FlashSize;
        public uint BID;
        public uint PID;
        public uint AckVal;
        public string name;
        public  string mapname;

    }

    class Sector
    {
        public string Name;
        public uint dwStartAddress;
        public uint dwAliasedAddress;
        public uint dwSectorIndex;
        public uint dwSectorSize;
        public uint bSectorType;
        public bool UseForOperation;
        public bool UseForErase;
        public bool UseForUpload;
        public bool UseForWriteProtect;
    }

    class OptionBytes
    {
        public string Name  ;
        public uint adress;
        public uint sizeoptionbytes;
        public uint typeoptionbytes;
        public uint UFOoptionbytes;

    }
}
