using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WiringPi;

namespace SPITest
{
  class LoopTest
  {
    //Main entry point
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
        result = SPI.wiringPiSPISetup(0, 32000000);
        if (result == -1)
        {
            Console.WriteLine("SPI init failed!");
            return result;
        }

        Console.WriteLine("SPI init completed, using channel 0 at 32MHz for loopback testing");

        //Do dummy 16 bit transfer over SPI, loopback should leave array as was loaded, no loopback will be all 1 or 0's
        byte[] buffer = new byte[2];

        buffer[0] = 0xAA;
        buffer[1] = 0x55;

        //a bit of unsafe fixed memory pointer action going on here, make sure the array you're pointing to is the right size!!!
        unsafe
        {
            fixed (byte* p = buffer)
            {
                // Do all pointer work, ie external calls within the fixed area. The gc or clr wont try to move the object in memory while we use it.
                SPI.wiringPiSPIDataRW(0, p, 2);
            }
        }

        if (buffer[0] == 0xAA && buffer[1] == 0x55)
        {
            Console.WriteLine("Loopback is connected!");
        }
        else if (buffer[0] == 0x55 && buffer[1] == 0xAA)
        {
            Console.WriteLine("Loopback is connected, bute data reversed!!");
        }
        else if (buffer[0] == 0x00 && buffer[1] == 0x00)
        {
            Console.WriteLine("All zeros read back");
        }
        else if (buffer[1] == 0xFF && buffer[1] == 0xFF)
        {
            Console.WriteLine("All ones read back");
        }

        return 0;
    }
  }
}
