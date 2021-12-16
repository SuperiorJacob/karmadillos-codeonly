using DarkRift.Client.Unity;
using UnityEngine;

namespace AberrationGames.Networking.Shared
{
    public struct PlayerData : DarkRift.IDarkRiftSerializable
    {
        public ushort clientID;
        public ushort lobby;
        public byte playerID;
 
        public PlayerInputData input;
        public PlayerStateData state;

        public string device;

        public PlayerData(ushort a_clientID, byte a_playerID, ushort a_lobby, PlayerStateData a_state = new PlayerStateData(), PlayerInputData a_input = new PlayerInputData(), string a_device = "Keyboard")
        {
            clientID = a_clientID;
            lobby = a_lobby;
            playerID = a_playerID;

            input = a_input;
            state = a_state;

            device = a_device;
        }

        public void Deserialize(DarkRift.DeserializeEvent e)
        {
            clientID = e.Reader.ReadUInt16();
            lobby = e.Reader.ReadUInt16();
            playerID = e.Reader.ReadByte();

            input.Deserialize(e);
            state.Deserialize(e);

            device = e.Reader.ReadString();
        }

        public void Serialize(DarkRift.SerializeEvent e)
        {
            e.Writer.Write(clientID);
            e.Writer.Write(lobby);
            e.Writer.Write(playerID);

            input.Serialize(e);
            state.Serialize(e);

            e.Writer.Write(device);
        }
    }

    public struct PlayerStateData : DarkRift.IDarkRiftSerializable
    {
        public ushort clientID;
        public Vector3 position;
        public Vector3 rotation;

        public PlayerStateData(ushort a_clientID, Vector3 a_position, Vector3 a_rotation)
        {
            clientID = a_clientID;
            position = a_position;
            rotation = a_rotation;
        }

        public void Deserialize(DarkRift.DeserializeEvent e)
        {
            clientID = e.Reader.ReadUInt16();

            position = new Vector3(e.Reader.ReadSingle(), e.Reader.ReadSingle(), e.Reader.ReadSingle());
            rotation = new Vector3(e.Reader.ReadSingle(), e.Reader.ReadSingle(), e.Reader.ReadSingle());
        }

        public void Serialize(DarkRift.SerializeEvent e)
        {
            e.Writer.Write(clientID);

            e.Writer.Write(position.x);
            e.Writer.Write(position.y);
            e.Writer.Write(position.z);

            e.Writer.Write(rotation.x);
            e.Writer.Write(rotation.y);
            e.Writer.Write(rotation.z);
        }
    }

    public struct PlayerInputData : DarkRift.IDarkRiftSerializable
    {
        public ushort clientID;
        public uint time;
        public PlayerInputs inputs;

        public PlayerInputData(ushort a_clientID, uint a_time, PlayerInputs a_inputs = new PlayerInputs())
        {
            clientID = a_clientID;
            time = a_time;
            inputs = a_inputs;
        }

        public void Deserialize(DarkRift.DeserializeEvent e)
        {
            clientID = e.Reader.ReadUInt16();

            time = e.Reader.ReadUInt32();

            inputs.Deserialize(e);
        }

        public void Serialize(DarkRift.SerializeEvent e)
        {
            e.Writer.Write(clientID);

            e.Writer.Write(time);

            inputs.Serialize(e);
        }
    }

    public struct PlayerInputs : DarkRift.IDarkRiftSerializable
    {
        public bool north;
        public bool south;
        public bool left;
        public bool right;

        public void Set(bool a_north = false, bool a_south = false, bool a_left = false, bool a_right = false)
        {
            north = a_north;
            south = a_south;
            left = a_left;
            right = a_right;
        }

        public void Deserialize(DarkRift.DeserializeEvent e)
        {
            north = e.Reader.ReadBoolean();
            south = e.Reader.ReadBoolean();
            left = e.Reader.ReadBoolean();
            right = e.Reader.ReadBoolean();
        }

        public void Serialize(DarkRift.SerializeEvent e)
        {
            e.Writer.Write(north);
            e.Writer.Write(south);
            e.Writer.Write(left);
            e.Writer.Write(right);
        }
    }
}
