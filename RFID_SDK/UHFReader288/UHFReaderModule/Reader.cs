// Decompiled with JetBrains decompiler
// Type: UHFReaderModule.Reader
// Assembly: UHFReader288, Version=1.0.5.5, Culture=neutral, PublicKeyToken=null
// MVID: CC6301A5-076A-44BC-8C3C-EE1B90F3208E
// Assembly location: C:\Users\Abduazim\DjangoProjects\RFID\RFID_SDK\UHFReader288.dll

using System;
using System.IO;
using System.IO.Ports;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UHF;

#nullable disable
namespace UHFReaderModule
{
  public class Reader
  {
    public RFIDCallBack ReceiveCallback;
    public RFIDRecvCallBack RecvCallback;
    public RFIDSendCallBack SendCallback;
    private SerialPort serialPort1;
    private ushort POLYNOMIAL = 33800;
    private ushort PRESET_VALUE = ushort.MaxValue;
    private byte[] RecvBuff = new byte[8000];
    private byte[] SendBuff = new byte[300];
    private int RecvLength;
    private TcpClient client;
    private Stream streamToTran;
    private int COM_TYPE = -1;
    private int inventoryScanTime;
    public string DevName = "";
    private byte fComAddr;

    public Reader() => this.serialPort1 = new SerialPort();

    private void GetCRC(byte[] pData, int ADataLen)
    {
      ushort num1 = this.PRESET_VALUE;
      int index1;
      for (index1 = 0; index1 <= ADataLen - 1; ++index1)
      {
        num1 ^= (ushort) pData[index1];
        for (int index2 = 0; index2 < 8; ++index2)
        {
          if (((int) num1 & 1) != 0)
            num1 = (ushort) ((uint) num1 >> 1 ^ (uint) this.POLYNOMIAL);
          else
            num1 >>= 1;
        }
      }
      byte[] numArray = pData;
      int index3 = index1;
      int index4 = index3 + 1;
      int num2 = (int) (byte) ((uint) num1 & (uint) byte.MaxValue);
      numArray[index3] = (byte) num2;
      pData[index4] = (byte) ((int) num1 >> 8 & (int) byte.MaxValue);
    }

    private int CheckCRC(byte[] pData, int len)
    {
      byte[] numArray = new byte[len];
      Array.Copy((Array) pData, (Array) numArray, len - 2);
      this.GetCRC(numArray, len - 2);
      return (int) pData[len - 2] == (int) numArray[len - 2] && (int) pData[len - 1] == (int) numArray[len - 1] ? 0 : 49;
    }

    private byte[] HexStringToByteArray(string s)
    {
      s = s.Replace(" ", "");
      byte[] byteArray = new byte[s.Length / 2];
      for (int startIndex = 0; startIndex < s.Length; startIndex += 2)
        byteArray[startIndex / 2] = Convert.ToByte(s.Substring(startIndex, 2), 16);
      return byteArray;
    }

    private string ByteArrayToHexString(byte[] data)
    {
      StringBuilder stringBuilder = new StringBuilder(data.Length * 3);
      foreach (byte num in data)
        stringBuilder.Append(Convert.ToString(num, 16).PadLeft(2, '0'));
      return stringBuilder.ToString().ToUpper();
    }

    private string ByteArrayToHexString2(byte[] data)
    {
      StringBuilder stringBuilder = new StringBuilder(data.Length * 3);
      foreach (byte num in data)
        stringBuilder.Append(Convert.ToString(num, 16).PadLeft(2, '0').PadLeft(3, ' '));
      return stringBuilder.ToString().ToUpper();
    }

    private int OpenCom(int Port, byte fbaud)
    {
      if (this.serialPort1.IsOpen)
        this.serialPort1.Close();
      try
      {
        this.serialPort1.PortName = "com" + Port.ToString();
        int num;
        switch (fbaud)
        {
          case 0:
            num = 9600;
            break;
          case 1:
            num = 19200;
            break;
          case 2:
            num = 38400;
            break;
          case 5:
            num = 57600;
            break;
          case 6:
            num = 115200;
            break;
          default:
            num = 57600;
            break;
        }
        this.serialPort1.BaudRate = num;
        this.serialPort1.ReadTimeout = 200;
        this.serialPort1.Open();
        this.DevName = this.serialPort1.PortName;
        return 0;
      }
      catch (Exception ex)
      {
        ex.ToString();
        return 48;
      }
    }

    public int OpenByCom(int Port, ref byte ComAddr, byte Baud)
    {
      if (this.OpenCom(Port, Baud) != 0)
        return 48;
      this.COM_TYPE = 0;
      byte address = ComAddr;
      byte[] VersionInfo = new byte[2];
      byte ReaderType = 0;
      byte TrType = 0;
      byte dmaxfre = 0;
      byte dminfre = 0;
      byte powerdBm = 0;
      byte ScanTime = 0;
      byte Ant = 0;
      byte BeepEn = 0;
      byte OutputRep = 0;
      byte CheckAnt = 0;
      if (this.GetReaderInformation(ref address, VersionInfo, ref ReaderType, ref TrType, ref dmaxfre, ref dminfre, ref powerdBm, ref ScanTime, ref Ant, ref BeepEn, ref OutputRep, ref CheckAnt) == 0)
      {
        this.DevName = "COM" + (object) Port;
        ComAddr = address;
        this.fComAddr = address;
        return 0;
      }
      this.serialPort1.Close();
      this.COM_TYPE = -1;
      return 48;
    }

    public int CloseByCom()
    {
      try
      {
        if (!this.serialPort1.IsOpen)
          return 48;
        this.serialPort1.Close();
        this.COM_TYPE = -1;
        return 0;
      }
      catch (Exception ex)
      {
        ex.ToString();
        return 48;
      }
    }

    private int OpenNet(string ipAddr, int Port)
    {
      try
      {
        IPAddress address = IPAddress.Parse(ipAddr);
        this.client = new TcpClient();
        this.client.Connect(address, Port);
        this.streamToTran = (Stream) this.client.GetStream();
        this.streamToTran.ReadTimeout = 2000;
        return 0;
      }
      catch (Exception ex)
      {
        ex.ToString();
        return 48;
      }
    }

    public int OpenByTcp(string ipAddr, int Port, ref byte ComAddr)
    {
      if (this.OpenNet(ipAddr, Port) != 0)
        return 48;
      this.COM_TYPE = 1;
      byte address = 0;
      byte[] VersionInfo = new byte[2];
      byte ReaderType = 0;
      byte TrType = 0;
      byte dmaxfre = 0;
      byte dminfre = 0;
      byte powerdBm = 0;
      byte ScanTime = 0;
      byte Ant = 0;
      byte BeepEn = 0;
      byte OutputRep = 0;
      byte CheckAnt = 0;
      if (this.GetReaderInformation(ref address, VersionInfo, ref ReaderType, ref TrType, ref dmaxfre, ref dminfre, ref powerdBm, ref ScanTime, ref Ant, ref BeepEn, ref OutputRep, ref CheckAnt) == 0)
      {
        this.DevName = ipAddr;
        ComAddr = address;
        this.fComAddr = address;
        return 0;
      }
      if (this.streamToTran != null)
        this.streamToTran.Dispose();
      if (this.client != null)
        this.client.Close();
      this.COM_TYPE = -1;
      return 48;
    }

    public int CloseByTcp()
    {
      try
      {
        if (this.streamToTran != null)
          this.streamToTran.Dispose();
        if (this.client != null)
          this.client.Close();
        this.COM_TYPE = -1;
        return 0;
      }
      catch (Exception ex)
      {
        ex.ToString();
        return 48;
      }
    }

    private int SendDataToPort(byte[] dataToSend, int BytesOfSend)
    {
      this.RecvLength = 0;
      Array.Clear((Array) this.RecvBuff, 0, 8000);
      if (this.COM_TYPE == 0)
      {
        try
        {
          if (!this.serialPort1.IsOpen)
            return 48;
          this.serialPort1.DiscardInBuffer();
          this.serialPort1.DiscardOutBuffer();
          this.serialPort1.Write(dataToSend, 0, BytesOfSend);
          byte[] numArray = new byte[BytesOfSend];
          Array.Copy((Array) dataToSend, (Array) numArray, BytesOfSend);
          this.SendCallback(this.ByteArrayToHexString2(numArray));
          return 0;
        }
        catch
        {
          return 48;
        }
      }
      else
      {
        try
        {
          lock (this.streamToTran)
          {
            this.streamToTran.Flush();
            this.streamToTran.Write(dataToSend, 0, BytesOfSend);
            byte[] numArray = new byte[BytesOfSend];
            Array.Copy((Array) dataToSend, (Array) numArray, BytesOfSend);
            if (this.SendCallback != null)
              this.SendCallback(this.ByteArrayToHexString2(numArray));
            return 0;
          }
        }
        catch
        {
          return 48;
        }
      }
    }

    private int SendDataToPort_Noclear(byte[] dataToSend, int BytesOfSend)
    {
      this.RecvLength = 0;
      Array.Clear((Array) this.RecvBuff, 0, 8000);
      if (this.COM_TYPE == 0)
      {
        try
        {
          if (!this.serialPort1.IsOpen)
            return 48;
          this.serialPort1.Write(dataToSend, 0, BytesOfSend);
          byte[] numArray = new byte[BytesOfSend];
          Array.Copy((Array) dataToSend, (Array) numArray, BytesOfSend);
          this.SendCallback(this.ByteArrayToHexString2(numArray));
          return 0;
        }
        catch
        {
          return 48;
        }
      }
      else
      {
        try
        {
          lock (this.streamToTran)
          {
            this.streamToTran.Write(dataToSend, 0, BytesOfSend);
            byte[] numArray = new byte[BytesOfSend];
            Array.Copy((Array) dataToSend, (Array) numArray, BytesOfSend);
            if (this.SendCallback != null)
              this.SendCallback(this.ByteArrayToHexString2(numArray));
            return 0;
          }
        }
        catch
        {
          return 48;
        }
      }
    }

    private byte[] ReadDataFromPort()
    {
      try
      {
        if (this.COM_TYPE == 0)
        {
          int bytesToRead = this.serialPort1.BytesToRead;
          if (bytesToRead <= 0)
            return (byte[]) null;
          byte[] numArray1 = new byte[bytesToRead];
          int length = this.serialPort1.Read(numArray1, 0, bytesToRead);
          if (length <= 0)
            return (byte[]) null;
          byte[] numArray2 = new byte[length];
          Array.Copy((Array) numArray1, (Array) numArray2, length);
          if (this.RecvCallback != null)
            this.RecvCallback(this.ByteArrayToHexString2(numArray2));
          return numArray2;
        }
        byte[] numArray3 = new byte[1024];
        int length1 = this.streamToTran.Read(numArray3, 0, numArray3.Length);
        if (length1 <= 0)
          return (byte[]) null;
        byte[] numArray4 = new byte[length1];
        Array.Copy((Array) numArray3, (Array) numArray4, length1);
        if (this.RecvCallback != null)
          this.RecvCallback(this.ByteArrayToHexString2(numArray4));
        return numArray4;
      }
      catch (Exception ex)
      {
        ex.ToString();
        return (byte[]) null;
      }
    }

    private int GetDataFromPort(int cmd, int endTime)
    {
      byte[] numArray1 = new byte[2000];
      int num1 = 0;
      long tickCount = (long) Environment.TickCount;
      try
      {
        while ((long) Environment.TickCount - tickCount < (long) endTime)
        {
          byte[] sourceArray = this.ReadDataFromPort();
          if (sourceArray != null)
          {
            int length = sourceArray.Length;
            if (length != 0)
            {
              byte[] numArray2 = new byte[length + num1];
              Array.Copy((Array) numArray1, 0, (Array) numArray2, 0, num1);
              Array.Copy((Array) sourceArray, 0, (Array) numArray2, num1, length);
              int sourceIndex = 0;
              while (numArray2.Length - sourceIndex > 4)
              {
                if (numArray2[sourceIndex] >= (byte) 4 && (int) numArray2[sourceIndex + 2] == cmd || numArray2[sourceIndex] == (byte) 5 && numArray2[sourceIndex + 2] == (byte) 0)
                {
                  int num2 = (int) numArray2[sourceIndex];
                  if (numArray2.Length >= sourceIndex + num2 + 1)
                  {
                    byte[] numArray3 = new byte[num2 + 1];
                    Array.Copy((Array) numArray2, sourceIndex, (Array) numArray3, 0, numArray3.Length);
                    if (this.CheckCRC(numArray3, numArray3.Length) == 0)
                    {
                      Array.Copy((Array) numArray3, 0, (Array) this.RecvBuff, 0, numArray3.Length);
                      this.RecvLength = numArray3.Length;
                      return 0;
                    }
                    ++sourceIndex;
                  }
                  else
                    break;
                }
                else
                  ++sourceIndex;
              }
              if (numArray2.Length > sourceIndex)
              {
                num1 = numArray2.Length - sourceIndex;
                Array.Copy((Array) numArray2, sourceIndex, (Array) numArray1, 0, num1);
              }
              else
                num1 = 0;
            }
          }
        }
      }
      catch (Exception ex)
      {
        ex.ToString();
      }
      return 48;
    }

    public int GetReaderInformation(
      ref byte address,
      byte[] VersionInfo,
      ref byte ReaderType,
      ref byte TrType,
      ref byte dmaxfre,
      ref byte dminfre,
      ref byte powerdBm,
      ref byte ScanTime,
      ref byte Ant,
      ref byte BeepEn,
      ref byte OutputRep,
      ref byte CheckAnt)
    {
      this.SendBuff[0] = (byte) 4;
      this.SendBuff[1] = address;
      this.SendBuff[2] = (byte) 33;
      this.GetCRC(this.SendBuff, 3);
      this.SendDataToPort(this.SendBuff, 5);
      int dataFromPort = this.GetDataFromPort(33, 1500);
      if (dataFromPort != 0)
        return dataFromPort;
      if (this.CheckCRC(this.RecvBuff, this.RecvLength) != 0)
        return 49;
      if (this.RecvBuff[2] != (byte) 33 || this.RecvBuff[3] != (byte) 0)
        return (int) this.RecvBuff[3];
      address = this.RecvBuff[1];
      Array.Copy((Array) this.RecvBuff, 4, (Array) VersionInfo, 0, 2);
      ReaderType = this.RecvBuff[6];
      TrType = this.RecvBuff[7];
      dmaxfre = this.RecvBuff[8];
      dminfre = this.RecvBuff[9];
      powerdBm = this.RecvBuff[10];
      ScanTime = this.RecvBuff[11];
      this.inventoryScanTime = (int) this.RecvBuff[11];
      Ant = this.RecvBuff[12];
      BeepEn = this.RecvBuff[13];
      OutputRep = this.RecvBuff[14];
      CheckAnt = this.RecvBuff[15];
      return (int) this.RecvBuff[3];
    }

    private int GetInventoryG1(
      int Scantime,
      byte[] pEPCList,
      ref byte outAnt,
      ref int dlen,
      ref int epcNum,
      int cmd)
    {
      epcNum = 0;
      dlen = 0;
      byte[] numArray1 = new byte[4096];
      int num1 = 0;
      long tickCount = (long) Environment.TickCount;
      try
      {
        do
        {
          byte[] sourceArray = this.ReadDataFromPort();
          if (sourceArray != null)
          {
            int length1 = sourceArray.Length;
            if (length1 != 0)
            {
              byte[] numArray2 = new byte[length1 + num1];
              Array.Copy((Array) numArray1, 0, (Array) numArray2, 0, num1);
              Array.Copy((Array) sourceArray, 0, (Array) numArray2, num1, length1);
              int sourceIndex1 = 0;
              while (numArray2.Length - sourceIndex1 > 5)
              {
                if (numArray2[sourceIndex1] >= (byte) 5 && (int) numArray2[sourceIndex1 + 2] == cmd)
                {
                  int num2 = (int) numArray2[sourceIndex1];
                  if (numArray2.Length >= sourceIndex1 + num2 + 1)
                  {
                    byte[] numArray3 = new byte[num2 + 1];
                    Array.Copy((Array) numArray2, sourceIndex1, (Array) numArray3, 0, numArray3.Length);
                    if (this.CheckCRC(numArray3, numArray3.Length) == 0)
                    {
                      tickCount = (long) Environment.TickCount;
                      int num3 = (int) numArray3[0] + 1;
                      sourceIndex1 += num3;
                      int inventoryG1 = (int) numArray3[3];
                      if (inventoryG1 == 1 || inventoryG1 == 2 || inventoryG1 == 3 || inventoryG1 == 4)
                      {
                        int num4 = (int) numArray3[5];
                        if (num4 > 0)
                        {
                          int sourceIndex2 = 6;
                          for (int index = 0; index < num4; ++index)
                          {
                            int length2 = (int) numArray3[sourceIndex2] & 63;
                            bool flag = false;
                            int num5 = 0;
                            int num6 = 0;
                            int num7 = 0;
                            if (((int) numArray3[sourceIndex2] & 64) > 0)
                              flag = true;
                            if (!flag)
                            {
                              Array.Copy((Array) numArray3, sourceIndex2, (Array) pEPCList, dlen, length2 + 2);
                              dlen += length2 + 2;
                            }
                            else
                            {
                              Array.Copy((Array) numArray3, sourceIndex2, (Array) pEPCList, dlen, length2 + 6);
                              num5 = (int) numArray3[sourceIndex2 + length2 + 2] * 256 + (int) numArray3[sourceIndex2 + length2 + 3];
                              num6 = (int) numArray3[sourceIndex2 + length2 + 4] * 256 + (int) numArray3[sourceIndex2 + length2 + 5];
                              num7 = ((int) numArray3[sourceIndex2 + length2 + 6] << 16) + ((int) numArray3[sourceIndex2 + length2 + 7] << 8) + (int) numArray3[sourceIndex2 + length2 + 8];
                              dlen += length2 + 9;
                            }
                            ++epcNum;
                            outAnt = numArray3[4];
                            if (this.ReceiveCallback != null)
                            {
                              RFIDTag mtag = new RFIDTag();
                              mtag.DeviceName = this.DevName;
                              mtag.ANT = numArray3[4];
                              mtag.LEN = numArray3[sourceIndex2];
                              mtag.phase_begin = num5;
                              mtag.phase_end = num6;
                              mtag.PacketParam = (byte) 0;
                              mtag.RSSI = numArray3[sourceIndex2 + 1 + length2];
                              mtag.Freqkhz = num7;
                              byte[] numArray4 = new byte[length2];
                              Array.Copy((Array) numArray3, sourceIndex2 + 1, (Array) numArray4, 0, numArray4.Length);
                              mtag.UID = this.ByteArrayToHexString(numArray4);
                              this.ReceiveCallback(mtag);
                            }
                            sourceIndex2 = flag ? sourceIndex2 + 9 + length2 : sourceIndex2 + 2 + length2;
                          }
                        }
                      }
                      if (inventoryG1 != 3)
                        return inventoryG1;
                    }
                    else
                      ++sourceIndex1;
                  }
                  else
                    break;
                }
                else
                  ++sourceIndex1;
              }
              if (numArray2.Length > sourceIndex1)
              {
                num1 = numArray2.Length - sourceIndex1;
                Array.Copy((Array) numArray2, sourceIndex1, (Array) numArray1, 0, num1);
              }
              else
                num1 = 0;
            }
          }
          else
            Thread.Sleep(1);
        }
        while ((long) Environment.TickCount - tickCount < (long) (Scantime * 2 + 2000));
      }
      catch (Exception ex)
      {
        ex.ToString();
      }
      return 48;
    }

    public int Inventory_G2(
      ref byte ComAdr,
      byte QValue,
      byte Session,
      byte MaskMem,
      byte[] MaskAdr,
      byte MaskLen,
      byte[] MaskData,
      byte MaskFlag,
      byte AdrTID,
      byte LenTID,
      byte TIDFlag,
      byte Target,
      byte InAnt,
      byte Scantime,
      byte FastFlag,
      byte[] pEPCList,
      ref byte Ant,
      ref int Totallen,
      ref int CardNum)
    {
      if (Scantime == (byte) 0)
        Scantime = (byte) 20;
      this.SendBuff[1] = ComAdr;
      this.SendBuff[2] = (byte) 1;
      this.SendBuff[3] = QValue;
      this.SendBuff[4] = Session;
      if (MaskFlag == (byte) 1)
      {
        this.SendBuff[5] = MaskMem;
        Array.Copy((Array) MaskAdr, 0, (Array) this.SendBuff, 6, 2);
        this.SendBuff[8] = MaskLen;
        int length = (int) MaskLen % 8 != 0 ? (int) MaskLen / 8 + 1 : (int) MaskLen / 8;
        Array.Copy((Array) MaskData, 0, (Array) this.SendBuff, 9, length);
        if (TIDFlag == (byte) 1)
        {
          if (FastFlag == (byte) 1)
          {
            this.SendBuff[length + 9] = AdrTID;
            this.SendBuff[length + 10] = LenTID;
            this.SendBuff[length + 11] = Target;
            this.SendBuff[length + 12] = InAnt;
            this.SendBuff[length + 13] = Scantime;
            this.SendBuff[0] = Convert.ToByte(15 + length);
          }
          else
          {
            this.SendBuff[length + 9] = AdrTID;
            this.SendBuff[length + 10] = LenTID;
            this.SendBuff[0] = Convert.ToByte(12 + length);
          }
        }
        else if (FastFlag == (byte) 1)
        {
          this.SendBuff[length + 9] = Target;
          this.SendBuff[length + 10] = InAnt;
          this.SendBuff[length + 11] = Scantime;
          this.SendBuff[0] = Convert.ToByte(13 + length);
        }
        else
          this.SendBuff[0] = Convert.ToByte(10 + length);
      }
      else if (TIDFlag == (byte) 1)
      {
        if (FastFlag == (byte) 1)
        {
          this.SendBuff[5] = AdrTID;
          this.SendBuff[6] = LenTID;
          this.SendBuff[7] = Target;
          this.SendBuff[8] = InAnt;
          this.SendBuff[9] = Scantime;
          this.SendBuff[0] = (byte) 11;
        }
        else
        {
          this.SendBuff[5] = AdrTID;
          this.SendBuff[6] = LenTID;
          this.SendBuff[0] = (byte) 8;
        }
      }
      else if (FastFlag == (byte) 1)
      {
        this.SendBuff[5] = Target;
        this.SendBuff[6] = InAnt;
        this.SendBuff[7] = Scantime;
        this.SendBuff[0] = (byte) 9;
      }
      else
        this.SendBuff[0] = (byte) 6;
      this.GetCRC(this.SendBuff, (int) this.SendBuff[0] - 1);
      this.SendDataToPort(this.SendBuff, (int) this.SendBuff[0] + 1);
      return this.GetInventoryG1((int) Scantime * 100, pEPCList, ref Ant, ref Totallen, ref CardNum, 1);
    }

    public int SetAddress(ref byte fComAdr, byte aNewComAdr)
    {
      this.SendBuff[0] = (byte) 5;
      this.SendBuff[1] = fComAdr;
      this.SendBuff[2] = (byte) 36;
      this.SendBuff[3] = aNewComAdr;
      this.GetCRC(this.SendBuff, 4);
      this.SendDataToPort(this.SendBuff, 6);
      int dataFromPort = this.GetDataFromPort(36, 1500);
      if (dataFromPort != 0)
        return dataFromPort;
      if (this.CheckCRC(this.RecvBuff, this.RecvLength) != 0)
        return 49;
      if (this.RecvBuff[2] != (byte) 36)
        return (int) this.RecvBuff[3];
      fComAdr = this.RecvBuff[1];
      return (int) this.RecvBuff[3];
    }

    public int SetRegion(ref byte fComAdr, byte dmaxfre, byte dminfreint)
    {
      this.SendBuff[0] = (byte) 6;
      this.SendBuff[1] = fComAdr;
      this.SendBuff[2] = (byte) 34;
      this.SendBuff[3] = dmaxfre;
      this.SendBuff[4] = dminfreint;
      this.GetCRC(this.SendBuff, 5);
      this.SendDataToPort(this.SendBuff, 7);
      int dataFromPort = this.GetDataFromPort(34, 1500);
      if (dataFromPort != 0)
        return dataFromPort;
      if (this.CheckCRC(this.RecvBuff, this.RecvLength) != 0)
        return 49;
      if (this.RecvBuff[2] != (byte) 34)
        return (int) this.RecvBuff[3];
      fComAdr = this.RecvBuff[1];
      return (int) this.RecvBuff[3];
    }

    public int SetInventoryScanTime(ref byte fComAdr, byte ScanTime)
    {
      this.SendBuff[0] = (byte) 5;
      this.SendBuff[1] = fComAdr;
      this.SendBuff[2] = (byte) 37;
      this.SendBuff[3] = ScanTime;
      this.GetCRC(this.SendBuff, 4);
      this.SendDataToPort(this.SendBuff, 6);
      int dataFromPort = this.GetDataFromPort(37, 1500);
      if (dataFromPort != 0)
        return dataFromPort;
      if (this.CheckCRC(this.RecvBuff, this.RecvLength) != 0)
        return 49;
      if (this.RecvBuff[2] != (byte) 37)
        return (int) this.RecvBuff[3];
      fComAdr = this.RecvBuff[1];
      return (int) this.RecvBuff[3];
    }

    public int SetBaudRate(ref byte fComAdr, byte baud)
    {
      this.SendBuff[0] = (byte) 5;
      this.SendBuff[1] = fComAdr;
      this.SendBuff[2] = (byte) 40;
      this.SendBuff[3] = baud;
      this.GetCRC(this.SendBuff, 4);
      this.SendDataToPort(this.SendBuff, 6);
      int dataFromPort = this.GetDataFromPort(40, 1500);
      if (dataFromPort != 0)
        return dataFromPort;
      if (this.CheckCRC(this.RecvBuff, this.RecvLength) != 0)
        return 49;
      if (this.RecvBuff[2] != (byte) 40)
        return (int) this.RecvBuff[3];
      fComAdr = this.RecvBuff[1];
      int num;
      switch (baud)
      {
        case 0:
          num = 9600;
          break;
        case 1:
          num = 19200;
          break;
        case 2:
          num = 38400;
          break;
        case 5:
          num = 57600;
          break;
        case 6:
          num = 115200;
          break;
        default:
          num = 57600;
          break;
      }
      this.serialPort1.BaudRate = num;
      this.serialPort1.Close();
      this.serialPort1.Open();
      return (int) this.RecvBuff[3];
    }

    public int SetRfPower(ref byte fComAdr, byte powerDbm)
    {
      this.SendBuff[0] = (byte) 5;
      this.SendBuff[1] = fComAdr;
      this.SendBuff[2] = (byte) 47;
      this.SendBuff[3] = powerDbm;
      this.GetCRC(this.SendBuff, 4);
      this.SendDataToPort(this.SendBuff, 6);
      int dataFromPort = this.GetDataFromPort(47, 1500);
      if (dataFromPort != 0)
        return dataFromPort;
      if (this.CheckCRC(this.RecvBuff, this.RecvLength) != 0)
        return 49;
      if (this.RecvBuff[2] != (byte) 47)
        return (int) this.RecvBuff[3];
      fComAdr = this.RecvBuff[1];
      return (int) this.RecvBuff[3];
    }

    public int SetRfPower(ref byte fComAdr, byte[] powerDbm)
    {
      this.SendBuff[0] = (byte) (4 + powerDbm.Length);
      this.SendBuff[1] = fComAdr;
      this.SendBuff[2] = (byte) 47;
      Array.Copy((Array) powerDbm, 0, (Array) this.SendBuff, 3, powerDbm.Length);
      this.GetCRC(this.SendBuff, (int) this.SendBuff[0] - 1);
      this.SendDataToPort(this.SendBuff, (int) this.SendBuff[0] + 1);
      int dataFromPort = this.GetDataFromPort(47, 1500);
      if (dataFromPort != 0)
        return dataFromPort;
      if (this.CheckCRC(this.RecvBuff, this.RecvLength) != 0)
        return 49;
      if (this.RecvBuff[2] != (byte) 47)
        return (int) this.RecvBuff[3];
      fComAdr = this.RecvBuff[1];
      return (int) this.RecvBuff[3];
    }

    public int BuzzerAndLEDControl(ref byte fComAdr, byte AvtiveTime, byte SilentTime, byte Times)
    {
      this.SendBuff[0] = (byte) 7;
      this.SendBuff[1] = fComAdr;
      this.SendBuff[2] = (byte) 51;
      this.SendBuff[3] = AvtiveTime;
      this.SendBuff[4] = SilentTime;
      this.SendBuff[5] = Times;
      this.GetCRC(this.SendBuff, 6);
      this.SendDataToPort(this.SendBuff, 8);
      int dataFromPort = this.GetDataFromPort(51, 1500);
      if (dataFromPort != 0)
        return dataFromPort;
      if (this.CheckCRC(this.RecvBuff, this.RecvLength) != 0)
        return 49;
      if (this.RecvBuff[2] != (byte) 51)
        return (int) this.RecvBuff[3];
      fComAdr = this.RecvBuff[1];
      return (int) this.RecvBuff[3];
    }

    public int SetAntennaMultiplexing(ref byte fComAdr, byte Ant)
    {
      this.SendBuff[0] = (byte) 5;
      this.SendBuff[1] = fComAdr;
      this.SendBuff[2] = (byte) 63;
      this.SendBuff[3] = Ant;
      this.GetCRC(this.SendBuff, 4);
      this.SendDataToPort(this.SendBuff, 6);
      int dataFromPort = this.GetDataFromPort(63, 1500);
      if (dataFromPort != 0)
        return dataFromPort;
      if (this.CheckCRC(this.RecvBuff, this.RecvLength) != 0)
        return 49;
      if (this.RecvBuff[2] != (byte) 63)
        return (int) this.RecvBuff[3];
      fComAdr = this.RecvBuff[1];
      return (int) this.RecvBuff[3];
    }

    public int SetAntennaMultiplexing(ref byte fComAdr, byte SetOnce, byte AntCfg1, byte AntCfg2)
    {
      this.SendBuff[0] = (byte) 7;
      this.SendBuff[1] = fComAdr;
      this.SendBuff[2] = (byte) 63;
      this.SendBuff[3] = SetOnce;
      this.SendBuff[4] = AntCfg1;
      this.SendBuff[5] = AntCfg2;
      this.GetCRC(this.SendBuff, 6);
      this.SendDataToPort(this.SendBuff, 8);
      int dataFromPort = this.GetDataFromPort(63, 1500);
      if (dataFromPort != 0)
        return dataFromPort;
      if (this.CheckCRC(this.RecvBuff, this.RecvLength) != 0)
        return 49;
      if (this.RecvBuff[2] != (byte) 63)
        return (int) this.RecvBuff[3];
      fComAdr = this.RecvBuff[1];
      return (int) this.RecvBuff[3];
    }

    public int SetBeepNotification(ref byte fComAdr, byte BeepEn)
    {
      this.SendBuff[0] = (byte) 5;
      this.SendBuff[1] = fComAdr;
      this.SendBuff[2] = (byte) 64;
      this.SendBuff[3] = BeepEn;
      this.GetCRC(this.SendBuff, 4);
      this.SendDataToPort(this.SendBuff, 6);
      int dataFromPort = this.GetDataFromPort(64, 1500);
      if (dataFromPort != 0)
        return dataFromPort;
      if (this.CheckCRC(this.RecvBuff, this.RecvLength) != 0)
        return 49;
      if (this.RecvBuff[2] != (byte) 64)
        return (int) this.RecvBuff[3];
      fComAdr = this.RecvBuff[1];
      return (int) this.RecvBuff[3];
    }

    public int SetGPIO(ref byte fComAdr, byte OutputPin)
    {
      this.SendBuff[0] = (byte) 5;
      this.SendBuff[1] = fComAdr;
      this.SendBuff[2] = (byte) 70;
      this.SendBuff[3] = OutputPin;
      this.GetCRC(this.SendBuff, 4);
      this.SendDataToPort(this.SendBuff, 6);
      int dataFromPort = this.GetDataFromPort(70, 1500);
      if (dataFromPort != 0)
        return dataFromPort;
      if (this.CheckCRC(this.RecvBuff, this.RecvLength) != 0)
        return 49;
      if (this.RecvBuff[2] != (byte) 70)
        return (int) this.RecvBuff[3];
      fComAdr = this.RecvBuff[1];
      return (int) this.RecvBuff[3];
    }

    public int GetGPIOStatus(ref byte fComAdr, ref byte OutputPin)
    {
      this.SendBuff[0] = (byte) 4;
      this.SendBuff[1] = fComAdr;
      this.SendBuff[2] = (byte) 71;
      this.GetCRC(this.SendBuff, 3);
      this.SendDataToPort(this.SendBuff, 5);
      int dataFromPort = this.GetDataFromPort(71, 1500);
      if (dataFromPort != 0)
        return dataFromPort;
      if (this.CheckCRC(this.RecvBuff, this.RecvLength) != 0)
        return 49;
      if (this.RecvBuff[2] != (byte) 71)
        return (int) this.RecvBuff[3];
      fComAdr = this.RecvBuff[1];
      OutputPin = this.RecvBuff[4];
      return (int) this.RecvBuff[3];
    }

    public int GetSeriaNo(ref byte fComAdr, byte[] SeriaNo)
    {
      this.SendBuff[0] = (byte) 4;
      this.SendBuff[1] = fComAdr;
      this.SendBuff[2] = (byte) 76;
      this.GetCRC(this.SendBuff, 3);
      this.SendDataToPort(this.SendBuff, 5);
      int dataFromPort = this.GetDataFromPort(76, 1500);
      if (dataFromPort != 0)
        return dataFromPort;
      if (this.CheckCRC(this.RecvBuff, this.RecvLength) != 0)
        return 49;
      if (this.RecvBuff[2] != (byte) 76)
        return (int) this.RecvBuff[3];
      fComAdr = this.RecvBuff[1];
      Array.Copy((Array) this.RecvBuff, 4, (Array) SeriaNo, 0, 4);
      return (int) this.RecvBuff[3];
    }

    public int SetCheckAnt(ref byte fComAdr, byte CheckAnt)
    {
      this.SendBuff[0] = (byte) 5;
      this.SendBuff[1] = fComAdr;
      this.SendBuff[2] = (byte) 102;
      this.SendBuff[3] = CheckAnt;
      this.GetCRC(this.SendBuff, 4);
      this.SendDataToPort(this.SendBuff, 6);
      int dataFromPort = this.GetDataFromPort(102, 1500);
      if (dataFromPort != 0)
        return dataFromPort;
      if (this.CheckCRC(this.RecvBuff, this.RecvLength) != 0)
        return 49;
      if (this.RecvBuff[2] != (byte) 102)
        return (int) this.RecvBuff[3];
      fComAdr = this.RecvBuff[1];
      return (int) this.RecvBuff[3];
    }

    public int SetWorkMode(ref byte fComAdr, byte Read_mode)
    {
      this.SendBuff[0] = (byte) 5;
      this.SendBuff[1] = fComAdr;
      this.SendBuff[2] = (byte) 53;
      this.SendBuff[3] = Read_mode;
      this.GetCRC(this.SendBuff, 4);
      this.SendDataToPort(this.SendBuff, 6);
      int dataFromPort = this.GetDataFromPort(53, 1500);
      if (dataFromPort != 0)
        return dataFromPort;
      if (this.CheckCRC(this.RecvBuff, this.RecvLength) != 0)
        return 49;
      if (this.RecvBuff[2] != (byte) 53)
        return (int) this.RecvBuff[3];
      fComAdr = this.RecvBuff[1];
      return (int) this.RecvBuff[3];
    }

    public int GetSystemParameter(
      ref byte fComAdr,
      ref byte Read_mode,
      ref byte Accuracy,
      ref byte RepCondition,
      ref byte RepPauseTime,
      ref byte ReadPauseTim,
      ref byte TagProtocol,
      ref byte MaskMem,
      byte[] MaskAdr,
      ref byte MaskLen,
      byte[] MaskData,
      ref byte TriggerTime,
      ref byte AdrTID,
      ref byte LenTID)
    {
      this.SendBuff[0] = (byte) 4;
      this.SendBuff[1] = fComAdr;
      this.SendBuff[2] = (byte) 54;
      this.GetCRC(this.SendBuff, 3);
      this.SendDataToPort(this.SendBuff, 5);
      int dataFromPort = this.GetDataFromPort(54, 1500);
      if (dataFromPort != 0)
        return dataFromPort;
      if (this.CheckCRC(this.RecvBuff, this.RecvLength) != 0)
        return 49;
      if (this.RecvBuff[2] != (byte) 54 || this.RecvBuff[3] != (byte) 0)
        return (int) this.RecvBuff[3];
      fComAdr = this.RecvBuff[1];
      Read_mode = this.RecvBuff[4];
      Accuracy = this.RecvBuff[5];
      RepCondition = this.RecvBuff[6];
      RepPauseTime = this.RecvBuff[7];
      ReadPauseTim = this.RecvBuff[8];
      TagProtocol = this.RecvBuff[9];
      MaskMem = this.RecvBuff[10];
      MaskAdr[0] = this.RecvBuff[11];
      MaskAdr[1] = this.RecvBuff[12];
      MaskLen = this.RecvBuff[13];
      Array.Copy((Array) this.RecvBuff, 14, (Array) MaskData, 0, 32);
      if (this.RecvBuff[0] == (byte) 47)
      {
        TriggerTime = (byte) 0;
      }
      else
      {
        TriggerTime = this.RecvBuff[46];
        AdrTID = this.RecvBuff[47];
        LenTID = this.RecvBuff[48];
      }
      return (int) this.RecvBuff[3];
    }

    public int SetEASSensitivity(ref byte fComAdr, byte Accuracy)
    {
      this.SendBuff[0] = (byte) 5;
      this.SendBuff[1] = fComAdr;
      this.SendBuff[2] = (byte) 55;
      this.SendBuff[3] = Accuracy;
      this.GetCRC(this.SendBuff, 4);
      this.SendDataToPort(this.SendBuff, 6);
      int dataFromPort = this.GetDataFromPort(55, 1500);
      if (dataFromPort != 0)
        return dataFromPort;
      if (this.CheckCRC(this.RecvBuff, this.RecvLength) != 0)
        return 49;
      if (this.RecvBuff[2] != (byte) 55)
        return (int) this.RecvBuff[3];
      fComAdr = this.RecvBuff[1];
      return (int) this.RecvBuff[3];
    }

    public int SetMask(
      ref byte fComAdr,
      byte MaskMem,
      byte[] MaskAdr,
      byte MaskLen,
      byte[] MaskData)
    {
      this.SendBuff[1] = fComAdr;
      this.SendBuff[2] = (byte) 59;
      this.SendBuff[3] = MaskMem;
      this.SendBuff[4] = MaskAdr[0];
      this.SendBuff[5] = MaskAdr[0];
      this.SendBuff[6] = MaskLen;
      int length = (int) MaskLen % 8 != 0 ? (int) MaskLen / 8 + 1 : (int) MaskLen / 8;
      Array.Copy((Array) MaskData, 0, (Array) this.SendBuff, 7, length);
      this.SendBuff[0] = Convert.ToByte(8 + length);
      this.GetCRC(this.SendBuff, 7 + length);
      this.SendDataToPort(this.SendBuff, 9 + length);
      int dataFromPort = this.GetDataFromPort(59, 1500);
      if (dataFromPort != 0)
        return dataFromPort;
      if (this.CheckCRC(this.RecvBuff, this.RecvLength) != 0)
        return 49;
      if (this.RecvBuff[2] != (byte) 59)
        return (int) this.RecvBuff[3];
      fComAdr = this.RecvBuff[1];
      return (int) this.RecvBuff[3];
    }

    public int SetResponsePamametersofAuto_runningMode(
      ref byte fComAdr,
      byte RepCondition,
      byte RepPauseTime)
    {
      this.SendBuff[0] = (byte) 6;
      this.SendBuff[1] = fComAdr;
      this.SendBuff[2] = (byte) 60;
      this.SendBuff[3] = RepCondition;
      this.SendBuff[4] = RepPauseTime;
      this.GetCRC(this.SendBuff, 5);
      this.SendDataToPort(this.SendBuff, 7);
      int dataFromPort = this.GetDataFromPort(60, 1500);
      if (dataFromPort != 0)
        return dataFromPort;
      if (this.CheckCRC(this.RecvBuff, this.RecvLength) != 0)
        return 49;
      if (this.RecvBuff[2] != (byte) 60)
        return (int) this.RecvBuff[3];
      fComAdr = this.RecvBuff[1];
      return (int) this.RecvBuff[3];
    }

    public int SetInventoryInterval(ref byte fComAdr, byte ReadPauseTim)
    {
      this.SendBuff[0] = (byte) 5;
      this.SendBuff[1] = fComAdr;
      this.SendBuff[2] = (byte) 61;
      this.SendBuff[3] = ReadPauseTim;
      this.GetCRC(this.SendBuff, 4);
      this.SendDataToPort(this.SendBuff, 6);
      int dataFromPort = this.GetDataFromPort(61, 1500);
      if (dataFromPort != 0)
        return dataFromPort;
      if (this.CheckCRC(this.RecvBuff, this.RecvLength) != 0)
        return 49;
      if (this.RecvBuff[2] != (byte) 61)
        return (int) this.RecvBuff[3];
      fComAdr = this.RecvBuff[1];
      return (int) this.RecvBuff[3];
    }

    public int SelectTagType(ref byte fComAdr, byte Protocol)
    {
      this.SendBuff[0] = (byte) 5;
      this.SendBuff[1] = fComAdr;
      this.SendBuff[2] = (byte) 62;
      this.SendBuff[3] = Protocol;
      this.GetCRC(this.SendBuff, 4);
      this.SendDataToPort(this.SendBuff, 6);
      int dataFromPort = this.GetDataFromPort(62, 1500);
      if (dataFromPort != 0)
        return dataFromPort;
      if (this.CheckCRC(this.RecvBuff, this.RecvLength) != 0)
        return 49;
      if (this.RecvBuff[2] != (byte) 62)
        return (int) this.RecvBuff[3];
      fComAdr = this.RecvBuff[1];
      return (int) this.RecvBuff[3];
    }

    public int SetReal_timeClock(ref byte fComAdr, byte[] paramer)
    {
      this.SendBuff[0] = (byte) 10;
      this.SendBuff[1] = fComAdr;
      this.SendBuff[2] = (byte) 65;
      Array.Copy((Array) paramer, 0, (Array) this.SendBuff, 3, 6);
      this.GetCRC(this.SendBuff, 9);
      this.SendDataToPort(this.SendBuff, 11);
      int dataFromPort = this.GetDataFromPort(65, 1500);
      if (dataFromPort != 0)
        return dataFromPort;
      if (this.CheckCRC(this.RecvBuff, this.RecvLength) != 0)
        return 49;
      if (this.RecvBuff[2] != (byte) 65)
        return (int) this.RecvBuff[3];
      fComAdr = this.RecvBuff[1];
      return (int) this.RecvBuff[3];
    }

    public int GetTime(ref byte fComAdr, byte[] paramer)
    {
      this.SendBuff[0] = (byte) 4;
      this.SendBuff[1] = fComAdr;
      this.SendBuff[2] = (byte) 66;
      this.GetCRC(this.SendBuff, 3);
      this.SendDataToPort(this.SendBuff, 5);
      int dataFromPort = this.GetDataFromPort(66, 1500);
      if (dataFromPort != 0)
        return dataFromPort;
      if (this.CheckCRC(this.RecvBuff, this.RecvLength) != 0)
        return 49;
      if (this.RecvBuff[2] != (byte) 66)
        return (int) this.RecvBuff[3];
      fComAdr = this.RecvBuff[1];
      Array.Copy((Array) this.RecvBuff, 4, (Array) paramer, 0, 6);
      return (int) this.RecvBuff[3];
    }

    public int GetTagBufferInfo(ref byte fComAdr, byte[] Data, ref int dataLength)
    {
      dataLength = 0;
      return 0;
    }

    public int ClearTagBuffer(ref byte fComAdr)
    {
      this.SendBuff[0] = (byte) 4;
      this.SendBuff[1] = fComAdr;
      this.SendBuff[2] = (byte) 68;
      this.GetCRC(this.SendBuff, 3);
      this.SendDataToPort(this.SendBuff, 5);
      int dataFromPort = this.GetDataFromPort(68, 1500);
      if (dataFromPort != 0)
        return dataFromPort;
      if (this.CheckCRC(this.RecvBuff, this.RecvLength) != 0)
        return 49;
      if (this.RecvBuff[2] != (byte) 68)
        return (int) this.RecvBuff[3];
      fComAdr = this.RecvBuff[1];
      return (int) this.RecvBuff[3];
    }

    public int SetRelay(ref byte fComAdr, byte RelayTime)
    {
      this.SendBuff[0] = (byte) 5;
      this.SendBuff[1] = fComAdr;
      this.SendBuff[2] = (byte) 69;
      this.SendBuff[3] = RelayTime;
      this.GetCRC(this.SendBuff, 4);
      this.SendDataToPort(this.SendBuff, 6);
      int dataFromPort = this.GetDataFromPort(69, 1500);
      if (dataFromPort != 0)
        return dataFromPort;
      if (this.CheckCRC(this.RecvBuff, this.RecvLength) != 0)
        return 49;
      if (this.RecvBuff[2] != (byte) 69)
        return (int) this.RecvBuff[3];
      fComAdr = this.RecvBuff[1];
      return (int) this.RecvBuff[3];
    }

    public int SetNotificationPulseOutput(ref byte fComAdr, byte OutputRep)
    {
      this.SendBuff[0] = (byte) 5;
      this.SendBuff[1] = fComAdr;
      this.SendBuff[2] = (byte) 72;
      this.SendBuff[3] = OutputRep;
      this.GetCRC(this.SendBuff, 4);
      this.SendDataToPort(this.SendBuff, 6);
      int dataFromPort = this.GetDataFromPort(72, 1500);
      if (dataFromPort != 0)
        return dataFromPort;
      if (this.CheckCRC(this.RecvBuff, this.RecvLength) != 0)
        return 49;
      if (this.RecvBuff[2] != (byte) 72)
        return (int) this.RecvBuff[3];
      fComAdr = this.RecvBuff[1];
      return (int) this.RecvBuff[3];
    }

    public int SetTriggerTime(ref byte fComAdr, byte TriggerTime)
    {
      this.SendBuff[0] = (byte) 5;
      this.SendBuff[1] = fComAdr;
      this.SendBuff[2] = (byte) 73;
      this.SendBuff[3] = TriggerTime;
      this.GetCRC(this.SendBuff, 4);
      this.SendDataToPort(this.SendBuff, 6);
      int dataFromPort = this.GetDataFromPort(73, 1500);
      if (dataFromPort != 0)
        return dataFromPort;
      if (this.CheckCRC(this.RecvBuff, this.RecvLength) != 0)
        return 49;
      if (this.RecvBuff[2] != (byte) 73)
        return (int) this.RecvBuff[3];
      fComAdr = this.RecvBuff[1];
      return (int) this.RecvBuff[3];
    }

    public int SetTIDParameter(ref byte fComAdr, byte AdrTID, byte LenTID)
    {
      this.SendBuff[0] = (byte) 6;
      this.SendBuff[1] = fComAdr;
      this.SendBuff[2] = (byte) 74;
      this.SendBuff[3] = AdrTID;
      this.SendBuff[4] = LenTID;
      this.GetCRC(this.SendBuff, 5);
      this.SendDataToPort(this.SendBuff, 7);
      int dataFromPort = this.GetDataFromPort(74, 1500);
      if (dataFromPort != 0)
        return dataFromPort;
      if (this.CheckCRC(this.RecvBuff, this.RecvLength) != 0)
        return 49;
      if (this.RecvBuff[2] != (byte) 74)
        return (int) this.RecvBuff[3];
      fComAdr = this.RecvBuff[1];
      return (int) this.RecvBuff[3];
    }

    public int ChangeATMode(ref byte fComAdr, byte ATMode)
    {
      this.SendBuff[0] = (byte) 5;
      this.SendBuff[1] = fComAdr;
      this.SendBuff[2] = (byte) 77;
      this.SendBuff[3] = ATMode;
      this.GetCRC(this.SendBuff, 4);
      this.SendDataToPort(this.SendBuff, 6);
      int dataFromPort = this.GetDataFromPort(77, 1500);
      if (dataFromPort != 0)
        return dataFromPort;
      if (this.CheckCRC(this.RecvBuff, this.RecvLength) != 0)
        return 49;
      if (this.RecvBuff[2] != (byte) 77)
        return (int) this.RecvBuff[3];
      fComAdr = this.RecvBuff[1];
      return (int) this.RecvBuff[3];
    }

    public int TransparentCMD(
      ref byte fComAdr,
      byte timeout,
      byte cmdlen,
      byte[] cmddata,
      ref byte recvLen,
      byte[] recvdata)
    {
      this.SendBuff[0] = Convert.ToByte(5 + (int) cmdlen);
      this.SendBuff[1] = fComAdr;
      this.SendBuff[2] = (byte) 78;
      this.SendBuff[3] = timeout;
      Array.Copy((Array) cmddata, 0, (Array) this.SendBuff, 4, (int) cmdlen);
      this.GetCRC(this.SendBuff, 4 + (int) cmdlen);
      this.SendDataToPort(this.SendBuff, 6 + (int) cmdlen);
      int dataFromPort = this.GetDataFromPort(78, 1500);
      if (dataFromPort != 0)
        return dataFromPort;
      if (this.CheckCRC(this.RecvBuff, this.RecvLength) != 0)
        return 49;
      if (this.RecvBuff[2] != (byte) 78 || this.RecvBuff[3] != (byte) 0)
        return (int) this.RecvBuff[3];
      fComAdr = this.RecvBuff[1];
      recvLen = Convert.ToByte(this.RecvLength - 6);
      if (recvLen > (byte) 0)
        Array.Copy((Array) this.RecvBuff, 4, (Array) recvdata, 0, (int) recvLen);
      return (int) this.RecvBuff[3];
    }

    public int SetQS(ref byte fComAdr, byte Qvalue, byte Session)
    {
      this.SendBuff[0] = (byte) 6;
      this.SendBuff[1] = fComAdr;
      this.SendBuff[2] = (byte) 129;
      this.SendBuff[3] = Qvalue;
      this.SendBuff[4] = Session;
      this.GetCRC(this.SendBuff, 5);
      this.SendDataToPort(this.SendBuff, 7);
      int dataFromPort = this.GetDataFromPort(129, 1500);
      if (dataFromPort != 0)
        return dataFromPort;
      if (this.CheckCRC(this.RecvBuff, this.RecvLength) != 0)
        return 49;
      if (this.RecvBuff[2] != (byte) 129)
        return (int) this.RecvBuff[3];
      fComAdr = this.RecvBuff[1];
      return (int) this.RecvBuff[3];
    }

    public int GetQS(ref byte fComAdr, ref byte Qvalue, ref byte Session)
    {
      this.SendBuff[0] = (byte) 4;
      this.SendBuff[1] = fComAdr;
      this.SendBuff[2] = (byte) 130;
      this.GetCRC(this.SendBuff, 3);
      this.SendDataToPort(this.SendBuff, 5);
      int dataFromPort = this.GetDataFromPort(130, 1500);
      if (dataFromPort != 0)
        return dataFromPort;
      if (this.CheckCRC(this.RecvBuff, this.RecvLength) != 0)
        return 49;
      if (this.RecvBuff[2] != (byte) 130)
        return (int) this.RecvBuff[3];
      fComAdr = this.RecvBuff[1];
      Qvalue = this.RecvBuff[4];
      Session = this.RecvBuff[5];
      return (int) this.RecvBuff[3];
    }

    public int GetModuleVersion(ref byte fComAdr, byte[] Version)
    {
      this.SendBuff[0] = (byte) 4;
      this.SendBuff[1] = fComAdr;
      this.SendBuff[2] = (byte) 131;
      this.GetCRC(this.SendBuff, 3);
      this.SendDataToPort(this.SendBuff, 5);
      int dataFromPort = this.GetDataFromPort(131, 1500);
      if (dataFromPort != 0)
        return dataFromPort;
      if (this.CheckCRC(this.RecvBuff, this.RecvLength) != 0)
        return 49;
      if (this.RecvBuff[2] != (byte) 131)
        return (int) this.RecvBuff[3];
      fComAdr = this.RecvBuff[1];
      Version[0] = this.RecvBuff[4];
      Version[1] = this.RecvBuff[5];
      return (int) this.RecvBuff[3];
    }

    public int SetFlashRom(ref byte fComAdr)
    {
      this.SendBuff[0] = (byte) 4;
      this.SendBuff[1] = fComAdr;
      this.SendBuff[2] = (byte) 132;
      this.GetCRC(this.SendBuff, 3);
      this.SendDataToPort(this.SendBuff, 5);
      int dataFromPort = this.GetDataFromPort(132, 1500);
      if (dataFromPort != 0)
        return dataFromPort;
      if (this.CheckCRC(this.RecvBuff, this.RecvLength) != 0)
        return 49;
      if (this.RecvBuff[2] != (byte) 132)
        return (int) this.RecvBuff[3];
      fComAdr = this.RecvBuff[1];
      return (int) this.RecvBuff[3];
    }

    public int ReadActiveModeData(byte[] ScanModeData, ref int ValidDatalength)
    {
      try
      {
        if (this.COM_TYPE == 0)
        {
          int bytesToRead = this.serialPort1.BytesToRead;
          if (bytesToRead <= 0)
            return 14;
          int length = this.serialPort1.Read(this.RecvBuff, 0, bytesToRead);
          if (length > 0)
          {
            ValidDatalength = length;
            Array.Copy((Array) this.RecvBuff, (Array) ScanModeData, length);
          }
          return 0;
        }
        int length1 = this.streamToTran.Read(this.RecvBuff, 0, 1000);
        if (length1 <= 0)
          return 14;
        ValidDatalength = length1;
        Array.Copy((Array) this.RecvBuff, (Array) ScanModeData, length1);
        return 0;
      }
      catch (Exception ex)
      {
        ex.ToString();
        return 48;
      }
    }

    public int ReadData_G2(
      ref byte fComAdr,
      byte[] EPC,
      byte ENum,
      byte Mem,
      byte WordPtr,
      byte Num,
      byte[] Password,
      byte MaskMem,
      byte[] MaskAdr,
      byte MaskLen,
      byte[] MaskData,
      byte[] Data,
      ref int errorcode)
    {
      this.SendBuff[1] = fComAdr;
      this.SendBuff[2] = (byte) 2;
      this.SendBuff[3] = ENum;
      if (ENum == byte.MaxValue)
      {
        this.SendBuff[4] = Mem;
        this.SendBuff[5] = WordPtr;
        this.SendBuff[6] = Num;
        Array.Copy((Array) Password, 0, (Array) this.SendBuff, 7, 4);
        this.SendBuff[11] = MaskMem;
        this.SendBuff[12] = MaskAdr[0];
        this.SendBuff[13] = MaskAdr[1];
        this.SendBuff[14] = MaskLen;
        int length = (int) MaskLen % 8 != 0 ? (int) MaskLen / 8 + 1 : (int) MaskLen / 8;
        Array.Copy((Array) MaskData, 0, (Array) this.SendBuff, 15, length);
        this.SendBuff[0] = Convert.ToByte(16 + length);
      }
      else
      {
        if (ENum < (byte) 0 || ENum >= (byte) 32)
          return (int) byte.MaxValue;
        Array.Copy((Array) EPC, 0, (Array) this.SendBuff, 4, (int) ENum * 2);
        this.SendBuff[(int) ENum * 2 + 4] = Mem;
        this.SendBuff[(int) ENum * 2 + 5] = WordPtr;
        this.SendBuff[(int) ENum * 2 + 6] = Num;
        Array.Copy((Array) Password, 0, (Array) this.SendBuff, (int) ENum * 2 + 7, 4);
        this.SendBuff[0] = Convert.ToByte((int) ENum * 2 + 12);
      }
      this.GetCRC(this.SendBuff, (int) this.SendBuff[0] - 1);
      this.SendDataToPort(this.SendBuff, (int) this.SendBuff[0] + 1);
      int dataFromPort = this.GetDataFromPort(2, 3000);
      if (dataFromPort != 0)
        return dataFromPort;
      if (this.CheckCRC(this.RecvBuff, this.RecvLength) != 0)
        return 49;
      if (this.RecvBuff[2] != (byte) 2)
        return (int) this.RecvBuff[3];
      if (this.RecvBuff[3] == (byte) 0)
      {
        fComAdr = this.RecvBuff[1];
        errorcode = 0;
        Array.Copy((Array) this.RecvBuff, 4, (Array) Data, 0, this.RecvLength - 6);
      }
      else if (this.RecvBuff[3] == (byte) 252)
        errorcode = (int) this.RecvBuff[4];
      return (int) this.RecvBuff[3];
    }

    public int ExtReadData_G2(
      ref byte fComAdr,
      byte[] EPC,
      byte ENum,
      byte Mem,
      byte[] WordPtr,
      byte Num,
      byte[] Password,
      byte MaskMem,
      byte[] MaskAdr,
      byte MaskLen,
      byte[] MaskData,
      byte[] Data,
      ref int errorcode)
    {
      this.SendBuff[1] = fComAdr;
      this.SendBuff[2] = (byte) 21;
      this.SendBuff[3] = ENum;
      if (ENum == byte.MaxValue)
      {
        this.SendBuff[4] = Mem;
        this.SendBuff[5] = WordPtr[0];
        this.SendBuff[6] = WordPtr[1];
        this.SendBuff[7] = Num;
        Array.Copy((Array) Password, 0, (Array) this.SendBuff, 8, 4);
        this.SendBuff[12] = MaskMem;
        this.SendBuff[13] = MaskAdr[0];
        this.SendBuff[14] = MaskAdr[1];
        this.SendBuff[15] = MaskLen;
        int length = (int) MaskLen % 8 != 0 ? (int) MaskLen / 8 + 1 : (int) MaskLen / 8;
        Array.Copy((Array) MaskData, 0, (Array) this.SendBuff, 16, length);
        this.SendBuff[0] = Convert.ToByte(17 + length);
      }
      else
      {
        if (ENum < (byte) 0 || ENum >= (byte) 32)
          return (int) byte.MaxValue;
        Array.Copy((Array) EPC, 0, (Array) this.SendBuff, 4, (int) ENum * 2);
        this.SendBuff[(int) ENum * 2 + 4] = Mem;
        this.SendBuff[(int) ENum * 2 + 5] = WordPtr[0];
        this.SendBuff[(int) ENum * 2 + 6] = WordPtr[1];
        this.SendBuff[(int) ENum * 2 + 7] = Num;
        Array.Copy((Array) Password, 0, (Array) this.SendBuff, (int) ENum * 2 + 8, 4);
        this.SendBuff[0] = Convert.ToByte((int) ENum * 2 + 13);
      }
      this.GetCRC(this.SendBuff, (int) this.SendBuff[0] - 1);
      this.SendDataToPort(this.SendBuff, (int) this.SendBuff[0] + 1);
      int dataFromPort = this.GetDataFromPort(21, 3000);
      if (dataFromPort != 0)
        return dataFromPort;
      if (this.CheckCRC(this.RecvBuff, this.RecvLength) != 0)
        return 49;
      if (this.RecvBuff[2] != (byte) 21)
        return (int) this.RecvBuff[3];
      if (this.RecvBuff[3] == (byte) 0)
      {
        fComAdr = this.RecvBuff[1];
        errorcode = 0;
        Array.Copy((Array) this.RecvBuff, 4, (Array) Data, 0, this.RecvLength - 6);
      }
      else if (this.RecvBuff[3] == (byte) 252)
        errorcode = (int) this.RecvBuff[4];
      return (int) this.RecvBuff[3];
    }

    public int WriteData_G2(
      ref byte fComAdr,
      byte[] EPC,
      byte WNum,
      byte ENum,
      byte Mem,
      byte WordPtr,
      byte[] Wdt,
      byte[] Password,
      byte MaskMem,
      byte[] MaskAdr,
      byte MaskLen,
      byte[] MaskData,
      ref int errorcode)
    {
      this.SendBuff[1] = fComAdr;
      this.SendBuff[2] = (byte) 3;
      this.SendBuff[3] = WNum;
      this.SendBuff[4] = ENum;
      if (ENum == byte.MaxValue)
      {
        this.SendBuff[5] = Mem;
        this.SendBuff[6] = WordPtr;
        Array.Copy((Array) Wdt, 0, (Array) this.SendBuff, 7, (int) WNum * 2);
        Array.Copy((Array) Password, 0, (Array) this.SendBuff, 7 + (int) WNum * 2, 4);
        this.SendBuff[11 + (int) WNum * 2] = MaskMem;
        this.SendBuff[12 + (int) WNum * 2] = MaskAdr[0];
        this.SendBuff[13 + (int) WNum * 2] = MaskAdr[1];
        this.SendBuff[14 + (int) WNum * 2] = MaskLen;
        int length = (int) MaskLen % 8 != 0 ? (int) MaskLen / 8 + 1 : (int) MaskLen / 8;
        Array.Copy((Array) MaskData, 0, (Array) this.SendBuff, 15 + (int) WNum * 2, length);
        this.SendBuff[0] = Convert.ToByte(16 + (int) WNum * 2 + length);
      }
      else
      {
        if (ENum < (byte) 0 || ENum >= (byte) 32)
          return (int) byte.MaxValue;
        Array.Copy((Array) EPC, 0, (Array) this.SendBuff, 5, (int) ENum * 2);
        this.SendBuff[(int) ENum * 2 + 5] = Mem;
        this.SendBuff[(int) ENum * 2 + 6] = WordPtr;
        Array.Copy((Array) Wdt, 0, (Array) this.SendBuff, (int) ENum * 2 + 7, (int) WNum * 2);
        Array.Copy((Array) Password, 0, (Array) this.SendBuff, (int) WNum * 2 + (int) ENum * 2 + 7, 4);
        this.SendBuff[0] = Convert.ToByte((int) ENum * 2 + (int) WNum * 2 + 12);
      }
      this.GetCRC(this.SendBuff, (int) this.SendBuff[0] - 1);
      this.SendDataToPort(this.SendBuff, (int) this.SendBuff[0] + 1);
      int dataFromPort = this.GetDataFromPort(3, 3000);
      if (dataFromPort != 0)
        return dataFromPort;
      if (this.CheckCRC(this.RecvBuff, this.RecvLength) != 0)
        return 49;
      if (this.RecvBuff[2] != (byte) 3)
        return (int) this.RecvBuff[3];
      if (this.RecvBuff[3] == (byte) 0)
      {
        fComAdr = this.RecvBuff[1];
        errorcode = 0;
      }
      else if (this.RecvBuff[3] == (byte) 252)
        errorcode = (int) this.RecvBuff[4];
      return (int) this.RecvBuff[3];
    }

    public int ExtWriteData_G2(
      ref byte fComAdr,
      byte[] EPC,
      byte WNum,
      byte ENum,
      byte Mem,
      byte[] WordPtr,
      byte[] Wdt,
      byte[] Password,
      byte MaskMem,
      byte[] MaskAdr,
      byte MaskLen,
      byte[] MaskData,
      ref int errorcode)
    {
      this.SendBuff[1] = fComAdr;
      this.SendBuff[2] = (byte) 22;
      this.SendBuff[3] = WNum;
      this.SendBuff[4] = ENum;
      if (ENum == byte.MaxValue)
      {
        this.SendBuff[5] = Mem;
        this.SendBuff[6] = WordPtr[0];
        this.SendBuff[7] = WordPtr[1];
        Array.Copy((Array) Wdt, 0, (Array) this.SendBuff, 8, (int) WNum * 2);
        Array.Copy((Array) Password, 0, (Array) this.SendBuff, 8 + (int) WNum * 2, 4);
        this.SendBuff[12 + (int) WNum * 2] = MaskMem;
        this.SendBuff[13 + (int) WNum * 2] = MaskAdr[0];
        this.SendBuff[14 + (int) WNum * 2] = MaskAdr[1];
        this.SendBuff[15 + (int) WNum * 2] = MaskLen;
        int length = (int) MaskLen % 8 != 0 ? (int) MaskLen / 8 + 1 : (int) MaskLen / 8;
        Array.Copy((Array) MaskData, 0, (Array) this.SendBuff, 16 + (int) WNum * 2, length);
        this.SendBuff[0] = Convert.ToByte(17 + (int) WNum * 2 + length);
      }
      else
      {
        if (ENum < (byte) 0 || ENum >= (byte) 32)
          return (int) byte.MaxValue;
        Array.Copy((Array) EPC, 0, (Array) this.SendBuff, 5, (int) ENum * 2);
        this.SendBuff[(int) ENum * 2 + 5] = Mem;
        this.SendBuff[(int) ENum * 2 + 6] = WordPtr[0];
        this.SendBuff[(int) ENum * 2 + 7] = WordPtr[1];
        Array.Copy((Array) Wdt, 0, (Array) this.SendBuff, (int) ENum * 2 + 8, (int) WNum * 2);
        Array.Copy((Array) Password, 0, (Array) this.SendBuff, (int) WNum * 2 + (int) ENum * 2 + 8, 4);
        this.SendBuff[0] = Convert.ToByte((int) ENum * 2 + (int) WNum * 2 + 13);
      }
      this.GetCRC(this.SendBuff, (int) this.SendBuff[0] - 1);
      this.SendDataToPort(this.SendBuff, (int) this.SendBuff[0] + 1);
      int dataFromPort = this.GetDataFromPort(22, 3000);
      if (dataFromPort != 0)
        return dataFromPort;
      if (this.CheckCRC(this.RecvBuff, this.RecvLength) != 0)
        return 49;
      if (this.RecvBuff[2] != (byte) 22)
        return (int) this.RecvBuff[3];
      if (this.RecvBuff[3] == (byte) 0)
      {
        fComAdr = this.RecvBuff[1];
        errorcode = 0;
      }
      else if (this.RecvBuff[3] == (byte) 252)
        errorcode = (int) this.RecvBuff[4];
      return (int) this.RecvBuff[3];
    }

    public int WriteEPC_G2(
      ref byte fComAdr,
      byte[] Password,
      byte[] WriteEPC,
      byte ENum,
      ref int errorcode)
    {
      this.SendBuff[1] = fComAdr;
      this.SendBuff[2] = (byte) 4;
      this.SendBuff[3] = ENum;
      Array.Copy((Array) Password, 0, (Array) this.SendBuff, 4, 4);
      Array.Copy((Array) WriteEPC, 0, (Array) this.SendBuff, 8, (int) ENum * 2);
      this.SendBuff[0] = Convert.ToByte((int) ENum * 2 + 9);
      this.GetCRC(this.SendBuff, (int) ENum * 2 + 8);
      this.SendDataToPort(this.SendBuff, (int) ENum * 2 + 10);
      int dataFromPort = this.GetDataFromPort(4, 1500);
      if (dataFromPort != 0)
        return dataFromPort;
      if (this.CheckCRC(this.RecvBuff, this.RecvLength) != 0)
        return 49;
      if (this.RecvBuff[2] != (byte) 4)
        return (int) this.RecvBuff[3];
      if (this.RecvBuff[3] == (byte) 0)
      {
        fComAdr = this.RecvBuff[1];
        errorcode = 0;
      }
      else if (this.RecvBuff[3] == (byte) 252)
        errorcode = (int) this.RecvBuff[4];
      return (int) this.RecvBuff[3];
    }

    public int KillTag_G2(
      ref byte fComAdr,
      byte[] EPC,
      byte ENum,
      byte[] Password,
      byte MaskMem,
      byte[] MaskAdr,
      byte MaskLen,
      byte[] MaskData,
      ref int errorcode)
    {
      this.SendBuff[1] = fComAdr;
      this.SendBuff[2] = (byte) 5;
      this.SendBuff[3] = ENum;
      if (ENum == byte.MaxValue)
      {
        Array.Copy((Array) Password, 0, (Array) this.SendBuff, 4, 4);
        this.SendBuff[8] = MaskMem;
        this.SendBuff[9] = MaskAdr[0];
        this.SendBuff[10] = MaskAdr[1];
        this.SendBuff[11] = MaskLen;
        int length = (int) MaskLen % 8 != 0 ? (int) MaskLen / 8 + 1 : (int) MaskLen / 8;
        Array.Copy((Array) MaskData, 0, (Array) this.SendBuff, 12, length);
        this.SendBuff[0] = Convert.ToByte(13 + length);
      }
      else
      {
        if (ENum < (byte) 0 || ENum >= (byte) 32)
          return (int) byte.MaxValue;
        Array.Copy((Array) EPC, 0, (Array) this.SendBuff, 4, (int) ENum * 2);
        Array.Copy((Array) Password, 0, (Array) this.SendBuff, (int) ENum * 2 + 4, 4);
        this.SendBuff[0] = Convert.ToByte(9 + (int) ENum * 2);
      }
      this.GetCRC(this.SendBuff, (int) this.SendBuff[0] - 1);
      this.SendDataToPort(this.SendBuff, (int) this.SendBuff[0] + 1);
      int dataFromPort = this.GetDataFromPort(5, 1500);
      if (dataFromPort != 0)
        return dataFromPort;
      if (this.CheckCRC(this.RecvBuff, this.RecvLength) != 0)
        return 49;
      if (this.RecvBuff[2] != (byte) 5)
        return (int) this.RecvBuff[3];
      if (this.RecvBuff[3] == (byte) 0)
      {
        fComAdr = this.RecvBuff[1];
        errorcode = 0;
      }
      else if (this.RecvBuff[3] == (byte) 252)
        errorcode = (int) this.RecvBuff[4];
      return (int) this.RecvBuff[3];
    }

    public int Lock_G2(
      ref byte fComAdr,
      byte[] EPC,
      byte ENum,
      byte select,
      byte setprotect,
      byte[] Password,
      byte MaskMem,
      byte[] MaskAdr,
      byte MaskLen,
      byte[] MaskData,
      ref int errorcode)
    {
      this.SendBuff[1] = fComAdr;
      this.SendBuff[2] = (byte) 6;
      this.SendBuff[3] = ENum;
      if (ENum == byte.MaxValue)
      {
        this.SendBuff[4] = select;
        this.SendBuff[5] = setprotect;
        Array.Copy((Array) Password, 0, (Array) this.SendBuff, 6, 4);
        this.SendBuff[10] = MaskMem;
        this.SendBuff[11] = MaskAdr[0];
        this.SendBuff[12] = MaskAdr[11];
        this.SendBuff[13] = MaskLen;
        int length = (int) MaskLen % 8 != 0 ? (int) MaskLen / 8 + 1 : (int) MaskLen / 8;
        Array.Copy((Array) MaskData, 0, (Array) this.SendBuff, 14, length);
        this.SendBuff[0] = Convert.ToByte(15 + length);
      }
      else
      {
        if (ENum < (byte) 0 || ENum >= (byte) 32)
          return (int) byte.MaxValue;
        Array.Copy((Array) EPC, 0, (Array) this.SendBuff, 4, (int) ENum * 2);
        this.SendBuff[(int) ENum * 2 + 4] = select;
        this.SendBuff[(int) ENum * 2 + 5] = setprotect;
        Array.Copy((Array) Password, 0, (Array) this.SendBuff, (int) ENum * 2 + 6, 4);
        this.SendBuff[0] = Convert.ToByte((int) ENum * 2 + 11);
      }
      this.GetCRC(this.SendBuff, (int) this.SendBuff[0] - 1);
      this.SendDataToPort(this.SendBuff, (int) this.SendBuff[0] + 1);
      int dataFromPort = this.GetDataFromPort(6, 1500);
      if (dataFromPort != 0)
        return dataFromPort;
      if (this.CheckCRC(this.RecvBuff, this.RecvLength) != 0)
        return 49;
      if (this.RecvBuff[2] != (byte) 6)
        return (int) this.RecvBuff[3];
      if (this.RecvBuff[3] == (byte) 0)
      {
        fComAdr = this.RecvBuff[1];
        errorcode = 0;
      }
      else if (this.RecvBuff[3] == (byte) 252)
        errorcode = (int) this.RecvBuff[4];
      return (int) this.RecvBuff[3];
    }

    public int BlockErase_G2(
      ref byte fComAdr,
      byte[] EPC,
      byte ENum,
      byte Mem,
      byte WordPtr,
      byte Num,
      byte[] Password,
      byte MaskMem,
      byte[] MaskAdr,
      byte MaskLen,
      byte[] MaskData,
      ref int errorcode)
    {
      this.SendBuff[1] = fComAdr;
      this.SendBuff[2] = (byte) 7;
      this.SendBuff[3] = ENum;
      if (ENum == byte.MaxValue)
      {
        this.SendBuff[4] = Mem;
        this.SendBuff[5] = WordPtr;
        this.SendBuff[6] = Num;
        Array.Copy((Array) Password, 0, (Array) this.SendBuff, 7, 4);
        this.SendBuff[11] = MaskMem;
        this.SendBuff[12] = MaskAdr[0];
        this.SendBuff[13] = MaskAdr[1];
        this.SendBuff[14] = MaskLen;
        int length = (int) MaskLen % 8 != 0 ? (int) MaskLen / 8 + 1 : (int) MaskLen / 8;
        Array.Copy((Array) MaskData, 0, (Array) this.SendBuff, 15, length);
        this.SendBuff[0] = Convert.ToByte(16 + length);
      }
      else
      {
        if (ENum < (byte) 0 || ENum >= (byte) 32)
          return (int) byte.MaxValue;
        Array.Copy((Array) EPC, 0, (Array) this.SendBuff, 4, (int) ENum * 2);
        this.SendBuff[(int) ENum * 2 + 4] = Mem;
        this.SendBuff[(int) ENum * 2 + 5] = WordPtr;
        this.SendBuff[(int) ENum * 2 + 6] = Num;
        Array.Copy((Array) Password, 0, (Array) this.SendBuff, (int) ENum * 2 + 7, 4);
        this.SendBuff[0] = Convert.ToByte((int) ENum * 2 + 12);
      }
      this.GetCRC(this.SendBuff, (int) this.SendBuff[0] - 1);
      this.SendDataToPort(this.SendBuff, (int) this.SendBuff[0] + 1);
      int dataFromPort = this.GetDataFromPort(7, 1500);
      if (dataFromPort != 0)
        return dataFromPort;
      if (this.CheckCRC(this.RecvBuff, this.RecvLength) != 0)
        return 49;
      if (this.RecvBuff[2] != (byte) 7)
        return (int) this.RecvBuff[3];
      if (this.RecvBuff[3] == (byte) 0)
      {
        fComAdr = this.RecvBuff[1];
        errorcode = 0;
      }
      else if (this.RecvBuff[3] == (byte) 252)
        errorcode = (int) this.RecvBuff[4];
      return (int) this.RecvBuff[3];
    }

    public int SetPrivacyByEPC_G2(
      ref byte fComAdr,
      byte[] EPC,
      byte ENum,
      byte[] Password,
      byte MaskMem,
      byte[] MaskAdr,
      byte MaskLen,
      byte[] MaskData,
      ref int errorcode)
    {
      this.SendBuff[1] = fComAdr;
      this.SendBuff[2] = (byte) 8;
      this.SendBuff[3] = ENum;
      if (ENum == byte.MaxValue)
      {
        Array.Copy((Array) Password, 0, (Array) this.SendBuff, 4, 4);
        this.SendBuff[8] = MaskMem;
        this.SendBuff[9] = MaskAdr[0];
        this.SendBuff[10] = MaskAdr[1];
        this.SendBuff[11] = MaskLen;
        int length = (int) MaskLen % 8 != 0 ? (int) MaskLen / 8 + 1 : (int) MaskLen / 8;
        Array.Copy((Array) MaskData, 0, (Array) this.SendBuff, 12, length);
        this.SendBuff[0] = Convert.ToByte(13 + length);
      }
      else
      {
        if (ENum < (byte) 0 || ENum >= (byte) 32)
          return (int) byte.MaxValue;
        Array.Copy((Array) EPC, 0, (Array) this.SendBuff, 4, (int) ENum * 2);
        Array.Copy((Array) Password, 0, (Array) this.SendBuff, (int) ENum * 2 + 4, 4);
        this.SendBuff[0] = Convert.ToByte((int) ENum * 2 + 9);
      }
      this.GetCRC(this.SendBuff, (int) this.SendBuff[0] - 1);
      this.SendDataToPort(this.SendBuff, (int) this.SendBuff[0] + 1);
      int dataFromPort = this.GetDataFromPort(8, 1500);
      if (dataFromPort != 0)
        return dataFromPort;
      if (this.CheckCRC(this.RecvBuff, this.RecvLength) != 0)
        return 49;
      if (this.RecvBuff[2] != (byte) 8)
        return (int) this.RecvBuff[3];
      if (this.RecvBuff[3] == (byte) 0)
      {
        fComAdr = this.RecvBuff[1];
        errorcode = 0;
      }
      else if (this.RecvBuff[3] == (byte) 252)
        errorcode = (int) this.RecvBuff[4];
      return (int) this.RecvBuff[3];
    }

    public int SetPrivacyWithoutEPC_G2(ref byte fComAdr, byte[] Password, ref int errorcode)
    {
      this.SendBuff[0] = (byte) 8;
      this.SendBuff[1] = fComAdr;
      this.SendBuff[2] = (byte) 9;
      Array.Copy((Array) Password, 0, (Array) this.SendBuff, 3, 4);
      this.GetCRC(this.SendBuff, (int) this.SendBuff[0] - 1);
      this.SendDataToPort(this.SendBuff, (int) this.SendBuff[0] + 1);
      int dataFromPort = this.GetDataFromPort(9, 1500);
      if (dataFromPort != 0)
        return dataFromPort;
      if (this.CheckCRC(this.RecvBuff, this.RecvLength) != 0)
        return 49;
      if (this.RecvBuff[2] != (byte) 9)
        return (int) this.RecvBuff[3];
      if (this.RecvBuff[3] == (byte) 0)
      {
        fComAdr = this.RecvBuff[1];
        errorcode = 0;
      }
      else if (this.RecvBuff[3] == (byte) 252)
        errorcode = (int) this.RecvBuff[4];
      return (int) this.RecvBuff[3];
    }

    public int ResetPrivacy_G2(ref byte fComAdr, byte[] Password, ref int errorcode)
    {
      this.SendBuff[0] = (byte) 8;
      this.SendBuff[1] = fComAdr;
      this.SendBuff[2] = (byte) 10;
      Array.Copy((Array) Password, 0, (Array) this.SendBuff, 3, 4);
      this.GetCRC(this.SendBuff, (int) this.SendBuff[0] - 1);
      this.SendDataToPort(this.SendBuff, (int) this.SendBuff[0] + 1);
      int dataFromPort = this.GetDataFromPort(10, 1500);
      if (dataFromPort != 0)
        return dataFromPort;
      if (this.CheckCRC(this.RecvBuff, this.RecvLength) != 0)
        return 49;
      if (this.RecvBuff[2] != (byte) 10)
        return (int) this.RecvBuff[3];
      if (this.RecvBuff[3] == (byte) 0)
      {
        fComAdr = this.RecvBuff[1];
        errorcode = 0;
      }
      else if (this.RecvBuff[3] == (byte) 252)
        errorcode = (int) this.RecvBuff[4];
      return (int) this.RecvBuff[3];
    }

    public int CheckPrivacy_G2(ref byte fComAdr, ref byte readpro, ref int errorcode)
    {
      this.SendBuff[0] = (byte) 4;
      this.SendBuff[1] = fComAdr;
      this.SendBuff[2] = (byte) 11;
      this.GetCRC(this.SendBuff, (int) this.SendBuff[0] - 1);
      this.SendDataToPort(this.SendBuff, (int) this.SendBuff[0] + 1);
      int dataFromPort = this.GetDataFromPort(11, 1500);
      if (dataFromPort != 0)
        return dataFromPort;
      if (this.CheckCRC(this.RecvBuff, this.RecvLength) != 0)
        return 49;
      if (this.RecvBuff[2] != (byte) 11)
        return (int) this.RecvBuff[3];
      if (this.RecvBuff[3] == (byte) 0)
      {
        fComAdr = this.RecvBuff[1];
        errorcode = 0;
        readpro = this.RecvBuff[4];
      }
      else if (this.RecvBuff[3] == (byte) 252)
        errorcode = (int) this.RecvBuff[4];
      return (int) this.RecvBuff[3];
    }

    public int EASConfigure_G2(
      ref byte fComAdr,
      byte[] EPC,
      byte ENum,
      byte[] Password,
      byte EAS,
      byte MaskMem,
      byte[] MaskAdr,
      byte MaskLen,
      byte[] MaskData,
      ref int errorcode)
    {
      this.SendBuff[1] = fComAdr;
      this.SendBuff[2] = (byte) 12;
      this.SendBuff[3] = ENum;
      if (ENum == byte.MaxValue)
      {
        Array.Copy((Array) Password, 0, (Array) this.SendBuff, 4, 4);
        this.SendBuff[8] = EAS;
        this.SendBuff[9] = MaskMem;
        this.SendBuff[10] = MaskAdr[0];
        this.SendBuff[11] = MaskAdr[1];
        this.SendBuff[12] = MaskLen;
        int length = (int) MaskLen % 8 != 0 ? (int) MaskLen / 8 + 1 : (int) MaskLen / 8;
        Array.Copy((Array) MaskData, 0, (Array) this.SendBuff, 13, length);
        this.SendBuff[0] = Convert.ToByte(14 + length);
      }
      else
      {
        if (ENum < (byte) 0 || ENum >= (byte) 32)
          return (int) byte.MaxValue;
        Array.Copy((Array) EPC, 0, (Array) this.SendBuff, 4, (int) ENum * 2);
        Array.Copy((Array) Password, 0, (Array) this.SendBuff, (int) ENum * 2 + 4, 4);
        this.SendBuff[(int) ENum * 2 + 8] = EAS;
        this.SendBuff[0] = Convert.ToByte((int) ENum * 2 + 10);
      }
      this.GetCRC(this.SendBuff, (int) this.SendBuff[0] - 1);
      this.SendDataToPort(this.SendBuff, (int) this.SendBuff[0] + 1);
      int dataFromPort = this.GetDataFromPort(12, 1500);
      if (dataFromPort != 0)
        return dataFromPort;
      if (this.CheckCRC(this.RecvBuff, this.RecvLength) != 0)
        return 49;
      if (this.RecvBuff[2] != (byte) 12)
        return (int) this.RecvBuff[3];
      if (this.RecvBuff[3] == (byte) 0)
      {
        fComAdr = this.RecvBuff[1];
        errorcode = 0;
      }
      else if (this.RecvBuff[3] == (byte) 252)
        errorcode = (int) this.RecvBuff[4];
      return (int) this.RecvBuff[3];
    }

    public int EASAlarm_G2(ref byte fComAdr, ref int errorcode)
    {
      this.SendBuff[0] = (byte) 4;
      this.SendBuff[1] = fComAdr;
      this.SendBuff[2] = (byte) 13;
      this.GetCRC(this.SendBuff, (int) this.SendBuff[0] - 1);
      this.SendDataToPort(this.SendBuff, (int) this.SendBuff[0] + 1);
      int dataFromPort = this.GetDataFromPort(13, 1500);
      if (dataFromPort != 0)
        return dataFromPort;
      if (this.CheckCRC(this.RecvBuff, this.RecvLength) != 0)
        return 49;
      if (this.RecvBuff[2] != (byte) 13)
        return (int) this.RecvBuff[3];
      if (this.RecvBuff[3] == (byte) 0)
      {
        fComAdr = this.RecvBuff[1];
        errorcode = 0;
      }
      else if (this.RecvBuff[3] == (byte) 252)
        errorcode = (int) this.RecvBuff[4];
      return (int) this.RecvBuff[3];
    }

    public int BlockLock_G2(
      ref byte fComAdr,
      byte[] EPC,
      byte ENum,
      byte Mem,
      byte[] Password,
      byte WrdPointer,
      byte MaskMem,
      byte[] MaskAdr,
      byte MaskLen,
      byte[] MaskData,
      ref int errorcode)
    {
      return 0;
    }

    public int BlockWrite_G2(
      ref byte fComAdr,
      byte[] EPC,
      byte WNum,
      byte ENum,
      byte Mem,
      byte WordPtr,
      byte[] Wdt,
      byte[] Password,
      byte MaskMem,
      byte[] MaskAdr,
      byte MaskLen,
      byte[] MaskData,
      ref int errorcode)
    {
      this.SendBuff[1] = fComAdr;
      this.SendBuff[2] = (byte) 16;
      this.SendBuff[3] = WNum;
      this.SendBuff[4] = ENum;
      if (ENum == byte.MaxValue)
      {
        this.SendBuff[5] = Mem;
        this.SendBuff[6] = WordPtr;
        Array.Copy((Array) Wdt, 0, (Array) this.SendBuff, 7, (int) WNum * 2);
        Array.Copy((Array) Password, 0, (Array) this.SendBuff, 7 + (int) WNum * 2, 4);
        this.SendBuff[11 + (int) WNum * 2] = MaskMem;
        this.SendBuff[12 + (int) WNum * 2] = MaskAdr[0];
        this.SendBuff[13 + (int) WNum * 2] = MaskAdr[1];
        this.SendBuff[14 + (int) WNum * 2] = MaskLen;
        int length = (int) MaskLen % 8 != 0 ? (int) MaskLen / 8 + 1 : (int) MaskLen / 8;
        Array.Copy((Array) MaskData, 0, (Array) this.SendBuff, 15 + (int) WNum * 2, length);
        this.SendBuff[0] = Convert.ToByte(16 + (int) WNum * 2 + length);
      }
      else
      {
        if (ENum < (byte) 0 || ENum >= (byte) 32)
          return (int) byte.MaxValue;
        Array.Copy((Array) EPC, 0, (Array) this.SendBuff, 5, (int) ENum * 2);
        this.SendBuff[(int) ENum * 2 + 5] = Mem;
        this.SendBuff[(int) ENum * 2 + 6] = WordPtr;
        Array.Copy((Array) Wdt, 0, (Array) this.SendBuff, (int) ENum * 2 + 7, (int) WNum * 2);
        Array.Copy((Array) Password, 0, (Array) this.SendBuff, (int) WNum * 2 + (int) ENum * 2 + 7, 4);
        this.SendBuff[0] = Convert.ToByte((int) ENum * 2 + (int) WNum * 2 + 12);
      }
      this.GetCRC(this.SendBuff, (int) this.SendBuff[0] - 1);
      this.SendDataToPort(this.SendBuff, (int) this.SendBuff[0] + 1);
      int dataFromPort = this.GetDataFromPort(16, 3000);
      if (dataFromPort != 0)
        return dataFromPort;
      if (this.CheckCRC(this.RecvBuff, this.RecvLength) != 0)
        return 49;
      if (this.RecvBuff[2] != (byte) 16)
        return (int) this.RecvBuff[3];
      if (this.RecvBuff[3] == (byte) 0)
      {
        fComAdr = this.RecvBuff[1];
        errorcode = 0;
      }
      else if (this.RecvBuff[3] == (byte) 252)
        errorcode = (int) this.RecvBuff[4];
      return (int) this.RecvBuff[3];
    }

    public int InventorySingle_6B(ref byte ConAddr, ref byte ant, byte[] ID_6B)
    {
      this.SendBuff[0] = (byte) 4;
      this.SendBuff[1] = ConAddr;
      this.SendBuff[2] = (byte) 80;
      this.GetCRC(this.SendBuff, (int) this.SendBuff[0] - 1);
      this.SendDataToPort(this.SendBuff, (int) this.SendBuff[0] + 1);
      int dataFromPort = this.GetDataFromPort(80, 3000);
      if (dataFromPort != 0)
        return dataFromPort;
      if (this.CheckCRC(this.RecvBuff, this.RecvLength) != 0)
        return 49;
      if (this.RecvBuff[2] != (byte) 80 || this.RecvBuff[3] != (byte) 0)
        return (int) this.RecvBuff[3];
      ant = this.RecvBuff[4];
      Array.Copy((Array) this.RecvBuff, 5, (Array) ID_6B, 0, 10);
      ConAddr = this.RecvBuff[1];
      return (int) this.RecvBuff[3];
    }

    public int InventoryMultiple_6B(
      ref byte ConAddr,
      byte Condition,
      byte StartAddress,
      byte mask,
      byte[] ConditionContent,
      ref byte ant,
      byte[] ID_6B,
      ref int Cardnum)
    {
      this.SendBuff[0] = (byte) 15;
      this.SendBuff[1] = ConAddr;
      this.SendBuff[2] = (byte) 81;
      this.SendBuff[3] = Condition;
      this.SendBuff[4] = StartAddress;
      this.SendBuff[5] = mask;
      Array.Copy((Array) ConditionContent, 0, (Array) this.SendBuff, 6, 8);
      this.GetCRC(this.SendBuff, (int) this.SendBuff[0] - 1);
      this.SendDataToPort(this.SendBuff, (int) this.SendBuff[0] + 1);
      int dataFromPort = this.GetDataFromPort(81, 3000);
      if (dataFromPort != 0)
        return dataFromPort;
      if (this.CheckCRC(this.RecvBuff, this.RecvLength) != 0)
        return 49;
      if (this.RecvBuff[2] != (byte) 81 || this.RecvBuff[3] != (byte) 21 && this.RecvBuff[3] != (byte) 22 && this.RecvBuff[3] != (byte) 23 && this.RecvBuff[3] != (byte) 24)
        return (int) this.RecvBuff[3];
      ant = this.RecvBuff[4];
      Cardnum = (int) this.RecvBuff[5];
      Array.Copy((Array) this.RecvBuff, 6, (Array) ID_6B, 0, this.RecvLength - 8);
      ConAddr = this.RecvBuff[1];
      return (int) this.RecvBuff[3];
    }

    public int ReadData_6B(
      ref byte ConAddr,
      byte[] ID_6B,
      byte StartAddress,
      byte Num,
      byte[] Data,
      ref int errorcode)
    {
      this.SendBuff[0] = (byte) 14;
      this.SendBuff[1] = ConAddr;
      this.SendBuff[2] = (byte) 82;
      this.SendBuff[3] = StartAddress;
      Array.Copy((Array) ID_6B, 0, (Array) this.SendBuff, 4, 8);
      this.SendBuff[12] = Num;
      this.GetCRC(this.SendBuff, (int) this.SendBuff[0] - 1);
      this.SendDataToPort(this.SendBuff, (int) this.SendBuff[0] + 1);
      int dataFromPort = this.GetDataFromPort(82, 3000);
      if (dataFromPort != 0)
        return dataFromPort;
      if (this.CheckCRC(this.RecvBuff, this.RecvLength) != 0)
        return 49;
      if (this.RecvBuff[2] != (byte) 82)
        return (int) this.RecvBuff[3];
      if (this.RecvBuff[3] == (byte) 0)
      {
        Array.Copy((Array) this.RecvBuff, 4, (Array) Data, 0, this.RecvLength - 6);
        ConAddr = this.RecvBuff[1];
        errorcode = 0;
      }
      else if (this.RecvBuff[3] == (byte) 252)
        errorcode = (int) this.RecvBuff[4];
      return (int) this.RecvBuff[3];
    }

    public int WriteData_6B(
      ref byte ConAddr,
      byte[] ID_6B,
      byte StartAddress,
      byte[] Writedata,
      byte Writedatalen,
      ref int writtenbyte,
      ref int errorcode)
    {
      this.SendBuff[0] = Convert.ToByte(13 + (int) Writedatalen);
      this.SendBuff[1] = ConAddr;
      this.SendBuff[2] = (byte) 83;
      this.SendBuff[3] = StartAddress;
      Array.Copy((Array) ID_6B, 0, (Array) this.SendBuff, 4, 8);
      Array.Copy((Array) Writedata, 0, (Array) this.SendBuff, 12, (int) Writedatalen);
      this.GetCRC(this.SendBuff, (int) this.SendBuff[0] - 1);
      this.SendDataToPort(this.SendBuff, (int) this.SendBuff[0] + 1);
      int dataFromPort = this.GetDataFromPort(83, 3000);
      if (dataFromPort != 0)
        return dataFromPort;
      if (this.CheckCRC(this.RecvBuff, this.RecvLength) != 0)
        return 49;
      if (this.RecvBuff[2] != (byte) 83)
        return (int) this.RecvBuff[3];
      if (this.RecvBuff[3] == (byte) 0)
        errorcode = 0;
      else if (this.RecvBuff[3] == (byte) 252)
        errorcode = (int) this.RecvBuff[4];
      return (int) this.RecvBuff[3];
    }

    public int Lock_6B(ref byte ConAddr, byte[] ID_6B, byte Address, ref int errorcode)
    {
      this.SendBuff[0] = (byte) 13;
      this.SendBuff[1] = ConAddr;
      this.SendBuff[2] = (byte) 85;
      this.SendBuff[3] = Address;
      Array.Copy((Array) ID_6B, 0, (Array) this.SendBuff, 4, 8);
      this.GetCRC(this.SendBuff, (int) this.SendBuff[0] - 1);
      this.SendDataToPort(this.SendBuff, (int) this.SendBuff[0] + 1);
      int dataFromPort = this.GetDataFromPort(85, 3000);
      if (dataFromPort != 0)
        return dataFromPort;
      if (this.CheckCRC(this.RecvBuff, this.RecvLength) != 0)
        return 49;
      if (this.RecvBuff[2] != (byte) 85)
        return (int) this.RecvBuff[3];
      if (this.RecvBuff[3] == (byte) 0)
        errorcode = 0;
      else if (this.RecvBuff[3] == (byte) 252)
        errorcode = (int) this.RecvBuff[4];
      return (int) this.RecvBuff[3];
    }

    public int CheckLock_6B(
      ref byte ConAddr,
      byte[] ID_6B,
      byte Address,
      ref byte ReLockState,
      ref int errorcode)
    {
      this.SendBuff[0] = (byte) 13;
      this.SendBuff[1] = ConAddr;
      this.SendBuff[2] = (byte) 84;
      this.SendBuff[3] = Address;
      Array.Copy((Array) ID_6B, 0, (Array) this.SendBuff, 4, 8);
      this.GetCRC(this.SendBuff, (int) this.SendBuff[0] - 1);
      this.SendDataToPort(this.SendBuff, (int) this.SendBuff[0] + 1);
      int dataFromPort = this.GetDataFromPort(84, 1500);
      if (dataFromPort != 0)
        return dataFromPort;
      if (this.CheckCRC(this.RecvBuff, this.RecvLength) != 0)
        return 49;
      if (this.RecvBuff[2] != (byte) 84)
        return (int) this.RecvBuff[3];
      if (this.RecvBuff[3] == (byte) 0)
      {
        ReLockState = this.RecvBuff[4];
        errorcode = 0;
      }
      else if (this.RecvBuff[3] == (byte) 252)
        errorcode = (int) this.RecvBuff[4];
      return (int) this.RecvBuff[3];
    }

    public int InventoryBuffer_G2(
      ref byte ComAdr,
      byte QValue,
      byte Session,
      byte MaskMem,
      byte[] MaskAdr,
      byte MaskLen,
      byte[] MaskData,
      byte MaskFlag,
      byte AdrTID,
      byte LenTID,
      byte TIDFlag,
      byte Target,
      byte InAnt,
      byte Scantime,
      byte FastFlag,
      ref int BufferCount,
      ref int TagNum)
    {
      this.SendBuff[1] = ComAdr;
      this.SendBuff[2] = (byte) 24;
      this.SendBuff[3] = QValue;
      this.SendBuff[4] = Session;
      if (MaskFlag == (byte) 1)
      {
        this.SendBuff[5] = MaskMem;
        Array.Copy((Array) MaskAdr, 0, (Array) this.SendBuff, 6, 2);
        this.SendBuff[8] = MaskLen;
        int length = (int) MaskLen % 8 != 0 ? (int) MaskLen / 8 + 1 : (int) MaskLen / 8;
        Array.Copy((Array) MaskData, 0, (Array) this.SendBuff, 9, length);
        if (TIDFlag == (byte) 1)
        {
          if (FastFlag == (byte) 1)
          {
            this.SendBuff[length + 9] = AdrTID;
            this.SendBuff[length + 10] = LenTID;
            this.SendBuff[length + 11] = Target;
            this.SendBuff[length + 12] = InAnt;
            this.SendBuff[length + 13] = Scantime;
            this.SendBuff[0] = Convert.ToByte(15 + length);
          }
          else
          {
            this.SendBuff[length + 9] = AdrTID;
            this.SendBuff[length + 10] = LenTID;
            this.SendBuff[0] = Convert.ToByte(12 + length);
          }
        }
        else if (FastFlag == (byte) 1)
        {
          this.SendBuff[length + 9] = Target;
          this.SendBuff[length + 10] = InAnt;
          this.SendBuff[length + 11] = Scantime;
          this.SendBuff[0] = Convert.ToByte(13 + length);
        }
        else
          this.SendBuff[0] = Convert.ToByte(10 + length);
      }
      else if (TIDFlag == (byte) 1)
      {
        if (FastFlag == (byte) 1)
        {
          this.SendBuff[5] = AdrTID;
          this.SendBuff[6] = LenTID;
          this.SendBuff[7] = Target;
          this.SendBuff[8] = InAnt;
          this.SendBuff[9] = Scantime;
          this.SendBuff[0] = (byte) 11;
        }
        else
        {
          this.SendBuff[5] = AdrTID;
          this.SendBuff[6] = LenTID;
          this.SendBuff[0] = (byte) 8;
        }
      }
      else if (FastFlag == (byte) 1)
      {
        this.SendBuff[5] = Target;
        this.SendBuff[6] = InAnt;
        this.SendBuff[7] = Scantime;
        this.SendBuff[0] = (byte) 9;
      }
      else
        this.SendBuff[0] = (byte) 6;
      this.GetCRC(this.SendBuff, (int) this.SendBuff[0] - 1);
      this.SendDataToPort(this.SendBuff, (int) this.SendBuff[0] + 1);
      int dataFromPort = this.GetDataFromPort(24, (int) Scantime * 2 * 100 + 3000);
      if (dataFromPort != 0)
        return dataFromPort;
      if (this.CheckCRC(this.RecvBuff, this.RecvLength) != 0)
        return 49;
      if (this.RecvBuff[2] != (byte) 24 || this.RecvBuff[3] != (byte) 0)
        return (int) this.RecvBuff[3];
      BufferCount = (int) this.RecvBuff[4] * 256 + (int) this.RecvBuff[5];
      TagNum = (int) this.RecvBuff[6] * 256 + (int) this.RecvBuff[7];
      return (int) this.RecvBuff[3];
    }

    public int SetSaveLen(ref byte fComAdr, byte SaveLen)
    {
      this.SendBuff[0] = (byte) 5;
      this.SendBuff[1] = fComAdr;
      this.SendBuff[2] = (byte) 112;
      this.SendBuff[3] = SaveLen;
      this.GetCRC(this.SendBuff, 4);
      this.SendDataToPort(this.SendBuff, 6);
      int dataFromPort = this.GetDataFromPort(112, 1500);
      if (dataFromPort != 0)
        return dataFromPort;
      if (this.CheckCRC(this.RecvBuff, this.RecvLength) != 0)
        return 49;
      if (this.RecvBuff[2] != (byte) 112)
        return (int) this.RecvBuff[3];
      fComAdr = this.RecvBuff[1];
      return (int) this.RecvBuff[3];
    }

    public int GetSaveLen(ref byte fComAdr, ref byte SaveLen)
    {
      this.SendBuff[0] = (byte) 4;
      this.SendBuff[1] = fComAdr;
      this.SendBuff[2] = (byte) 113;
      this.GetCRC(this.SendBuff, 3);
      this.SendDataToPort(this.SendBuff, 5);
      int dataFromPort = this.GetDataFromPort(113, 1500);
      if (dataFromPort != 0)
        return dataFromPort;
      if (this.CheckCRC(this.RecvBuff, this.RecvLength) != 0)
        return 49;
      if (this.RecvBuff[2] != (byte) 113)
        return (int) this.RecvBuff[3];
      fComAdr = this.RecvBuff[1];
      SaveLen = this.RecvBuff[4];
      return (int) this.RecvBuff[3];
    }

    public int ClearBuffer_G2(ref byte fComAdr)
    {
      this.SendBuff[0] = (byte) 4;
      this.SendBuff[1] = fComAdr;
      this.SendBuff[2] = (byte) 115;
      this.GetCRC(this.SendBuff, 3);
      this.SendDataToPort(this.SendBuff, 5);
      int dataFromPort = this.GetDataFromPort(115, 1500);
      if (dataFromPort != 0)
        return dataFromPort;
      if (this.CheckCRC(this.RecvBuff, this.RecvLength) != 0)
        return 49;
      if (this.RecvBuff[2] != (byte) 115)
        return (int) this.RecvBuff[3];
      fComAdr = this.RecvBuff[1];
      return (int) this.RecvBuff[3];
    }

    public int GetBufferCnt_G2(ref byte fComAdr, ref int Count)
    {
      this.SendBuff[0] = (byte) 4;
      this.SendBuff[1] = fComAdr;
      this.SendBuff[2] = (byte) 116;
      this.GetCRC(this.SendBuff, 3);
      this.SendDataToPort(this.SendBuff, 5);
      int dataFromPort = this.GetDataFromPort(116, 1500);
      if (dataFromPort != 0)
        return dataFromPort;
      if (this.CheckCRC(this.RecvBuff, this.RecvLength) != 0)
        return 49;
      if (this.RecvBuff[2] != (byte) 116)
        return (int) this.RecvBuff[3];
      fComAdr = this.RecvBuff[1];
      Count = (int) this.RecvBuff[4] * 256 + (int) this.RecvBuff[5];
      return (int) this.RecvBuff[3];
    }

    private int GetDatafromBuff(ref int dlen, ref int epcNum, byte[] pEPCList)
    {
      epcNum = 0;
      dlen = 0;
      byte[] numArray1 = new byte[4096];
      int num1 = 0;
      long tickCount = (long) Environment.TickCount;
      try
      {
        do
        {
          byte[] sourceArray = this.ReadDataFromPort();
          if (sourceArray != null)
          {
            tickCount = (long) Environment.TickCount;
            int length = sourceArray.Length;
            if (length != 0)
            {
              byte[] numArray2 = new byte[length + num1];
              Array.Copy((Array) numArray1, 0, (Array) numArray2, 0, num1);
              Array.Copy((Array) sourceArray, 0, (Array) numArray2, num1, length);
              int sourceIndex = 0;
              while (numArray2.Length - sourceIndex > 5)
              {
                if (numArray2[sourceIndex] >= (byte) 5 && numArray2[sourceIndex + 2] == (byte) 114)
                {
                  int num2 = (int) numArray2[sourceIndex];
                  if (numArray2.Length >= sourceIndex + num2 + 1)
                  {
                    byte[] numArray3 = new byte[num2 + 1];
                    Array.Copy((Array) numArray2, sourceIndex, (Array) numArray3, 0, numArray3.Length);
                    if (this.CheckCRC(numArray3, numArray3.Length) == 0)
                    {
                      tickCount = (long) Environment.TickCount;
                      int num3 = (int) numArray3[0] + 1;
                      sourceIndex += num3;
                      int datafromBuff = (int) numArray3[3];
                      if (datafromBuff == 1 || datafromBuff == 2 || datafromBuff == 3 || datafromBuff == 4)
                      {
                        int num4 = (int) numArray3[4];
                        if (num4 > 0)
                        {
                          epcNum += num4;
                          Array.Copy((Array) numArray3, 5, (Array) pEPCList, dlen, (int) numArray3[0] - 6);
                          dlen += (int) numArray3[0] - 6;
                        }
                      }
                      if (datafromBuff != 3)
                        return datafromBuff;
                    }
                    else
                      ++sourceIndex;
                  }
                  else
                    break;
                }
                else
                  ++sourceIndex;
              }
              if (numArray2.Length > sourceIndex)
              {
                num1 = numArray2.Length - sourceIndex;
                Array.Copy((Array) numArray2, sourceIndex, (Array) numArray1, 0, num1);
              }
              else
                num1 = 0;
            }
          }
          else
            Thread.Sleep(5);
        }
        while ((long) Environment.TickCount - tickCount < 2000L);
      }
      catch (Exception ex)
      {
        ex.ToString();
      }
      return 48;
    }

    public int ReadBuffer_G2(ref byte fComAdr, ref int Totallen, ref int CardNum, byte[] pEPCList)
    {
      this.SendBuff[0] = (byte) 4;
      this.SendBuff[1] = fComAdr;
      this.SendBuff[2] = (byte) 114;
      this.GetCRC(this.SendBuff, 3);
      this.SendDataToPort(this.SendBuff, 5);
      return this.GetDatafromBuff(ref Totallen, ref CardNum, pEPCList);
    }

    public int SetReadMode(ref byte fComAdr, byte ReadMode)
    {
      this.SendBuff[0] = (byte) 5;
      this.SendBuff[1] = fComAdr;
      this.SendBuff[2] = (byte) 118;
      this.SendBuff[3] = ReadMode;
      this.GetCRC(this.SendBuff, 4);
      this.SendDataToPort(this.SendBuff, 6);
      int dataFromPort = this.GetDataFromPort(118, 1500);
      if (dataFromPort != 0)
        return dataFromPort;
      if (this.CheckCRC(this.RecvBuff, this.RecvLength) != 0)
        return 49;
      if (this.RecvBuff[2] != (byte) 118)
        return (int) this.RecvBuff[3];
      fComAdr = this.RecvBuff[1];
      return (int) this.RecvBuff[3];
    }

    public int SetReadParameter(
      ref byte fComAdr,
      byte[] Parameter,
      byte MaskMem,
      byte[] MaskAdr,
      byte MaskLen,
      byte[] MaskData,
      byte MaskFlag,
      byte AdrTID,
      byte LenTID,
      byte TIDFlag)
    {
      this.SendBuff[1] = fComAdr;
      this.SendBuff[2] = (byte) 117;
      Array.Copy((Array) Parameter, 0, (Array) this.SendBuff, 3, 5);
      if (MaskFlag == (byte) 1)
      {
        this.SendBuff[8] = MaskMem;
        this.SendBuff[9] = MaskAdr[0];
        this.SendBuff[10] = MaskAdr[1];
        this.SendBuff[11] = MaskLen;
        int length = (int) MaskLen % 8 != 0 ? (int) MaskLen / 8 + 1 : (int) MaskLen / 8;
        Array.Copy((Array) MaskData, 0, (Array) this.SendBuff, 12, length);
        if (TIDFlag == (byte) 1)
        {
          this.SendBuff[12 + length] = AdrTID;
          this.SendBuff[13 + length] = LenTID;
          this.SendBuff[0] = Convert.ToByte(length + 15);
        }
        else
          this.SendBuff[0] = Convert.ToByte(length + 13);
      }
      else if (TIDFlag == (byte) 1)
      {
        this.SendBuff[8] = AdrTID;
        this.SendBuff[9] = LenTID;
        this.SendBuff[0] = (byte) 11;
      }
      else
        this.SendBuff[0] = (byte) 9;
      this.GetCRC(this.SendBuff, (int) this.SendBuff[0] - 1);
      this.SendDataToPort(this.SendBuff, (int) this.SendBuff[0] + 1);
      int dataFromPort = this.GetDataFromPort(117, 1500);
      if (dataFromPort != 0)
        return dataFromPort;
      if (this.CheckCRC(this.RecvBuff, this.RecvLength) != 0)
        return 49;
      if (this.RecvBuff[2] != (byte) 117)
        return (int) this.RecvBuff[3];
      fComAdr = this.RecvBuff[1];
      return (int) this.RecvBuff[3];
    }

    public int GetReadParameter(ref byte fComAdr, byte[] Parameter)
    {
      this.SendBuff[0] = (byte) 4;
      this.SendBuff[1] = fComAdr;
      this.SendBuff[2] = (byte) 119;
      this.GetCRC(this.SendBuff, 3);
      this.SendDataToPort(this.SendBuff, 5);
      int dataFromPort = this.GetDataFromPort(119, 1500);
      if (dataFromPort != 0)
        return dataFromPort;
      if (this.CheckCRC(this.RecvBuff, this.RecvLength) != 0)
        return 49;
      if (this.RecvBuff[2] != (byte) 119)
        return (int) this.RecvBuff[3];
      fComAdr = this.RecvBuff[1];
      Array.Copy((Array) this.RecvBuff, 4, (Array) Parameter, 0, this.RecvLength - 6);
      return (int) this.RecvBuff[3];
    }

    public int WriteRfPower(ref byte fComAdr, byte PowerDbm)
    {
      this.SendBuff[0] = (byte) 5;
      this.SendBuff[1] = fComAdr;
      this.SendBuff[2] = (byte) 121;
      this.SendBuff[3] = PowerDbm;
      this.GetCRC(this.SendBuff, 4);
      this.SendDataToPort(this.SendBuff, 6);
      int dataFromPort = this.GetDataFromPort(121, 1500);
      if (dataFromPort != 0)
        return dataFromPort;
      if (this.CheckCRC(this.RecvBuff, this.RecvLength) != 0)
        return 49;
      if (this.RecvBuff[2] != (byte) 121)
        return (int) this.RecvBuff[3];
      fComAdr = this.RecvBuff[1];
      return (int) this.RecvBuff[3];
    }

    public int ReadRfPower(ref byte fComAdr, ref byte PowerDbm)
    {
      this.SendBuff[0] = (byte) 4;
      this.SendBuff[1] = fComAdr;
      this.SendBuff[2] = (byte) 122;
      this.GetCRC(this.SendBuff, 3);
      this.SendDataToPort(this.SendBuff, 5);
      int dataFromPort = this.GetDataFromPort(122, 1500);
      if (dataFromPort != 0)
        return dataFromPort;
      if (this.CheckCRC(this.RecvBuff, this.RecvLength) != 0)
        return 49;
      if (this.RecvBuff[2] != (byte) 122)
        return (int) this.RecvBuff[3];
      fComAdr = this.RecvBuff[1];
      PowerDbm = this.RecvBuff[4];
      return (int) this.RecvBuff[3];
    }

    public int RetryTimes(ref byte fComAdr, ref byte Times)
    {
      this.SendBuff[0] = (byte) 5;
      this.SendBuff[1] = fComAdr;
      this.SendBuff[2] = (byte) 123;
      this.SendBuff[3] = Times;
      this.GetCRC(this.SendBuff, 4);
      this.SendDataToPort(this.SendBuff, 6);
      int dataFromPort = this.GetDataFromPort(123, 1500);
      if (dataFromPort != 0)
        return dataFromPort;
      if (this.CheckCRC(this.RecvBuff, this.RecvLength) != 0)
        return 49;
      if (this.RecvBuff[2] != (byte) 123)
        return (int) this.RecvBuff[3];
      fComAdr = this.RecvBuff[1];
      Times = this.RecvBuff[4];
      return (int) this.RecvBuff[3];
    }

    public int InventoryMix_G2(
      ref byte ComAdr,
      byte QValue,
      byte Session,
      byte MaskMem,
      byte[] MaskAdr,
      byte MaskLen,
      byte[] MaskData,
      byte MaskFlag,
      byte ReadMem,
      byte[] ReadAdr,
      byte ReadLen,
      byte[] Psd,
      byte Target,
      byte InAnt,
      byte Scantime,
      byte FastFlag,
      byte[] pEPCList,
      ref byte Ant,
      ref int Totallen,
      ref int CardNum)
    {
      if (Scantime == (byte) 0)
        Scantime = (byte) 20;
      this.SendBuff[1] = ComAdr;
      this.SendBuff[2] = (byte) 25;
      this.SendBuff[3] = QValue;
      this.SendBuff[4] = Session;
      if (MaskFlag == (byte) 1)
      {
        this.SendBuff[5] = MaskMem;
        Array.Copy((Array) MaskAdr, 0, (Array) this.SendBuff, 6, 2);
        this.SendBuff[8] = MaskLen;
        int length = (int) MaskLen % 8 != 0 ? (int) MaskLen / 8 + 1 : (int) MaskLen / 8;
        Array.Copy((Array) MaskData, 0, (Array) this.SendBuff, 9, length);
        this.SendBuff[length + 9] = ReadMem;
        this.SendBuff[length + 10] = ReadAdr[0];
        this.SendBuff[length + 11] = ReadAdr[1];
        this.SendBuff[length + 12] = ReadLen;
        Array.Copy((Array) Psd, 0, (Array) this.SendBuff, 13, 4);
        if (FastFlag == (byte) 1)
        {
          this.SendBuff[length + 17] = Target;
          this.SendBuff[length + 18] = InAnt;
          this.SendBuff[length + 19] = Scantime;
          this.SendBuff[0] = Convert.ToByte(21 + length);
        }
        else
          this.SendBuff[0] = Convert.ToByte(18 + length);
      }
      else
      {
        this.SendBuff[5] = ReadMem;
        this.SendBuff[6] = ReadAdr[0];
        this.SendBuff[7] = ReadAdr[1];
        this.SendBuff[8] = ReadLen;
        Array.Copy((Array) Psd, 0, (Array) this.SendBuff, 9, 4);
        if (FastFlag == (byte) 1)
        {
          this.SendBuff[13] = Target;
          this.SendBuff[14] = InAnt;
          this.SendBuff[15] = Scantime;
          this.SendBuff[0] = (byte) 17;
        }
        else
          this.SendBuff[0] = (byte) 14;
      }
      this.GetCRC(this.SendBuff, (int) this.SendBuff[0] - 1);
      this.SendDataToPort(this.SendBuff, (int) this.SendBuff[0] + 1);
      return this.GetInventoryMixG1((int) Scantime * 100, pEPCList, ref Ant, ref Totallen, ref CardNum);
    }

    public int Inventory_FastBID_NXP(
      ref byte ComAdr,
      byte QValue,
      byte Session,
      byte Target,
      byte InAnt,
      byte Scantime,
      byte FastFlag,
      byte[] pEPCList,
      ref byte Ant,
      ref int Totallen,
      ref int CardNum)
    {
      if (Scantime == (byte) 0)
        Scantime = (byte) 20;
      this.SendBuff[1] = ComAdr;
      this.SendBuff[2] = (byte) 150;
      this.SendBuff[3] = QValue;
      this.SendBuff[4] = Session;
      if (FastFlag == (byte) 1)
      {
        this.SendBuff[5] = Target;
        this.SendBuff[6] = InAnt;
        this.SendBuff[7] = Scantime;
        this.SendBuff[0] = (byte) 9;
      }
      else
        this.SendBuff[0] = (byte) 6;
      this.GetCRC(this.SendBuff, (int) this.SendBuff[0] - 1);
      this.SendDataToPort(this.SendBuff, (int) this.SendBuff[0] + 1);
      return this.GetInventoryG1((int) Scantime * 100, pEPCList, ref Ant, ref Totallen, ref CardNum, 150);
    }

    private int GetInventoryMixG1(
      int Scantime,
      byte[] pEPCList,
      ref byte outAnt,
      ref int dlen,
      ref int epcNum)
    {
      epcNum = 0;
      dlen = 0;
      byte[] numArray1 = new byte[4096];
      int num1 = 0;
      long tickCount = (long) Environment.TickCount;
      try
      {
        do
        {
          byte[] sourceArray = this.ReadDataFromPort();
          if (sourceArray != null)
          {
            int length1 = sourceArray.Length;
            if (length1 != 0)
            {
              byte[] numArray2 = new byte[length1 + num1];
              Array.Copy((Array) numArray1, 0, (Array) numArray2, 0, num1);
              Array.Copy((Array) sourceArray, 0, (Array) numArray2, num1, length1);
              int sourceIndex1 = 0;
              while (numArray2.Length - sourceIndex1 > 5)
              {
                if (numArray2[sourceIndex1] >= (byte) 5 && numArray2[sourceIndex1 + 2] == (byte) 25)
                {
                  int num2 = (int) numArray2[sourceIndex1];
                  if (numArray2.Length >= sourceIndex1 + num2 + 1)
                  {
                    byte[] numArray3 = new byte[num2 + 1];
                    Array.Copy((Array) numArray2, sourceIndex1, (Array) numArray3, 0, numArray3.Length);
                    if (this.CheckCRC(numArray3, numArray3.Length) == 0)
                    {
                      tickCount = (long) Environment.TickCount;
                      int num3 = (int) numArray3[0] + 1;
                      sourceIndex1 += num3;
                      int inventoryMixG1 = (int) numArray3[3];
                      if (inventoryMixG1 == 1 || inventoryMixG1 == 2 || inventoryMixG1 == 3 || inventoryMixG1 == 4)
                      {
                        int num4 = (int) numArray3[5];
                        if (num4 > 0)
                        {
                          int sourceIndex2 = 6;
                          for (int index = 0; index < num4; ++index)
                          {
                            int length2 = (int) numArray3[sourceIndex2 + 1] & 63;
                            bool flag = false;
                            int num5 = 0;
                            int num6 = 0;
                            int num7 = 0;
                            if (((int) numArray3[sourceIndex2 + 1] & 64) > 0)
                              flag = true;
                            if (!flag)
                            {
                              Array.Copy((Array) numArray3, sourceIndex2, (Array) pEPCList, dlen, length2 + 3);
                              dlen += length2 + 3;
                            }
                            else
                            {
                              Array.Copy((Array) numArray3, sourceIndex2, (Array) pEPCList, dlen, length2 + 7);
                              dlen += length2 + 7;
                              num5 = (int) numArray3[sourceIndex2 + length2 + 3] * 256 + (int) numArray3[sourceIndex2 + length2 + 4];
                              num6 = (int) numArray3[sourceIndex2 + length2 + 5] * 256 + (int) numArray3[sourceIndex2 + length2 + 6];
                              num7 = ((int) numArray3[sourceIndex2 + length2 + 7] << 16) + ((int) numArray3[sourceIndex2 + length2 + 8] << 8) + (int) numArray3[sourceIndex2 + length2 + 9];
                            }
                            ++epcNum;
                            outAnt = numArray3[4];
                            if (this.ReceiveCallback != null)
                            {
                              RFIDTag mtag = new RFIDTag();
                              mtag.DeviceName = this.DevName;
                              mtag.ANT = numArray3[4];
                              mtag.LEN = numArray3[sourceIndex2 + 1];
                              mtag.PacketParam = numArray3[sourceIndex2];
                              mtag.phase_begin = num5;
                              mtag.phase_end = num6;
                              mtag.RSSI = numArray3[sourceIndex2 + 2 + length2];
                              mtag.Freqkhz = num7;
                              byte[] numArray4 = new byte[length2];
                              Array.Copy((Array) numArray3, sourceIndex2 + 2, (Array) numArray4, 0, numArray4.Length);
                              mtag.UID = this.ByteArrayToHexString(numArray4);
                              this.ReceiveCallback(mtag);
                            }
                            sourceIndex2 = flag ? sourceIndex2 + 10 + length2 : sourceIndex2 + 3 + length2;
                          }
                        }
                      }
                      if (inventoryMixG1 != 3)
                        return inventoryMixG1;
                    }
                    else
                      ++sourceIndex1;
                  }
                  else
                    break;
                }
                else
                  ++sourceIndex1;
              }
              if (numArray2.Length > sourceIndex1)
              {
                num1 = numArray2.Length - sourceIndex1;
                Array.Copy((Array) numArray2, sourceIndex1, (Array) numArray1, 0, num1);
              }
              else
                num1 = 0;
            }
          }
          else
            Thread.Sleep(5);
        }
        while ((long) Environment.TickCount - tickCount < (long) (Scantime * 2 + 2000));
      }
      catch (Exception ex)
      {
        ex.ToString();
      }
      return 48;
    }

    public int SetDRM(ref byte fComAdr, byte DRMMode)
    {
      this.SendBuff[0] = (byte) 5;
      this.SendBuff[1] = fComAdr;
      this.SendBuff[2] = (byte) 144;
      this.SendBuff[3] = (byte) ((uint) DRMMode | 128U);
      this.GetCRC(this.SendBuff, 4);
      this.SendDataToPort(this.SendBuff, 6);
      int dataFromPort = this.GetDataFromPort(144, 1500);
      if (dataFromPort != 0)
        return dataFromPort;
      if (this.CheckCRC(this.RecvBuff, this.RecvLength) != 0)
        return 49;
      if (this.RecvBuff[2] != (byte) 144)
        return (int) this.RecvBuff[3];
      fComAdr = this.RecvBuff[1];
      return (int) this.RecvBuff[3];
    }

    public int GetDRM(ref byte fComAdr, ref byte DRMMode)
    {
      this.SendBuff[0] = (byte) 5;
      this.SendBuff[1] = fComAdr;
      this.SendBuff[2] = (byte) 144;
      this.SendBuff[3] = (byte) 0;
      this.GetCRC(this.SendBuff, 4);
      this.SendDataToPort(this.SendBuff, 6);
      int dataFromPort = this.GetDataFromPort(144, 1500);
      if (dataFromPort != 0)
        return dataFromPort;
      if (this.CheckCRC(this.RecvBuff, this.RecvLength) != 0)
        return 49;
      if (this.RecvBuff[2] != (byte) 144)
        return (int) this.RecvBuff[3];
      fComAdr = this.RecvBuff[1];
      DRMMode = this.RecvBuff[4];
      return (int) this.RecvBuff[3];
    }

    public int GetReaderTemperature(ref byte fComAdr, ref byte PlusMinus, ref byte Temperature)
    {
      this.SendBuff[0] = (byte) 4;
      this.SendBuff[1] = fComAdr;
      this.SendBuff[2] = (byte) 146;
      this.GetCRC(this.SendBuff, 3);
      this.SendDataToPort(this.SendBuff, 5);
      int dataFromPort = this.GetDataFromPort(146, 1500);
      if (dataFromPort != 0)
        return dataFromPort;
      if (this.CheckCRC(this.RecvBuff, this.RecvLength) != 0)
        return 49;
      if (this.RecvBuff[2] != (byte) 146)
        return (int) this.RecvBuff[3];
      fComAdr = this.RecvBuff[1];
      PlusMinus = this.RecvBuff[4];
      Temperature = this.RecvBuff[5];
      return (int) this.RecvBuff[3];
    }

    public int MeasureReturnLoss(
      ref byte fComAdr,
      byte[] TestFreq,
      byte Ant,
      byte dalayTime,
      ref byte ReturnLoss,
      ref int ADC)
    {
      this.SendBuff[0] = (byte) 10;
      this.SendBuff[1] = fComAdr;
      this.SendBuff[2] = (byte) 145;
      Array.Copy((Array) TestFreq, 0, (Array) this.SendBuff, 3, 4);
      this.SendBuff[7] = Ant;
      this.SendBuff[8] = dalayTime;
      this.GetCRC(this.SendBuff, 9);
      this.SendDataToPort(this.SendBuff, 11);
      int dataFromPort = this.GetDataFromPort(145, 1500);
      if (dataFromPort != 0)
        return dataFromPort;
      if (this.CheckCRC(this.RecvBuff, this.RecvLength) != 0)
        return 49;
      if (this.RecvBuff[2] != (byte) 145)
        return (int) this.RecvBuff[3];
      fComAdr = this.RecvBuff[1];
      ReturnLoss = this.RecvBuff[4];
      ADC = (int) this.RecvBuff[5] * 256 + (int) this.RecvBuff[6];
      return (int) this.RecvBuff[3];
    }

    public int SelectCmdWithCarrier(
      ref byte ComAdr,
      byte Antenna,
      byte Session,
      byte SelAction,
      byte MaskMem,
      byte[] MaskAdr,
      byte MaskLen,
      byte[] MaskData,
      byte Truncate,
      byte CarrierTime)
    {
      byte length = (byte) (((int) MaskLen + 7) / 8);
      this.SendBuff[0] = (byte) (13U + (uint) length);
      this.SendBuff[1] = ComAdr;
      this.SendBuff[2] = (byte) 152;
      this.SendBuff[3] = Antenna;
      this.SendBuff[4] = Session;
      this.SendBuff[5] = SelAction;
      this.SendBuff[6] = MaskMem;
      this.SendBuff[7] = MaskAdr[0];
      this.SendBuff[8] = MaskAdr[1];
      this.SendBuff[9] = MaskLen;
      if (length > (byte) 0)
      {
        Array.Copy((Array) MaskData, 0, (Array) this.SendBuff, 10, (int) length);
        this.SendBuff[10 + (int) length] = Truncate;
        this.SendBuff[11 + (int) length] = CarrierTime;
      }
      else
      {
        this.SendBuff[10] = Truncate;
        this.SendBuff[11] = CarrierTime;
      }
      this.GetCRC(this.SendBuff, 12 + (int) length);
      this.SendDataToPort(this.SendBuff, 14 + (int) length);
      int dataFromPort = this.GetDataFromPort(152, 1500);
      if (dataFromPort != 0)
        return dataFromPort;
      if (this.CheckCRC(this.RecvBuff, this.RecvLength) != 0)
        return 49;
      if (this.RecvBuff[2] != (byte) 152)
        return (int) this.RecvBuff[3];
      ComAdr = this.RecvBuff[1];
      return (int) this.RecvBuff[3];
    }

    public int SelectCmd(
      ref byte ComAdr,
      byte Antenna,
      byte Session,
      byte SelAction,
      byte MaskMem,
      byte[] MaskAdr,
      byte MaskLen,
      byte[] MaskData,
      byte Truncate)
    {
      byte length = (byte) (((int) MaskLen + 7) / 8);
      this.SendBuff[0] = (byte) (12U + (uint) length);
      this.SendBuff[1] = ComAdr;
      this.SendBuff[2] = (byte) 154;
      this.SendBuff[3] = Antenna;
      this.SendBuff[4] = Session;
      this.SendBuff[5] = SelAction;
      this.SendBuff[6] = MaskMem;
      this.SendBuff[7] = MaskAdr[0];
      this.SendBuff[8] = MaskAdr[1];
      this.SendBuff[9] = MaskLen;
      if (length > (byte) 0)
      {
        Array.Copy((Array) MaskData, 0, (Array) this.SendBuff, 10, (int) length);
        this.SendBuff[10 + (int) length] = Truncate;
      }
      else
        this.SendBuff[10] = Truncate;
      this.GetCRC(this.SendBuff, (int) this.SendBuff[0] - 1);
      this.SendDataToPort(this.SendBuff, 13 + (int) length);
      int dataFromPort = this.GetDataFromPort(154, 1500);
      if (dataFromPort != 0)
        return dataFromPort;
      if (this.CheckCRC(this.RecvBuff, this.RecvLength) != 0)
        return 49;
      if (this.RecvBuff[2] != (byte) 154)
        return (int) this.RecvBuff[3];
      ComAdr = this.RecvBuff[1];
      return (int) this.RecvBuff[3];
    }

    public int SelectCmd(
      ref byte ComAdr,
      int Antenna,
      byte Session,
      byte SelAction,
      byte MaskMem,
      byte[] MaskAdr,
      byte MaskLen,
      byte[] MaskData,
      byte Truncate)
    {
      byte length = (byte) (((int) MaskLen + 7) / 8);
      this.SendBuff[0] = (byte) (13U + (uint) length);
      this.SendBuff[1] = ComAdr;
      this.SendBuff[2] = (byte) 154;
      this.SendBuff[3] = (byte) (Antenna >> 8);
      this.SendBuff[4] = (byte) (Antenna & (int) byte.MaxValue);
      this.SendBuff[5] = Session;
      this.SendBuff[6] = SelAction;
      this.SendBuff[7] = MaskMem;
      this.SendBuff[8] = MaskAdr[0];
      this.SendBuff[9] = MaskAdr[1];
      this.SendBuff[10] = MaskLen;
      if (length > (byte) 0)
      {
        Array.Copy((Array) MaskData, 0, (Array) this.SendBuff, 11, (int) length);
        this.SendBuff[11 + (int) length] = Truncate;
      }
      else
        this.SendBuff[11] = Truncate;
      this.GetCRC(this.SendBuff, (int) this.SendBuff[0] - 1);
      this.SendDataToPort(this.SendBuff, 14 + (int) length);
      int dataFromPort = this.GetDataFromPort(154, 1500);
      if (dataFromPort != 0)
        return dataFromPort;
      if (this.CheckCRC(this.RecvBuff, this.RecvLength) != 0)
        return 49;
      if (this.RecvBuff[2] != (byte) 154)
        return (int) this.RecvBuff[3];
      ComAdr = this.RecvBuff[1];
      return (int) this.RecvBuff[3];
    }

    public int MeasureReturnLoss(ref byte ComAdr, byte[] TestFreq, byte Ant, ref byte ReturnLoss)
    {
      this.SendBuff[0] = (byte) 9;
      this.SendBuff[1] = ComAdr;
      this.SendBuff[2] = (byte) 145;
      Array.Copy((Array) TestFreq, 0, (Array) this.SendBuff, 3, 4);
      this.SendBuff[7] = Ant;
      this.GetCRC(this.SendBuff, 8);
      this.SendDataToPort(this.SendBuff, 10);
      int dataFromPort = this.GetDataFromPort(145, 1500);
      if (dataFromPort != 0)
        return dataFromPort;
      if (this.CheckCRC(this.RecvBuff, this.RecvLength) != 0)
        return 49;
      if (this.RecvBuff[2] != (byte) 145)
        return (int) this.RecvBuff[3];
      ComAdr = this.RecvBuff[1];
      ReturnLoss = this.RecvBuff[4];
      return (int) this.RecvBuff[3];
    }

    public int SetAntennaPower(ref byte ComAdr, byte[] powerDbm, int length)
    {
      this.SendBuff[0] = (byte) (4 + length);
      this.SendBuff[1] = ComAdr;
      this.SendBuff[2] = (byte) 47;
      Array.Copy((Array) powerDbm, 0, (Array) this.SendBuff, 3, length);
      this.GetCRC(this.SendBuff, (int) this.SendBuff[0] - 1);
      this.SendDataToPort(this.SendBuff, (int) this.SendBuff[0] + 1);
      int dataFromPort = this.GetDataFromPort(47, 1500);
      if (dataFromPort != 0)
        return dataFromPort;
      if (this.CheckCRC(this.RecvBuff, this.RecvLength) != 0)
        return 49;
      if (this.RecvBuff[2] != (byte) 47)
        return (int) this.RecvBuff[3];
      ComAdr = this.RecvBuff[1];
      return (int) this.RecvBuff[3];
    }

    public int GetAntennaPower(ref byte ComAdr, byte[] powerDbm, ref int length)
    {
      this.SendBuff[0] = (byte) 4;
      this.SendBuff[1] = ComAdr;
      this.SendBuff[2] = (byte) 148;
      this.GetCRC(this.SendBuff, (int) this.SendBuff[0] - 1);
      this.SendDataToPort(this.SendBuff, (int) this.SendBuff[0] + 1);
      int dataFromPort = this.GetDataFromPort(148, 1500);
      if (dataFromPort != 0)
        return dataFromPort;
      if (this.CheckCRC(this.RecvBuff, this.RecvLength) != 0)
        return 49;
      if (this.RecvBuff[2] != (byte) 148)
        return (int) this.RecvBuff[3];
      ComAdr = this.RecvBuff[1];
      length = (int) this.RecvBuff[0] - 5;
      Array.Copy((Array) this.RecvBuff, 4, (Array) powerDbm, 0, length);
      return (int) this.RecvBuff[3];
    }

    public int SetProfile(ref byte ComAdr, ref byte Profile)
    {
      this.SendBuff[0] = (byte) 5;
      this.SendBuff[1] = ComAdr;
      this.SendBuff[2] = (byte) 127;
      this.SendBuff[3] = Profile;
      this.GetCRC(this.SendBuff, (int) this.SendBuff[0] - 1);
      this.SendDataToPort(this.SendBuff, (int) this.SendBuff[0] + 1);
      int dataFromPort = this.GetDataFromPort((int) sbyte.MaxValue, 1500);
      if (dataFromPort != 0)
        return dataFromPort;
      if (this.CheckCRC(this.RecvBuff, this.RecvLength) != 0)
        return 49;
      if (this.RecvBuff[2] != (byte) 127)
        return (int) this.RecvBuff[3];
      ComAdr = this.RecvBuff[1];
      if (this.RecvBuff[3] == (byte) 0)
        Profile = this.RecvBuff[4];
      return (int) this.RecvBuff[3];
    }

    public int StopImmediately(ref byte ComAdr)
    {
      this.SendBuff[0] = (byte) 4;
      this.SendBuff[1] = ComAdr;
      this.SendBuff[2] = (byte) 147;
      this.GetCRC(this.SendBuff, (int) this.SendBuff[0] - 1);
      this.SendDataToPort_Noclear(this.SendBuff, (int) this.SendBuff[0] + 1);
      return 0;
    }

    public int SetCfgParameter(ref byte ComAdr, byte opt, byte cfgNum, byte[] data, int len)
    {
      this.SendBuff[0] = (byte) (6 + len);
      this.SendBuff[1] = ComAdr;
      this.SendBuff[2] = (byte) 234;
      this.SendBuff[3] = opt;
      this.SendBuff[4] = cfgNum;
      if (len > 0)
        Array.Copy((Array) data, 0, (Array) this.SendBuff, 5, len);
      this.GetCRC(this.SendBuff, (int) this.SendBuff[0] - 1);
      this.SendDataToPort(this.SendBuff, (int) this.SendBuff[0] + 1);
      int dataFromPort = this.GetDataFromPort(234, 1500);
      if (dataFromPort != 0)
        return dataFromPort;
      if (this.CheckCRC(this.RecvBuff, this.RecvLength) != 0)
        return 49;
      if (this.RecvBuff[2] != (byte) 234)
        return (int) this.RecvBuff[3];
      ComAdr = this.RecvBuff[1];
      return (int) this.RecvBuff[3];
    }

    public int GetCfgParameter(ref byte ComAdr, byte cfgNo, byte[] cfgData, ref int len)
    {
      this.SendBuff[0] = (byte) 5;
      this.SendBuff[1] = ComAdr;
      this.SendBuff[2] = (byte) 235;
      this.SendBuff[3] = cfgNo;
      this.GetCRC(this.SendBuff, (int) this.SendBuff[0] - 1);
      this.SendDataToPort(this.SendBuff, (int) this.SendBuff[0] + 1);
      int dataFromPort = this.GetDataFromPort(235, 1500);
      if (dataFromPort != 0)
        return dataFromPort;
      if (this.CheckCRC(this.RecvBuff, this.RecvLength) != 0)
        return 49;
      if (this.RecvBuff[2] != (byte) 235)
        return (int) this.RecvBuff[3];
      ComAdr = this.RecvBuff[1];
      if (this.RecvBuff[3] == (byte) 0)
      {
        len = this.RecvLength - 6;
        if (len > 0)
          Array.Copy((Array) this.RecvBuff, 4, (Array) cfgData, 0, len);
      }
      return (int) this.RecvBuff[3];
    }

    public int StartRead(ref byte ComAdr, byte Target)
    {
      this.SendBuff[0] = (byte) 5;
      this.SendBuff[1] = ComAdr;
      this.SendBuff[2] = (byte) 80;
      this.SendBuff[3] = Target;
      this.GetCRC(this.SendBuff, (int) this.SendBuff[0] - 1);
      this.SendDataToPort(this.SendBuff, (int) this.SendBuff[0] + 1);
      int dataFromPort = this.GetDataFromPort(80, 1500);
      if (dataFromPort != 0)
        return dataFromPort;
      if (this.CheckCRC(this.RecvBuff, this.RecvLength) != 0)
        return 49;
      if (this.RecvBuff[2] != (byte) 80)
        return (int) this.RecvBuff[3];
      ComAdr = this.RecvBuff[1];
      return (int) this.RecvBuff[3];
    }

    public int StopRead(ref byte ComAdr)
    {
      this.SendBuff[0] = (byte) 4;
      this.SendBuff[1] = ComAdr;
      this.SendBuff[2] = (byte) 81;
      this.GetCRC(this.SendBuff, (int) this.SendBuff[0] - 1);
      this.SendDataToPort_Noclear(this.SendBuff, (int) this.SendBuff[0] + 1);
      int dataFromPort = this.GetDataFromPort(81, 1500);
      if (dataFromPort != 0)
        return dataFromPort;
      if (this.CheckCRC(this.RecvBuff, this.RecvLength) != 0)
        return 49;
      if (this.RecvBuff[2] != (byte) 81)
        return (int) this.RecvBuff[3];
      ComAdr = this.RecvBuff[1];
      return (int) this.RecvBuff[3];
    }

    public int Protected_mode_G2(
      ref byte fComAdr,
      byte[] EPC,
      byte ENum,
      byte enable,
      byte[] Password,
      byte MaskMem,
      byte[] MaskAdr,
      byte MaskLen,
      byte[] MaskData,
      ref int errorcode)
    {
      this.SendBuff[1] = fComAdr;
      this.SendBuff[2] = (byte) 233;
      this.SendBuff[3] = ENum;
      if (ENum == byte.MaxValue)
      {
        this.SendBuff[4] = enable;
        Array.Copy((Array) Password, 0, (Array) this.SendBuff, 5, 4);
        this.SendBuff[9] = MaskMem;
        this.SendBuff[10] = MaskAdr[0];
        this.SendBuff[11] = MaskAdr[11];
        this.SendBuff[12] = MaskLen;
        int length = (int) MaskLen % 8 != 0 ? (int) MaskLen / 8 + 1 : (int) MaskLen / 8;
        Array.Copy((Array) MaskData, 0, (Array) this.SendBuff, 14, length);
        this.SendBuff[0] = Convert.ToByte(13 + length);
      }
      else
      {
        if (ENum < (byte) 0 || ENum >= (byte) 32)
          return (int) byte.MaxValue;
        Array.Copy((Array) EPC, 0, (Array) this.SendBuff, 4, (int) ENum * 2);
        this.SendBuff[(int) ENum * 2 + 4] = enable;
        Array.Copy((Array) Password, 0, (Array) this.SendBuff, (int) ENum * 2 + 5, 4);
        this.SendBuff[0] = Convert.ToByte((int) ENum * 2 + 10);
      }
      this.GetCRC(this.SendBuff, (int) this.SendBuff[0] - 1);
      this.SendDataToPort(this.SendBuff, (int) this.SendBuff[0] + 1);
      int dataFromPort = this.GetDataFromPort(233, 1500);
      if (dataFromPort != 0)
        return dataFromPort;
      if (this.CheckCRC(this.RecvBuff, this.RecvLength) != 0)
        return 49;
      if (this.RecvBuff[2] != (byte) 233)
        return (int) this.RecvBuff[3];
      if (this.RecvBuff[3] == (byte) 0)
      {
        fComAdr = this.RecvBuff[1];
        errorcode = 0;
      }
      else if (this.RecvBuff[3] == (byte) 252)
        errorcode = (int) this.RecvBuff[4];
      return (int) this.RecvBuff[3];
    }
  }
}
