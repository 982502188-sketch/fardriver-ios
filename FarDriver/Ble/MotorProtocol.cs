using System;

namespace FarDriver.Ble
{
    public static class BleUuids
    {
        public const string DeviceNamePrefix = "YuanQu";
        public const string ServiceMain = "0000ffe0-0000-1000-8000-00805f9b34fb";
        public const string CharacteristicNotify = "0000ffec-0000-1000-8000-00805f9b34fb";
        public const string CharacteristicCommand = "0000fff7-0000-1000-8000-00805f9b34fb";
        public const string CharacteristicUpgrade = "0000fff8-0000-1000-8000-00805f9b34fb";
    }

    public static class MotorProtocol
    {
        public const byte Header = 0x5A;
        public const byte CmdWritePara = 0x01;
        public const byte CmdReadPara = 0x02;
        public const byte CmdUpgrade = 0x03;
        public const byte CmdFlash = 0x04;
        public const byte CmdSave = 0x05;
        public const byte CmdReset = 0x06;
        public const byte CmdProduct = 0x07;
        public const byte CmdDetect = 0x08;
        public const byte CmdBmsRead = 0x09;
        public const byte CmdCurveStart = 0x0A;
        public const byte CmdCurveStop = 0x0B;

        public static byte[] Build(byte cmd, byte seq, byte[] payload)
        {
            var pkt = new byte[payload.Length + 6];
            pkt[0] = Header;
            pkt[1] = (byte)(payload.Length + 2);
            pkt[2] = cmd;
            pkt[3] = seq;
            Array.Copy(payload, 0, pkt, 4, payload.Length);
            ushort crc = Crc16(pkt, 0, pkt.Length - 2);
            pkt[pkt.Length - 2] = (byte)(crc >> 8);
            pkt[pkt.Length - 1] = (byte)(crc & 0xFF);
            return pkt;
        }

        public static ushort Crc16(byte[] data, int offset, int count)
        {
            ushort crc = 0x0000;
            for (int i = offset; i < offset + count; i++)
            {
                crc ^= (ushort)(data[i] << 8);
                for (int j = 0; j < 8; j++)
                {
                    if ((crc & 0x8000) != 0)
                        crc = (ushort)((crc << 1) ^ 0x1021);
                    else
                        crc <<= 1;
                }
            }
            return crc;
        }

        public static bool TryParse(byte[] data, out byte cmd, out byte seq, out byte[] payload)
        {
            cmd = seq = 0;
            payload = Array.Empty<byte>();
            if (data == null || data.Length < 6) return false;
            if (data[0] != Header) return false;
            int len = data[1];
            if (len < 2 || data.Length < len + 2) return false;
            cmd = data[2];
            seq = data[3];
            int plen = len - 2;
            payload = new byte[plen];
            Array.Copy(data, 4, payload, 0, plen);
            return true;
        }

        public static class Commands
        {
            public static byte[] ReadAll(byte seq) => Build(CmdReadPara, seq, new byte[] { 0x00, 0x00 });
            public static byte[] WriteParam(byte seq, ushort addr, ushort value)
            {
                var payload = new byte[]
                {
                    (byte)(addr >> 8), (byte)(addr & 0xFF),
                    (byte)(value >> 8), (byte)(value & 0xFF)
                };
                return Build(CmdWritePara, seq, payload);
            }
            public static byte[] Save(byte seq) => Build(CmdSave, seq, Array.Empty<byte>());
            public static byte[] Reset(byte seq) => Build(CmdReset, seq, Array.Empty<byte>());
            public static byte[] Detect(byte seq) => Build(CmdDetect, seq, Array.Empty<byte>());
            public static byte[] ReadBms(byte seq) => Build(CmdBmsRead, seq, Array.Empty<byte>());
            public static byte[] CurveStart(byte seq, ushort intervalMs)
            {
                var payload = new byte[] { (byte)(intervalMs >> 8), (byte)(intervalMs & 0xFF) };
                return Build(CmdCurveStart, seq, payload);
            }
            public static byte[] CurveStop(byte seq) => Build(CmdCurveStop, seq, Array.Empty<byte>());
        }
    }
}
