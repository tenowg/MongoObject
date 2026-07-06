namespace MongoObject.CliTool.Data
{
    public class CliOperation : Dictionary<string, object>
    {
        public CliOperation(string typeName)
        {
            this["_t"] = typeName;
        }
    }

    public class OperationDictionary : Dictionary<string, List<object>>
    {
        public new List<object> this[string key]
        {
            get
            {
                if (!TryGetValue(key, out var value))
                {
                    value = [];
                    Add(key, value);
                }
                return value;
            }
            set { base[key] = value; }
        }    
    }
}