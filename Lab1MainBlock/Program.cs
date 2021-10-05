using System;
using System.Collections.Generic;
using LabMainBlock;
using YOLOv4MLNet.DataStructures;

namespace Lab1MainBlock
{
    class Program
    {
        static void Main(string[] args)
        {
            var new_var = Program1.MainBlock(@"Assets\Images");
            Tuple<string, IReadOnlyList<YoloV4Result>> res;
            while (!Program1.isEnded)
            {
                while (Program1.yoloResults.TryDequeue(out res))
                {
                    Console.WriteLine(res.Item1);
                    foreach (var obj in res.Item2)
                    {
                        Console.WriteLine(obj.Label + " " + obj.BBox[0] + " " + obj.BBox[1] + " " + obj.BBox[2] + " " + obj.BBox[3]);
                    }
                    Console.WriteLine(" ");
                }
            }
            Console.WriteLine("\nDear user, work finished, press enter to continue");
        }
    }
}
