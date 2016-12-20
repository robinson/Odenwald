using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Odenwald
{
    public interface IAdapter
    {
        void Configure();
        void Start();
        void Stop();
    }
}
