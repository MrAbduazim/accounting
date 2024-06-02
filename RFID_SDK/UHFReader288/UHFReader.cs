// Decompiled with JetBrains decompiler
// Type: UHF.UHFReader
// Assembly: UHFReader288, Version=1.0.5.5, Culture=neutral, PublicKeyToken=null
// MVID: CC6301A5-076A-44BC-8C3C-EE1B90F3208E
// Assembly location: C:\Users\Abduazim\DjangoProjects\RFID\RFID_SDK\UHFReader288.dll

using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Text;
using System.Threading;
using UHFReaderModule;

#nullable disable
namespace UHF
{
  public class UHFReader
  {
    public Reader uhf = new Reader();
    public bool isConnect;
    private bool isScanning;
    private RFIDCallBack Callback;
    private volatile bool toStopThread;
    private Thread mythread;
    private byte ComAddr = byte.MaxValue;

    public void InitRFIDCallBack(RFIDCallBack t) => this.uhf.ReceiveCallback = t;

    public int OpenNetPort(int Port, string IPaddr, ref byte ComAddr)
    {
      if (this.isConnect)
        return 53;
      int num = this.uhf.OpenByTcp(IPaddr, Port, ref ComAddr);
      if (num == 0)
      {
        this.isScanning = false;
        this.isConnect = true;
      }
      return num;
    }

    public int CloseNetPort()
    {
      if (this.isScanning)
        return 51;
      if (!this.isConnect)
        return 0;
      int num = this.uhf.CloseByTcp();
      if (num == 0)
      {
        this.isConnect = false;
        this.isScanning = false;
      }
      return num;
    }

    public int OpenComPort(int Port, ref byte ComAddr, byte Baud)
    {
      if (this.isConnect)
        return 53;
      int num = this.uhf.OpenByCom(Port, ref ComAddr, Baud);
      if (num == 0)
      {
        this.isScanning = false;
        this.isConnect = true;
      }
      return num;
    }

    public int CloseComPort()
    {
      if (this.isScanning)
        return 51;
      if (!this.isConnect)
        return 0;
      int num = this.uhf.CloseByCom();
      if (num == 0)
      {
        this.isConnect = false;
        this.isScanning = false;
      }
      return num;
    }

    public int AutoOpenComPort(ref int Port, ref byte ComAddr, byte Baud)
    {
      string[] portNames = SerialPort.GetPortNames();
      List<string> stringList = new List<string>();
      for (int index = 0; index < portNames.Length; ++index)
        stringList.Add(portNames[index]);
      if (stringList.Count == 0)
        return 48;
      int num = 48;
      foreach (string str in stringList)
      {
        int int32 = Convert.ToInt32(str.Substring(3));
        num = this.uhf.OpenByCom(int32, ref ComAddr, Baud);
        if (num == 0)
        {
          Port = int32;
          break;
        }
      }
      return num;
    }

    public int CloseSpecComPort()
    {
      if (this.isScanning)
        return 51;
      if (!this.isConnect)
        return 0;
      int num = this.uhf.CloseByCom();
      if (num == 0)
      {
        this.isConnect = false;
        this.isScanning = false;
      }
      return num;
    }

    public int GetReaderInformation(
      ref byte ComAdr,
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
      return this.uhf.GetReaderInformation(ref ComAdr, VersionInfo, ref ReaderType, ref TrType, ref dmaxfre, ref dminfre, ref powerdBm, ref ScanTime, ref Ant, ref BeepEn, ref OutputRep, ref CheckAnt);
    }

    public int SetRegion(ref byte ComAdr, byte dmaxfre, byte dminfre)
    {
      return this.uhf.SetRegion(ref ComAdr, dmaxfre, dminfre);
    }

    public int SetAddress(ref byte ComAdr, byte ComAdrData)
    {
      return this.uhf.SetAddress(ref ComAdr, ComAdrData);
    }

    public int SetInventoryScanTime(ref byte ComAdr, byte ScanTime)
    {
      return this.uhf.SetInventoryScanTime(ref ComAdr, ScanTime);
    }

    public int SetBaudRate(ref byte ComAdr, byte baud) => this.uhf.SetBaudRate(ref ComAdr, baud);

    public int SetRfPower(ref byte ComAdr, byte powerDbm)
    {
      return this.uhf.SetRfPower(ref ComAdr, powerDbm);
    }

    public int SetRfPower(ref byte ComAdr, byte[] powerDbm)
    {
      return powerDbm.Length != 4 && powerDbm.Length != 8 && powerDbm.Length != 16 ? (int) byte.MaxValue : this.uhf.SetRfPower(ref ComAdr, powerDbm);
    }

    public int BuzzerAndLEDControl(ref byte ComAdr, byte AvtiveTime, byte SilentTime, byte Times)
    {
      return this.uhf.BuzzerAndLEDControl(ref ComAdr, AvtiveTime, SilentTime, Times);
    }

    public int SetWorkMode(ref byte ComAdr, byte Read_mode)
    {
      return this.uhf.SetWorkMode(ref ComAdr, Read_mode);
    }

    public int SetAntennaMultiplexing(ref byte ComAdr, byte SetOnce, byte AntCfg1, byte AntCfg2)
    {
      return this.uhf.SetAntennaMultiplexing(ref ComAdr, SetOnce, AntCfg1, AntCfg2);
    }

    public int SetAntennaMultiplexing(ref byte ComAdr, byte Ant)
    {
      return this.uhf.SetAntennaMultiplexing(ref ComAdr, Ant);
    }

    public int SetBeepNotification(ref byte ComAdr, byte BeepEn)
    {
      return this.uhf.SetBeepNotification(ref ComAdr, BeepEn);
    }

    public int SetReal_timeClock(ref byte ComAdr, byte[] paramer)
    {
      return this.uhf.SetReal_timeClock(ref ComAdr, paramer);
    }

    public int GetTime(ref byte ComAdr, byte[] paramer) => this.uhf.GetTime(ref ComAdr, paramer);

    public int SetRelay(ref byte ComAdr, byte RelayTime)
    {
      return this.uhf.SetRelay(ref ComAdr, RelayTime);
    }

    public int SetGPIO(ref byte ComAdr, byte OutputPin) => this.uhf.SetGPIO(ref ComAdr, OutputPin);

    public int GetGPIOStatus(ref byte ComAdr, ref byte OutputPin)
    {
      return this.uhf.GetGPIOStatus(ref ComAdr, ref OutputPin);
    }

    public int SetNotificationPulseOutput(ref byte ComAdr, byte OutputRep)
    {
      return this.uhf.SetNotificationPulseOutput(ref ComAdr, OutputRep);
    }

    public int GetSystemParameter(
      ref byte ComAdr,
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
      return this.uhf.GetSystemParameter(ref ComAdr, ref Read_mode, ref Accuracy, ref RepCondition, ref RepPauseTime, ref ReadPauseTim, ref TagProtocol, ref MaskMem, MaskAdr, ref MaskLen, MaskData, ref TriggerTime, ref AdrTID, ref LenTID);
    }

    public int SetEASSensitivity(ref byte ComAdr, byte Accuracy)
    {
      return this.uhf.SetEASSensitivity(ref ComAdr, Accuracy);
    }

    public int SetTriggerTime(ref byte ComAdr, byte TriggerTime)
    {
      return this.uhf.SetTriggerTime(ref ComAdr, TriggerTime);
    }

    public int SetTIDParameter(ref byte ComAdr, byte AdrTID, byte LenTID)
    {
      return this.uhf.SetTIDParameter(ref ComAdr, AdrTID, LenTID);
    }

    public int SetMask(
      ref byte ComAdr,
      byte MaskMem,
      byte[] MaskAdr,
      byte MaskLen,
      byte[] MaskData)
    {
      return this.uhf.SetMask(ref ComAdr, MaskMem, MaskAdr, MaskLen, MaskData);
    }

    public int SetResponsePamametersofAuto_runningMode(
      ref byte ComAdr,
      byte RepCondition,
      byte RepPauseTime)
    {
      return this.uhf.SetResponsePamametersofAuto_runningMode(ref ComAdr, RepCondition, RepPauseTime);
    }

    public int SetInventoryInterval(ref byte ComAdr, byte ReadPauseTim)
    {
      return this.uhf.SetInventoryInterval(ref ComAdr, ReadPauseTim);
    }

    public int SelectTagType(ref byte ComAdr, byte Protocol)
    {
      return this.uhf.SelectTagType(ref ComAdr, Protocol);
    }

    public int GetTagBufferInfo(ref byte ComAdr, byte[] Data, ref int dataLength)
    {
      return this.uhf.GetTagBufferInfo(ref ComAdr, Data, ref dataLength);
    }

    public int ClearTagBuffer(ref byte ComAdr) => this.uhf.ClearTagBuffer(ref ComAdr);

    public int ReadActiveModeData(byte[] ScanModeData, ref int ValidDatalength)
    {
      return this.uhf.ReadActiveModeData(ScanModeData, ref ValidDatalength);
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
      if (this.isScanning)
        return 51;
      this.isScanning = true;
      int num = this.uhf.Inventory_G2(ref ComAdr, QValue, Session, MaskMem, MaskAdr, MaskLen, MaskData, MaskFlag, AdrTID, LenTID, TIDFlag, Target, InAnt, Scantime, FastFlag, pEPCList, ref Ant, ref Totallen, ref CardNum);
      this.isScanning = false;
      return num;
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
      if (this.isScanning)
        return 51;
      this.isScanning = true;
      int num = this.uhf.InventoryMix_G2(ref ComAdr, QValue, Session, MaskMem, MaskAdr, MaskLen, MaskData, MaskFlag, ReadMem, ReadAdr, ReadLen, Psd, Target, InAnt, Scantime, FastFlag, pEPCList, ref Ant, ref Totallen, ref CardNum);
      this.isScanning = false;
      return num;
    }

    public int ReadData_G2(
      ref byte ComAdr,
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
      return this.uhf.ReadData_G2(ref ComAdr, EPC, ENum, Mem, WordPtr, Num, Password, MaskMem, MaskAdr, MaskLen, MaskData, Data, ref errorcode);
    }

    public int WriteData_G2(
      ref byte ComAdr,
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
      return this.uhf.WriteData_G2(ref ComAdr, EPC, WNum, ENum, Mem, WordPtr, Wdt, Password, MaskMem, MaskAdr, MaskLen, MaskData, ref errorcode);
    }

    public int WriteEPC_G2(
      ref byte ComAdr,
      byte[] Password,
      byte[] WriteEPC,
      byte ENum,
      ref int errorcode)
    {
      return this.uhf.WriteEPC_G2(ref ComAdr, Password, WriteEPC, ENum, ref errorcode);
    }

    public int KillTag_G2(
      ref byte ComAdr,
      byte[] EPC,
      byte ENum,
      byte[] Password,
      byte MaskMem,
      byte[] MaskAdr,
      byte MaskLen,
      byte[] MaskData,
      ref int errorcode)
    {
      return this.uhf.KillTag_G2(ref ComAdr, EPC, ENum, Password, MaskMem, MaskAdr, MaskLen, MaskData, ref errorcode);
    }

    public int Lock_G2(
      ref byte ComAdr,
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
      return this.uhf.Lock_G2(ref ComAdr, EPC, ENum, select, setprotect, Password, MaskMem, MaskAdr, MaskLen, MaskData, ref errorcode);
    }

    public int BlockErase_G2(
      ref byte ComAdr,
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
      return this.uhf.BlockErase_G2(ref ComAdr, EPC, ENum, Mem, WordPtr, Num, Password, MaskMem, MaskAdr, MaskLen, MaskData, ref errorcode);
    }

    public int SetPrivacyWithoutEPC_G2(ref byte ComAdr, byte[] Password, ref int errorcode)
    {
      return this.uhf.SetPrivacyWithoutEPC_G2(ref ComAdr, Password, ref errorcode);
    }

    public int SetPrivacyByEPC_G2(
      ref byte ComAdr,
      byte[] EPC,
      byte ENum,
      byte[] Password,
      byte MaskMem,
      byte[] MaskAdr,
      byte MaskLen,
      byte[] MaskData,
      ref int errorcode)
    {
      return this.uhf.SetPrivacyByEPC_G2(ref ComAdr, EPC, ENum, Password, MaskMem, MaskAdr, MaskLen, MaskData, ref errorcode);
    }

    public int ResetPrivacy_G2(ref byte ComAdr, byte[] Password, ref int errorcode)
    {
      return this.uhf.ResetPrivacy_G2(ref ComAdr, Password, ref errorcode);
    }

    public int CheckPrivacy_G2(ref byte ComAdr, ref byte readpro, ref int errorcode)
    {
      return this.uhf.CheckPrivacy_G2(ref ComAdr, ref readpro, ref errorcode);
    }

    public int EASConfigure_G2(
      ref byte ComAdr,
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
      return this.uhf.EASConfigure_G2(ref ComAdr, EPC, ENum, Password, EAS, MaskMem, MaskAdr, MaskLen, MaskData, ref errorcode);
    }

    public int EASAlarm_G2(ref byte ComAdr, ref int errorcode)
    {
      return this.uhf.EASAlarm_G2(ref ComAdr, ref errorcode);
    }

    public int BlockWrite_G2(
      ref byte ComAdr,
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
      return this.uhf.BlockWrite_G2(ref ComAdr, EPC, WNum, ENum, Mem, WordPtr, Wdt, Password, MaskMem, MaskAdr, MaskLen, MaskData, ref errorcode);
    }

    public int ChangeATMode(ref byte ConAddr, byte ATMode)
    {
      return this.uhf.ChangeATMode(ref ConAddr, ATMode);
    }

    public int TransparentCMD(
      ref byte ConAddr,
      byte timeout,
      byte cmdlen,
      byte[] cmddata,
      ref byte recvLen,
      byte[] recvdata)
    {
      return this.uhf.TransparentCMD(ref ConAddr, timeout, cmdlen, cmddata, ref recvLen, recvdata);
    }

    public int GetSeriaNo(ref byte ConAddr, byte[] SeriaNo)
    {
      return this.uhf.GetSeriaNo(ref ConAddr, SeriaNo);
    }

    public int SetCheckAnt(ref byte ComAdr, byte CheckAnt)
    {
      return this.uhf.SetCheckAnt(ref ComAdr, CheckAnt);
    }

    public int InventorySingle_6B(ref byte ConAddr, ref byte ant, byte[] ID_6B)
    {
      return this.uhf.InventorySingle_6B(ref ConAddr, ref ant, ID_6B);
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
      return this.uhf.InventoryMultiple_6B(ref ConAddr, Condition, StartAddress, mask, ConditionContent, ref ant, ID_6B, ref Cardnum);
    }

    public int ReadData_6B(
      ref byte ConAddr,
      byte[] ID_6B,
      byte StartAddress,
      byte Num,
      byte[] Data,
      ref int errorcode)
    {
      return this.uhf.ReadData_6B(ref ConAddr, ID_6B, StartAddress, Num, Data, ref errorcode);
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
      return this.uhf.WriteData_6B(ref ConAddr, ID_6B, StartAddress, Writedata, Writedatalen, ref writtenbyte, ref errorcode);
    }

    public int Lock_6B(ref byte ConAddr, byte[] ID_6B, byte Address, ref int errorcode)
    {
      return this.uhf.Lock_6B(ref ConAddr, ID_6B, Address, ref errorcode);
    }

    public int CheckLock_6B(
      ref byte ConAddr,
      byte[] ID_6B,
      byte Address,
      ref byte ReLockState,
      ref int errorcode)
    {
      return this.uhf.CheckLock_6B(ref ConAddr, ID_6B, Address, ref ReLockState, ref errorcode);
    }

    public int SetQS(ref byte ConAddr, byte Qvalue, byte Session)
    {
      return this.uhf.SetQS(ref ConAddr, Qvalue, Session);
    }

    public int GetQS(ref byte ConAddr, ref byte Qvalue, ref byte Session)
    {
      return this.uhf.GetQS(ref ConAddr, ref Qvalue, ref Session);
    }

    public int SetFlashRom(ref byte ConAddr) => this.uhf.SetFlashRom(ref ConAddr);

    public int GetModuleVersion(ref byte ConAddr, byte[] Version)
    {
      return this.uhf.GetModuleVersion(ref ConAddr, Version);
    }

    public int ExtReadData_G2(
      ref byte ComAdr,
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
      return this.uhf.ExtReadData_G2(ref ComAdr, EPC, ENum, Mem, WordPtr, Num, Password, MaskMem, MaskAdr, MaskLen, MaskData, Data, ref errorcode);
    }

    public int ExtWriteData_G2(
      ref byte ComAdr,
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
      return this.uhf.ExtWriteData_G2(ref ComAdr, EPC, WNum, ENum, Mem, WordPtr, Wdt, Password, MaskMem, MaskAdr, MaskLen, MaskData, ref errorcode);
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
      return this.uhf.InventoryBuffer_G2(ref ComAdr, QValue, Session, MaskMem, MaskAdr, MaskLen, MaskData, MaskFlag, AdrTID, LenTID, TIDFlag, Target, InAnt, Scantime, FastFlag, ref BufferCount, ref TagNum);
    }

    public int SetSaveLen(ref byte ComAdr, byte SaveLen)
    {
      return this.uhf.SetSaveLen(ref ComAdr, SaveLen);
    }

    public int GetSaveLen(ref byte ComAdr, ref byte SaveLen)
    {
      return this.uhf.GetSaveLen(ref ComAdr, ref SaveLen);
    }

    public int ReadBuffer_G2(ref byte ComAdr, ref int Totallen, ref int CardNum, byte[] pEPCList)
    {
      return this.uhf.ReadBuffer_G2(ref ComAdr, ref Totallen, ref CardNum, pEPCList);
    }

    public int ClearBuffer_G2(ref byte ComAdr) => this.uhf.ClearBuffer_G2(ref ComAdr);

    public int GetBufferCnt_G2(ref byte ComAdr, ref int Count)
    {
      return this.uhf.GetBufferCnt_G2(ref ComAdr, ref Count);
    }

    public int SetReadMode(ref byte ComAdr, byte ReadMode)
    {
      return this.uhf.SetReadMode(ref ComAdr, ReadMode);
    }

    public int SetReadParameter(
      ref byte ComAdr,
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
      return this.uhf.SetReadParameter(ref ComAdr, Parameter, MaskMem, MaskAdr, MaskLen, MaskData, MaskFlag, AdrTID, LenTID, TIDFlag);
    }

    public int GetReadParameter(ref byte ComAdr, byte[] Parameter)
    {
      return this.uhf.GetReadParameter(ref ComAdr, Parameter);
    }

    public int WriteRfPower(ref byte ComAdr, byte powerDbm)
    {
      return this.uhf.WriteRfPower(ref ComAdr, powerDbm);
    }

    public int ReadRfPower(ref byte ComAdr, ref byte powerDbm)
    {
      return this.uhf.ReadRfPower(ref ComAdr, ref powerDbm);
    }

    public int RetryTimes(ref byte ComAdr, ref byte Times)
    {
      return this.uhf.RetryTimes(ref ComAdr, ref Times);
    }

    public int SetDRM(ref byte ComAdr, byte DRM) => this.uhf.SetDRM(ref ComAdr, DRM);

    public int GetDRM(ref byte ComAdr, ref byte DRM) => this.uhf.GetDRM(ref ComAdr, ref DRM);

    public int GetReaderTemperature(ref byte ComAdr, ref byte PlusMinus, ref byte Temperature)
    {
      return this.uhf.GetReaderTemperature(ref ComAdr, ref PlusMinus, ref Temperature);
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
      return this.uhf.SelectCmdWithCarrier(ref ComAdr, Antenna, Session, SelAction, MaskMem, MaskAdr, MaskLen, MaskData, Truncate, CarrierTime);
    }

    public int MeasureReturnLoss(ref byte ComAdr, byte[] TestFreq, byte Ant, ref byte ReturnLoss)
    {
      return this.uhf.MeasureReturnLoss(ref ComAdr, TestFreq, Ant, ref ReturnLoss);
    }

    public int SetAntennaPower(ref byte ComAdr, byte[] powerDbm, int length)
    {
      return this.uhf.SetAntennaPower(ref ComAdr, powerDbm, length);
    }

    public int GetAntennaPower(ref byte ComAdr, byte[] powerDbm, ref int length)
    {
      return this.uhf.GetAntennaPower(ref ComAdr, powerDbm, ref length);
    }

    public int SetProfile(ref byte fComAdr, ref byte Profile)
    {
      return this.uhf.SetProfile(ref fComAdr, ref Profile);
    }

    public int StopImmediately(ref byte ComAdr) => this.uhf.StopImmediately(ref ComAdr);

    public int SetCfgParameter(ref byte ComAddr, byte opt, byte cfgNum, byte[] data, int len)
    {
      return this.uhf.SetCfgParameter(ref ComAddr, opt, cfgNum, data, len);
    }

    public int GetCfgParameter(ref byte ComAddr, byte cfgNo, byte[] cfgData, ref int len)
    {
      return this.uhf.GetCfgParameter(ref ComAddr, cfgNo, cfgData, ref len);
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
      return this.uhf.SelectCmd(ref this.ComAddr, Antenna, Session, SelAction, MaskMem, MaskAdr, MaskLen, MaskData, Truncate);
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
      return this.uhf.SelectCmd(ref this.ComAddr, Antenna, Session, SelAction, MaskMem, MaskAdr, MaskLen, MaskData, Truncate);
    }

    public byte[] HexStringToByteArray(string s)
    {
      if (s == "" || s == null)
        return (byte[]) null;
      s = s.Replace(" ", "");
      byte[] byteArray = new byte[s.Length / 2];
      for (int startIndex = 0; startIndex < s.Length; startIndex += 2)
        byteArray[startIndex / 2] = Convert.ToByte(s.Substring(startIndex, 2), 16);
      return byteArray;
    }

    public string ByteArrayToHexString(byte[] data)
    {
      StringBuilder stringBuilder = new StringBuilder(data.Length * 3);
      foreach (byte num in data)
        stringBuilder.Append(Convert.ToString(num, 16).PadLeft(2, '0'));
      return stringBuilder.ToString().ToUpper();
    }

    public bool CheckCRC(string s)
    {
      byte[] byteArray = this.HexStringToByteArray(s);
      int num1 = (int) ushort.MaxValue;
      for (int index1 = 0; index1 <= byteArray.Length - 1; ++index1)
      {
        num1 ^= (int) byteArray[index1];
        for (int index2 = 0; index2 < 8; ++index2)
        {
          if ((num1 & 1) != 0)
            num1 = num1 >> 1 ^ 33800;
          else
            num1 >>= 1;
        }
      }
      byte num2 = Convert.ToByte(num1 & (int) byte.MaxValue);
      return Convert.ToByte(num1 >> 8 & (int) byte.MaxValue) == (byte) 0 && num2 == (byte) 0;
    }

    public int StartInventory(ref byte ComAdr, byte Target, RFIDCallBack t)
    {
      if (this.uhf.StartRead(ref ComAdr, Target) == 0)
      {
        this.ComAddr = ComAdr;
        this.Callback = t;
        if (this.mythread == null)
        {
          this.toStopThread = false;
          this.mythread = new Thread(new ThreadStart(this.workProcess));
          this.mythread.IsBackground = true;
          this.mythread.Start();
        }
      }
      return 0;
    }

    public int StopInventory(ref byte ComAdr)
    {
      this.toStopThread = true;
      this.StopImmediately(ref ComAdr);
      while (this.mythread != null && this.mythread.IsAlive)
        Thread.Sleep(1);
      return this.uhf.StopRead(ref ComAdr);
    }

    public void workProcess()
    {
      string str1 = "";
      long tickCount = (long) Environment.TickCount;
      while (!this.toStopThread)
      {
        byte[] numArray1 = new byte[4096];
        int ValidDatalength = 0;
        if (this.uhf.ReadActiveModeData(numArray1, ref ValidDatalength) == 0)
        {
          tickCount = (long) Environment.TickCount;
          try
          {
            byte[] numArray2 = new byte[ValidDatalength];
            Array.Copy((Array) numArray1, 0, (Array) numArray2, 0, ValidDatalength);
            string hexString = this.ByteArrayToHexString(numArray2);
            str1 += hexString;
            int length = str1.Length;
            while (str1.Length > 18)
            {
              string str2 = "EE00";
              int num1 = str1.IndexOf(str2);
              if (num1 > 3)
              {
                str1 = str1.Substring(num1 - 4);
                int num2 = Convert.ToInt32(str1.Substring(0, 2), 16) * 2 + 2;
                if (str1.Length >= num2)
                {
                  string s = str1.Substring(0, num2);
                  str1 = str1.Substring(num2);
                  if (this.CheckCRC(s))
                  {
                    string str3 = s.Substring(8, 2);
                    int int32 = Convert.ToInt32(Convert.ToString(Convert.ToInt32(s.Substring(10, 2), 16), 10), 10);
                    bool flag = false;
                    int num3 = 0;
                    int num4 = 0;
                    int num5 = 0;
                    if ((int32 & 64) > 0)
                      flag = true;
                    string str4 = s.Substring(12, (int32 & 63) * 2);
                    string str5 = s.Substring(12 + (int32 & 63) * 2, 2);
                    if (flag)
                    {
                      string str6 = s.Substring(s.Length - 18, 14);
                      num3 = Convert.ToInt32(str6.Substring(0, 4), 16);
                      num4 = Convert.ToInt32(str6.Substring(4, 4), 16);
                      num5 = Convert.ToInt32(str6.Substring(8, 6), 16);
                    }
                    if (this.Callback != null)
                      this.Callback(new RFIDTag()
                      {
                        ANT = Convert.ToByte(str3, 16),
                        LEN = (byte) (str4.Length / 2),
                        RSSI = Convert.ToByte(str5, 16),
                        UID = str4,
                        phase_begin = num3,
                        phase_end = num4,
                        DeviceName = this.uhf.DevName,
                        Freqkhz = num5
                      });
                  }
                }
                else
                  break;
              }
              else
                str1 = str1.Substring(2);
            }
          }
          catch (Exception ex)
          {
            ex.ToString();
          }
        }
        else if ((long) Environment.TickCount - tickCount > 10000L)
        {
          byte TrType = 0;
          byte[] VersionInfo = new byte[2];
          byte ReaderType = 0;
          byte ScanTime = 0;
          byte dmaxfre = 0;
          byte dminfre = 0;
          byte powerdBm = 0;
          byte Ant = 0;
          byte BeepEn = 0;
          byte OutputRep = 0;
          byte CheckAnt = 0;
          tickCount = (long) Environment.TickCount;
          this.GetReaderInformation(ref this.ComAddr, VersionInfo, ref ReaderType, ref TrType, ref dmaxfre, ref dminfre, ref powerdBm, ref ScanTime, ref Ant, ref BeepEn, ref OutputRep, ref CheckAnt);
        }
      }
      this.mythread = (Thread) null;
    }

    public int Protected_mode_Enabled(
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
      byte enable = 1;
      return this.uhf.Protected_mode_G2(ref fComAdr, EPC, ENum, enable, Password, MaskMem, MaskAdr, MaskLen, MaskData, ref errorcode);
    }

    public int Protected_mode_Disabled(
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
      byte enable = 0;
      return this.uhf.Protected_mode_G2(ref fComAdr, EPC, ENum, enable, Password, MaskMem, MaskAdr, MaskLen, MaskData, ref errorcode);
    }
  }
}
