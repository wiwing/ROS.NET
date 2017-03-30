using System;
using System.Collections.Generic;
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
            Struct,
            IDFK
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


        // Type tag and values
        private ValueType _type;
        public XmlRpcValue[] asArray;
        public byte[] asBinary;

        public bool asBool;
        public double asDouble;
        public int asInt;
        public string asString;
        public Dictionary<string, XmlRpcValue> asStruct;
        public tm asTime;

        public XmlRpcValue()
        {
            _type = ValueType.Invalid;
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
            asBool = value;
            _type = ValueType.Boolean;
        }

        public XmlRpcValue(int value)
        {
            asInt = value;
            _type = ValueType.Int;
        }

        public XmlRpcValue(double value)
        {
            asDouble = value;
            _type = ValueType.Double;
        }

        public XmlRpcValue(string value)
        {
            asString = value;
            _type = ValueType.String;
        }

        public int Length
        {
            get
            {
                switch (_type)
                {
                    case ValueType.String:
                        return asString.Length;
                    case ValueType.Base64:
                        return asBinary.Length;
                    case ValueType.Array:
                        return asArray.Length;
                    case ValueType.Struct:
                        return asStruct.Count;
                    default:
                        break;
                }

                XmlRpcUtil.log(XmlRpcUtil.XMLRPC_LOG_LEVEL.DEBUG, "Trying to get size of something without a size! -- type={0}", _type);
                throw new XmlRpcException($"Invalid or unkown type: {_type}. Expected {ValueType.String}, " +
                    $"{ValueType.Base64}, {ValueType.Array} or {ValueType.Struct}");
            }
        }

        public bool IsValid
        {
            get { return _type != ValueType.Invalid; }
        }

        public ValueType Type
        {
            get { return _type; }
        }

        public int Size
        {
            get
            {
                if (!IsValid || Type == ValueType.Invalid || Type == ValueType.IDFK)
                {
                    return 0;
                }
                if (Type != ValueType.String && Type != ValueType.Struct && Type != ValueType.Array)
                    return 0;
                if (Type == ValueType.Array)
                    return asArray.Length;
                if (Type == ValueType.String)
                    return asString.Length;
                if (Type == ValueType.Struct)
                    return asStruct.Count;
                return 0;
            }
        }

        public XmlRpcValue this[int key]
        {
            get
            {
                EnsureArraySize(key);
                return Get(key);
            }
            set
            {
                EnsureArraySize(key);
                Set(key, value);
            }
        }

        public XmlRpcValue this[string key]
        {
            get { return Get(key); }
            set { Set(key, value); }
        }

        private void SetFromObject(int key, object value)
        {
            int parsedInt = 0;
            double parsedDouble = 0;
            bool parsedBool = false;
            if (value == null)
            {
                Set(key, "");
                return;
            }
            Type type = value.GetType();
            if (type.Equals(typeof (String)))
                Set(key, value != null ? value.ToString() : "");
            else if (type.Equals(typeof (Int32)) && int.TryParse(value.ToString(), out parsedInt))
                Set(key, parsedInt);
            else if (type.Equals(typeof (Double)) && double.TryParse(value.ToString(), out parsedDouble))
                Set(key, parsedDouble);
            else if (type.Equals(typeof (Boolean)) && bool.TryParse(value.ToString(), out parsedBool))
                Set(key, parsedBool);
            else
            {
                throw new XmlRpcException($"Invalid type {type} or error while parsing {value.ToString()} as {type}");
            }
        }

        private void AssertArray(int size)
        {
            if (_type == ValueType.Invalid)
            {
                _type = ValueType.Array;
                asArray = new XmlRpcValue[size];
            }
            else if (_type == ValueType.Array)
            {
                if (asArray.Length < size)
                    Array.Resize(ref asArray, size);
            }
            else
                throw new XmlRpcException("type error: expected an array");
        }

        private void AssertStruct()
        {
            if (_type == ValueType.Invalid)
            {
                _type = ValueType.Struct;
                asStruct = new Dictionary<string, XmlRpcValue>();
            }
            else if (_type != ValueType.Struct)
                throw new XmlRpcException("type error: expected a struct");
        }

        // Predicate for tm equality
        private static bool tmEq(tm t1, tm t2)
        {
            return t1.tm_sec == t2.tm_sec && t1.tm_min == t2.tm_min &&
                   t1.tm_hour == t2.tm_hour && t1.tm_mday == t2.tm_mday &&
                   t1.tm_mon == t2.tm_mon && t1.tm_year == t2.tm_year;
        }

        public override bool Equals(object obj)
        {
            XmlRpcValue other = (XmlRpcValue) obj;

            if (_type != other._type)
                return false;

            switch (_type)
            {
                case ValueType.Boolean:
                    return asBool == other.asBool;
                case ValueType.Int:
                    return asInt == other.asInt;
                case ValueType.Double:
                    return asDouble == other.asDouble;
                case ValueType.DateTime:
                    return tmEq(asTime, other.asTime);
                case ValueType.String:
                    return asString.Equals(other.asString);
                case ValueType.Base64:
                    return asBinary == other.asBinary;
                case ValueType.Array:
                    return asArray == other.asArray;

                    // The map<>::operator== requires the definition of value< for kcc
                case ValueType.Struct: //return *_value.asStruct == *other._value.asStruct;
                {
                    if (asStruct.Count != other.asStruct.Count)
                        return false;
                    var aenum = asStruct.GetEnumerator();
                    var benum = other.asStruct.GetEnumerator();

                    while (aenum.MoveNext() && benum.MoveNext())
                    {
                        if (!aenum.Current.Value.Equals(benum.Current.Value))
                            return false;
                    }
                    return true;
                }
                default:
                    break;
            }
            return true; // Both invalid values ...
        }

        // Works for strings, binary data, arrays, and structs.

        public void Copy(XmlRpcValue other)
        {
            switch (other._type)
            {
                case ValueType.Boolean:
                    asBool = other.asBool;
                    break;
                case ValueType.Int:
                    asInt = other.asInt;
                    break;
                case ValueType.Double:
                    asDouble = other.asDouble;
                    break;
                case ValueType.DateTime:
                    asTime = other.asTime;
                    break;
                case ValueType.String:
                    asString = other.asString;
                    break;
                case ValueType.Base64:
                    asBinary = other.asBinary;
                    break;
                case ValueType.Array:
                    asArray = other.asArray;
                    break;

                    // The map<>::operator== requires the definition of value< for kcc
                case ValueType.Struct: //return *_value.asStruct == *other._value.asStruct;
                    asStruct = other.asStruct;
                    break;
            }
            _type = other._type;
        }

        // Checks for existence of struct member
        public bool hasMember(string name)
        {
            return _type == ValueType.Struct && asStruct.ContainsKey(name);
        }

        private void parseString(XmlNode node)
        {
            _type = ValueType.String;
            asString = node.InnerText;
        }

        public bool fromXml(XmlNode value)
        {
            //int val = offset;
            //offset = 0;
            try
            {
                //XmlElement value = node["value"];
                if (value == null)
                    return false;

                string tex = value.InnerText;
                XmlElement val;
                if ((val = value[BOOLEAN_TAG]) != null)
                {
                    _type = ValueType.Boolean;
                    int tmp = 0;
                    if (!int.TryParse(tex, out tmp))
                        return false;
                    if (tmp != 0 && tmp != 1)
                        return false;
                    asBool = (tmp == 0 ? false : true);
                }
                else if ((val = value[I4_TAG]) != null)
                {
                    _type = ValueType.Int;
                    return int.TryParse(tex, out asInt);
                }
                else if ((val = value[INT_TAG]) != null)
                {
                    _type = ValueType.Int;
                    return int.TryParse(tex, out asInt);
                }
                else if ((val = value[DOUBLE_TAG]) != null)
                {
                    _type = ValueType.Double;
                    return double.TryParse(tex, out asDouble);
                }
                else if ((val = value[DATETIME_TAG]) != null)
                {
                    // TODO: implement
                }
                else if ((val = value[BASE64_TAG]) != null)
                {
                    // TODO: implement
                }
                else if ((val = value[STRING_TAG]) != null)
                {
                    _type = ValueType.String;
                    asString = tex;
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
                        if (!xmlValue.fromXml(selection[i]))
                            return false;
                        asArray[i] = xmlValue;
                    }
                }
                else if ((val = value[STRUCT_TAG]) != null)
                {
                    // TODO: implement
                }
                else
                {
                    _type = ValueType.String;
                    asString = tex;
                }
            }
            catch
            {
                return false;
            }
            return true;
        }

        public string toXml()
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
                toXml(doc, doc);
                doc.WriteContentTo(writer);
            }
            string result = sw.ToString();
            return result;
        }

        public XmlNode toXml(XmlDocument doc, XmlNode parent)
        {
            XmlElement root = doc.CreateElement(VALUE_TAG);
            XmlElement el = null;
            switch (_type)
            {
                case ValueType.Boolean:
                    el = doc.CreateElement(BOOLEAN_TAG);
                    el.AppendChild(doc.CreateTextNode(asBool.ToString()));
                    break;
                case ValueType.Int:
                    el = doc.CreateElement(INT_TAG);
                    el.AppendChild(doc.CreateTextNode(asInt.ToString()));
                    break;
                case ValueType.Double:
                    el = doc.CreateElement(BOOLEAN_TAG);
                    el.AppendChild(doc.CreateTextNode(asDouble.ToString()));
                    break;
                case ValueType.DateTime:
                    el = doc.CreateElement(DATETIME_TAG);
                    el.AppendChild(doc.CreateTextNode(asTime.ToString()));
                    break;
                case ValueType.String:
                    //asString = other.asString;
                    el = doc.CreateElement(STRING_TAG);
                    el.AppendChild(doc.CreateTextNode(asString));
                    break;
                case ValueType.Base64:
                    //asBinary = other.asBinary;
                    el = doc.CreateElement(BASE64_TAG);
                    var base64 = Convert.ToBase64String(asBinary);
                    el.AppendChild(doc.CreateTextNode(base64));
                    break;
                case ValueType.Array:
                    el = doc.CreateElement(ARRAY_TAG);
                    var elData = doc.CreateElement(DATA_TAG);
                    el.AppendChild(elData);
                    for (int i = 0; i < Size; i++)
                    {
                        asArray[i].toXml(doc, elData);
                    }
                    break;
                case ValueType.Struct:
                    el = doc.CreateElement(STRUCT_TAG);
                    foreach (var record in asStruct)
                    {
                        var member = doc.CreateElement(MEMBER_TAG);
                        var name = doc.CreateElement(NAME_TAG);
                        name.AppendChild(doc.CreateTextNode(record.Key));
                        member.AppendChild(name);
                        record.Value.toXml(doc, member);
                        el.AppendChild(member);
                    }
                    break;
            }

            if (el != null)
                root.AppendChild(el);

            parent.AppendChild(root);
            return root;
        }

        public void Set<T>(T t)
        {
            Type type = t.GetType();
            if (type.Equals(typeof (String)))
            {
                _type = ValueType.String;
                asString = (string) (object) t;
            }
            else if (type.Equals(typeof (Int32)))
            {
                _type = ValueType.Int;
                asInt = (int) (object) t;
            }
            else if (type.Equals(typeof (XmlRpcValue)))
            {
                Copy(t as XmlRpcValue);
            }
            else if (type.Equals(typeof (Boolean)))
            {
                asBool = (bool) (object) t;
                _type = ValueType.Boolean;
            }
            else if (type.Equals(typeof (Double)))
            {
                asDouble = (double) (object) t;
                _type = ValueType.Double;
            }
            else
            {
                throw new XmlRpcException($"Invalid type {type}. Expected types are String, Int32, XmlRpcValue, Bool and Double");
            }
        }

        public void EnsureArraySize(int size)
        {
            if (_type != ValueType.Invalid && _type != ValueType.Array)
                throw new XmlRpcException($"Cannot convert {_type} to array");
            int before = 0;
            if (asArray != null)
            {
                before = asArray.Length;
                if (asArray.Length < size + 1)
                    Array.Resize(ref asArray, size + 1);
            }
            else
                asArray = new XmlRpcValue[size + 1];
            for (int i = before; i < asArray.Length; i++)
                asArray[i] = new XmlRpcValue();
            _type = ValueType.Array;
        }

        public void Set<T>(int key, T t)
        {
            EnsureArraySize(key);
            if (asArray[key] == null)
            {
                asArray[key] = new XmlRpcValue();
            }
            this[key].Set(t);
        }

        public void SetArray(int maxSize)
        {
            _type = ValueType.Array;
            asArray = new XmlRpcValue[maxSize];
        }

        public void Set<T>(string key, T t)
        {
            this[key].Set(t);
        }

        public T Get<T>()
        {
            if (!IsValid)
            {
                XmlRpcUtil.log(XmlRpcUtil.XMLRPC_LOG_LEVEL.WARNING, "Trying to Get() the value of an Invalid XmlRpcValue!");
                return (T) (object) null;
            }
            Type type = typeof (T);
            if (type.Equals(typeof (String)))
            {
                return (T) (object) asString;
            }
            if (type.Equals(typeof (Int32)))
            {
                return (T) (object) asInt;
            }
            if (type.Equals(typeof (Boolean)))
            {
                return (T) (object) asBool;
            }
            if (type.Equals(typeof (Double)))
            {
                return (T) (object) asDouble;
            }
            if (type.Equals(typeof (XmlRpcValue)))
            {
                return (T) (object) asArray;
            }
            throw new Exception($"Trying to Get {type.FullName} from: {ToString()}");
        }

        private T Get<T>(int key)
        {
            return this[key].Get<T>();
        }

        private T Get<T>(string key)
        {
            return this[key].Get<T>();
        }

        private XmlRpcValue Get(int key)
        {
            return asArray[key];
        }

        private XmlRpcValue Get(string key)
        {
            if (asStruct.ContainsKey(key))
                return asStruct[key];
            return null;
        }

        public int GetInt()
        {
            return asInt;
        }

        public string GetString()
        {
            return asString;
        }

        public bool GetBool()
        {
            return asBool;
        }

        public double GetDouble()
        {
            return asDouble;
        }

        public override string ToString()
        {
            if (!this.IsValid)
                return "INVALID";
            return toXml();
        }

        public class tm
        {
            public int tm_hour; /* hours since midnight - [0,23] */
            public int tm_isdst; /* daylight savings time flag */
            public int tm_mday; /* day of the month - [1,31] */
            public int tm_min; /* minutes after the hour - [0,59] */
            public int tm_mon; /* months since January - [0,11] */
            public int tm_sec; /* seconds after the minute - [0,59] */
            public int tm_wday; /* days since Sunday - [0,6] */
            public int tm_yday; /* days since January 1 - [0,365] */
            public int tm_year; /* years since 1900 */
        };
    }
}