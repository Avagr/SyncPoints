using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SynPointLib
{
    [Serializable]
    public class Vertice
    {
        public HashSet<Vertice> ConnectedTo { get; }

        public Vertice()
        {
            ConnectedTo = new HashSet<Vertice>();
        }
    }
}
