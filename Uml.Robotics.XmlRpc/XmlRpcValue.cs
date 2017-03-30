using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Xml;

namespace Uml.Robotics.XmlRpc
{
    public class XmlRpcValue
    {
        public enum ValueType
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

        private const string VALUE_TAG = "value";
        private const string BOOLEAN_TAG = "boolean";
        private const string DOUBLE_TAG = "double";
        private const string INT_TAG = "int";
        private const string I4_TAG = "i4";
        private const string STRING_TAG = "string";
        private const string DATETIME_TAG = "dateTime.iso8601";
        private const string BASE64_TAG = "base64";
        private const string ARRAY_TAG = "array";
        private const string DATA_TAG = "data";
        private const string STRUCT_TAG = "struct";
        private const string MEMBER_TAG = "member";
        private const string NAME_TAG = "name";

        private ValueType type;
        object value;

        public XmlRpcValue()
        {
            type = ValueType.Invalid;
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
                    case ValueType.String:
                        return GetString().Length;
                    case ValueType.Base64:
                        return GetBinary().Length;
                    case ValueType.Array:
                        return GetArray().Length;
                    case ValueType.Struct:
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
            get { return type != ValueType.Invalid; }
        }

        public ValueType Type
        {
            get { return type; }
        }

        public bool IsArray
        {
            get { return type == ValueType.Array; }
        }

        public int Size
        {
            get
            {
                switch (type)
                {
                    case ValueType.Array:
                        return this.GetArray().Length;
                    case ValueType.String:
                        return this.GetString().Length;
                    case ValueType.Struct:
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
                EnsureArraySize(index);
                return Get(index);
            }
            set
            {
                EnsureArraySize(index);
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

        private void SetFromObject(int key, object value)
        {
            if (value == null)
            {
                Set(key, "");
                return;
            }

            if (value is string)
                Set(key, (string)value);
            else if (value is int)
                Set(key, (int)value);
            else if (value is double)
                Set(key, (double)value);
            else if (value is bool)
                Set(key, (bool)value);
            else
            {
                throw new XmlRpcException($"Invalid type {type} or error while parsing {value.ToString()} as {type}");
            }
        }

        public override bool Equals(object obj)
        {
            var other = obj as XmlRpcValue;

            if (other == null || type != other.type)
                return false;

            switch (type)
            {
                case ValueType.Boolean:
                case ValueType.Int:
                case ValueType.Double:
                case ValueType.String:
                case ValueType.DateTime:
                    return object.Equals(value, other.value);
                case ValueType.Base64:
                    return this.GetBinary().SequenceEqual(other.GetBinary());
                case ValueType.Array:
                    return this.GetArray().SequenceEqual(other.GetArray());
                case ValueType.Struct:
                    return this.GetStruct().SequenceEqual(other.GetStruct());
                case ValueType.Invalid:
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
                case ValueType.Base64:
                    value = other.GetBinary().Clone();
                    break;
                case ValueType.Array:
                    value = other.GetArray().Clone();
                    break;
                case ValueType.Struct:
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
            return type == ValueType.Struct && GetStruct().ContainsKey(name);
        }

        public bool FromXml(XmlNode value)
        {
            try
            {
                if (value == null)
                    return false;

                string tex = value.InnerText;
                XmlElement val;
                if ((val = value[BOOLEAN_TAG]) != null)
                {
                    int i;
                    if (!int.TryParse(tex, out i))
                        return false;
                    if (i != 0 && i != 1)
                        return false;
                    Set(i == 0);
                }
                else if ((val = value[I4_TAG]) != null)
                {
                    int i;
                    if (!int.TryParse(tex, out i))
                        return false;
                    Set(i);
                    return true;
                }
                else if ((val = value[INT_TAG]) != null)
                {
                    int i;
                    if (!int.TryParse(tex, out i))
                        return false;
                    Set(i);
                    return true;
                }
                else if ((val = value[DOUBLE_TAG]) != null)
                {
                    double d;
                    if (!double.TryParse(tex, out d))
                        return false;
                    Set(d);
                    return true;
                }
                else if ((val = value[DATETIME_TAG]) != null)
                {
                    throw new NotImplementedException();        // TODO: implement

                }
                else if ((val = value[BASE64_TAG]) != null)
                {
                    throw new NotImplementedException();         // TODO: implement
                }
                else if ((val = value[STRING_TAG]) != null)
                {
                    Set(tex);
                }
                else if ((val = value[ARRAY_TAG]) != null)
                {
                    var data = val[DATA_TAG];
                    if (data == null)
                        return false;
                    var selection = data.SelectNodes(VALUE_TAG);
                    SetArray(selection.Count);
                    for (int i = 0; i < selection.Count; i++)
                    {
                        var xmlValue = new XmlRpcValue();
                        if (!xmlValue.FromXml(selection[i]))
                            return false;
                        Set(i, xmlValue);
                    }
                }
                else if ((val = value[STRUCT_TAG]) != null)
                {
                    throw new NotImplementedException();         // TODO: implement
                }
                else
                {
                    Set(tex);
                }
            }
            catch
            {
                return false;
            }
            return true;
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
                var doc = new XmlDocument();
                ToXml(doc, doc);
                doc.WriteContentTo(writer);
            }
            return sw.ToString();
        }

        public XmlNode ToXml(XmlDocument doc, XmlNode parent)
        {
            XmlElement root = doc.CreateElement(VALUE_TAG);
            XmlElement el = null;
            switch (type)
            {
                case ValueType.Boolean:
                    el = doc.CreateElement(BOOLEAN_TAG);
                    el.AppendChild(doc.CreateTextNode(XmlConvert.ToString(GetBool() ? 1 : 0)));
                    break;
                case ValueType.Int:
                    el = doc.CreateElement(INT_TAG);
                    el.AppendChild(doc.CreateTextNode(XmlConvert.ToString(GetInt())));
                    break;
                case ValueType.Double:
                    el = doc.CreateElement(BOOLEAN_TAG);
                    el.AppendChild(doc.CreateTextNode(XmlConvert.ToString(GetDouble())));
                    break;
                case ValueType.DateTime:
                    el = doc.CreateElement(DATETIME_TAG);
                    XmlConvert.ToString(GetDateTime(), XmlDateTimeSerializationMode.RoundtripKind);
                    break;
                case ValueType.String:
                    el = doc.CreateElement(STRING_TAG);
                    el.AppendChild(doc.CreateTextNode(GetString()));
                    break;
                case ValueType.Base64:
                    el = doc.CreateElement(BASE64_TAG);
                    var base64 = Convert.ToBase64String(GetBinary());
                    el.AppendChild(doc.CreateTextNode(base64));
                    break;
                case ValueType.Array:
                    el = doc.CreateElement(ARRAY_TAG);
                    var elData = doc.CreateElement(DATA_TAG);
                    el.AppendChild(elData);
                    foreach (var x in GetArray())
                        x.ToXml(doc, elData);
                    break;
                case ValueType.Struct:
                    el = doc.CreateElement(STRUCT_TAG);
                    foreach (var record in GetStruct())
                    {
                        var member = doc.CreateElement(MEMBER_TAG);
                        var name = doc.CreateElement(NAME_TAG);
                        name.AppendChild(doc.CreateTextNode(record.Key));
                        member.AppendChild(name);
                        record.Value.ToXml(doc, member);
                        el.AppendChild(member);
                    }
                    break;
            }

            if (el != null)
                root.AppendChild(el);

            parent.AppendChild(root);
            return root;
        }

        public void Set(string value)
        {
            type = ValueType.String;
            this.value = value;
        }

        public void Set(int value)
        {
            type = ValueType.Int;
            this.value = value;
        }

        public void Set(bool value)
        {
            type = ValueType.Boolean;
            this.value = value;
        }

        public void Set(double value)
        {
            type = ValueType.Double;
            this.value = value;
        }

        public void Set(DateTime value)
        {
            type = ValueType.DateTime;
            this.value = value;
        }

        public void Set(byte[] value)
        {
            type = ValueType.Base64;
            this.value = value;
        }

        public void Set(XmlRpcValue value)
        {
            Copy(value);
        }

        public void SetArray(int elementCount)
        {
            type = ValueType.Array;
            value = new XmlRpcValue[elementCount];
        }

        public void Set(string key, string value) => this[key].Set(value);
        public void Set(string key, int value) => this[key].Set(value);
        public void Set(string key, bool value) => this[key].Set(value);
        public void Set(string key, double value) => this[key].Set(value);
        public void Set(string key, byte[] value) => this[key].Set(value);
        public void Set(string key, XmlRpcValue value) => this[key].Set(value);

        public void Set(int index, string value) => this[index].Set(value);
        public void Set(int index, int value) => this[index].Set(value);
        public void Set(int index, bool value) => this[index].Set(value);
        public void Set(int index, double value) => this[index].Set(value);
        public void Ste(int index, byte[] value) => this[index].Set(value);
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
            if (type != ValueType.Invalid && type != ValueType.Array)
                throw new XmlRpcException($"Cannot convert {type} to array");

            int before = 0;
            var array = value as XmlRpcValue[];
            if (array == null)
            {
                array = new XmlRpcValue[size + 1];
            }
            else
            {
                before = array.Length;
                if (array.Length < size + 1)
                {
                    Array.Resize(ref array, size + 1);
                }
            }

            for (int i = before; i < array.Length; i++)
                array[i] = new XmlRpcValue();

            value = array;
            type = ValueType.Array;
        }

        private XmlRpcValue Get(int index) => this.GetArray()[index];

        private XmlRpcValue Get(string key)
        {
            var s = this.GetStruct();
            return s.ContainsKey(key) ? s[key] : null;
        }
    }
}