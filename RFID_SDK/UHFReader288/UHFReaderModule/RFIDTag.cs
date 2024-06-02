// Decompiled with JetBrains decompiler
// Type: UHFReaderModule.RFIDTag
// Assembly: UHFReader288, Version=1.0.5.5, Culture=neutral, PublicKeyToken=null
// MVID: CC6301A5-076A-44BC-8C3C-EE1B90F3208E
// Assembly location: C:\Users\Abduazim\DjangoProjects\RFID\RFID_SDK\UHFReader288.dll

#nullable disable
namespace UHFReaderModule
{
  public class RFIDTag
  {
    public byte PacketParam;
    public byte LEN;
    public string UID;
    public int phase_begin;
    public int phase_end;
    public byte RSSI;
    public int Freqkhz;
    public byte ANT;
    public string DeviceName;
  }
}
