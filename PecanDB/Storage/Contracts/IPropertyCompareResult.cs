namespace PecanDb.Storage
{
    using System;

    public interface IPropertyCompareResult
    {
        string DocumentName { get; set; }

        string Name { get; set; }

        string OldValue { get; set; }

        string NewValue { get; set; }

        DateTime DateTime { get; set; }
    }
}