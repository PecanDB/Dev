namespace PecanDB.Remoting
{
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading;

    public class PecanDbWebApiService : IDisposable
    {
        public PecanDocumentStore store { get; set; }

        public PecanDbWebApiService()
        {
            ServicePointManager.MaxServicePointIdleTime = Timeout.Infinite;
            var path = "PecanDBRemoteServer";
            try
            {
                this.store = new PecanDocumentStore(path,
                    false,
                    new DatabaseOptions
                    {
                        DontWaitForWritesOnCreate = true
                    });
            }
            catch (Exception e)
            {
                throw new Exception(path, e);
            }
        }

        public dynamic Load(string id, string database)
        {
            using (ISession session = this.store.OpenSession())
            {
                var result = session.Load<dynamic>(id, database);
                return result;
            }
        }

        public string GetETag(string id, string database)
        {
            using (ISession session = this.store.OpenSession())
            {
                var result = session.GetETagFor<dynamic>(id, database);
                return result;
            }
        }

        public List<string> LoadAll(string database)
        {
            using (ISession session = this.store.OpenSession())
            {
                return session.QueryDocument<dynamic>((docs) => docs.Select(doc => doc), database).Select(x => JsonConvert.SerializeObject(x)).ToList();
            }
        }

        public string Save(string data, string database, string id = null)
        {
            using (ISession session = this.store.OpenSession())
            {
                var result = session.Save(database, data, id);
                return result;
            }
        }

        public void Dispose()
        {
            this.store?.Dispose();
        }
    }
}