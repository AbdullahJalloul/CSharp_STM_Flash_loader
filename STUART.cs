using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.IO;
using System.Threading;
using System.Globalization;

namespace Flashing
{
    class STUART
    {
        const uint INIT_CON = 0x7F;

        const uint GET_CMD = 0x00; //Get the version and the allowed commands supported by the current version of the boot loader
        const uint GET_VER_ROPS_CMD = 0x01; //Get the BL version and the Read Protection status of the NVM
        const uint GET_ID_CMD = 0x02; //Get the chip ID
        const uint SET_SPEED_CMD = 0x03; //set the new baudrate
        const uint READ_CMD = 0x11; //Read up to 256 uints of memory starting from an address specified by the user
        const uint GO_CMD = 0x21; //Jump to an address specified by the user to execute (a loaded) code
        const uint WRITE_CMD = 0x31; //Write maximum 256 uints to the RAM or the NVM starting from an address specified by the user
        const uint ERASE_CMD = 0x43; //Erase from one to all the NVM sectors
        const uint ERASE_EXT_CMD = 0x44; //Erase from one to all the NVM sectors
        const uint WRITE_PROTECT_CMD = 0x63; //Enable the write protection in a permanent way for some sectors
        const uint WRITE_TEMP_UNPROTECT_CMD = 0x71; //Disable the write protection in a temporary way for all NVM sectors
        const uint WRITE_PERM_UNPROTECT_CMD = 0x73; //Disable the write protection in a permanent way for all NVM sectors
        const uint READOUT_PROTECT_CMD = 0x82; //Enable the readout protection in a permanent way
        const uint READOUT_TEMP_UNPROTECT_CMD = 0x91; //Disable the readout protection in a temporary way
        const uint READOUT_PERM_UNPROTECT_CMD = 0x92; //Disable the readout protection in a permanent way


        const uint SUCCESS = 0x00; // No error 
        const uint ERROR_OFFSET = 0x00; //error offset 

        const uint COM_ERROR_OFFSET = ERROR_OFFSET + 0x00;
        const uint NO_CON_AVAILABLE = COM_ERROR_OFFSET + 0x01;  // No serial port opened
        const uint COM_ALREADY_OPENED = COM_ERROR_OFFSET + 0x02;  // Serial port already opened
        const uint CANT_OPEN_COM = COM_ERROR_OFFSET + 0x03;  // Fail to open serial port
        const uint SEND_FAIL = COM_ERROR_OFFSET + 0x04;  // send over serial port fail
        const uint READ_FAIL = COM_ERROR_OFFSET + 0x05;  // Read from serial port fail

        const uint SYS_MEM_ERROR_OFFSET = ERROR_OFFSET + 0x10;
        const uint CANT_INIT_BL = SYS_MEM_ERROR_OFFSET + 0x01; // Fail to start system memory BL
        const uint UNREOGNIZED_DEVICE = SYS_MEM_ERROR_OFFSET + 0x02; // Unreconized device
        const uint CMD_NOT_ALLOWED = SYS_MEM_ERROR_OFFSET + 0x03; // Command not allowed
        const uint CMD_FAIL = SYS_MEM_ERROR_OFFSET + 0x04; // command failed

        const uint PROGRAM_ERROR_OFFSET = ERROR_OFFSET + 0x20;
        const uint INPUT_PARAMS_ERROR = PROGRAM_ERROR_OFFSET + 0x01;
        const uint INPUT_PARAMS_MEMORY_ALLOCATION_ERROR = PROGRAM_ERROR_OFFSET + 0x02;


        const uint FILES_ERROR_OFFSET = (0x12340000 + 0x6000);

        const uint FILES_NOERROR = (0x12340000 + 0x0000);
        const uint FILES_BADSUFFIX = (FILES_ERROR_OFFSET + 0x0002);
        const uint FILES_UNABLETOOPENFILE = (FILES_ERROR_OFFSET + 0x0003);
        const uint FILES_UNABLETOOPENTEMPFILE = (FILES_ERROR_OFFSET + 0x0004);
        const uint FILES_BADFORMAT = (FILES_ERROR_OFFSET + 0x0005);
        const uint FILES_BADADDRESSRANGE = (FILES_ERROR_OFFSET + 0x0006);
        const uint FILES_BADPARAMETER = (FILES_ERROR_OFFSET + 0x0008);
        const uint FILES_UNEXPECTEDERROR = (FILES_ERROR_OFFSET + 0x000A);
        const uint FILES_FILEGENERALERROR = (FILES_ERROR_OFFSET + 0x000D);

        const uint STPRT_ERROR_OFFSET = (0x12340000 + 0x5000);

        const uint STPRT_NOERROR = (0x12340000);
        const uint STPRT_UNABLETOLAUNCHTHREAD = (STPRT_ERROR_OFFSET + 0x0001);
        const uint STPRT_ALREADYRUNNING = (STPRT_ERROR_OFFSET + 0x0007);
        const uint STPRT_BADPARAMETER = (STPRT_ERROR_OFFSET + 0x0008);
        const uint STPRT_BADFIRMWARESTATEMACHINE = (STPRT_ERROR_OFFSET + 0x0009);
        const uint STPRT_UNEXPECTEDERROR = (STPRT_ERROR_OFFSET + 0x000A);
        const uint STPRT_ERROR = (STPRT_ERROR_OFFSET + 0x000B);
        const uint STPRT_RETRYERROR = (STPRT_ERROR_OFFSET + 0x000C);
        const uint STPRT_UNSUPPORTEDFEATURE = (STPRT_ERROR_OFFSET + 0x000D);


        uint ADDR_F0_OPB = 0x1FFFF800;
        uint ADDR_F1_OPB = 0x1FFFF800;
        uint ADDR_F2_OPB = 0x1FFFC000;
        uint ADDR_L1_OPB = 0x1FF80000;
        uint ADDR_XX_OPB = 0x1FFFF800;


        ACKS ACK_VALUE = 0x0;
        byte ACK = 0x00;
        byte NACK = 0x00;
        uint MAX_DATA_SIZE = 0xFF;  // Packet size(in byte)
        MAPPING pmMapping;
        enum ACKS { UNDEFINED = 0x00, ST75 = 0x75, ST79 = 0x79 };
        enum INTERFACE_TYPE { UART, CAN };

        enum EBaudRate
        {
            brCustom, br110, br300, br600, br1200, br2400, br4800, br9600, br14400, br19200, br38400,
            br56000, br57600, br115200, br128000, br256000
        };// Port Numbers ( custom or COM1..COM16 }
        enum EPortNumber
        {
            pnCustom, pnCOM1, pnCOM2, pnCOM3, pnCOM4, pnCOM5, pnCOM6, pnCOM7, pnCOM8, pnCOM9, pnCOM10,
            pnCOM11, pnCOM12, pnCOM13, pnCOM14, pnCOM15, pnCOM16
        };// Data bits ( 5, 6, 7, 8 }
        enum EDataBits { db5BITS, db6BITS, db7BITS, db8BITS };
        // Stop bits ( 1, 1.5, 2 }
        enum EStopBits { sb1BITS, sb1HALFBITS, sb2BITS };
        // Parity ( None, odd, even, mark, space }
        enum EParity { ptNONE, ptODD, ptEVEN, ptMARK, ptSPACE };
        // Hardware Flow Control ( None, None + RTS always on, RTS/CTS }
        enum EHwFlowControl { hfNONE, hfNONERTSON, hfRTSCTS };
        // Software Flow Control ( None, XON/XOFF }
        enum ESwFlowControl { sfNONE, sfXONXOFF };
        // What to do with incomplete (incoming} packets ( Discard, Pass }
        enum EPacketMode { pmDiscard, pmPass };

        enum OPERATION { NONE, ERASE, UPLOAD, DNLOAD, DIS_R_PROT, DIS_W_PROT, ENA_R_PROT, ENA_W_PROT };

        SerialPort Cur_COM;
        TARGET_DESCRIPTOR Target;
        int family = 0;

        public int StartEraseDownload( string mapfilepath,string filenamehex)
        {

            byte Res = (byte)SUCCESS;
            //BYTE User, RDP, Data0, Data1, WRP0, WRP1, WRP2, WRP3;
            bool WaitForMoreSubOpt = true;



            int nsec = 0;
            uint address = 0x08000000;
            uint size = 0x00000000;
            string filename = filenamehex;
            string devname = "STM32F1_Low-density_32K.STmap";
            bool Verify = false;
            bool optimize = false;
            int becho = 0;

          //  Cur_COM.Open();
            // Opening serial connection
            Res = Convert.ToByte(!Cur_COM.IsOpen);
            SetTimeout(1000);


            if ((Res != SUCCESS) && (Res != COM_ALREADY_OPENED))
            {
                Console.WriteLine("Opening Port 0 , 0 , 0 ,0, KO");
                Console.WriteLine("Cannot open the com port, the port may \n be used by another application {0} \n", Cur_COM.PortName);

                if (COM_is_Open())
                    COM_Close();

                return 2;  /* return with communication error */
            }
            else
                Console.WriteLine("Opening Port, 0 ,0, 0, OK");

            string Ext;
            string currentPath = mapfilepath;
            Console.WriteLine(currentPath);
            pmMapping = null;
            ushort PacketSize = 0;
            pmMapping = null;
            uint Size = 0;


            // Get the number of sectors in the flash target: pmMapping should be NULL
            // number of sectors is returned in the Size value
            byte PagePerSector = 0;

            if (!File.Exists(currentPath))
            {
                Console.WriteLine("This version is not intended to support the {0} target\n", currentPath);


                if (COM_is_Open())
                    COM_Close();


                return 3;   /* return with Operation error */
            }

            pmMapping = new MAPPING();
            FILES_GetMemoryMapping(currentPath, ref Size, currentPath, ref PacketSize, ref pmMapping, (byte)PagePerSector);
            // Allocate the mapping structure memory
            pmMapping = new MAPPING();
            pmMapping.NbSectors = 0;
            pmMapping.pSectors = new List<MAPPINGSECTOR>();

            // Get the mapping info
            FILES_GetMemoryMapping(currentPath, ref Size, currentPath, ref PacketSize, ref pmMapping, (byte)PagePerSector);

            SetPaketSize((byte)PacketSize);

            //sending BL config byte (0x7F) & identifing target

            Res = (byte)STBL_Init_BL();

            if (Res == UNREOGNIZED_DEVICE)
            {
                Console.WriteLine("Activating device, 0 ,0, 0, KO");

                if (COM_is_Open())
                    COM_Close();

                Console.WriteLine("Unrecognized device... Please, reset your device then try again \n");
                if (COM_is_Open())
                    COM_Close();


                Console.WriteLine("Please, reset your device then press any key to continue \n");
                Console.WriteLine("\n Press any key to continue ...\n");
                return 1;
            }
            else if (Res != SUCCESS)
            {
                Console.WriteLine("Activating device, 0 ,0, 0, KO");
                Console.WriteLine("No response from the target, the Boot loader can not be started. \nPlease, verify the boot mode configuration, reset your device then try again. \n");

                if (COM_is_Open())
                    COM_Close();

                Console.WriteLine("Please, reset your device then then press any key to continue \n");
                return 1;
            }

            Thread.Sleep(100);

            Console.WriteLine("Activating device, 0 ,0, 0, OK");
            //Getting Target informations (version, available commands)
            byte Version;
            Commands pCmds;

            Res = (byte)STBL_GET(out Version, out pCmds);
            if (Res != SUCCESS)
            {
                if (COM_is_Open())
                    COM_Close();

                return 3;   /* return with Operation error */
            }

            SetTimeout(30000);

            family = 0; // STM32F1 is selected by default
            ADDR_XX_OPB = ADDR_F1_OPB;

            if (ACK_VALUE == ACKS.ST75)
                family = 1;  // STR750 is selected 

            else if (ACK_VALUE == ACKS.ST79)   // STM32, STR911 or STM8 is used, need to check other criteria    
            {
                if (!pCmds.GET_ID_CMD)  // check if the GET ID command is available
                {
                    family = 2;  // STM8 is used
                }
                else
                {     // STM32 or STR911 is used
                    byte sizeb = 0x00;
                    byte[] pID;  //Get the Device ID
                    //Get the ID size in bytes before 
                    if (STBL_GET_ID(ref sizeb) == SUCCESS)
                    {
                        pID = new byte[sizeb];

                        if (STBL_GET_ID(ref sizeb, pID) == SUCCESS) // Get the ID
                        {
                            uint PID = 0x00000000;
                            for (int i = 0; i < sizeb; i++)
                            {
                                PID = PID << 8;
                                PID = PID + (pID[i]);
                            }


                            if (PID == 0x25966041)
                            {
                                family = 3; // STR911 is used 
                            }
                            else if ((PID == 0x416) || (PID == 0x436) || (PID == 0x427) || (PID == 0x429) || (PID == 0x437) || (PID == 0x417) || (PID == 0x447) || (PID == 0x425) || (PID == 0x457))
                            {
                                family = 4; // STM32L0 and STM32L1  is used
                                ADDR_XX_OPB = ADDR_L1_OPB;
                            }
                            else if ((PID == 0x411) || (PID == 0x413) || (PID == 0x419) || (PID == 0x423) || (PID == 0x433) || (PID == 0x443) || (PID == 0x421) || (PID == 0x441) || (PID == 0x434) || (PID == 0x443) || (PID == 0x449) || (PID == 0x451))
                            {
                                family = 5; // STM32F2 and STM32F4 is used 
                                ADDR_XX_OPB = ADDR_F2_OPB;
                            }

                            else if ((PID == 0x440) || (PID == 0x432) || (PID == 0x422) || (PID == 0x444) || (PID == 0x448) || (PID == 0x438) || (PID == 0x445) || (PID == 0x439) || (PID == 0x446) || (PID == 0x442))
                            {
                                family = 6; // STM32F0 or STM32F3 is used 
                                ADDR_XX_OPB = ADDR_F0_OPB;
                            }
                        }
                    }
                }
            }

            family = 5; // STM32F2 and STM32F4 is used 
            ADDR_XX_OPB = ADDR_F2_OPB;

            Console.WriteLine("\n ERASING ... \n");


            WaitForMoreSubOpt = false;
            Res = (byte)STBL_ERASE(0xFFFF);



            if (Res != SUCCESS)
                Console.WriteLine("erasing all pages, 0 ,0, 0, KO");
            else
                Console.WriteLine("erasing all pages, 0 ,0, 0, OK");

            //============================ DOWNLOAD ==============================================

            Verify = true;
            if (File.Exists(filename)) ;
            Console.WriteLine(filename );
            Ext = "FLASH.HEX";
            MAPPINGSECTOR pSector = pmMapping.pSectors[0];
            for (int i = 1; i <= (int)pmMapping.NbSectors; i++)
            {
                if ((Ext.LastIndexOf(".bin") != -1) && (i == 0))
                    address = pSector.dwStartAddress;
                pSector.UseForOperation = true;
            }

            CImage Handle;
            ushort NbElements = 0;
            if (FILES_ImageFromFile(filename, out Handle, 1) == FILES_NOERROR)
            {
                FILES_SetImageName(ref Handle, filename);

                if (FILES_GetImageNbElement(ref Handle, ref NbElements) == FILES_NOERROR)
                {
                    if (NbElements > 0)
                    {   // if binary file -> change the elemnts address
                        if (Ext.LastIndexOf(".bin") != -1)
                        {
                            for (int i = 0; i < (int)NbElements; i++)
                            {
                                IMAGEELEMENT Element = new IMAGEELEMENT();
                                if (FILES_GetImageElement(ref Handle, (uint)i, ref Element) == FILES_NOERROR)
                                {
                                    Element.Data = new byte[Element.dwDataLength];
                                    if (FILES_GetImageElement(ref Handle, (uint)i, ref Element) == FILES_NOERROR)
                                    {
                                        Element.dwAddress = Element.dwAddress + address;
                                        FILES_SetImageElement(Handle, (uint)i, false, Element);
                                    }
                                }
                            }
                        }
                    }
                }

                FILES_FilterImageForOperation(Handle, pmMapping, 2, optimize);
            }

            NbElements = 0;
            if (FILES_GetImageNbElement(ref Handle, ref NbElements) == FILES_NOERROR)
            {
                for (int el = 0; el < (int)NbElements; el++)
                {
                    IMAGEELEMENT Element = new IMAGEELEMENT();
                    if (FILES_GetImageElement(ref Handle, (uint)el, ref Element) == FILES_NOERROR)
                    {
                        Element.Data = new byte[Element.dwDataLength];
                        if (FILES_GetImageElement(ref Handle, (uint)el, ref Element) == FILES_NOERROR)
                        {
                            if ((Ext.LastIndexOf(".bin") != -1) && (el == 0))
                                Element.dwAddress = address;

                            Console.WriteLine("[Downloading] Start, Sector: " + el.ToString() + " " + Element.dwAddress.ToString("X"));
                            if (STBL_DNLOAD(Element.dwAddress, Element.Data, Element.dwDataLength, optimize) != SUCCESS)
                            {
                                Console.WriteLine("downloading" + el.ToString() + " " + Element.dwAddress.ToString()
                                    + ((float)Element.dwDataLength / (float)1024).ToString() + " KO");
                                Console.WriteLine("Error: Retry or contact Spinomix");
                               // Console.WriteLine("The flash may be read protected; use - disable write protection. Fail");

                                if (COM_is_Open())
                                    COM_Close();

                                return 3;  /* return with Operation error */
                            }

                            Console.WriteLine("[Downloading] Downloaded! " + el.ToString() + " " + Element.dwAddress.ToString("X") + " " + ((float)Element.dwDataLength / (float)1024).ToString() + "KBytes Success");
                        }
                    }
                }
            }

            bool setVerify = true;
            bool VerifySuccess = true;
            if(setVerify == true)
            {
                for (int el = 0; el < (int)NbElements; el++)
                {
                    IMAGEELEMENT Element = new IMAGEELEMENT();
                    if (FILES_GetImageElement(ref Handle, (uint)el, ref Element) == FILES_NOERROR)
                    {
                        Element.Data = new byte[Element.dwDataLength];
                        if (FILES_GetImageElement(ref Handle, (uint)el, ref Element) == FILES_NOERROR)
                        {
                            if ((Ext.LastIndexOf(".bin") != -1) && (el == 0))
                                Element.dwAddress = address;
                            Console.WriteLine("[Verifying] Start, Sector: " + el.ToString() + " " + Element.dwAddress.ToString("X"));
                            if (STBL_VERIFY(Element.dwAddress, Element.Data, Element.dwDataLength, optimize) != SUCCESS)
                            {
                                VerifySuccess = false;
                                Console.WriteLine("[Verifying] Sector: " + el.ToString() + " " + Element.dwAddress.ToString("X") + " " +  ((float)Element.dwDataLength / (float)1024).ToString() + "Kbytes OK");
                                Console.WriteLine("Error: Retry or contact Spinomix");

                                if (COM_is_Open())
                                    COM_Close();

                                return 3;   /* return with Operation error */
                            }

                            Console.WriteLine("[Verifying] Passed! Sector: " + el.ToString() + " " + Element.dwAddress.ToString("X") + " " + ((float)Element.dwDataLength / (float)1024).ToString() + "Kbytes OK");
                        }
                    }
                }
            }

            COM_Close();
            return 0;

        }
        



        public bool COM_is_Open()
        {
            return Cur_COM.IsOpen;
        }


        public void COM_Close()
        {
            Cur_COM.Close();
        }

        uint Progress = 0;
        public STUART(string portname, int baudrate)
        {
            Cur_COM = new SerialPort(portname, baudrate);
            Cur_COM.StopBits = StopBits.One;
            Cur_COM.DataBits = 8;
            Cur_COM.Parity = Parity.Even;
            Cur_COM.RtsEnable = false;
            Cur_COM.ReadTimeout = 15000;
            Cur_COM.WriteTimeout = 15000;
            Cur_COM.ReadBufferSize = 0x1000000; 
            Cur_COM.Open();
        }

        public void Open()
        {
            Cur_COM.Open();
        }
        public void SetTimeout(int to)
        {
            Cur_COM.ReadTimeout = 15000 + to;
        }

        byte SetPaketSize(byte size)
        {
            MAX_DATA_SIZE = (uint)(size / 4) * 4;
            return (byte)SUCCESS;
        }
        public void InitSectors()
        {

        }

        public uint STBL_Init_BL()
        {
            if (!Cur_COM.IsOpen) return NO_CON_AVAILABLE;


            byte[] RQ_Buffer = new byte[] { (byte)INIT_CON };
            //if (Cur_COM.setTxd(FALSE)) _sleep(100);

            Cur_COM.Write(RQ_Buffer, 0, RQ_Buffer.Length);

            if (Cur_COM.Read(RQ_Buffer, 0, RQ_Buffer.Length) != RQ_Buffer.Length)
                return READ_FAIL;



            //Work-Around : in case of the device send a 0x00 value 
            //after system reset , we reveive  again the real ack

            if (RQ_Buffer[0] == 0x00)
                if (Cur_COM.Read(RQ_Buffer, 0, RQ_Buffer.Length) != RQ_Buffer.Length)
                    return READ_FAIL;

            //if (Cur_COM.setTxd(FALSE)) 
            //_sleep(100);

            switch (RQ_Buffer[0])
            {
                case 0x75:
                    { // STR750 used
                        ACK_VALUE = ACKS.ST75;
                        ACK = 0x75;
                        NACK = 0x3F;
                        // Commented to avoid DTR/RTS reset signals Cur_COM.SetParity(0); // Set NONE parity  
                    } break;
                case 0x79:
                    { // STM32, STR911 or STM8 used
                        ACK_VALUE = ACKS.ST79;
                        ACK = 0x79;
                        NACK = 0x1F;
                        // Commented to avoid DTR/RTS reset signalsCur_COM.SetParity(2); // Set EVEN parity
                    } break;
                default:
                    { // Undefined device
                        ACK_VALUE = ACKS.UNDEFINED;
                        ACK = RQ_Buffer[0];
                        return UNREOGNIZED_DEVICE;
                    }
            }

            return SUCCESS;
        }


        byte STBL_GET_ID(ref byte size, byte[] pID = null)
        {
            if (!Cur_COM.IsOpen) return (byte)NO_CON_AVAILABLE;
            if (!Target.GET_ID_CMD) return (byte)CMD_NOT_ALLOWED;

            //if (!pID) return INPUT_PARAMS_MEMORY_ALLOCATION_ERROR;

            STBL_Request pRQ = new STBL_Request();
            pRQ._target = new TARGET_DESCRIPTOR();

            pRQ._cmd = (byte) GET_ID_CMD;

            byte Result = (byte) Send_RQ(pRQ);
            if (Result != SUCCESS) return Result;

            size = pRQ._target.PIDLen;
            if (pID != null)
            {

                for (int i = 0; i < size; i++)
                {
                    pID[i] = pRQ._target.PID[0];
                }
            }

            return (byte) SUCCESS;
        }  




        uint STBL_GET(out byte Version, out Commands pCmds)
        {
            pCmds = new Commands();
            Version =0;
            if (!Cur_COM.IsOpen) return NO_CON_AVAILABLE;

            
            if (ACK_VALUE == ACKS.ST75)
            {
                pCmds.GET_CMD = true; //Get the version and the allowed commands supported by the current version of the boot loader
                pCmds.GET_VER_ROPS_CMD = true; //Get the BL version and the Read Protection status of the NVM
                pCmds.GET_ID_CMD = true; //Get the chip ID
                pCmds.READ_CMD = true; //Read up to 256 bytes of memory starting from an address specified by the user
                pCmds.GO_CMD = true; //Jump to an address specified by the user to execute (a loaded) code
                pCmds.WRITE_CMD = true; //Write maximum 256 bytes to the RAM or the NVM starting from an address specified by the user
                pCmds.ERASE_CMD = true; //Erase from one to all the NVM sectors
                pCmds.WRITE_PROTECT_CMD = true; //Enable the write protection in a permanent way for some sectors
                pCmds.WRITE_TEMP_UNPROTECT_CMD = true; //Disable the write protection in a temporary way for all NVM sectors
                pCmds.WRITE_PERM_UNPROTECT_CMD = true; //Disable the write protection in a permanent way for all NVM sectors
                pCmds.READOUT_PROTECT_CMD = true; //Enable the readout protection in a permanent way
                pCmds.READOUT_TEMP_UNPROTECT_CMD = true; //Disable the readout protection in a temporary way
                pCmds.READOUT_PERM_UNPROTECT_CMD = true; //Disable the readout protection in a permanent way

                Target.GET_CMD = true; //Get the version and the allowed commands supported by the current version of the boot loader
                Target.GET_VER_ROPS_CMD = true; //Get the BL version and the Read Protection status of the NVM
                Target.GET_ID_CMD = true; //Get the chip ID
                Target.READ_CMD = true; //Read up to 256 bytes of memory starting from an address specified by the user
                Target.GO_CMD = true; //Jump to an address specified by the user to execute (a loaded) code
                Target.WRITE_CMD = true; //Write maximum 256 bytes to the RAM or the NVM starting from an address specified by the user
                Target.ERASE_CMD = true; //Erase from one to all the NVM sectors
                Target.WRITE_PROTECT_CMD = true; //Enable the write protection in a permanent way for some sectors
                Target.WRITE_TEMP_UNPROTECT_CMD = true; //Disable the write protection in a temporary way for all NVM sectors
                Target.WRITE_PERM_UNPROTECT_CMD = true; //Disable the write protection in a permanent way for all NVM sectors
                Target.READOUT_PERM_PROTECT_CMD = true; //Enable the readout protection in a permanent way
                Target.READOUT_TEMP_UNPROTECT_CMD = true; //Disable the readout protection in a temporary way
                Target.READOUT_PERM_UNPROTECT_CMD = true; //Disable the readout protection in a permanent way

                byte Ver = 0; byte ROPEnabled = 0; byte ROPDisabled = 0;
                byte Result = (byte)STBL_GET_VER_ROPS(ref Ver, ref ROPEnabled, ref ROPDisabled);
                if (Result != SUCCESS) return Result;
                Version = Ver;

            }
            else if (ACK_VALUE == ACKS.ST79)
            {

                STBL_Request pRQ = new STBL_Request();
                pRQ._target = new TARGET_DESCRIPTOR();

                pRQ._cmd = (byte)GET_CMD;

                byte Result = (byte)Send_RQ(pRQ);
                if (Result != SUCCESS) return Result;


                pCmds.GET_CMD = pRQ._target.GET_CMD; //Get the version and the allowed commands supported by the current version of the boot loader
                pCmds.GET_VER_ROPS_CMD = pRQ._target.GET_VER_ROPS_CMD; //Get the BL version and the Read Protection status of the NVM
                pCmds.GET_ID_CMD = pRQ._target.GET_ID_CMD; //Get the chip ID
                pCmds.READ_CMD = pRQ._target.READ_CMD; //Read up to 256 bytes of memory starting from an address specified by the user
                pCmds.GO_CMD = pRQ._target.GO_CMD; //Jump to an address specified by the user to execute (a loaded) code
                pCmds.WRITE_CMD = pRQ._target.WRITE_CMD; //Write maximum 256 bytes to the RAM or the NVM starting from an address specified by the user
                pCmds.ERASE_CMD = pRQ._target.ERASE_CMD; //Erase from one to all the NVM sectors
                pCmds.ERASE_EXT_CMD = pRQ._target.ERASE_EXT_CMD; //Erase from one to all the NVM sectors
                pCmds.WRITE_PROTECT_CMD = pRQ._target.WRITE_PROTECT_CMD; //Enable the write protection in a permanent way for some sectors
                pCmds.WRITE_TEMP_UNPROTECT_CMD = pRQ._target.WRITE_TEMP_UNPROTECT_CMD; //Disable the write protection in a temporary way for all NVM sectors
                pCmds.WRITE_PERM_UNPROTECT_CMD = pRQ._target.WRITE_PERM_UNPROTECT_CMD; //Disable the write protection in a permanent way for all NVM sectors
                pCmds.READOUT_PROTECT_CMD = pRQ._target.READOUT_PERM_PROTECT_CMD; //Enable the readout protection in a permanent way
                pCmds.READOUT_TEMP_UNPROTECT_CMD = pRQ._target.READOUT_TEMP_UNPROTECT_CMD; //Disable the readout protection in a temporary way
                pCmds.READOUT_PERM_UNPROTECT_CMD = pRQ._target.READOUT_PERM_UNPROTECT_CMD; //Disable the readout protection in a permanent way

            }
            return SUCCESS;
        }

        private uint STBL_GET_VER_ROPS(ref byte Version, ref byte ROPEnabled, ref byte ROPDisabled)
        {
            if (!Cur_COM.IsOpen) return NO_CON_AVAILABLE;
            if (!Target.GET_VER_ROPS_CMD) return CMD_NOT_ALLOWED;

            STBL_Request pRQ = new STBL_Request();
            pRQ._target = new TARGET_DESCRIPTOR();

            pRQ._cmd = (byte)GET_VER_ROPS_CMD;

            byte Result = (byte)Send_RQ(pRQ);
            if (Result != SUCCESS) return Result;

            ROPEnabled = pRQ._target.ROPD;
            ROPDisabled = pRQ._target.ROPE;
            Version = Target.Version;

            return SUCCESS;
        }

        private uint Send_RQ(STBL_Request pRQ)
        {
            byte DataSize = 1;

            if (!Cur_COM.IsOpen) return NO_CON_AVAILABLE;

            if (Target == null) Target = new TARGET_DESCRIPTOR();

            byte[] RQ_Buffer = new byte[2];

            // put command code in the buffer

            RQ_Buffer[0] = pRQ._cmd;

            if (ACK_VALUE == ACKS.ST79)
            {
                // put XOR command code in the buffer
                RQ_Buffer[1] = (byte)~(pRQ._cmd);
                DataSize = 2;
            }

            byte auxcmd = RQ_Buffer[0];
            byte auxxorcmd = RQ_Buffer[1];

            // Send command code (and its XOR value) 
            Cur_COM.Write(RQ_Buffer, 0, (int)DataSize);

            DataSize = 1;

            // Get ACK (verify if the command was accepted)
            if (/*ACK_VALUE == ST79) */!((ACK_VALUE == ACKS.ST75) && (pRQ._cmd == GET_VER_ROPS_CMD)))
            {
                RQ_Buffer = new byte[1];
                if (Cur_COM.Read(RQ_Buffer, 0, 1) != 1)
                    return READ_FAIL;

                if (RQ_Buffer[0] != ACK)
                    return CMD_NOT_ALLOWED;
            }

            switch ((uint)pRQ._cmd)
            {
                case GET_CMD: //Get the version and the allowed commands supported by the current version of the boot loader
                    {
                        pRQ._target = new TARGET_DESCRIPTOR();
                        // Get number of bytes (Version + commands)
                        RQ_Buffer = new byte[1];
                        if (Cur_COM.Read(RQ_Buffer, 0, 1) != 1)
                            return READ_FAIL;

                        pRQ._target.CmdCount = RQ_Buffer[0];

                        // Get boot loader version
                        if (Cur_COM.Read(RQ_Buffer, 0, 1) != 1)
                            return READ_FAIL;

                        pRQ._target.Version = RQ_Buffer[0];

                        // Get supported commands
                        RQ_Buffer = new byte[(pRQ._target.CmdCount)];
                        if (Cur_COM.Read(RQ_Buffer, 0, pRQ._target.CmdCount) != pRQ._target.CmdCount)
                            return READ_FAIL;

                        for (int i = 0; i < pRQ._target.CmdCount; i++)
                        {
                            switch ((uint)RQ_Buffer[i])
                            {
                                case GET_CMD: pRQ._target.GET_CMD = true;
                                    break;
                                case GET_VER_ROPS_CMD: pRQ._target.GET_VER_ROPS_CMD = true;
                                    break;
                                case GET_ID_CMD: pRQ._target.GET_ID_CMD = true;
                                    break;
                                case READ_CMD: pRQ._target.READ_CMD = true;
                                    break;
                                case GO_CMD: pRQ._target.GO_CMD = true;
                                    break;
                                case WRITE_CMD: pRQ._target.WRITE_CMD = true;
                                    break;
                                case ERASE_CMD: pRQ._target.ERASE_CMD = true;
                                    break;
                                case ERASE_EXT_CMD: pRQ._target.ERASE_EXT_CMD = true;
                                    break;
                                case WRITE_PROTECT_CMD: pRQ._target.WRITE_PROTECT_CMD = true;
                                    break;
                                case WRITE_TEMP_UNPROTECT_CMD: pRQ._target.WRITE_TEMP_UNPROTECT_CMD = true;
                                    break;
                                case WRITE_PERM_UNPROTECT_CMD: pRQ._target.WRITE_PERM_UNPROTECT_CMD = true;
                                    break;
                                case READOUT_PROTECT_CMD: pRQ._target.READOUT_PERM_PROTECT_CMD = true;
                                    break;
                                case READOUT_TEMP_UNPROTECT_CMD: pRQ._target.READOUT_TEMP_UNPROTECT_CMD = true;
                                    break;
                                case READOUT_PERM_UNPROTECT_CMD: pRQ._target.READOUT_PERM_UNPROTECT_CMD = true;
                                    break;
                            }
                        }

                        // Get ACK byte
                        RQ_Buffer = new byte[1];
                        if (Cur_COM.Read(RQ_Buffer, 0, 1) != 1)
                            return READ_FAIL;

                        if (RQ_Buffer[0] != ACK)
                            return CMD_FAIL;

                        Target = pRQ._target.ShallowCopy();

                    } break;
                case GET_VER_ROPS_CMD: //Get the BL version and the Read Protection status of the NVM
                    {
                        pRQ._target = Target.ShallowCopy();

                        // Get Version
                        RQ_Buffer = new byte[1];
                        if (Cur_COM.Read(RQ_Buffer, 0, 1) != 1)
                            return READ_FAIL;

                        pRQ._target.Version = RQ_Buffer[0];

                        // Get ROPE
                        if (Cur_COM.Read(RQ_Buffer, 0, 1) != 1)
                            return READ_FAIL;

                        pRQ._target.ROPE = RQ_Buffer[0];

                        // Get ROPD
                        if (Cur_COM.Read(RQ_Buffer, 0, 1) != 1)
                            return READ_FAIL;

                        pRQ._target.ROPD = RQ_Buffer[0];

                        // Get ACK
                        if (Cur_COM.Read(RQ_Buffer, 0, 1) != 1)
                            return READ_FAIL;

                        if (RQ_Buffer[0] != ACK)
                            return CMD_FAIL;

                        Target = pRQ._target.ShallowCopy();

                    } break;
                case GET_ID_CMD: //Get the chip ID
                    {
                        pRQ._target = Target.ShallowCopy();

                        // Get PID Length
                        RQ_Buffer = new byte[1];
                        if (Cur_COM.Read(RQ_Buffer, 0, 1) != 1)
                            return READ_FAIL;

                        pRQ._target.PIDLen = (byte)(RQ_Buffer[0] + 1);

                        // Get PID
                        RQ_Buffer = new byte[pRQ._target.PIDLen];
                        if (Cur_COM.Read(RQ_Buffer, 0, pRQ._target.PIDLen) != pRQ._target.PIDLen)
                            return READ_FAIL;

                        pRQ._target.PID = new byte[pRQ._target.PIDLen];
                        for (int i = 0; i < pRQ._target.PIDLen; i++)
                        {
                            pRQ._target.PID[i] = RQ_Buffer[i];
                        }

                        // Get ACK
                        if (Cur_COM.Read(RQ_Buffer, 0, 1) != 1)
                            return READ_FAIL;

                        if (RQ_Buffer[0] != ACK)
                            return CMD_FAIL;

                        Target = pRQ._target.ShallowCopy();

                    } break;
                case READ_CMD: //Read up to 256 bytes of memory starting from an address specified by the user
                    {
                        pRQ._target = Target.ShallowCopy();
                        Cur_COM.BaseStream.Flush() ;
                        Cur_COM.DiscardInBuffer();
                        // Send Read address and checksum
                        if (ACK_VALUE == ACKS.ST79) DataSize = 5;
                        else DataSize = 4;

                        RQ_Buffer = new byte[5];
                        byte Checksum = 0x00;

                        RQ_Buffer[0] = (byte)((pRQ._address >> 24) & 0x000000FF); Checksum = (byte)(Checksum ^ RQ_Buffer[0]);
                        RQ_Buffer[1] = (byte)((pRQ._address >> 16) & 0x000000FF); Checksum = (byte)(Checksum ^ RQ_Buffer[1]);
                        RQ_Buffer[2] = (byte)((pRQ._address >> 8) & 0x000000FF); Checksum = (byte)(Checksum ^ RQ_Buffer[2]);
                        RQ_Buffer[3] = (byte)((pRQ._address) & 0x000000FF); Checksum = (byte)(Checksum ^ RQ_Buffer[3]);
                        RQ_Buffer[4] = Checksum;

                        Cur_COM.Write(RQ_Buffer, 0, DataSize);

                        // Get ACK
                        if (Cur_COM.Read(RQ_Buffer, 0, 1) != 1)
                            return READ_FAIL;

                        if (RQ_Buffer[0] != ACK)
                            return CMD_FAIL;

                        // send data size to be read
                        RQ_Buffer[0] = (byte)(pRQ._length - 1);
                        RQ_Buffer[1] = (byte)~(pRQ._length - 1);

                        if (ACK_VALUE == ACKS.ST79) DataSize = 2;
                        else DataSize = 1;

                        Cur_COM.Write(RQ_Buffer, 0, DataSize);


                        // Get ACK
                        if (ACK_VALUE == ACKS.ST79)
                        {
                            if (Cur_COM.Read(RQ_Buffer, 0, 1) != 1)
                                return READ_FAIL;

                            if (RQ_Buffer[0] != ACK)
                                return CMD_FAIL;
                        }

                        pRQ._data = new byte[pRQ._length];
                        // Get data
                       // Thread.Sleep(200);
                        int timeoutome = 0;
                        int valuereceived;
                        valuereceived = Cur_COM.BytesToRead;
                        while ( Cur_COM.BytesToRead < pRQ._length)
                        {
                            if (valuereceived < Cur_COM.BytesToRead)
                            {
                                valuereceived = Cur_COM.BytesToRead;
                                timeoutome = 0;
                            }
                            else
                            {
                                timeoutome++;
                                Thread.Sleep(1);
                                if (timeoutome > 10 * UInt16.MaxValue)
                                {
                                    return  READ_FAIL;
                                }
                            }
                           
                        }

                        if (Cur_COM.Read(pRQ._data, 0, pRQ._length) != pRQ._length)
                            return READ_FAIL;

                    } break;
                case GO_CMD: //Jump to an address specified by the user to execute (a loaded) code
                    {
                        pRQ._target = Target.ShallowCopy();
                        // Send Go address and checksum
                        byte Checksum = 0x00;

                        if (ACK_VALUE == ACKS.ST79) DataSize = 5;
                        else DataSize = 4;


                        RQ_Buffer = new byte[5];

                        RQ_Buffer[0] = (byte)((pRQ._address >> 24) & 0x000000FF); Checksum = (byte)(Checksum ^ RQ_Buffer[0]);
                        RQ_Buffer[1] = (byte)((pRQ._address >> 16) & 0x000000FF); Checksum = (byte)(Checksum ^ RQ_Buffer[1]);
                        RQ_Buffer[2] = (byte)((pRQ._address >> 8) & 0x000000FF); Checksum = (byte)(Checksum ^ RQ_Buffer[2]);
                        RQ_Buffer[3] = (byte)((pRQ._address) & 0x000000FF); Checksum = (byte)(Checksum ^ RQ_Buffer[3]);
                        RQ_Buffer[4] = Checksum;

                        Cur_COM.Write(RQ_Buffer, 0, DataSize);

                        // Get ACK
                        if (Cur_COM.Read(RQ_Buffer, 0, 1) != 1)
                            return READ_FAIL;

                        // to be added when Go command can return ACK when performed
                        //if (RQ_Buffer[0] != ACK )
                        //return CMD_FAIL; 

                    } break;
                case WRITE_CMD: //Write maximum 256 bytes to the RAM or the NVM starting from an address specified by the user
                    {
                        pRQ._target = Target.ShallowCopy();

                        // Send Read address and checksum
                        byte Checksum = 0x00;

                        if (ACK_VALUE == ACKS.ST79) DataSize = 5;
                        else DataSize = 4;

                        RQ_Buffer = new byte[5];


                        RQ_Buffer[0] = (byte)((pRQ._address >> 24) & 0x000000FF); Checksum = (byte)(Checksum ^ RQ_Buffer[0]);
                        RQ_Buffer[1] = (byte)((pRQ._address >> 16) & 0x000000FF); Checksum = (byte)(Checksum ^ RQ_Buffer[1]);
                        RQ_Buffer[2] = (byte)((pRQ._address >> 8) & 0x000000FF); Checksum = (byte)(Checksum ^ RQ_Buffer[2]);
                        RQ_Buffer[3] = (byte)((pRQ._address) & 0x000000FF); Checksum = (byte)(Checksum ^ RQ_Buffer[3]);
                        RQ_Buffer[4] = (byte)(Checksum);

                        Cur_COM.Write(RQ_Buffer, 0, DataSize);

                        // Get ACK
                        if (Cur_COM.Read(RQ_Buffer, 0, 1) != 1)
                            return READ_FAIL;

                        if (RQ_Buffer[0] != ACK)
                            return CMD_FAIL;

                        byte checksum = 0x00;

                        // send data size to be writen
                        RQ_Buffer[0] = (byte)(pRQ._length - 1);
                        checksum = /*checksum ^*/ RQ_Buffer[0];

                        Cur_COM.Write(RQ_Buffer, 0, 1);


                        if (ACK_VALUE == ACKS.ST79)
                        {
                            for (int i = 0; i < pRQ._length; i++)
                                checksum = (byte)(checksum ^ pRQ._data[i]);
                        }

                        // Send data
                        Cur_COM.Write(pRQ._data, 0, pRQ._length);

                        if (ACK_VALUE == ACKS.ST79)
                        {
                            // send checksum
                            RQ_Buffer[0] = checksum;

                            Cur_COM.Write(RQ_Buffer, 0, 1);

                        }

                        // Get ACK
                        if (Cur_COM.Read(RQ_Buffer, 0, 1) != 1)
                            return READ_FAIL;

                        if (RQ_Buffer[0] != ACK)
                            return CMD_FAIL;

                        /*if (ACK_VALUE == ST79)
                        {
                            if (Cur_COM.receiveData(1, RQ_Buffer) != 1);
                               //return READ_FAIL;

                            if (RQ_Buffer[0] != ACK );
                               //return CMD_FAIL; 
                        }*/

                        //free(RQ_Buffer);
                    } break;


                /* TO DO */


                case ERASE_EXT_CMD: //Erase from one to all the NVM sectors
                    {
                        pRQ._target = Target.ShallowCopy();

                        if ((pRQ._wbSectors & 0xFF00) != 0xFF00)
                        {
                            byte checksum = (byte)(((pRQ._wbSectors) - 1) >> 8);
                            checksum = (byte)(checksum ^ ((pRQ._wbSectors) - 1));

                            RQ_Buffer = new byte[pRQ._length * 2 + 3];

                            for (int i = 0; i < RQ_Buffer.Length; i++)
                            {
                                RQ_Buffer[i] = 0xFF;
                            }

                            RQ_Buffer[0] = (byte)(((pRQ._wbSectors) - 1) >> 8);
                            RQ_Buffer[1] = (byte)(((pRQ._wbSectors) - 1));

                            for (int i = 2; i <= pRQ._wbSectors * 2; i += 2)
                            {
                                RQ_Buffer[i] = pRQ._data[i - 1];
                                RQ_Buffer[i + 1] = pRQ._data[i - 2];


                                checksum = (byte)(checksum ^ pRQ._data[i - 1]);
                                checksum = (byte)(checksum ^ pRQ._data[i - 2]);


                            }
                            RQ_Buffer[pRQ._wbSectors * 2 + 2] = checksum;
                        }
                        else
                        {
                            RQ_Buffer = new byte[3];

                            RQ_Buffer[0] = (byte)(pRQ._wbSectors >> 8);
                            RQ_Buffer[1] = (byte)(pRQ._wbSectors);
                            RQ_Buffer[2] = (byte)(RQ_Buffer[0] ^ RQ_Buffer[1]);
                        }

                        if (ACK_VALUE == ACKS.ST79) DataSize = 3;
                        else DataSize = 1;


                        Cur_COM.Write(RQ_Buffer, 0, pRQ._length * 2 + DataSize);

                        // Get ACK
                        if (Cur_COM.Read(RQ_Buffer, 0, 1) != 1)
                            return READ_FAIL;

                        if (RQ_Buffer[0] != ACK)
                            return CMD_FAIL;

                        /*if (ACK_VALUE == ST79)
                        {
                            // Get ACK
                            if (Cur_COM.receiveData(1, RQ_Buffer) != 1);
                               //return READ_FAIL;

                            if (RQ_Buffer[0] != ACK );
                               //return CMD_FAIL; 
                        }*/


                    } break;
                case ERASE_CMD: //Erase from one to all the NVM sectors
                    {
                        pRQ._target = Target.ShallowCopy();

                        if (pRQ._nbSectors != 0xFF)
                        {
                            byte checksum = /*0x00 ^ */(byte)(pRQ._nbSectors - 1);

                            RQ_Buffer = new byte[pRQ._length + 2];
                            for (int i = 0; i < RQ_Buffer.Length; i++)
                            {
                                RQ_Buffer[i] = 0xFF;
                            }

                            RQ_Buffer[0] = (byte)(pRQ._nbSectors - 1);
                            for (int i = 1; i <= pRQ._nbSectors; i++)
                            {
                                RQ_Buffer[i] = pRQ._data[i - 1];
                                checksum = (byte)(checksum ^ pRQ._data[i - 1]);
                            }
                            RQ_Buffer[pRQ._nbSectors + 1] = checksum;
                        }
                        else
                        {
                            RQ_Buffer = new byte[2];

                            RQ_Buffer[0] = pRQ._nbSectors;
                            RQ_Buffer[1] = (byte)~pRQ._nbSectors;
                        }

                        if (ACK_VALUE == ACKS.ST79) DataSize = 2;
                        else DataSize = 1;


                        Cur_COM.Write(RQ_Buffer, 0, pRQ._length + DataSize);

                        // Get ACK
                        if (Cur_COM.Read(RQ_Buffer, 0, 1) != 1)
                            return READ_FAIL;

                        if (RQ_Buffer[0] != ACK)
                            return CMD_FAIL;

                        /*if (ACK_VALUE == ST79)
                        {
                            // Get ACK
                            if (Cur_COM.receiveData(1, RQ_Buffer) != 1);
                               //return READ_FAIL;

                            if (RQ_Buffer[0] != ACK );
                               //return CMD_FAIL; 
                        }*/


                    } break;
                case WRITE_PROTECT_CMD: //Enable the write protection in a permanent way for some sectors
                    {

                        pRQ._target = Target.ShallowCopy();

                        byte checksum = (byte)(0x00 ^ (pRQ._nbSectors - 1));

                        RQ_Buffer = new byte[(pRQ._length + 2)];
                        RQ_Buffer[0] = (byte)(pRQ._nbSectors - 1);
                        for (int i = 1; i <= pRQ._nbSectors; i++)
                        {
                            RQ_Buffer[i] = pRQ._data[i - 1];
                            checksum = (byte)(checksum ^ pRQ._data[i - 1]);
                        }
                        RQ_Buffer[pRQ._nbSectors + 1] = checksum;


                        Cur_COM.Write(RQ_Buffer, 0, pRQ._length + 2);
                        return SEND_FAIL;

                        // Get ACK
                        if (Cur_COM.Read(RQ_Buffer, 0, 1) != 1)
                            return READ_FAIL;

                        if (RQ_Buffer[0] != ACK)
                            return CMD_FAIL;

                    } break;
                case WRITE_TEMP_UNPROTECT_CMD:
                    {
                        // Get ACK
                        if (Cur_COM.Read(RQ_Buffer, 0, 1) != 1)
                            return READ_FAIL;

                        if (RQ_Buffer[0] != ACK)
                            return CMD_FAIL;

                    } break; //Disable the write protection in a temporary way for all NVM sectors 
                case WRITE_PERM_UNPROTECT_CMD:
                    {
                        // Get ACK
                        if (Cur_COM.Read(RQ_Buffer, 0, 1) != 1)
                            return READ_FAIL;

                        if (RQ_Buffer[0] != ACK)
                            return CMD_FAIL;

                    } break; //Disable the write protection in a permanent way for all NVM sectors
                case READOUT_PROTECT_CMD:
                    {
                        // Get ACK
                        if (Cur_COM.Read(RQ_Buffer, 0, 1) != 1)
                            return READ_FAIL;

                        if (RQ_Buffer[0] != ACK)
                            return CMD_FAIL;

                    } break; //Disable the readout protection in a temporary way
                case READOUT_TEMP_UNPROTECT_CMD:
                    {
                        // Get ACK
                        if (Cur_COM.Read(RQ_Buffer, 0, 1) != 1)
                            return READ_FAIL;

                        if (RQ_Buffer[0] != ACK)
                            return CMD_FAIL;

                    } break; //Disable the readout protection in a permanent way
                case READOUT_PERM_UNPROTECT_CMD:
                    {
                        // Get ACK
                        if (Cur_COM.Read(RQ_Buffer, 0, 1) != 1)
                            return READ_FAIL;

                        if (RQ_Buffer[0] != ACK)
                            return CMD_FAIL;

                    } break;
            }

            return SUCCESS;


        }

        private uint STBL_ERASE(ushort NbSectors, byte[] pSectors = null)
        {
            if (!Cur_COM.IsOpen) return NO_CON_AVAILABLE;
            if ((Target.ERASE_CMD == false) && (Target.ERASE_EXT_CMD == false)) return CMD_NOT_ALLOWED;

            if ((Target.ERASE_CMD))
            {


                STBL_Request pRQ = new STBL_Request();
                pRQ._target = new TARGET_DESCRIPTOR();

                pRQ._cmd = (byte)ERASE_CMD;


                if (NbSectors == 0xFFFF)
                {
                    pRQ._nbSectors = 0xFF;
                    pRQ._length = 0;

                    byte Result = (byte)Send_RQ(pRQ);
                    if (Result != SUCCESS) return Result;

                    Progress = 0xFF / 10;
                }
                else
                {
                    ushort nErase = (ushort)(NbSectors / 10);
                    ushort Remain = (ushort)(NbSectors % 10);

                    int i = 0;
                    int j = 0; /*  This is for WORD */


                    if (nErase > 0)
                    {
                        for (i = 0; i < nErase; i++)
                        {
                            pRQ._length = 10;
                            pRQ._nbSectors = 10;
                            pRQ._data = new byte[10];


                            byte[] Convert = new byte[0xFF];


                            for (j = 0; j < 10; j++)
                            {

                                Convert[j] = pSectors[i * 10 * 2 + j * 2];

                            }

                            for (int k = 0; k < 10; k++)
                            {
                                pRQ._data[k] = Convert[k];
                            }

                            byte Result = (byte)Send_RQ(pRQ);
                            if (Result != SUCCESS) return Result;

                            Progress++;
                        }
                    }

                    if (Remain > 0)
                    {
                        pRQ._length = Remain;
                        pRQ._nbSectors = (byte)Remain;
                        pRQ._data = new byte[Remain];

                        byte[] Convert = new byte[0xFF];


                        for (j = 0; j < Remain; j++)
                        {

                            Convert[j] = pSectors[i * 10 * 2 + j * 2];

                        }


                        for (int k = 0; k < Remain; k++)
                        {
                            pRQ._data[k] = Convert[k];
                        }



                        byte Result = (byte)Send_RQ(pRQ);
                        if (Result != SUCCESS) return Result;

                        Progress++;
                    }


                }
            }


            if ((Target.ERASE_EXT_CMD))
            {


                STBL_Request pRQ = new STBL_Request();
                pRQ._target = new TARGET_DESCRIPTOR();

                pRQ._cmd = (byte)ERASE_EXT_CMD;


                if (NbSectors == 0xFFFF)
                {
                    pRQ._wbSectors = 0xFFFF;
                    pRQ._length = 0;

                    byte Result = (byte)Send_RQ(pRQ);
                    if (Result != SUCCESS) return Result;

                    Progress = 0xFF / 10;
                }
                else
                {
                    ushort nErase = (ushort)(NbSectors / 10);
                    ushort Remain = (ushort)(NbSectors % 10);

                    int i = 0;
                    int j = 0; /*  This is for WORD */


                    if (nErase > 0)
                    {
                        for (i = 0; i < nErase; i++)
                        {
                            pRQ._length = 10;
                            pRQ._wbSectors = 10;
                            pRQ._data = new byte[10 * 2];


                            ushort[] Convert = new ushort[0xFF];


                            for (j = 0; j < 10 * 2; j++)
                            {

                                Convert[j] = pSectors[i * 10 * 2 + j];

                            }



                            /*memcpy(pRQ._data, pSectors+(i*10), 10);*/


                            for (int k = 0; k < 10 * 2; k++)
                            {
                                pRQ._data[k] = (byte)Convert[k];
                            }

                            byte Result = (byte)Send_RQ(pRQ);
                            if (Result != SUCCESS) return Result;

                            Progress++;
                        }
                    }

                    if (Remain > 0)
                    {
                        pRQ._length = Remain;
                        pRQ._wbSectors = Remain;
                        pRQ._data = new byte[Remain * 2];

                        byte[] Convert = new byte[0xFF];


                        for (j = 0; j < Remain * 2; j++)
                        {

                            Convert[j] = pSectors[i * 10 * 2 + j];

                        }


                        for (int k = 0; k < Remain * 2; k++)
                        {
                            pRQ._data[k] = Convert[k];

                        }




                        byte Result = (byte)Send_RQ(pRQ);
                        if (Result != SUCCESS) return Result;

                        Progress++;
                    }


                }
            }


            return SUCCESS;
        }

        private uint FILES_ImageFromFile(string pPathFile, out CImage pImage, byte nAlternate)
        {
            uint Ret = FILES_BADPARAMETER;
            pImage = null;
            CImage obImage = new CImage(nAlternate, pPathFile, false, null);
            if (obImage != null)
            {
                if (obImage.GetImageState())
                {
                    pImage = obImage;
                    Ret = FILES_NOERROR;
                }
                else
                {
                    Ret = FILES_BADFORMAT;
                }
            }

            return Ret;
        }

        private uint FILES_SetImageName(ref CImage Image, string Name)
        {
            //AFX_MANAGE_STATE(AfxGetStaticModuleState());

            uint dwRet = FILES_NOERROR;

            CImage pImage = Image;
            pImage.SetName(Name);

            return dwRet;
        }

        private uint FILES_GetImageNbElement(ref CImage handle, ref ushort numberofelements)
        {
            uint dwRet = FILES_NOERROR;
            numberofelements = (ushort)handle.GetNbElements();
            return dwRet;

        }

        private uint FILES_GetImageElement(ref CImage Handle, uint dwRank, ref IMAGEELEMENT pElement)
        {
            uint dwRet = FILES_NOERROR;
            Handle.GetImageElement(dwRank, ref pElement);
            return dwRet;
        }

        private uint FILES_GetMemoryMapping(string pPathFile, ref uint Size, string MapName, ref ushort PacketSize, ref MAPPING pMapping, byte PagesPerSector)
        {
            int NumberOfSections = 0;

            NumberOfSections = STProductdescription.Sectors.Count + 1;


            if (Size == 0)
            {
                Size = (uint)(NumberOfSections - 1);
                return 0;
            }



            for (int pos = 0; pos < NumberOfSections; pos++)
            {
                MAPPINGSECTOR sector = new MAPPINGSECTOR();
                if (PacketSize == 0)
                {

                    short vPacketSize = (short)STProductdescription.prod.PacketSize;
                    PacketSize =(ushort) vPacketSize;

                    MapName = STProductdescription.prod.mapname;

                    PagesPerSector = (byte)STProductdescription.prod.PagesPerSector;
                    continue;
                }

                sector.Name = STProductdescription.Sectors[pos - 1].Name;
                sector.dwSectorIndex = STProductdescription.Sectors[pos - 1].dwSectorIndex;
                sector.dwStartAddress = STProductdescription.Sectors[pos - 1].dwStartAddress;
                sector.dwSectorSize = STProductdescription.Sectors[pos - 1].dwSectorSize;
                sector.bSectorType = (byte)STProductdescription.Sectors[pos - 1].bSectorType;

                sector.UseForOperation = true; //Ini.GetInt((LPCTSTR)section,(LPCTSTR)"UFO"    ,OPERATION_UPLOAD);
                sector.UseForErase = true;//Ini.GetInt((LPCTSTR)section,(LPCTSTR)"UFO"    ,OPERATION_UPLOAD);
                sector.UseForUpload = true; //Ini.GetInt((LPCTSTR)section,(LPCTSTR)"UFO"    ,OPERATION_UPLOAD);

                pMapping.pSectors.Add(sector);
                pMapping.NbSectors++;

            }

            return 0;
        }

        private uint FILES_SetImageElement(CImage Handle, uint dwRank, bool bInsert, IMAGEELEMENT Element)
        {

            uint dwRet = FILES_NOERROR;
            Handle.SetImageElement(dwRank, bInsert, Element);

            return dwRet;
        }

        private uint FILES_FilterImageForOperation(CImage Handle, MAPPING pMapping, uint Operation, bool bTruncateLeadFFForUpgrade)
        {
            uint dwRet = FILES_NOERROR;

            if (pMapping == null)
                return FILES_BADPARAMETER;

            Handle.FilterImageForOperation(pMapping, Operation, bTruncateLeadFFForUpgrade);

            return dwRet;
        }

        private uint STBL_DNLOAD(uint Address, byte[] pData, uint Length, bool bTruncateLeadFFForDnLoad)
        {
            if (!Cur_COM.IsOpen) return NO_CON_AVAILABLE;
            if (!Target.WRITE_CMD) return CMD_NOT_ALLOWED;

            //Progress = 0;
            byte[] Holder = pData;
            byte Result = (byte)SUCCESS;
            byte[] buffer = new byte[MAX_DATA_SIZE];

            uint nbuffer = (uint)(Length / MAX_DATA_SIZE);
            uint ramain = (uint)(Length % MAX_DATA_SIZE);
            uint startvalue = 0;

            uint Newramain = ramain;

            byte[] Empty = new byte[MAX_DATA_SIZE];

            for (int i = 0; i < MAX_DATA_SIZE; i++)
            {
                Empty[i] = 0xFF;
            }


            if (nbuffer > 0)
            {
                for (uint i = 1; i <= nbuffer; i++)
                {
                    for (uint j = 0; j < MAX_DATA_SIZE ; j++)
                    {
                        buffer[j] = 0xFF;
                        buffer[j] = pData[startvalue + j];
                    }

                    bool AllFFs = false;
                    bool isDiff = true;
                    for (uint j = 0; j < MAX_DATA_SIZE; j++)
                    {
                        if (buffer[j] != Empty[j])
                        {
                            isDiff = false;
                            break;
                        }
                    }
                    if (isDiff && bTruncateLeadFFForDnLoad)
                    {
                        AllFFs = true;
                        //_sleep(1);
                    }

                    if (!AllFFs)
                    {
                        Result = STBL_WRITE((uint)Address, (byte)MAX_DATA_SIZE, buffer);
                        if (Result != SUCCESS) return Result;
                    }

                    startvalue += MAX_DATA_SIZE;     
                    Address += MAX_DATA_SIZE;
                    Progress++;
                }
            }

            if (ramain > 0)
            {
                for (int j = 0; j < MAX_DATA_SIZE; j++)
                {
                    buffer[j] = 0xFF;
                }
                /* This is a workaround for an issue on STM32 Boot-loader to be verified in v2.3.0*/

                uint newdiv = (uint)(ramain % 4);

                /*if((ramain%2) != 0) Newramain++;
                if((ramain%4) != 0) Newramain+=4;*/

                Newramain += 4 - newdiv;


                /* end of */

                Result = STBL_READ(Address, (byte)Newramain, buffer);
                if (Result != SUCCESS) return Result;

                for (int k = 0; k < ramain; k++)
                {
                    buffer[k] = pData[k];

                }


                bool AllFFs = false;
                bool isDiff = true;
                for (int j = 0; j < ramain; j++)
                {
                    if (Empty[j] != buffer[j])
                    {
                        isDiff = false;
                        break;
                    }
                }


                if (isDiff && bTruncateLeadFFForDnLoad)
                    AllFFs = true;

                if (!AllFFs)
                {

                    Result = STBL_WRITE(Address, (byte)Newramain/* ramain*/, buffer);
                    if (Result != SUCCESS) return Result;
                }
                Progress++;
            }

            pData = Holder;
            return Result;
        }

        byte STBL_WRITE(uint address, byte size, byte[] pData)
        {
            if (!Cur_COM.IsOpen) return (byte)NO_CON_AVAILABLE;
            if (!Target.WRITE_CMD) return (byte)CMD_NOT_ALLOWED;

            if (pData == null) return (byte)INPUT_PARAMS_MEMORY_ALLOCATION_ERROR;

            STBL_Request pRQ = new STBL_Request();
            pRQ._target = new TARGET_DESCRIPTOR();

            pRQ._cmd = (byte)WRITE_CMD;
            pRQ._address = address;

            if ((size % 2) != 0) size++;
            pRQ._length = size;

            pRQ._data = new byte[MAX_DATA_SIZE];

            for (int i = 0; i < MAX_DATA_SIZE; i++)
            {
                pRQ._data[i] = 0xFF;
                if (i < size)
                {
                    pRQ._data[i] = pData[i];
                }
            }

            byte Result = (byte)Send_RQ(pRQ);

            if (Result != SUCCESS)
            {
                return Result;
            }


            return (byte)SUCCESS;
        }


        byte STBL_READ(uint Address, byte Size, byte[] pData)
        {
            if (!Cur_COM.IsOpen) return (byte)NO_CON_AVAILABLE;
            if (!Target.READ_CMD) return (byte)CMD_NOT_ALLOWED;

            if (pData == null) return (byte)INPUT_PARAMS_MEMORY_ALLOCATION_ERROR;

            STBL_Request pRQ = new STBL_Request();
            pRQ._target = new TARGET_DESCRIPTOR();
            pRQ._cmd = (byte)READ_CMD;
            pRQ._address = Address;

            if ((Size % 2) != 0) Size++;
            pRQ._length = Size;

            pRQ._data = new byte[(MAX_DATA_SIZE)];

            for (int j = 0; j < MAX_DATA_SIZE; j++)
            {
                pRQ._data[j] = 0xFF;
            }


            byte Result = (byte)Send_RQ(pRQ);

            if (Result != SUCCESS)
                return Result;

            for (int j = 0; j < Size; j++)
            {
                pData[j] = pRQ._data[j];
            }

            return (byte)SUCCESS;
        }

        byte STBL_VERIFY(uint Address, byte[] pData, uint Length, bool bTruncateLeadFFForDnLoad)
        {
            if (!Cur_COM.IsOpen) return (byte)NO_CON_AVAILABLE;
            if (!Target.READ_CMD) return (byte)CMD_NOT_ALLOWED;
            bool AllFFs;
            byte[] Holder = pData;
            byte Result = (byte)SUCCESS;
            byte[] buffer = new byte[MAX_DATA_SIZE];
            bool isDiff;
            uint nbuffer = Length / MAX_DATA_SIZE;
            uint ramain = Length % MAX_DATA_SIZE;
            uint startvalue = 0;
            byte[] Empty = new byte[MAX_DATA_SIZE];

            for (int j = 0; j < MAX_DATA_SIZE; j++)
            {
                Empty[j] = 0xFF;
            }


            if (nbuffer > 0)
            {
                for (uint i = 1; i <= nbuffer; i++)
                {
                     AllFFs = false;

                    isDiff = true;
                    for (int j = 0; j < MAX_DATA_SIZE; j++)
                    {
                        if (Empty[j] != pData[startvalue + j])
                        {
                            isDiff = false;
                            break;
                        }
                    }

                    if (isDiff && bTruncateLeadFFForDnLoad)
                    {
                        AllFFs = true;
                        //_sleep(1);
                    }

                    if (!AllFFs)
                    {

                        for (int j = 0; j < MAX_DATA_SIZE; j++)
                        {
                            buffer[j] = 0xFF;
                        }

                        Result = STBL_READ(Address, (byte)MAX_DATA_SIZE, buffer);
                        if (Result != SUCCESS)
                        {
                            return Result;
                        }

                        isDiff = true;
                        for (int j = 0; j < MAX_DATA_SIZE; j++)
                        {
                            if (buffer[j] != pData[startvalue + j])
                            {
                                isDiff = false;
                                break;
                            }
                        }

                        if (!isDiff)
                        {
                            return (byte)CMD_NOT_ALLOWED; // verify fail
                        }
                    }
                    startvalue = startvalue + MAX_DATA_SIZE;
                    Address += MAX_DATA_SIZE;
                    Progress++;
                }
            }

            if (ramain > 0)
            {
                AllFFs = false;
                isDiff = true;
                for (int j = 0; j < MAX_DATA_SIZE; j++)
                {
                    if (buffer[j] != pData[startvalue + j])
                    {
                        isDiff = false;
                        break;
                    }
                }
                if (isDiff && bTruncateLeadFFForDnLoad)
                    AllFFs = true;

                if (!AllFFs)
                {
                    for (int j = 0; j < MAX_DATA_SIZE; j++)
                    {
                        buffer[j] = 0xFF;
                    }

                    Result = STBL_READ(Address, (byte)ramain, buffer);
                    if (Result != SUCCESS) return Result;

                    isDiff = true;
                    for (int j = 0; j < MAX_DATA_SIZE; j++)
                    {
                        if (Empty[j] != pData[startvalue + j])
                        {
                            isDiff = false;
                            break;
                        }
                    }
                    if (!isDiff)
                    {
                        return (byte)CMD_NOT_ALLOWED; // verify fail
                    }
                }
                Progress++;
            }

            pData = Holder;
            return Result;
        }

    }


    class Commands
    {
        public bool GET_CMD; //Get the version and the allowed commands supported by the current version of the boot loader
        public bool GET_VER_ROPS_CMD; //Get the BL version and the Read Protection status of the NVM
        public bool GET_ID_CMD; //Get the chip ID
        public bool SET_SPEED_CMD; //Change the CAN baudrate
        public bool READ_CMD; //Read up to 256 bytes of memory starting from an address specified by the user
        public bool GO_CMD; //Jump to an address specified by the user to execute (a loaded) code
        public bool WRITE_CMD; //Write maximum 256 bytes to the RAM or the NVM starting from an address specified by the user
        public bool ERASE_CMD; //Erase from one to all the NVM sectors
        public bool ERASE_EXT_CMD; //Erase from one to all the NVM sectors
        public bool WRITE_PROTECT_CMD; //Enable the write protection in a permanent way for some sectors
        public bool WRITE_TEMP_UNPROTECT_CMD; //Disable the write protection in a temporary way for all NVM sectors
        public bool WRITE_PERM_UNPROTECT_CMD; //Disable the write protection in a permanent way for all NVM sectors
        public bool READOUT_PROTECT_CMD; //Enable the readout protection in a permanent way
        public bool READOUT_TEMP_UNPROTECT_CMD; //Disable the readout protection in a temporary way
        public bool READOUT_PERM_UNPROTECT_CMD; //Disable the readout protection in a permanent way
    }


    class CImage
    {

        const uint OPERATION_DETACH =	0;
        const uint OPERATION_RETURN	=1;
        const uint OPERATION_UPLOAD	=2;
        const uint OPERATION_ERASE	=	3;
        const uint	OPERATION_DNLOAD=	4;

                    const uint  BIT_READABLE=	1;
             const uint  BIT_ERASABLE=	2;
             const uint BIT_WRITEABLE=	4;



        private string m_LastError;
        private byte m_bAlternate;
        private List<IMAGEELEMENT> m_pElements = new List<IMAGEELEMENT>();
        private bool m_ImageState;
        private bool m_bNamed;
        //UNICODE	char		m_Name[255];
        private string m_Name;

        //private bool LoadS19(string pFilePath);
        private bool LoadHEX(string pFilePath)
        {

            IMAGEELEMENT Element = new IMAGEELEMENT(), pPrevElement;
            bool bRet = true;
            bool bConcatenate;


            //fp = _tfopen(pFilePath, _T("r"));
            string text = System.IO.File.ReadAllText(pFilePath);

            ulong target_address = 0,                 /* address of array index */
                          base_address = 0,
                          extended_address = 0,
                          address = 0,                        /* offset/address from hexfile */
                          i = 0,                              /* counterindex */
                          checksum = 0,                       /* checksum from hexfile */
                          byte_count = 0,                     /* bytes per line in hexfile */
                          sum_var = 0;                        /* to calculate checksum */

            string separator = "0";                  /* separator string in hexfile */
            string character = "0";                    /* date from hexfile */
            char colon = '0';                       /* begin of line in hexfile */
            ushort lineno = 0;
            bool last_byte = false;						    /* conversion  end/begin */
            List<byte> bufferlinked = new List<byte>();

            //UNICODE char message[255];		// error message
            int currentpos = 0;
            string texttest = "";
            do
            {
                sum_var = 0;                          /* checksum calculation begin */
                //fscanf(fp,"%1c",&colon);
                //fscanf_s(fp, "%1c", &colon, sizeof(colon));
                colon = text[currentpos];
                currentpos++;
                if (currentpos + 1 == text.Length)
                { // Detect End Of File & Exit if S9 or S8 or S7 record not found
                    last_byte = true;
                }
                else if (colon == ':')
                {                   /* do only if intel hexfile */
                    //fscanf(fp,"%2x",&byte_count);
                    //fscanf_s(fp, "%2x", &byte_count, sizeof(byte_count));
                    byte_count = ulong.Parse(text.Substring(currentpos, 2), NumberStyles.HexNumber);
                    currentpos += 2;
                    sum_var += byte_count;
                    //fscanf(fp,"%4x",&address);
                    //fscanf_s(fp, "%4x", &address, sizeof(address));
                     texttest = text.Substring(currentpos, 4);
                    address = ulong.Parse(text.Substring(currentpos, 4), NumberStyles.HexNumber);
                    currentpos += 4;
                    //fscanf(fp,"%2x",&separator);
                    //fscanf_s(fp, "%2x", &separator, sizeof(separator));
                    separator = text.Substring(currentpos, 2);
                    currentpos += 2;

                    if (ulong.Parse(separator, NumberStyles.HexNumber)== 0)
                    {
                        Element = new IMAGEELEMENT();
                        target_address = (extended_address << 16) + (base_address << 4) + address;
                        sum_var = sum_var + (address >> 8) + (address % 256);
                        Element.dwAddress = (uint)target_address;
                        Element.dwDataLength = (uint)byte_count;
                        Element.Data = new byte[Element.dwDataLength];

                        for (i = 0; i < byte_count; i++)
                        {
                            //fscanf_s(fp,"%2x",&character, sizeof(character));
                            character = (text.Substring(currentpos, 2));
                            currentpos += 2;
                            sum_var += ulong.Parse(character, NumberStyles.HexNumber);
                            Element.Data[i] = (byte) ulong.Parse(character, NumberStyles.HexNumber);
                        }

                        sum_var += ulong.Parse(separator, NumberStyles.HexNumber);
                        //fscanf_s(fp,"%2x",&checksum, sizeof(checksum));
                        checksum = ulong.Parse(text.Substring(currentpos, 2), NumberStyles.HexNumber);
                        currentpos += 2;
                        sum_var = (sum_var % 256);
                        if (((checksum + sum_var) % 256) != 0)
                        {
                            //UNICODE wsprintf(message, "FILE : line {0}: Bad hexadecimal checksum!", lineno);
                            Console.Write("FILE : line {0}: Bad hexadecimal checksum!", lineno);
                            //LDisplayError(message);
                            bRet = false;
                            break;
                        }
                        // The Element is correct. Check if this element is contiguous with this one. In this case we'll not
                        // create a new element but concatenate data
                        bConcatenate = false;
                        if (GetNbElements() != 0)
                        {
                            pPrevElement = m_pElements[(int)(GetNbElements() - 1)];

                            if (pPrevElement.dwAddress + pPrevElement.dwDataLength == Element.dwAddress)
                                bConcatenate = true;
                            else
                            {
                                bConcatenate = false;
                            }


                            if (!bConcatenate)
                                SetImageElement(GetNbElements(), true, Element);
                            else
                            {
                                // pPrevElement.Data = new byte[pPrevElement.dwDataLength + Element.dwDataLength];

                                //memcpy(pPrevElement.Data+pPrevElement.dwDataLength, Element.Data, Element.dwDataLength);

                                for (int j = 0; j < Element.dwDataLength; j++)
                                {
                                    bufferlinked.Add(Element.Data[j]);
                                }

                                pPrevElement.dwDataLength = pPrevElement.dwDataLength + Element.dwDataLength;
                                pPrevElement.Data = bufferlinked.ToArray();
                            }
                        }
                        else
                        {
                            bufferlinked = new List<byte>();
                            for (int j = 0; j < Element.Data.Length; j++)
                            {
                                bufferlinked.Add(Element.Data[j]);
                            }
                            SetImageElement(GetNbElements(), true, Element);
                        }
                    }
                    else if (ulong.Parse(separator, NumberStyles.HexNumber) == 1)
                    {
                        sum_var = sum_var + (address >> 8) + (address % 256) + ulong.Parse(separator, NumberStyles.HexNumber);
                        sum_var = (sum_var % 256);
                        //fscanf_s(fp,"%2x",&checksum, sizeof(checksum));

                        checksum = ulong.Parse(text.Substring(currentpos, 2), NumberStyles.HexNumber);
                        currentpos += 2;

                        if (((checksum + sum_var) % 256) != 0)
                        {
                            //UNICODE wsprintf(message, "FILE : line {0}: Bad hexadecimal checksum!", lineno);
                            Console.Write("FILE : line {0}: Bad hexadecimal checksum!", lineno);
                            //LDisplayError(message);
                            bRet = true;
                            break;
                        }
                        else
                        {
                            last_byte = true;         /* eof */
                        }
                    }
                    else if (ulong.Parse(separator, NumberStyles.HexNumber) == 2)
                        {
                            //fscanf_s(fp,"%4x",&base_address,sizeof(base_address));
                            checksum = ulong.Parse(text.Substring(currentpos, 4), NumberStyles.HexNumber);
                            currentpos += 4;
                            //fscanf(fp,"%2x",&checksum);
                            //fscanf_s(fp, "%2x", &checksum, sizeof(checksum));
                            checksum = ulong.Parse(text.Substring(currentpos, 2), NumberStyles.HexNumber);
                            currentpos += 2;

                            sum_var = sum_var + (address >> 8) + (address % 256) + ulong.Parse(separator, NumberStyles.HexNumber)
                                              + (base_address >> 8) + (base_address % 256);
                            sum_var = (sum_var % 256);
                            if (((checksum + sum_var) % 256) != 0)
                            {
                                //wsprintf(message, "FILE : line {0}: Bad hexadecimal checksum!", lineno);
                                Console.Write("FILE : line {0}: Bad hexadecimal checksum!", lineno);
                                //LDisplayError(message);
                                bRet = false;
                                break;
                            }
                        }
                        else if (ulong.Parse(separator, NumberStyles.HexNumber) == 3)
                            {
                                sum_var = sum_var + (address >> 8) + (address % 256) + ulong.Parse(separator, NumberStyles.HexNumber);
                                for (i = 0; i < byte_count; i++)
                                {
                                    //fscanf(fp,"%2x",&character);
                                    //fscanf_s(fp, "%2x", &character, sizeof(character));
                                    character = text.Substring(currentpos, 2);
                                    currentpos += 2;
                                    sum_var += ulong.Parse(character, NumberStyles.HexNumber);
                                }

                                //fscanf_s(fp, "%2x", &checksum, sizeof(checksum));
                                checksum = ulong.Parse(text.Substring(currentpos, 2), NumberStyles.HexNumber);
                                currentpos += 2;

                                sum_var = (sum_var % 256);
                                if (((checksum + sum_var) % 256) != 0)
                                {
                                    //wsprintf(message, "FILE : line {0}: Bad hexadecimal checksum!", lineno);
                                    Console.Write("FILE : line {0}: Bad hexadecimal checksum!", lineno);
                                    //LDisplayError(message);
                                    bRet = false;
                                    break;
                                }
                            }
                            else if (ulong.Parse(separator, NumberStyles.HexNumber) == 4)
                                {
                                    extended_address = ulong.Parse(text.Substring(currentpos, 4), NumberStyles.HexNumber);
                                    currentpos += 4;
                                    string value = text.Substring(currentpos, 2);
                                    checksum = ulong.Parse(text.Substring(currentpos, 2), NumberStyles.HexNumber);
                                    currentpos += 2;

                                    sum_var = sum_var + (address >> 8) + (address % 256) + ulong.Parse(separator, NumberStyles.HexNumber)
                                             + (extended_address >> 8) + (extended_address % 256);
                                    sum_var = (sum_var % 256);
                                    if (((checksum + sum_var) % 256) != 0)
                                    {
                                        //wsprintf(message, "FILE : line {0}: Bad hexadecimal checksum!", lineno);
                                        Console.Write("FILE : line {0}: Bad hexadecimal checksum!", lineno);
                                        //LDisplayError(message);
                                        bRet = false;
                                        break;
                                    }
                                }
                                else if (separator.LastIndexOf((0x5).ToString()) != -1)
                                    {
                                        sum_var = sum_var + (address >> 8) + (address % 256) + ulong.Parse(separator, NumberStyles.HexNumber);
                                        for (i = 0; i < byte_count; i++)
                                        {
                                            //fscanf(fp,"%2x",&character);
                                            //fscanf_s(fp, "%2x", &character, sizeof(character));
                                            character = text.Substring(currentpos, 2);
                                            currentpos += 2;
                                            sum_var += ulong.Parse(character, NumberStyles.HexNumber);
                                        }

                                        checksum = ulong.Parse(text.Substring(currentpos, 2), NumberStyles.HexNumber);
                                        currentpos += 2;

                                        sum_var = (sum_var % 256);
                                        if (((checksum + sum_var) % 256) != 0)
                                        {
                                            //wsprintf(message, "FILE : line {0}: Bad hexadecimal checksum!", lineno);
                                            Console.Write("FILE : line {0}: Bad hexadecimal checksum!", lineno);
                                            //LDisplayError(message);
                                            bRet = false;
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        //wsprintf(message, "FILE : line {0}: Not in Intel Hex format!", lineno);
                                        Console.Write("FILE : line {0}: Not in Intel Hex format!", lineno);
                                        //LDisplayError(message);
                                        bRet = false;
                                        break;
                                    }
                }
                else if ((colon == '\r') || (colon == '\n'))
                { // Skip CR:0x0D & LF:0x0A Characters
                    lineno++; // increment number of line
                    if (text.Length == currentpos + 1)
                    { // Detect End Of File & Exit if last record not found
                        last_byte = true;         /* eof */
                    }
                }
                else if (colon == ' ')
                { // Skip Space ' ' Characters
                    if (currentpos + 1 == text.Length)
                    { // Detect End Of File & Exit if last record not found
                        last_byte = true;         /* eof */
                    }
                }
                else
                {
                    //wsprintf(message, "FILE : line %i: Not in Intel Hex format!", lineno);
                    Console.Write("FILE : line {0}: Not in Intel Hex format!", lineno);
                    //LDisplayError(message);
                    bRet = false;
                    break;
                }
                
            }
            while (!last_byte);

            if (bRet)
                CompactElements();
            return bRet;
        }

        private bool LoadBIN(string pFilePath)
        {
            return false;
        }

        private bool SaveS19(string pFilePath)
        {
            return false;
        }
        private bool SaveHEX(string pFilePath)
        {
            return false;
        }

        void LDisplayError(string Str)
        { //UNICODE lstrcpy(m_LastError, Str);
            m_LastError = Str;
        }
        private bool ExistsElementsAtAddress(uint Address)
        {
            return false;
        }

        void CompactElements()
        {

            int el;
            List<IMAGEELEMENT> pArray =m_pElements;

            pArray.Sort();

            for (el = m_pElements.Count- 1; el > 0; el--)
            {
                IMAGEELEMENT pElement2, pElement1;
                pElement2 = m_pElements[el];
                pElement1 = m_pElements[el - 1];

                if (pElement1.dwAddress + pElement1.dwDataLength == pElement2.dwAddress) // Contiguous ?
                {
                    pElement1.Data = new byte[pElement1.dwDataLength + pElement2.dwDataLength];
//memcpy(pElement1.Data + pElement1.dwDataLength, pElement2.Data, pElement2.dwDataLength);
                    for (int j = 0; j < pElement2.dwDataLength;j++)
                    {
                        pElement1.Data[pElement1.dwDataLength+j] = pElement2.Data[j];
                    }

                    pElement1.dwDataLength += pElement2.dwDataLength;
                    m_pElements.RemoveAt(el);
                }
            }

        }

        public CImage(byte bAlternate, bool bNamed, string Name)
        {
            m_bAlternate = bAlternate;
            m_ImageState = true;
            m_pElements = new List<IMAGEELEMENT>();
            m_bNamed = bNamed;
            if (bNamed)
                // UNICODE	lstrcpy(m_Name, Name);
                m_Name = Name;
        }

        public CImage(CImage pSource)
        {
            int i;

            m_bAlternate = pSource.m_bAlternate;
            m_ImageState = pSource.m_ImageState;
            // UNICODE	strcpy(m_LastError, pSource.m_LastError);
            m_LastError = pSource.m_LastError;
            m_bNamed = pSource.m_bNamed;
            if (m_bNamed)
                // UNICODE	lstrcpy(m_Name, pSource.m_Name);
                m_Name = pSource.m_Name;

            m_pElements = new List<IMAGEELEMENT>();
            for (i = 0; i < pSource.m_pElements.Count(); i++)
            {
                IMAGEELEMENT pElementSource, pElementDest;

                pElementSource = pSource.m_pElements[i];

                pElementDest = new IMAGEELEMENT();
                pElementDest.dwAddress = pElementSource.dwAddress;
                pElementDest.dwDataLength = pElementSource.dwDataLength;

                pElementDest.Data = new byte[pElementDest.dwDataLength];

                for (int j = 0; j < pElementDest.dwDataLength; j++)
                {
                    pElementDest.Data[j] = pElementSource.Data[j];
                }
                m_pElements.Add(pElementDest);
            }
        }

        public CImage(MAPPING pMapping, bool bNamed, string Name)
        {
            // We need to handle each sector in mapping
            int sec;

            m_bAlternate = pMapping.nAlternate;
            m_pElements = new List<IMAGEELEMENT>();
            List<MAPPINGSECTOR> pSector = pMapping.pSectors;

            for (sec = 0; sec < pMapping.NbSectors; sec++)
            {
                if (!pSector[sec].UseForOperation)
                {
                    continue;
                }

                IMAGEELEMENT pNewElement = new IMAGEELEMENT();

                //pNewElement.Index = pSector.dwSectorSize;
                pNewElement.dwAddress = pSector[sec].dwStartAddress;
                pNewElement.dwDataLength = pSector[sec].dwSectorSize;
                // Allocate the memory for the data but leave it uninitialized
                pNewElement.Data = new byte[pNewElement.dwDataLength];
                for (int i = 0; i < pNewElement.dwDataLength; i++)
                {
                    pNewElement.Data[i] = 0xFF;
                }
                m_pElements.Add(pNewElement);

                pNewElement = null;
            }

            m_ImageState = true;
            m_bNamed = bNamed;
            if (bNamed)
                //UNICODE	lstrcpy(m_Name, Name);
                m_Name = Name;

        }

        public CImage(byte bAlternate, string pFilePath, bool bNamed, string Name)
        {
            string Drive, Ext;

            bool bRet = false;

            m_bAlternate = bAlternate;
            m_ImageState = true;
            m_pElements = new List<IMAGEELEMENT>();

            Ext = Path.GetExtension(pFilePath);
            Ext = Ext.ToUpper();
            Drive = Path.GetPathRoot(pFilePath);

            if (Ext.IndexOf(".S19") != -1)
                bRet = false;// LoadS19(pFilePath);
            else if (Ext.IndexOf(".HEX") != -1)
                bRet = LoadHEX(pFilePath);
            else if (Ext.IndexOf(".BIN") != -1)
                bRet = false;// LoadBIN(pFilePath);

            m_ImageState = bRet;
            m_bNamed = bNamed;
            if (bNamed)
                //UNICODE lstrcpy(m_Name, Name);
                m_Name = Name;
        }

        public bool DumpToFile(string pFilePath)
        {
            return false;
        }

        public byte GetAlternate() { return m_bAlternate; }
        public bool GetImageState() { return m_ImageState; }

        public bool GetName(string Name)
        {
            if (m_bNamed)
                //lstrcpy(Name, m_Name);
                Name = m_Name;
            return m_bNamed;
        }
        public void SetName(string Name)
        { //lstrcpy(m_Name, Name);
            Name = m_Name;
            m_bNamed = true;
        }
        public bool GetBuffer(uint dwAddress, uint dwSize, byte[] pBuffer)
        {
            int el;
            uint elStart, elEnd, secStart, secEnd;

            if (!m_ImageState)
                return false;
            for (int i = 0; i < dwSize; i++)
            {
                pBuffer[i] = 0xFF;

            }
            secStart = dwAddress;
            secEnd = dwAddress + dwSize;

            for (el = m_pElements.Count - 1; el >= 0; el--)
            {
                IMAGEELEMENT pElement;
                pElement = m_pElements[el];

                elStart = pElement.dwAddress;
                elEnd = pElement.dwDataLength + elStart;

                if ((elStart >= secEnd) || (elEnd <= secStart))
                    continue;

                if ((elStart <= secStart) && (elEnd <= secEnd))
                {
                    for (int i = 0; i < elEnd - secStart; i++)
                    {
                        pBuffer[i] = pElement.Data[secStart + i];
                    }
                }
                else if ((elStart >= secStart) && (elEnd <= secEnd))
                {
                   for (int i = 0; i <  pElement.dwDataLength; i++)
                    {
                        pBuffer[i + elStart - secStart] = pElement.Data[secStart + i];
                    }
                }
                else if ((elStart >= secStart) && (elEnd >= secEnd))
                {
                    for (int i = 0; i < secEnd - elStart; i++)
                    {
                        pBuffer[i + elStart - secStart] = pElement.Data[i];
                    }
                }
                else if ((elStart <= secStart) && (elEnd >= secEnd))
                {
                    for (int i = 0; i < dwSize; i++)
                    {
                        pBuffer[i] = pElement.Data[i + secStart];
                    }
                }
            }

            return true;
        }

        public uint GetNbElements() 
        {
            return (uint)m_pElements.Count; 
        }


        public bool SetImageElement(uint dwRank, bool bInsert, IMAGEELEMENT Element)
        {
            IMAGEELEMENT pNewElement;

            if (!m_ImageState)
                return false;

            if (dwRank > (uint)m_pElements.Count)
                return false;

            if (bInsert)
            {
                pNewElement = new IMAGEELEMENT();
                pNewElement.dwAddress = Element.dwAddress;
                pNewElement.dwDataLength = Element.dwDataLength;
                pNewElement.Data = new byte[Element.dwDataLength];

                for (int i = 0; i < Element.dwDataLength; i++)
                {
                    pNewElement.Data[i] = Element.Data[i];
                }

                m_pElements.Insert((int)dwRank, pNewElement);
            }
            else
            {
                
                 m_pElements[(int)dwRank].dwAddress = Element.dwAddress;
                 m_pElements[(int)dwRank].dwDataLength = Element.dwDataLength;
                 m_pElements[(int)dwRank].Data = new byte[m_pElements[(int)dwRank].dwDataLength];

                 for (int i = 0; i < Element.dwDataLength; i++)
                {
                      m_pElements[(int)dwRank].Data[i] = Element.Data[i];
                }

            }
            return true;
        }

        public bool GetImageElement(uint dwRank, ref IMAGEELEMENT pElement)
        {
            IMAGEELEMENT pListElement;

            if (!m_ImageState)
                return false;
            if (dwRank >= (uint)m_pElements.Count())
                return false;

            if (pElement == null)
                return false;

            pListElement = m_pElements[(int)dwRank];
            pElement.dwAddress = pListElement.dwAddress;
            pElement.dwDataLength = pListElement.dwDataLength;
            if (pElement.Data != null)
            {
                for (int i = 0; i < pElement.dwDataLength; i++)
                {
                    pElement.Data[i] = pListElement.Data[i];
                }
            }

            return true;
        }

        public bool FilterImageForOperation(MAPPING pMapping, uint Operation, bool bTruncateLeadFF)
        {


            int el;
            int sec;
            uint elStart, elEnd, secStart, secEnd;
            int Cnt;

            if (!m_ImageState)
                return false;

            if (Operation == OPERATION_ERASE)
            {
                // ERase is a bit different, as it relies on sectors
                List<IMAGEELEMENT> NewElements = new List<IMAGEELEMENT>();

                // Let's browse all the sectors
                for (sec = (int)pMapping.NbSectors - 1; sec >= 0; sec--)
                {
                    byte[] Buffer;

                    // We need to handle each sector in mapping
                    MAPPINGSECTOR pSector = pMapping.pSectors[sec];
                    IMAGEELEMENT pNewElement;

                    if (!pSector.UseForOperation)
                        continue;

                    if ((Operation == OPERATION_ERASE) && ((pSector.bSectorType & BIT_ERASABLE) != BIT_ERASABLE))
                        continue;

                    secStart = pSector.dwStartAddress;
                    secEnd = pSector.dwStartAddress + pSector.dwSectorSize;
                    Buffer = new byte[pSector.dwSectorSize];

                    if (GetBuffer(secStart, pSector.dwSectorSize, Buffer))
                    {
                        uint i;
                        bool bAllFFs = true;
                        //if (bTruncateLeadFF)
                        {
                            for (i = 0; i < pSector.dwSectorSize; i++)
                            {
                                if (Buffer[i] != 0xFF)
                                {
                                    bAllFFs = false;
                                    break;
                                }
                            }
                        }
                        //else bAllFFs = FALSE;

                        if (!bAllFFs)
                        {
                            pNewElement = new IMAGEELEMENT();
                            pNewElement.dwAddress = secStart;
                            pNewElement.dwDataLength = 0;
                            pNewElement.Data = null;
                            NewElements.Add(pNewElement);
                        }

                    }

                }
                for (el = m_pElements.Count - 1; el >= 0; el--)
                    DestroyImageElement((uint)el);
                for (el = NewElements.Count - 1; el >= 0; el--)
                    m_pElements.Add(NewElements[el]);
                return true;

            }

            for (el = m_pElements.Count - 1; el >= 0; el--)
            {
                Cnt = 0;
                IMAGEELEMENT pElement;
                bool bAllFFs = true;
                pElement = m_pElements[el];

                elStart = pElement.dwAddress;
                elEnd = pElement.dwDataLength + elStart;

                if (Operation == OPERATION_DNLOAD)
                {
                    uint i;

                    if (bTruncateLeadFF)
                    {
                        for (i = 0; i < pElement.dwDataLength; i++)
                        {
                            if (pElement.Data[i] != 0xFF)
                            {
                                bAllFFs = false;
                                break;
                            }
                        }
                    }
                    else bAllFFs = false;

                    if (bAllFFs)
                    {
                        DestroyImageElement((uint)(el + Cnt));
                        continue; // Skip all FFs elements for erase and upgrade. Useless
                    }
                }

                // Let's browse all the sectors
                for (sec = (int)pMapping.NbSectors - 1; sec >= 0; sec--)
                {
                    // We need to handle each sector in mapping
                    MAPPINGSECTOR pSector = pMapping.pSectors[sec];
                    IMAGEELEMENT pNewElement;

                    if (!pSector.UseForOperation)
                        continue;

                    if (Operation == OPERATION_DETACH)  // No need for any element for a detach
                        continue;

                    if (((Operation == OPERATION_RETURN) || // We could need the readable sectors for the return in case of the state machine with _DNLOAD with wLength=0 is not supported
                           (Operation == OPERATION_UPLOAD)) &&
                         ((pSector.bSectorType & BIT_READABLE) != BIT_READABLE)
                       )
                        continue;

                    if ((Operation == OPERATION_DNLOAD) && ((pSector.bSectorType & BIT_WRITEABLE) != BIT_WRITEABLE))
                        continue;

                    secStart = pSector.dwStartAddress;
                    secEnd = pSector.dwStartAddress + pSector.dwSectorSize;

                    if ((elStart >= secEnd) || (elEnd <= secStart))
                        continue;

                    pNewElement = new IMAGEELEMENT();

                    // We can have four types of covering
                    if ((elStart <= secStart) && (elEnd >= secEnd))
                    {
                        pNewElement.dwAddress = secStart;
                        pNewElement.dwDataLength = secEnd - secStart;
                        pNewElement.Data = new byte[pNewElement.dwDataLength];

                        for (int j = 0; j < pNewElement.dwDataLength; j++)
                        {
                            pNewElement.Data[j] = pElement.Data[secStart - elStart + j];
                        }
                    }
                    else if ((elStart >= secStart) && (elEnd >= secEnd))
                    {
                        pNewElement.dwAddress = elStart;
                        pNewElement.dwDataLength = secEnd - elStart;
                        pNewElement.Data = new byte[pNewElement.dwDataLength];

                        for (int j = 0; j < pNewElement.dwDataLength; j++)
                        {
                            pNewElement.Data[j] = pElement.Data[j];
                        }
                    }
                    else if ((elStart >= secStart) && (elEnd <= secEnd))
                    {
                        pNewElement.dwAddress = elStart;
                        pNewElement.dwDataLength = elEnd - elStart;
                        pNewElement.Data = new byte[pNewElement.dwDataLength];
                        for (int j = 0; j < pNewElement.dwDataLength; j++)
                        {
                            pNewElement.Data[j] = pElement.Data[j];
                        }
                    }
                    else
                        if ((elStart <= secStart) && (elEnd <= secEnd))
                        {
                            pNewElement.dwAddress = secStart;
                            pNewElement.dwDataLength = elEnd - secStart;
                            pNewElement.Data = new byte[pNewElement.dwDataLength];
                            for (int j = 0; j < pNewElement.dwDataLength; j++)
                            {
                                pNewElement.Data[j] = pElement.Data[secStart - elStart + j];
                            }
                        }

                    if ((Operation == OPERATION_DNLOAD) && bTruncateLeadFF)
                    {
                    }

                    SetImageElement((uint) el, true, pNewElement);
                    Cnt++;
                }
                // We need to remove current element, as it could have been taken into account in the upper loop
                DestroyImageElement((uint) (el + Cnt));
            }

            return true;
        }

        public bool DestroyImageElement(uint dwRank)
        {
            IMAGEELEMENT pElement;

            if (!m_ImageState)
                return false;
            if (dwRank >= (uint)m_pElements.Count)
                return false;

            pElement =m_pElements[(int)dwRank];
            if (pElement.Data != null)
                pElement.Data = null;

            m_pElements.RemoveAt((int)dwRank);
            if (m_pElements.Count == 0)
                m_pElements.Clear();
            return true;
        }
    }


    class MAPPING
    {
        public Byte nAlternate;
        public string Name;
        public UInt32 NbSectors;
        public List<MAPPINGSECTOR> pSectors = new List<MAPPINGSECTOR>();
    }

    class IMAGEELEMENT : IComparable<IMAGEELEMENT>
    {
        public UInt32 dwAddress;
        public UInt32 dwDataLength;
        public byte[] Data;

        public int CompareTo(IMAGEELEMENT ele)
        {
            return this.dwAddress.CompareTo(ele.dwAddress);
        }
    }

    class MAPPINGSECTOR
    {

        public string Name;
        public UInt32 dwStartAddress;
        public UInt32 dwAliasedAddress;
        public UInt32 dwSectorIndex;
        public UInt32 dwSectorSize;
        public Byte bSectorType;
        public Boolean UseForOperation;
        public Boolean UseForErase;
        public Boolean UseForUpload;
        public Boolean UseForWriteProtect;
    }

    class TARGET_DESCRIPTOR
    {
        public byte Version = 0x00;
        public byte CmdCount = 0x00;
        public byte PIDLen = 0x00;
        public byte[] PID = new byte[2];

        public byte ROPE = 0x0;
        public byte ROPD = 0x0;

        public bool GET_CMD = false; //Get the version and the allowed commands supported by the current version of the boot loader
        public bool GET_VER_ROPS_CMD = false; //Get the BL version and the Read Protection status of the NVM
        public bool GET_ID_CMD = false; //Get the chip ID
        public bool SET_SPEED_CMD = false;
        public bool READ_CMD = false; //Read up to 256 bytes of memory starting from an address specified by the user
        public bool GO_CMD = false; //Jump to an address specified by the user to execute (a loaded) code
        public bool WRITE_CMD = false; //Write maximum 256 bytes to the RAM or the NVM starting from an address specified by the user
        public bool ERASE_CMD = false; //Erase from one to all the NVM sectors
        public bool ERASE_EXT_CMD = false; //Erase from one to all the NVM sectors
        public bool WRITE_PROTECT_CMD = false; //Enable the write protection in a permanent way for some sectors
        public bool WRITE_TEMP_UNPROTECT_CMD = false; //Disable the write protection in a temporary way for all NVM sectors
        public bool WRITE_PERM_UNPROTECT_CMD = false; //Disable the write protection in a permanent way for all NVM sectors
        public bool READOUT_PERM_PROTECT_CMD = false; //Enable the readout protection in a permanent way
        public bool READOUT_TEMP_UNPROTECT_CMD = false; //Disable the readout protection in a temporary way
        public bool READOUT_PERM_UNPROTECT_CMD = false; //Disable the readout protection in a permanent way

        public TARGET_DESCRIPTOR ShallowCopy()
        {
            TARGET_DESCRIPTOR other =  new TARGET_DESCRIPTOR();

            other.Version = this.Version;
            other.CmdCount = this.CmdCount;
            other.PIDLen = this.PIDLen;
            other.PID = new byte[this.PID.Length];

            for (int i = 0; i < this.PID.Length; i++)
            {
                other.PID[i] = this.PID[i];
            }


            other.GET_CMD = this.GET_CMD;
            other.GET_VER_ROPS_CMD = this.GET_VER_ROPS_CMD;
            other.GET_ID_CMD = this.GET_ID_CMD;
            other.SET_SPEED_CMD = this.SET_SPEED_CMD;
            other.READ_CMD = this.READ_CMD;
            other.GO_CMD = this.GO_CMD;
            other.WRITE_CMD = this.WRITE_CMD;
            other.ERASE_CMD = this.ERASE_CMD;
            other.ERASE_EXT_CMD = this.ERASE_EXT_CMD;
            other.WRITE_PROTECT_CMD = this.WRITE_PROTECT_CMD;
            other.WRITE_TEMP_UNPROTECT_CMD = this.WRITE_TEMP_UNPROTECT_CMD;
            other.WRITE_PERM_UNPROTECT_CMD = this.WRITE_PERM_UNPROTECT_CMD;
            other.WRITE_TEMP_UNPROTECT_CMD = this.WRITE_TEMP_UNPROTECT_CMD;
            other.READOUT_PERM_PROTECT_CMD = this.READOUT_PERM_PROTECT_CMD;
            other.READOUT_TEMP_UNPROTECT_CMD = this.READOUT_TEMP_UNPROTECT_CMD;
            other.READOUT_PERM_UNPROTECT_CMD = this.READOUT_PERM_UNPROTECT_CMD;
            return other;
        }
    }

    class STBL_Request
    {
        public byte _cmd;
        public uint _address;
        public ushort _length;
        public byte _nbSectors;
        public TARGET_DESCRIPTOR _target;
        public byte[] _data = new byte[4];
        public uint _wbSectors;

        public STBL_Request ShallowCopy()
        {
            STBL_Request other = new STBL_Request();
            other._target = this._target.ShallowCopy();
            other._cmd = this._cmd;
            other._address = this._address;
            other._data = new byte[this._data.Length];
            for (int i = 0; i < this._data.Length; i++)
            {
                other._data[i] = this._data[i];
            }
            other._data = this._data;
            other._length = this._length;
            other._nbSectors = this._nbSectors;
            other._wbSectors = this._wbSectors;


            return other;
        }
    } 
}
