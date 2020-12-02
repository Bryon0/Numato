using System;
using System.Runtime.CompilerServices;
using System.Text;
using System.Diagnostics;
using System.IO.Ports;

/// <summary>
/// The class implements a numato serial device at this time. Refer to here: https://numato.com/
/// </summary>
/// <remarks></remarks>
public partial class Numato
{
    #region Data Members
    private SerialPort _serialport;
    private bool serialportopen = false;
    private static bool bDataAtserialPort = false;
    private static string[] strSerialPortData = new string[129];
    private static int nNumberOfMessages = 0;
    private string strCurrentSerialPort = "";
    public string strVersion = "";
    public string strId = "";
    #endregion

    #region Serial Port
    /// <summary>
    /// This is the serial port interface for the device.
    /// </summary>
    private SerialPort serialport
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        get
        {
            return _serialport;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        set
        {
            _serialport = value;
        }
    }

    /// <summary>
    /// Get the current serial port name.
    /// </summary>
    public string GetSerialPort
    {
        get { return strCurrentSerialPort;}
    }
    #endregion

    /// <summary>
    /// Gets the number of messages available.
    /// </summary>
    /// <returns></returns>
    /// <remarks></remarks>
    public int GetNumberOfMessages()
    {
        return nNumberOfMessages;
    }
    /// <summary>
    /// Gets the next message
    /// </summary>
    /// <returns></returns>
    /// <remarks></remarks>
    public string GetNextMessage()
    {
        nNumberOfMessages -= nNumberOfMessages;
        return strSerialPortData[nNumberOfMessages];
    }
    /// <summary>
    /// Opens a device.
    /// </summary>
    /// <param name="nbaudRate"></param>
    /// <returns></returns>
    /// <remarks></remarks>
    public int OpenDevice(int nbaudRate)
    {
        Stopwatch sw = new Stopwatch();
        string strResponse = "";
        string[] strAvailablePorts = System.IO.Ports.SerialPort.GetPortNames();
        int ret = -1;
        for (int i = 0, loopTo = strAvailablePorts.Length - 1; i <= loopTo; i += 1)
        {
            if (OpenPort(strAvailablePorts[i], nbaudRate) == 1)
            {
                RequestVersion();
                sw.Start();
                while(sw.ElapsedMilliseconds < 200000000) 
                {
                    ReadSerialPort(ref strResponse);
                    if (!string.IsNullOrEmpty(Parse(strResponse)))
                    {
                        ret = 1;
                        break;
                    }
                    else
                    {
                        ClosePort();
                        break;
                    }
                }
                sw.Stop();
            }
        }

        return ret;
    }

    /// <summary>
    /// Opens a device.
    /// </summary>
    /// <param name="strPortName"></param>
    /// <param name="nBaudRate"></param>
    /// <returns></returns>
    /// <remarks></remarks>
    public int OpenPort(string strPortName, int nBaudRate)
    {
        int ret = 0;
        try
        {
            if(serialport == null)
            {
                serialport = new SerialPort();
            }
            else
            {
                ClosePort();
            }

            serialport.PortName = strPortName;
            serialport.BaudRate = nBaudRate;
            serialport.Open();
            serialport.ReadTimeout = 50;
            serialportopen = true;
            strCurrentSerialPort = strPortName;
            // AddHandler serialport.DataReceived, AddressOf DataReceivedHandler
            ret = 1;
        }
        catch (Exception x)
        {
            ret = -1;
            strCurrentSerialPort = "";
        }

        return ret;
    }

    /// <summary>
    /// Closes the current port.
    /// </summary>
    /// <returns></returns>
    /// <remarks></remarks>
    public int ClosePort()
    {
        int ret = 0;
        try
        {
            if (serialport != null || serialport.IsOpen)
            {
                serialport.Close();
            }

            serialportopen = false;
            ret = 1;
            strCurrentSerialPort = "";
        }
        catch (Exception x)
        {
            ret = -1;
        }

        return ret;
    }
    /// <summary>
    /// Command to request the firmware version.
    /// </summary>
    /// <returns></returns>
    /// <remarks></remarks>
    public int RequestVersion()
    {
        int ret = 0;
        try
        {
            if (serialport.IsOpen)
            {
                string s = "ver" + System.Environment.NewLine;
                serialport.Write(s);                
                Debug.WriteLine("Sending " + s);
            }

            ret = 1;
        }
        catch (Exception x)
        {
            ret = -1;
        }

        return ret;
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="strId"></param>
    /// <returns></returns>
    /// <remarks></remarks>
    public int SetId(string strId)
    {
        int ret = 0;
        try
        {
            if (serialport.IsOpen)
            {
                string s = "Id set" + " " + strId + System.Environment.NewLine;
                serialport.Write(s);                
                Debug.WriteLine("Sending " + s);
            }

            ret = 1;
        }
        catch (Exception x)
        {
            ret = -1;
        }

        return ret;
    }
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    /// <remarks></remarks>
    public int GetId()
    {
        int ret = 0;
        try
        {
            if (serialport.IsOpen)
            {
                string s = @"Id get\n";
                serialport.Write(s);                
                Debug.WriteLine("Sending " + s);
            }

            ret = 1;
        }
        catch (Exception x)
        {
            ret = -1;
        }

        return ret;
    }

    // Set mask for selectively update multiple GPIOs with writeall/iodir command. 
    // A hexadecimal value(xx) must be specified with desired bit positions 
    // set to 0 or 1 with no “0x” prepended (eg 02, ff). A 0 in a bit position mask 
    // the corresponding GPIO and any update to that GPIO is ignored during writeall/iodir 
    // command. A 1 in a bit position will unmask that particular GPIO and any updates 
    // using writeall/iodir command will be applied to that GPIO. This mask does not a
    // ffect the operation of set and clear commands.

    // gpio iomask ff – Unmask all GPIOs. 
    // gpio iomask 00 – mask all GPIOs. 
    public int GPIO_IOMask(byte gpio)
    {
        int ret = 0;
        int hex = Convert.ToInt32(gpio.ToString(), 16);
        try
        {
            if (serialport.IsOpen)
            {
                string s = "gpio iomask" + " " + hex.ToString() + System.Environment.NewLine;
                serialport.Write(s);                
                Debug.WriteLine("Sending " + s);
            }

            ret = 1;
        }
        catch (Exception x)
        {
            ret = -1;
        }

        return ret;
    }
    // Sets the direction of all GPIO in a single operation. A hexadecimal 
    // value(xx) must be specified with desired bit positions set to 0 or 1 with 
    // no “0x” prepended (eg 02, ff). A 0 in a bit position configures that 
    // GPIO as output and 1 configures as input. Before using gpio readall/writeall 
    // commands, the direction of GPIO must be set using “gpio iodir xx” command. 
    // GPIO direction set by using iodir command will be modified with subsequent 
    // set/clear/read commands 
    // (only affects the GPIO accessed using these commands).
    // gpio iodir 00 – Sets all GPIO to output 
    public int GPIO_IODir(byte gpio)
    {
        int ret = 0;
        string hexformat = "";
        int hex = Convert.ToInt32(gpio.ToString(), 16);
        try
        {
            if (serialport.IsOpen)
            {               
                if(hex <= 0xF)
                {
                    hexformat = "0" + hex.ToString(); 
                }
                else
                {
                    hexformat = hex.ToString();                 }


                string s = "gpio iodir" + " " + hex  + System.Environment.NewLine;
                serialport.Write(s);                
                Debug.WriteLine("Sending " + s);
            }

            ret = 1;
        }
        catch (Exception x)
        {
            ret = -1;
        }

        return ret;
    }

    // Sets the GPIO output status to high. Here “x” is the number of the GPIO. 
    // This command accepts GPIO number from 0 -7, total 8 values Please see examples below.
    // gpio set 0 – Sets GPIO 0 to high state
    // gpio set 4 – Sets GPIO 4 to high state
    public int GPIO_Write(byte gpio)
    {
        int ret = 0;
        try
        {
            if (serialport.IsOpen)
            {
                string s = "gpio set" + " " + gpio.ToString() + System.Environment.NewLine;
                serialport.Write(s);
                Debug.WriteLine("Sending " + s);
            }

            ret = 1;
        }
        catch (Exception x)
        {
            ret = -1;
        }

        return ret;
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="gpio"></param>
    /// <returns></returns>
    /// <remarks></remarks>
    public int GPIO_Writeall(byte gpio)
    {
        int ret = 0;
        int hex = Convert.ToInt32(gpio.ToString(), 16);
        try
        {
            if (serialport.IsOpen)
            {
                string s = "gpio writeall" + " " + hex.ToString() + System.Environment.NewLine;
                serialport.Write(s);                
                Debug.WriteLine("Sending " + s);
            }

            ret = 1;
        }
        catch (Exception x)
        {
            ret = -1;
        }

        return ret;
    }


    // Sets the GPIO output status to low. Here “x” is the number of the GPIO. 
    // This command accepts GPIO number from 0 -7, total 8 values. Please see examples below.
    // gpio clear 0 – Sets GPIO 0 to low state
    // gpio clear 4 – Sets GPIO 4to low state
    /// <summary>
    /// 
    /// </summary>
    /// <param name="gpio"></param>
    /// <returns></returns>
    /// <remarks></remarks>
    public int GPIO_Clear(byte gpio)
    {
        int ret = 0;
        try
        {
            if (serialport.IsOpen)
            {
                string s = "gpio clear" + " " + gpio.ToString() + System.Environment.NewLine;
                serialport.Write(s);                
                Debug.WriteLine("Sending " + s);
            }

            ret = 1;
        }
        catch (Exception x)
        {
            ret = -1;
        }

        return ret;
    }

    // Reads the digital status present at the input mentioned. Here “x” stands for the number of GPIO. 
    // This command accepts GPIO number from 0 -7, total 8 values. The response will be either “on” or “off” depending on the current digital state of the GPIO. Please see examples below.
    // gpio read 0 – Reads GPIO 0 status
    // gpio read 4 – Reads GPIO 4 status
    /// <summary>
    /// 
    /// </summary>
    /// <param name="gpio"></param>
    /// <returns></returns>
    /// <remarks></remarks>
    public int GPIO_Read(byte gpio)
    {
        int ret = 0;
        try
        {
            if (serialport.IsOpen)
            {
                string s = "gpio read" + " " + gpio.ToString() + System.Environment.NewLine;
                serialport.Write(s);                
                Debug.WriteLine("Sending " + s);
            }

            ret = 1;
        }
        catch (Exception x)
        {
            ret = -1;
        }

        return ret;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    /// <remarks></remarks>
    public int GPIO_Readall()
    {
        int ret = 0;
        try
        {
            if (serialport.IsOpen)
            {
                string s = "gpio readall" + System.Environment.NewLine;
                serialport.Write(s);                
                Debug.WriteLine("Sending " + s);
            }

            ret = 1;
        }
        catch (Exception x)
        {
            ret = -1;
        }

        return ret;
    }
    // Reads the analog voltage present at the ADC input mentioned. “x” stands for the number of ADC input. 
    // The response will be a number that ranges from 0 – 1023. Please see examples below.
    // adc read 0 – Reads analog input 0 
    // adc read 4 – Reads analog input 4
    /// <summary>
    /// 
    /// </summary>
    /// <param name="adc"></param>
    /// <returns></returns>
    /// <remarks></remarks>
    public int ADC_Read(byte adc)
    {
        int ret = 0;
        try
        {
            if (serialport.IsOpen)
            {
                string s = "adc read" + " " + adc.ToString() + System.Environment.NewLine;
                serialport.Write(s);                
                Debug.WriteLine("Sending " + s);
            }

            ret = 1;
        }
        catch (Exception x)
        {
            ret = -1;
        }

        return ret;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="str"></param>
    /// <param name="x"></param>
    /// <returns></returns>
    /// <remarks></remarks>
    public int ReadSerialPort(ref string str, int x)
    {
        int ret = 0;
        var buffer = new byte[257];
        int index = 0;
        int b = 0;
        if (serialport.IsOpen)
        {
            str = "";
            for (int i = 0, loopTo = x; i <= loopTo; i += 1)
            {
                if (serialport.BytesToRead != 0)
                {
                    break;
                }
                else
                {
                    //Thread.Sleep(1);
                }
            }

            while (serialport.BytesToRead > 0)
            {
                b = serialport.ReadByte();
                if (b != -1)
                {
                    buffer[index] = Convert.ToByte(b);
                    index += 1;
                }
            }

            serialport.DiscardInBuffer();
            str = Encoding.UTF8.GetString(buffer);
            ret = 1;
        }

        return ret;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="str"></param>
    /// <param name="x"></param>
    /// <returns></returns>
    public int ReadSerialPort(ref string str)
    {
        int ret = 0;
        var buffer = new byte[257];
        int index = 0;
        int b = 0;
        if (serialport.IsOpen)
        {
            int nBytes = serialport.BytesToRead;
            str = "";

            if(nBytes != 0)
            {
                while (serialport.BytesToRead > 0)
                {
                    b = serialport.ReadByte();
                    if (b != -1)
                    {
                        buffer[index] = Convert.ToByte(b);
                        index += 1;
                    }
                }

            }             
 
            serialport.DiscardInBuffer();
            str = Encoding.UTF8.GetString(buffer);
            ret = nBytes;
        }

        return ret;
    }

    public string Parse(string str)
    {
        string[] strSplit;
        string returnString = "";
        if (str.Contains("ver"))
        {
            strSplit = str.Split();
            returnString = strSplit[3];
        }
        else if (str.Contains("gpio readall"))
        {
            if (str.Contains(">"))
            {
                strSplit = str.Split();
                returnString = strSplit[3];
            }
        }
        else if (str.Contains("adc read"))
        {
            strSplit = str.Split();
            returnString = strSplit[4];
        }

        return returnString;
    }
}