namespace BMPC.Core.Structs
{
    public struct KeyValueString
    {
        public string Key { get; set; }
        public string Value { get; set; }

        public KeyValueString(string key, string value)
        {
            this.Key = key;
            this.Value = value;
        }

        public override string ToString()
        {
            return $"\"{this.Key}\"\t\"{this.Value}\"";
        }
    }
}
