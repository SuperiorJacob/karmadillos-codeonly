using DarkRift;
using UnityEngine;

namespace AberrationGames.Networking.Shared
{
    /// <summary>
    /// Room data, when networked equates to 117 bytes or 936 bits (almost a kb!)
    /// Try not to send this constantly.
    /// </summary>
    public struct RoomData : IDarkRiftSerializable
    {
        public ushort roomID; // 2 bytes
        public string name; // a string is 2 bytes * length (insanely big, estimated 50 bytes)
        public string mapAddress; // estimated 30 bytes
        public byte slots; // 1 byte
        public byte maxSlots; // 1 byte

        public byte masterPlayer; // 1 byte
        public byte roomInfo; // 1 byte

        public Vector3 spawnPos; // 12 bytes
        public Vector3 spawnRot; // 12 bytes

        public byte[] playerInfo; // estimated 4 bytes, if in use that is.

        public RoomData(ushort a_roomID, string a_name, string a_mapAddress, byte a_slots, byte a_maxSlots, 
            byte a_masterPlayer, byte a_roomInfo, Vector3 a_spawnPos, Vector3 a_spawnRot, byte[] a_playerInfo)
        {
            roomID = a_roomID;
            name = a_name;
            mapAddress = a_mapAddress;
            slots = a_slots;
            maxSlots = a_maxSlots;

            masterPlayer = a_masterPlayer;

            roomInfo = a_roomInfo;

            spawnPos = a_spawnPos;
            spawnRot = a_spawnRot;

            playerInfo = a_playerInfo;
        }

        public void Deserialize(DeserializeEvent e)
        {
            roomID = e.Reader.ReadUInt16();
            name = e.Reader.ReadString();
            mapAddress = e.Reader.ReadString();
            slots = e.Reader.ReadByte();
            maxSlots = e.Reader.ReadByte();

            masterPlayer = e.Reader.ReadByte();
            roomInfo = e.Reader.ReadByte();

            spawnPos = new Vector3(e.Reader.ReadSingle(), e.Reader.ReadSingle(), e.Reader.ReadSingle());
            spawnRot = new Vector3(e.Reader.ReadSingle(), e.Reader.ReadSingle(), e.Reader.ReadSingle());

            playerInfo = e.Reader.ReadBytes();
        }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(roomID);
            e.Writer.Write(name);
            e.Writer.Write(mapAddress);
            e.Writer.Write(slots);
            e.Writer.Write(maxSlots);

            e.Writer.Write(masterPlayer);
            e.Writer.Write(roomInfo);

            e.Writer.Write(spawnPos.x);
            e.Writer.Write(spawnPos.y);
            e.Writer.Write(spawnPos.z);

            e.Writer.Write(spawnRot.x);
            e.Writer.Write(spawnRot.y);
            e.Writer.Write(spawnRot.z);

            e.Writer.Write(playerInfo);
        }

        public override string ToString()
        {
            return $"Shared.<color=lime>RoomData</color> {'{'} roomID = <color=white>{roomID}</color>, name = <color=white>{name}</color>, mapAddress = <color=white>{mapAddress}</color>, slots = <color=white>{slots}</color>, maxSlots = <color=white>{maxSlots}</color> {'}'}";
        }
    }
}
