using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace WeatherDataIngestFuncApp
{
    public interface IWriteToBlob
    {
        public Task WriteToblob(string content, string blobFileName);
    }
}
