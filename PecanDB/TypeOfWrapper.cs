namespace PecanDB
{
    using System;

    internal class TypeOfWrapper
    {
        internal string Name { set; get; }

        internal string FullName { get; set; }

        internal static TypeOfWrapper TypeOf(Type t)
        {
            var temp = new TypeOfWrapper
            {
                Name = t.Name,
                FullName = t.FullName
            };
            return temp;
        }
    }
}