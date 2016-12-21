using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Odenwald.Output.InfluxDbAdapter
{
    public class InfluxDbAdapter : IOutputAdapter
    {
        public IOutputProcessor Processor
        {
            get;set;
        }

        public void Configure()
        {
            throw new NotImplementedException();
        }

        public void Flush()
        {
            throw new NotImplementedException();
        }

        public void Start()
        {
            throw new NotImplementedException();
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }

        public void Write(IOutputMetric metric)
        {
            throw new NotImplementedException();
        }
    }
}
