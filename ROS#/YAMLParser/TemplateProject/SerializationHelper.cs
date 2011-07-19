﻿#region USINGZ

using System;
using System.Collections;
using System.Runtime.InteropServices;

#endregion

namespace Messages
{
    public static class SerializationHelper
    {
        public static T Deserialize<T>(byte[] bytes)
        {
            T thestructure = default(T);
            IntPtr pIP = Marshal.AllocHGlobal(Marshal.SizeOf(thestructure));
            Marshal.Copy(bytes, 0, pIP, Marshal.SizeOf(thestructure));
            thestructure = (T) Marshal.PtrToStructure(pIP, typeof (T));
            Marshal.FreeHGlobal(pIP);
            /*StructTranslator thisone = new StructTranslator();
            T thestructure = default(T);
            if (thisone.Read<T>(bytes, 0, ref thestructure))
                Console.WriteLine("YAY!");*/
            return thestructure;
        }

        public static byte[] Serialize<T>(T outgoing) where T : IRosMessage
        {
            if (outgoing.Serialized != null)
                return outgoing.Serialized;
            outgoing.Serialized = new byte[Marshal.SizeOf(outgoing.data)];
            GCHandle h = GCHandle.Alloc(outgoing.Serialized, GCHandleType.Pinned);

            // copy the struct into int byte[] mem alloc 
            Marshal.StructureToPtr(outgoing, h.AddrOfPinnedObject(), false);

            h.Free(); //Allow GC to do its job 

            return outgoing.Serialized;
        }
    }

    public class TypedMessage<M> : IRosMessage where M : struct
    {
        public new M data;

        public new string MessageDefinition 
        {
            get
            {
                string def =  TypeHelper.MessageDefinitions[type];
                base.MessageDefinition = def;
                return def;
            }
        }

        public new MsgTypes type
        {
            get
            {
                string fullName = typeof(M).FullName;
                if (fullName != null)
                {
                    MsgTypes t = (MsgTypes)Enum.Parse(typeof(MsgTypes), fullName.Replace("Messages.","").Replace(".","__"));
                    base.type = t;
                    return t;
                }
                return MsgTypes.Unknown;
            }
        }

        public TypedMessage()
        {
        }

        public TypedMessage(M d)
        {
            data = d;
        }

        public TypedMessage(byte[] SERIALIZEDSTUFF)
        {
            Deserialize(SERIALIZEDSTUFF);
        }

        public override void Deserialize(byte[] SERIALIZEDSTUFF)
        {
            data = SerializationHelper.Deserialize<M>(SERIALIZEDSTUFF);
        }

        public override byte[] Serialize()
        {
            return SerializationHelper.Serialize(this);
        }
    }

    public class IRosMessage
    {
        public bool HasHeader;
        public bool KnownSize = true;

        public struct data
        {
        }

        public string MessageDefinition = "";

        public byte[] Serialized;
        public IDictionary connection_header;
        public MsgTypes type = MsgTypes.Unknown;

        public IRosMessage()
        {
        }

        public IRosMessage(byte[] SERIALIZEDSTUFF)
        {
            Deserialize(SERIALIZEDSTUFF);
        }

        public virtual void Deserialize(byte[] SERIALIZEDSTUFF)
        {
            throw new NotImplementedException();
        }

        public virtual byte[] Serialize()
        {
            return null;
        }
    }
}