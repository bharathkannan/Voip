using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SIPSorcery.Net;
using System.Net;
using System.Reflection;
using BlueFace.Voip.Net;

namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            Program p = new Program() ;
            for (int i = 0; i < typelist.Length; i++)
            {
                Console.WriteLine(typelist[i].Name);
            }
            Console.Read();


        }
        public Type[] GetTypesInNamespace(Assembly assembly, string nameSpace)
        {
            return assembly.GetTypes().Where(t => String.Equals(t.Namespace, nameSpace, StringComparison.Ordinal)).ToArray();
        }

    }
}
