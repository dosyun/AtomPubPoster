using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthenticationWsse
{
    class Program
    {
        static void Main(string[] args)
        {

            var ap = new AtomPub();
            ap.Post(
                "Title",
                @"Body"
                );
        }
    }
}
