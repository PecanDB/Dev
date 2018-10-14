namespace PecanDb.Storage
{
    using PecanDB;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    public class HistoryService
    {
        public static void UpdateDocumentHistory<TDocumentWithObject, TObjectOnly>(TDocumentWithObject original, TDocumentWithObject dbObject, IStorageMechanism storageMechanism, string databaseName, DatabaseService DatabaseService)
            where TDocumentWithObject : IStandardDocument<TObjectOnly>
        {
            if (TypeOfWrapper.TypeOf(typeof(TDocumentWithObject)).Name == TypeOfWrapper.TypeOf(typeof(History<>)).Name || TypeOfWrapper.TypeOf(typeof(TDocumentWithObject)).Name == TypeOfWrapper.TypeOf(typeof(SystemDb<>)).Name || original?.Id == null)
                return;

            List<PropertyCompareResult> compares = Compare<TDocumentWithObject, TObjectOnly>(original, dbObject);

            using (StorageDatabase<History<object>, object> history = DatabaseService.DatabaseAccessRegardlessOfTransaction<History<object>, object>(storageMechanism, null))
            {
                history.CreateAll(
                    compares.Select(
                        x => new History<object>
                        {
                            OldValue = x.OldValue,
                            NewValue = x.NewValue,
                            DocumentName = x.DocumentName,
                            DateTime = x.DateTime,
                            Name = x.Name
                        }).ToList());
            }
        }

        public static List<PropertyCompareResult> Compare<TDocumentWithObject, TObjectOnly>(TDocumentWithObject oldObject, TDocumentWithObject newObject)
        {
            if (TypeOfWrapper.TypeOf(typeof(TDocumentWithObject)).Name == TypeOfWrapper.TypeOf(typeof(History<TObjectOnly>)).Name || TypeOfWrapper.TypeOf(typeof(TDocumentWithObject)).Name == TypeOfWrapper.TypeOf(typeof(SystemDb<TObjectOnly>)).Name)

                return new List<PropertyCompareResult>();
            PropertyInfo[] properties = typeof(TDocumentWithObject).GetProperties();
            var result = new List<PropertyCompareResult>();

            foreach (PropertyInfo pi in properties)
            {
                if (pi.CustomAttributes.Any(ca => ca.AttributeType == typeof(IgnorePropertyCompareAttribute)))
                    continue;

                object oldValue = pi.GetValue(oldObject), newValue = pi.GetValue(newObject);

                if (!Equals(oldValue, newValue))
                    result.Add(new PropertyCompareResult(pi.Name, oldValue?.ToString() ?? "", newValue?.ToString() ?? "", TypeOfWrapper.TypeOf(typeof(TDocumentWithObject)).Name));
            }
            //Console.WriteLine("  Property name: {0} -- old: {1}, new: {2}",resultItem.Name, resultItem.OldValue ?? "<null>", resultItem.NewValue ?? "<null>");
            return result;
        }
    }
}