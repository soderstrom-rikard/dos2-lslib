﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSLib.LS.Osiris
{
    public class Adapter : OsirisSerializable
    {
        /// <summary>
        /// Unique identifier of this adapter
        /// </summary>
        public UInt32 Index;
        /// <summary>
        /// Constant output values
        /// </summary>
        public Tuple Constants;
        /// <summary>
        /// Contains input logical column indices for each output physical column.
        /// A -1 means that the output column is a constant or null value; otherwise
        /// the output column maps to the specified logical index from the input tuple.
        /// </summary>
        public List<sbyte> LogicalIndices;
        /// <summary>
        /// Logical index => physical index map of the output tuple
        /// </summary>
        public Dictionary<byte, byte> LogicalToPhysicalMap;
        /// <summary>
        /// Node that we're attached to
        /// </summary>
        public Node OwnerNode;

        public void Read(OsiReader reader)
        {
            Index = reader.ReadUInt32();
            Constants = new Tuple();
            Constants.Read(reader);

            LogicalIndices = new List<sbyte>();
            var count = reader.ReadByte();
            while (count-- > 0)
            {
                LogicalIndices.Add(reader.ReadSByte());
            }

            LogicalToPhysicalMap = new Dictionary<byte, byte>();
            count = reader.ReadByte();
            while (count-- > 0)
            {
                var key = reader.ReadByte();
                var value = reader.ReadByte();
                LogicalToPhysicalMap.Add(key, value);
            }
        }

        public void Write(OsiWriter writer)
        {
            writer.Write(Index);
            Constants.Write(writer);

            writer.Write((byte)LogicalIndices.Count);
            foreach (var index in LogicalIndices)
            {
                writer.Write(index);
            }

            writer.Write((byte)LogicalToPhysicalMap.Count);
            foreach (var pair in LogicalToPhysicalMap)
            {
                writer.Write(pair.Key);
                writer.Write(pair.Value);
            }
        }

        public Tuple Adapt(Tuple columns)
        {
            var result = new Tuple();
            for (var i = 0; i < LogicalIndices.Count; i++)
            {
                var index = LogicalIndices[i];
                // If a logical index is present, emit a column from the input tuple
                if (index != -1)
                {
                    var value = columns.Logical[index];
                    result.Physical.Add(value);
                }
                // Otherwise check if a constant is mapped to the specified logical index
                else if (Constants.Logical.ContainsKey(i))
                {
                    var value = Constants.Logical[i];
                    result.Physical.Add(value);
                }
                // If we haven't found a constant, emit a null variable
                else
                {
                    var nullValue = new Variable();
                    nullValue.TypeId = (uint)Value.Type.Unknown;
                    nullValue.Unused = true;
                    result.Physical.Add(nullValue);
                }
            }

            // Generate logical => physical mappings for the output tuple
            foreach (var map in LogicalToPhysicalMap)
            {
                result.Logical.Add(map.Key, result.Physical[map.Value]);
            }

            return result;
        }

        public void DebugDump(TextWriter writer, Story story)
        {
            writer.Write("Adapter - ");
            if (OwnerNode != null && OwnerNode.Name.Length > 0)
            {
                writer.WriteLine("Node {0}({1})", OwnerNode.Name, OwnerNode.NumParams);
            }
            else if (OwnerNode != null)
            {
                writer.WriteLine("Node <{0}>", OwnerNode.TypeName());
            }
            else
            {
                writer.WriteLine("(Not owned)");
            }

            if (Constants.Logical.Count > 0)
            {
                writer.Write("    Constants: ");
                Constants.DebugDump(writer, story);
                writer.WriteLine("");
            }

            if (LogicalIndices.Count > 0)
            {
                writer.Write("    Logical indices: ");
                foreach (var index in LogicalIndices)
                {
                    writer.Write("{0}, ", index);
                }
                writer.WriteLine("");
            }

            if (LogicalToPhysicalMap.Count > 0)
            {
                writer.Write("    Logical to physical mappings: ");
                foreach (var pair in LogicalToPhysicalMap)
                {
                    writer.Write("{0} -> {1}, ", pair.Key, pair.Value);
                }
                writer.WriteLine("");
            }
        }
    }
}
