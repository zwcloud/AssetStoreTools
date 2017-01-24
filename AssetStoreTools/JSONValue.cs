using System;
using System.Collections.Generic;

internal struct JSONValue
{
	private object data;

	public JSONValue this[string index]
	{
		get
		{
			Dictionary<string, JSONValue> dictionary = this.AsDict(false);
			return dictionary[index];
		}
		set
		{
			if (this.data == null)
			{
				this.data = new Dictionary<string, JSONValue>();
			}
			Dictionary<string, JSONValue> dictionary = this.AsDict(false);
			dictionary[index] = value;
		}
	}

	public JSONValue(object o)
	{
		this.data = o;
	}

	public bool IsString()
	{
		return this.data is string;
	}

	public bool IsFloat()
	{
		return this.data is float;
	}

	public bool IsList()
	{
		return this.data is List<JSONValue>;
	}

	public bool IsDict()
	{
		return this.data is Dictionary<string, JSONValue>;
	}

	public bool IsBool()
	{
		return this.data is bool;
	}

	public bool IsNull()
	{
		return this.data == null;
	}

	public string AsString(bool nothrow = false)
	{
		if (this.data is string)
		{
			return (string)this.data;
		}
		if (!nothrow)
		{
			throw new JSONTypeException("Tried to read non-string json value as string");
		}
		return string.Empty;
	}

	public float AsFloat(bool nothrow = false)
	{
		if (this.data is float)
		{
			return (float)this.data;
		}
		if (!nothrow)
		{
			throw new JSONTypeException("Tried to read non-float json value as float");
		}
		return 0f;
	}

	public bool AsBool(bool nothrow = false)
	{
		if (this.data is bool)
		{
			return (bool)this.data;
		}
		if (!nothrow)
		{
			throw new JSONTypeException("Tried to read non-bool json value as bool");
		}
		return false;
	}

	public List<JSONValue> AsList(bool nothrow = false)
	{
		if (this.data is List<JSONValue>)
		{
			return (List<JSONValue>)this.data;
		}
		if (!nothrow)
		{
			throw new JSONTypeException("Tried to read " + this.data.GetType().Name + " json value as list");
		}
		return null;
	}

	public Dictionary<string, JSONValue> AsDict(bool nothrow = false)
	{
		if (this.data is Dictionary<string, JSONValue>)
		{
			return (Dictionary<string, JSONValue>)this.data;
		}
		if (!nothrow)
		{
			throw new JSONTypeException("Tried to read non-dictionary json value as dictionary");
		}
		return null;
	}

	public static JSONValue NewString(string val)
	{
		return new JSONValue(val);
	}

	public static JSONValue NewFloat(float val)
	{
		return new JSONValue(val);
	}

	public static JSONValue NewDict()
	{
		return new JSONValue(new Dictionary<string, JSONValue>());
	}

	public static JSONValue NewList()
	{
		return new JSONValue(new List<JSONValue>());
	}

	public static JSONValue NewBool(bool val)
	{
		return new JSONValue(val);
	}

	public static JSONValue NewNull()
	{
		return new JSONValue(null);
	}

	public JSONValue InitList()
	{
		this.data = new List<JSONValue>();
		return this;
	}

	public JSONValue InitDict()
	{
		this.data = new Dictionary<string, JSONValue>();
		return this;
	}

	public bool ContainsKey(string index)
	{
		return this.IsDict() && this.AsDict(false).ContainsKey(index);
	}

	public JSONValue Get(string key, out bool found)
	{
		found = false;
		if (!this.IsDict())
		{
			return new JSONValue(null);
		}
		JSONValue result = this;
		string[] array = key.Split(new char[]
		{
			'.'
		});
		for (int i = 0; i < array.Length; i++)
		{
			string index = array[i];
			if (!result.ContainsKey(index))
			{
				return new JSONValue(null);
			}
			result = result[index];
		}
		found = true;
		return result;
	}

	public JSONValue Get(string key)
	{
		bool flag;
		return this.Get(key, out flag);
	}

	public bool Copy(string key, ref string dest)
	{
		return this.Copy(key, ref dest, true);
	}

	public bool Copy(string key, ref string dest, bool allowCopyNull)
	{
		bool flag;
		JSONValue jSONValue = this.Get(key, out flag);
		if (flag && (!jSONValue.IsNull() || allowCopyNull))
		{
			dest = ((!jSONValue.IsNull()) ? jSONValue.AsString(false) : null);
		}
		return flag;
	}

	public bool Copy(string key, ref bool dest)
	{
		bool flag;
		JSONValue jSONValue = this.Get(key, out flag);
		if (flag && !jSONValue.IsNull())
		{
			dest = jSONValue.AsBool(false);
		}
		return flag;
	}

	public bool Copy(string key, ref int dest)
	{
		bool flag;
		JSONValue jSONValue = this.Get(key, out flag);
		if (flag && !jSONValue.IsNull())
		{
			dest = (int)jSONValue.AsFloat(false);
		}
		return flag;
	}

	public void Set(string key, string value)
	{
		this.Set(key, value, true);
	}

	public void Set(string key, string value, bool allowNull)
	{
		if (value != null)
		{
			this[key] = JSONValue.NewString(value);
			return;
		}
		if (!allowNull)
		{
			return;
		}
		this[key] = JSONValue.NewNull();
	}

	public void Set(string key, float value)
	{
		this[key] = JSONValue.NewFloat(value);
	}

	public void Set(string key, bool value)
	{
		this[key] = JSONValue.NewBool(value);
	}

	public void Add(string value)
	{
		List<JSONValue> list = this.AsList(false);
		if (value == null)
		{
			list.Add(JSONValue.NewNull());
			return;
		}
		list.Add(JSONValue.NewString(value));
	}

	public void Add(float value)
	{
		List<JSONValue> list = this.AsList(false);
		list.Add(JSONValue.NewFloat(value));
	}

	public void Add(bool value)
	{
		List<JSONValue> list = this.AsList(false);
		list.Add(JSONValue.NewBool(value));
	}

	public override string ToString()
	{
		return this.ToString(null, string.Empty);
	}

	public string ToString(string curIndent, string indent)
	{
		bool flag = curIndent != null;
		if (this.IsString())
		{
			return "\"" + JSONValue.EncodeString(this.AsString(false)) + "\"";
		}
		if (this.IsFloat())
		{
			return this.AsFloat(false).ToString();
		}
		if (this.IsList())
		{
			string str = "[";
			string str2 = string.Empty;
			foreach (JSONValue current in this.AsList(false))
			{
				str = str + str2 + current.ToString();
				str2 = ", ";
			}
			return str + "]";
		}
		if (this.IsDict())
		{
			string text = "{" + ((!flag) ? string.Empty : "\n");
			string text2 = string.Empty;
			foreach (KeyValuePair<string, JSONValue> current2 in this.AsDict(false))
			{
				string text3 = text;
				text = string.Concat(new object[]
				{
					text3,
					text2,
					curIndent,
					indent,
					'"',
					JSONValue.EncodeString(current2.Key),
					"\" : ",
					current2.Value.ToString(curIndent + indent, indent)
				});
				text2 = ", " + ((!flag) ? string.Empty : "\n");
			}
			return text + ((!flag) ? string.Empty : ("\n" + curIndent)) + "}";
		}
		if (this.IsBool())
		{
			return (!this.AsBool(false)) ? "false" : "true";
		}
		if (this.IsNull())
		{
			return "null";
		}
		throw new JSONTypeException("Cannot serialize json value of unknown type");
	}

	private static string EncodeString(string str)
	{
		str = str.Replace("\\", "\\\\");
		str = str.Replace("\"", "\\\"");
		str = str.Replace("/", "\\/");
		str = str.Replace("\b", "\\b");
		str = str.Replace("\f", "\\f");
		str = str.Replace("\n", "\\n");
		str = str.Replace("\r", "\\r");
		str = str.Replace("\t", "\\t");
		return str;
	}

	public static implicit operator JSONValue(string s)
	{
		return new JSONValue(s);
	}

	public static implicit operator string(JSONValue s)
	{
		return s.AsString(false);
	}

	public static implicit operator JSONValue(float s)
	{
		return new JSONValue(s);
	}

	public static implicit operator float(JSONValue s)
	{
		return s.AsFloat(false);
	}

	public static implicit operator JSONValue(bool s)
	{
		return new JSONValue(s);
	}

	public static implicit operator bool(JSONValue s)
	{
		return s.AsBool(false);
	}

	public static implicit operator JSONValue(int s)
	{
		return new JSONValue((float)s);
	}

	public static implicit operator int(JSONValue s)
	{
		return (int)s.AsFloat(false);
	}

	public static implicit operator JSONValue(List<JSONValue> s)
	{
		return new JSONValue(s);
	}

	public static implicit operator List<JSONValue>(JSONValue s)
	{
		return s.AsList(false);
	}

	public static implicit operator Dictionary<string, JSONValue>(JSONValue s)
	{
		return s.AsDict(false);
	}
}
