using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Xml.Linq;
using System.Xml;

namespace Uml.Robotics.XmlRpc
{
    public enum XmlRpcType
    {
        Invalid,
        Boolean,
        Int,
        Double,
        String,
        DateTime,
        Base64,
        Array,
        Struct
    }

    public class XmlRpcValue
    {
        private static readonly XName VALUE_TAG = "value";
        private static readonly XName BOOLEAN_TAG = "boolean";
        private static readonly XName DOUBLE_TAG = "double";
        private static readonly XName INT_TAG = "int";
        private static readonly XName I4_TAG = "i4";
        private static readonly XName STRING_TAG = "string";
        private static readonly XName DATETIME_TAG = "dateTime.iso8601";
        private static readonly XName BASE64_TAG = "base64";
        private static readonly XName ARRAY_TAG = "array";
        private static readonly XName DATA_TAG = "data";
        private static readonly XName STRUCT_TAG = "struct";
        private static readonly XName MEMBER_TAG = "member";
        private static readonly XName NAME_TAG = "name";

        private XmlRpcType type;
        object value;

        public XmlRpcValue()
        {
            type = XmlRpcType.Invalid;
        }

        public XmlRpcValue(params Object[] initialvalues)
            : this()
        {
            SetArray(initialvalues.Length);
            for (int i = 0; i < initialvalues.Length; i++)
            {
                SetFromObject(i, initialvalues[i]);
            }
        }

        public XmlRpcValue(bool value)
        {
            Set(value);
        }

        public XmlRpcValue(int value)
        {
            Set(value);
        }

        public XmlRpcValue(double value)
        {
            Set(value);
        }

        public XmlRpcValue(string value)
        {
            Set(value);
        }

        public int Length
        {
            get
            {
                switch (type)
                {
                    case XmlRpcType.String:
                        return GetString().Length;
                    case XmlRpcType.Base64:
                        return GetBinary().Length;
                    case XmlRpcType.Array:
                        return GetArray().Length;
                    case XmlRpcType.Struct:
                        return GetStruct().Count;
                    default:
                        XmlRpcUtil.log(XmlRpcUtil.XMLRPC_LOG_LEVEL.DEBUG, "Trying to get size of value without size (type:{0})", type);
                        throw new XmlRpcException(
                            $"Invalid or unkown type: {type}. Expected String, Base64, Array or Struct"
                        );
                }
            }
        }

        public bool IsValid
        {
            get { return type != XmlRpcType.Invalid; }
        }

        public XmlRpcType Type
        {
            get { return type; }
        }

        public bool IsArray
        {
            get { return type == XmlRpcType.Array; }
        }

        public int Size
        {
            get
            {
                switch (type)
                {
                    case XmlRpcType.Array:
                        return this.GetArray().Length;
                    case XmlRpcType.String:
                        return this.GetString().Length;
                    case XmlRpcType.Struct:
                        return this.GetStruct().Count;
                    default:
                        return 0;
                }
            }
        }

        public XmlRpcValue this[int index]
        {
            get
            {
                EnsureArraySize(index + 1);
                return Get(index);
            }
            set
            {
                EnsureArraySize(index + 1);
                var array = this.GetArray();
                if (array[index] == null)
                {
                    array[index] = new XmlRpcValue();
                }
                array[index].Set(value);
            }
        }

        public XmlRpcValue this[string key]
        {
            get { return Get(key); }
            set { Set(key, value); }
        }

        private void SetFromObject(int index, object value)
        {
            if (value == null)
                Set(index, string.Empty);
            else if (value is string)
                Set(index, (string)value);
            else if (value is int)
                Set(index, (int)value);
            else if (value is double)
                Set(index, (double)value);
            else if (value is bool)
                Set(index, (bool)value);
            else
                throw new XmlRpcException($"Invalid type {type} or error while parsing {value.ToString()} as {type}");
        }

        public override bool Equals(object obj)
        {
            var other = obj as XmlRpcValue;

            if (other == null || type != other.type)
                return false;

            switch (type)
            {
                case XmlRpcType.Boolean:
                case XmlRpcType.Int:
                case XmlRpcType.Double:
                case XmlRpcType.String:
                case XmlRpcType.DateTime:
                    return object.Equals(value, other.value);
                case XmlRpcType.Base64:
                    return this.GetBinary().SequenceEqual(other.GetBinary());
                case XmlRpcType.Array:
                    return this.GetArray().SequenceEqual(other.GetArray());
                case XmlRpcType.Struct:
                    return this.GetStruct().SequenceEqual(other.GetStruct());
                case XmlRpcType.Invalid:
                    return true;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return value != null ? value.GetHashCode() : base.GetHashCode();
        }

        public void Copy(XmlRpcValue other)
        {
            switch (other.type)
            {
                case XmlRpcType.Base64:
                    value = other.GetBinary().Clone();
                    break;
                case XmlRpcType.Array:
                    value = other.GetArray().Clone();
                    break;
                case XmlRpcType.Struct:
                    value = new Dictionary<string, XmlRpcValue>(other.GetStruct());
                    break;
                default:
                    value = other.value;
                    break;
            }

            type = other.type;
        }

        public bool HasMember(string name)
        {
            return type == XmlRpcType.Struct && GetStruct().ContainsKey(name);
        }

        public void FromXml(string xml)
        {
            FromXElement(XElement.Parse(xml));
        }

        public void FromXElement(XElement valueElement)
        {
            if (valueElement == null)
                throw new ArgumentNullException(nameof(valueElement), "Value element must not be null.");

            var content = valueElement.Elements().FirstOrDefault();
            if (content == null)
            {
                Set(valueElement.Value);
            }
            else if (content.Name == BOOLEAN_TAG)
            {
                int x = (int)content;
                if (x == 0)
                    Set(false);
                else if (x == 1)
                    Set(true);
                else
                    throw new XmlRpcException("XML-RPC boolean value must be '0' or '1'.");
            }
            else if (content.Name == I4_TAG || content.Name == INT_TAG)
            {
                Set((int)content);
            }
            else if (content.Name == DOUBLE_TAG)
            {
                Set((double)content);
            }
            else if (content.Name == DATETIME_TAG)
            {
                Set(XmlConvert.ToDateTime(content.Value, XmlDateTimeSerializationMode.RoundtripKind));
            }
            else if (content.Name == BASE64_TAG)
            {
                Set(Convert.FromBase64String(content.Value));
            }
            else if (content.Name == STRING_TAG)
            {
                Set(valueElement.Value);
            }
            else if (content.Name == ARRAY_TAG)
            {
                var dataElement = content.Element(DATA_TAG);
                if (dataElement == null)
                    throw new XmlRpcException("Expected <data> element is missing.");
                var valueElements = dataElement.Elements(VALUE_TAG).ToList();
                SetArray(valueElements.Count);
                for (int i = 0; i < valueElements.Count; i++)
                {
                    var v = new XmlRpcValue();
                    v.FromXElement(valueElements[i]);
                    Set(i, v);
                }
            }
            else if (content.Name == STRUCT_TAG)
            {
                foreach (var memberElement in content.Elements(MEMBER_TAG))
                {
                    var nameElement = memberElement.Element(NAME_TAG);
                    if (nameElement == null)
                        throw new XmlRpcException("Expected <name> element is missing.");
                    var name = nameElement.Value;
                    var v = new XmlRpcValue();
                    v.FromXElement(memberElement.Element(VALUE_TAG));
                    Set(name, v);
                }
            }
            else
            {
                Set(valueElement.Value);
            }
        }

        public string ToXml()
        {
            var settings = new XmlWriterSettings()
            {
                OmitXmlDeclaration = true,
                ConformanceLevel = ConformanceLevel.Fragment,
                CloseOutput = false
            };

            var sw = new StringWriter();
            using (var writer = XmlWriter.Create(sw, settings))
            {
                var valueElement = this.ToXElement();
                valueElement.WriteTo(writer);
            }
            return sw.ToString();
        }

        public XElement ToXElement()
        {
            var valueElement = new XElement(VALUE_TAG);
            switch (type)
            {
                case XmlRpcType.Boolean:
                    valueElement.Add(new XElement(BOOLEAN_TAG, GetBool() ? 1 : 0));
                    break;
                case XmlRpcType.Int:
                    valueElement.Add(new XElement(INT_TAG, GetInt()));
                    break;
                case XmlRpcType.Double:
                    valueElement.Add(new XElement(DOUBLE_TAG, GetDouble()));
                    break;
                case XmlRpcType.DateTime:
                    valueElement.Add(new XElement(DATETIME_TAG, XmlConvert.ToString(GetDateTime(), XmlDateTimeSerializationMode.RoundtripKind)));
                    break;
                case XmlRpcType.String:
                    valueElement.Add(new XElement(STRING_TAG, GetString()));
                    break;
                case XmlRpcType.Base64:
                    valueElement.Add(new XElement(BASE64_TAG, Convert.ToBase64String(GetBinary())));
                    break;
                case XmlRpcType.Array:
                    valueElement.Add(new XElement(ARRAY_TAG, new XElement(DATA_TAG, GetArray().Select(x => x.ToXElement()))));
                    break;
                case XmlRpcType.Struct:
                    valueElement.Add(
                        new XElement(STRUCT_TAG,
                            GetStruct()
                            .Select(x => new XElement(MEMBER_TAG,
                                new XElement(NAME_TAG, x.Key), x.Value.ToXElement())
                            )
                        )
                    );
                    break;
                default:
                    throw new XmlRpcException($"Cannot serialize XmlRpcValue type '${type}'.");
            }

            return valueElement;
        }

        public void Set(string value)
        {
            type = XmlRpcType.String;
            this.value = value;
        }

        public void Set(int value)
        {
            type = XmlRpcType.Int;
            this.value = value;
        }

        public void Set(bool value)
        {
            type = XmlRpcType.Boolean;
            this.value = value;
        }

        public void Set(double value)
        {
            type = XmlRpcType.Double;
            this.value = value;
        }

        public void Set(DateTime value)
        {
            type = XmlRpcType.DateTime;
            this.value = value;
        }

        public void Set(byte[] value)
        {
            type = XmlRpcType.Base64;
            this.value = value;
        }

        public void Set(XmlRpcValue value)
        {
            Copy(value);
        }

        public void SetArray(int elementCount)
        {
            type = XmlRpcType.Array;
            EnsureArraySize(elementCount);
        }

        public void Set(string name, string value) => this[name].Set(value);
        public void Set(string name, int value) => this[name].Set(value);
        public void Set(string name, bool value) => this[name].Set(value);
        public void Set(string name, double value) => this[name].Set(value);
        public void Set(string name, byte[] value) => this[name].Set(value);
        public void Set(string name, XmlRpcValue value) => this[name].Set(value);

        public void Set(int index, string value) => this[index].Set(value);
        public void Set(int index, int value) => this[index].Set(value);
        public void Set(int index, bool value) => this[index].Set(value);
        public void Set(int index, double value) => this[index].Set(value);
        public void Set(int index, byte[] value) => this[index].Set(value);
        public void Set(int index, XmlRpcValue value) => this[index].Set(value);

        public IDictionary<string, XmlRpcValue> GetStruct() => (IDictionary<string, XmlRpcValue>)value;
        public XmlRpcValue[] GetArray() => (XmlRpcValue[])value;
        public int GetInt() => (int)value;
        public string GetString() => (string)value;
        public bool GetBool() => (bool)value;
        public double GetDouble() => (double)value;
        public DateTime GetDateTime() => (DateTime)value;
        public byte[] GetBinary() => (byte[])value;

        public override string ToString()
        {
            if (!this.IsValid)
                return "INVALID";
            return ToXml();
        }

        private void EnsureArraySize(int size)
        {
            if (type != XmlRpcType.Invalid && type != XmlRpcType.Array)
                throw new XmlRpcException($"Cannot convert {type} to array");

            int before = 0;
            var array = value as XmlRpcValue[];
            if (array == null)
            {
                array = new XmlRpcValue[size];
            }
            else
            {
                before = array.Length;
                if (array.Length < size)
                {
                    Array.Resize(ref array, size);
                }
            }

            for (int i = before; i < array.Length; i++)
                array[i] = new XmlRpcValue();

            value = array;
            type = XmlRpcType.Array;
        }

        private XmlRpcValue Get(int index) => this.GetArray()[index];

        private XmlRpcValue Get(string key)
        {
            var s = this.GetStruct();
            return s.ContainsKey(key) ? s[key] : null;
        }
    }
}