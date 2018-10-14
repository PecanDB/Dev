namespace PecanDB.Remoting
{
    using System.Collections.Generic;
    using System.Web.Http;

    public class PecanDbWebApi
    {
        public class PecanController : ApiController
        {
            public PecanDbWebApiService Service { get; set; }

            public PecanController()
            {
                this.Service = new PecanDbWebApiService();
            }

            [HttpGet]
            public dynamic Load(string id, string database)
            {
                return this.Service.Load(id, database);
            }

            [HttpGet]
            public List<string> LoadAll(string database)
            {
                return this.Service.LoadAll(database);
            }

            [HttpGet]
            public string GetETag(string id, string database)
            {
                return this.Service.GetETag(id, database);
            }

            [HttpGet]
            public string Save(string data, string database, string id = null)
            {
                var result = this.Service.Save(data, database, id);

                return result;
            }

            protected override void Dispose(bool disposing)
            {
                this.Service.Dispose();
            }
        }
    }
}