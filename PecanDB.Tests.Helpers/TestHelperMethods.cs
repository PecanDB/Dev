namespace PecanDB.Tests.Helpers
{
    using System;
    using System.Threading.Tasks;

    public class TestHelperMethods
    {
        public static void AssertAwait(int durationMilliseconds = 5000)
        {
            Task.Run(() => Task.Delay(TimeSpan.FromMilliseconds(durationMilliseconds))).Wait();
        }

        public static void AssertAwait(Action action, int durationMilliseconds = 5000)
        {
            DateTime now = DateTime.Now;
            bool passed = false;
            var lastException = new Exception();

            int i = 1;
            while ((DateTime.Now - now).TotalMilliseconds <= durationMilliseconds)
            {
                i *= 2;

                try
                {
                    action();
                    passed = true;
                    break;
                }
                catch (Exception e)
                {
                    lastException = e;
                    Task.Run(() => Task.Delay(TimeSpan.FromMilliseconds(1000 * i))).Wait();
                }
                //  System.Threading.Thread.Sleep(sleepIntervalMilliseconds);
            }
            if (!passed)
                action();
        }
    }
}