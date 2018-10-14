namespace PecanDb.Storage
{
    using System;

    public class PropertyCompareResult : IPropertyCompareResult
    {
        public PropertyCompareResult(string name, string oldValue, string newValue, string documentName)
        {
            this.Name = name;
            this.OldValue = oldValue;
            this.NewValue = newValue;
            this.DocumentName = documentName;
            this.DateTime = DateTime.Now;
        }

        public string DocumentName { get; set; }

        public string Name { get; set; }

        public string OldValue { get; set; }

        public string NewValue { get; set; }

        public DateTime DateTime { get; set; }
    }
}