using FYIStockPile.Storage;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MVVMC;
using FYIStockPile.Interfaces;

namespace FYIStockPile.Storage
{
    public class AppConfigRepository
    {
        private static LiteDatabase Database = new LiteDatabase(@"fyi_data.db");

        private static Slack slack = new Slack();
        public AppRepository Pile;

        public AppConfigRepository()
        {
            Pile = new AppRepository(Database, slack);
        }
    }
}
