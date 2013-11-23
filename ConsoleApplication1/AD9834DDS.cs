using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WiringPi;
using System.Threading;

namespace SPITest
{
    class AD9834DDS
    {
#region NOT FINISHED!!
        private static byte m_ConfigRegister0;
        private static byte m_ConfigRegister1;
        private const Int32 OscFreq = 50000000;
        private static double Power28 = Math.Pow(2, 28);        //2^28

        public static int RunTest()
        {
            //Init WiringPi library
            int result = Init.WiringPiSetup();

            if (result == -1)
            {
                Console.WriteLine("WiringPi init failed!");
                return result;
            }

            //Init WiringPi SPI library
            result = SPI.wiringPiSPISetup(1, 20000000); ;
            if (result == -1)
            {
                Console.WriteLine("SPI init failed!");
                return result;
            }

            Console.WriteLine("SPI init completed, using channel 1 at 20MHz for DDS Output");

            InitDDS(true, false, true, true);

            SetFrequency(10000000);         //Set output to 10MHz

            ReleaseReset();

            return 0;
        }

        public static void SetFrequency(float Frequency)
        {
            //32 bits of register data
            UInt32 FreqReg = (UInt32)((Frequency / OscFreq) * Power28);

            //Clear the bytes to write, 4 bytes, 2 bytes per 14 bit write, total 28 bits plus control codes (4 bits wide)
            byte b0 = (byte)0x0;                    //Very first byte to write, the LSB of LSB's
            byte b1 = (byte)64;                     //MSB, we're setting the freq 0 register

            byte b2 = (byte)0x0;                    //3rd byte to write
            byte b3 = (byte)64;                     //MSB, we're setting the freq 0 register

            //Now split the 32 bits up into bytes, don't forget to make sure the 2 MSB's are left alone!!!
            //Lower 14 LSB
            UInt32 tmp = FreqReg;
            b0 = (byte)(tmp &= 255);
            FreqReg = FreqReg >> 6;                 //Right shift by 6 bits,  and shift again by 2 bit
            tmp = FreqReg;
            byte temp = (byte)(tmp &= 255);     //copy the 8 bits
            temp = (byte)(temp >> 2);               //Shift right by 2 bits
            b1 |= temp;                             //OR with the second lsb byte

            //Upper 14 MSB's, FreqReq will be at the right place as we shifted it down, right.
            tmp = FreqReg;
            b2 = (byte)(tmp &= 255);            //Copy lower 8 bits
            tmp = FreqReg / 256 / 256;                 //Shift a 2 bytes right and loose the lower bytes
            b3 |= (byte)tmp;                    //OR with the last byte to write

            WriteWord(b0, b1);                  //10MHz from 50MHz will give 
            WriteWord(b2, b3);
        }

        private static void ReleaseReset()
        {
            byte b0 = m_ConfigRegister0;
            byte b1 = m_ConfigRegister1;

            //Reset is lsb of 2nd byte ie b1
            b1 &= 254;

            //Write to the config register
            WriteWord(b0, b1);
        }

        //Recommended are true, false, true, true for starters
        private static void InitDDS(bool UseSineRom, bool DivideBy2, bool UseMSBNotSine, bool EnableSBO)
        {
            //Set Reset register high
            byte b0 = (byte)0x0;    //Lower byte, this is shifted in first, LSB 1st with MSB trailing
            byte b1 = (byte)0x0;    //Upper byte

            //b1 is already reset so just turn on the bits we need.
            b1 = (byte)33;          //Set to Reset and 2 consequetive writes per freq register

            if (!UseSineRom)
                b0 |= 2;
            if (!DivideBy2)
                b0 |= 8;
            if (!UseMSBNotSine)
                b0 |= 16;
            if (EnableSBO)
                b0 |= 32;

            //Store the values for later use, bit flipping Reset for example
            m_ConfigRegister0 = b0;
            m_ConfigRegister1 = b1;

            //Write the configuration
            WriteWord(b0, b1);
        }

        private static void WriteWord(byte b0, byte b1)
        {
            //Writes the data LSB first of B0, then B1
            byte[] buffer = new byte[2];

            buffer[0] = b0;
            buffer[1] = b1;

            //a bit of unsafe fixed memory pointer action going on here, make sure the array you're pointing to is the right size!!!
            unsafe
            {
                fixed (byte* p = buffer)
                {
                    // Do all pointer work, ie external calls within the fixed area. The gc or clr wont try to move the object in memory while we use it.
                    SPI.wiringPiSPIDataRW(1, p, 2); //1 is SPI channel, p is byte array, 2 is number of bytes
                }
            }
        }

    }
}
#endregion