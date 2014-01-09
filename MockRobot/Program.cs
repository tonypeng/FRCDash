using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

using RhesusNet.NET;

namespace MockRobot
{
    class Program
    {
        static void Main(string[] args)
        {
            NetServer server = new NetServer(846);

            server.Open();

            Console.WriteLine("Server listening on port 846");

            Random r = new Random();

            bool b = false;

            float slope = 1f;

            float value = 0f;

            Stopwatch sw = new Stopwatch();

            float totalTime = 0f;

            while (true)
            {
                NetBuffer buff = new NetBuffer();

                value = (float)Math.Abs(Math.Sin(totalTime));
                totalTime += (float)sw.Elapsed.TotalSeconds;
                //value = (float)r.NextDouble();

                sw.Restart();

                if (value <= 0)
                    slope = 1;
                else if (value >= 1)
                    slope = -1;

                buff.Write((byte)MessageType.FRONT_SHOOTER_DATA_SPEED);
                buff.Write(totalTime);
                buff.Write(value);

                Console.WriteLine("Sending value of {0}", value);

                server.SendToAll(buff, NetChannel.NET_UNRELIABLE_SEQUENCED, 1);

                value = (totalTime - (float)Math.Floor(totalTime));
                //value = (float)Math.Floor(totalTime) / (totalTime == 0 ? 0.01f : totalTime);
                //value = totalTime - (totalTime * totalTime);

                buff = new NetBuffer();
                buff.Write((byte)MessageType.BACK_SHOOTER_DATA_SPEED);
                buff.Write(totalTime);
                buff.Write(value);

                server.SendToAll(buff, NetChannel.NET_UNRELIABLE_SEQUENCED, 1);

                Thread.Sleep(50);
            }
        }
    }
}
