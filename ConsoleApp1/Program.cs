using System;
using System.Net;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            DateTime d1 = DateTime.Now;

            string s1 = WebUtility.UrlEncode(d1.ToString());
            Console.WriteLine($"s1={s1}");

            var s2= WebUtility.UrlDecode(s1);


            DateTime d2 = DateTime.Parse(s2);

            Console.WriteLine($"d2={d2}");


        }
    }
}
