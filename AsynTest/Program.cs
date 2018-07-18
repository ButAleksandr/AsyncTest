using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AsynTest
{
    partial class Program
    {
        #region Field and Declaration

        private static Random random;
        private const int minValue = 300;
        private const int maxValue = 1000;
        private delegate bool TestDelegate(TestOption option);
        private static Object locker = new Object();
        private static List<bool> firstTestResults;
        private static string consoleLocker;
        private static AutoResetEvent waitHandle;
        private static TextWriter textWriter;

        static Program()
        {
            random = new Random();
            firstTestResults = new List<bool>();
            consoleLocker = string.Empty;
            waitHandle = new AutoResetEvent(false);
            textWriter = new StreamWriter(OptionContainer.FullTestFilePath);
        }

        #endregion

        static void Main(string[] args)
        {
            var testMethod = new TestDelegate(TestMethod);
            var testCounter = Enumerable.Range(1, 20).ToList();

            #region Test 1 - working with Timer

            Console.WriteLine($"Timer created: {DateTime.Now}");
            var timerCB = new TimerCallback((x) => { Console.WriteLine($"Timer CB is called: {DateTime.Now}"); });
            var timer = new Timer(timerCB, null, 200, 500);

            #endregion

            #region Test 2 - Begin/End invoke

            testCounter.ForEach(x =>
            {
                testMethod.BeginInvoke(new TestOption { Value = RandomInt, Name = "Test 2" }, (y) => {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Test 2 - Added: {((List<bool>)y.AsyncState).LastOrDefault()}");
                    Console.ForegroundColor = ConsoleColor.White;
                }, firstTestResults);
            });

            #endregion

            #region Test 3 - Parallel

            Parallel.ForEach(testCounter, x =>
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Test 2 - Added: {TestMethod(new TestOption { Value = RandomInt, Name = "Test 3" })}");
                Console.ForegroundColor = ConsoleColor.White;
            });

            #endregion

            #region Test 4 - Delegate, new Thread, AutoReset

            waitHandle.Reset();

            testCounter.ForEach(x =>
            {
                var backgroundThread = new Thread(() => TestMethodCover(new TestOption { Value = RandomInt, Name = "Test 4" }));

                backgroundThread.Start();

                waitHandle.WaitOne();
                waitHandle.Reset();
            });

            #endregion

            #region Test 5 - With using async

            var taskList = new List<Task<bool>>();

            testCounter.ForEach(x =>
            {
                taskList.Add(TestMethodAsync(new TestOption { Value = RandomInt, Name = "Test 5" }));
            });

            var taskArray = taskList.ToArray();

            Task.WaitAll(taskArray);

            taskArray.ToList().ForEach(x =>
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Test 5 - Added: {TestMethod(new TestOption { Value = RandomInt, Name = "Test 4" })}");
                Console.ForegroundColor = ConsoleColor.White;
            });

            #endregion

            #region Test 6 - ThreadPool

            WaitCallback workItem = new WaitCallback((x) => {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"{((TestOption)x).Name} - Added: {TestMethod((TestOption)x)}");
                Console.ForegroundColor = ConsoleColor.White;
            });

            testCounter.ForEach(x => {
                ThreadPool.QueueUserWorkItem(workItem, new TestOption { Value = RandomInt, Name = "Test 6" });
            });

            #endregion

            Console.ReadLine();
        }

        #region Methods

        static private bool TestMethod(TestOption option)
        {
            var str = $"{Environment.NewLine}{option.Name} - {DateTime.Now} {option.Value}";

            lock (textWriter) {
                textWriter.WriteLine(str);
            }           

            return option.Value > (maxValue - minValue) / 2;
        }

        static private async Task<bool> TestMethodAsync(TestOption option)
        {
            var str = $"{Environment.NewLine}{option.Name} - {DateTime.Now} {option.Value}";
            var encodedText = Encoding.Unicode.GetBytes(str);

            lock (textWriter)
            {
                textWriter.Write(str);
            }           

            return option.Value > (maxValue - minValue) / 2;
        }

        static private void TestMethodCover(TestOption option)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Test 3 - Added: {TestMethod(option)}");
            Console.ForegroundColor = ConsoleColor.White;

            waitHandle.Set();
        }

        #endregion

        #region Properties

        private static int RandomInt => random.Next(minValue, maxValue);

        #endregion
    }
}
