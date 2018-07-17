using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

        static Program()
        {
            random = new Random();
            firstTestResults = new List<bool>();
            consoleLocker = string.Empty;
            waitHandle = new AutoResetEvent(false);
        }

        #endregion

        static void Main(string[] args)
        {
            var testMethod = new TestDelegate(TestMethod);
            var testCounter = Enumerable.Range(1, 20).ToList();

            #region Test 1

            testCounter.ForEach(x =>
            {
                testMethod.BeginInvoke(new TestOption { Value = RandomInt, Name = "Test 1" }, (y) => {
                    lock (consoleLocker)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Test 1 - Added: {((List<bool>)y.AsyncState).LastOrDefault()}");
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                }, firstTestResults);
            });

            #endregion

            #region Test 2

            Parallel.ForEach(testCounter, x =>
            {
                lock (consoleLocker)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Test 2 - Added: {TestMethod(new TestOption { Value = RandomInt, Name = "Test 2" })}");
                    Console.ForegroundColor = ConsoleColor.White;
                }
            });

            #endregion

            #region Test 3

            waitHandle.Reset();

            testCounter.ForEach(x =>
            {
                var backgroundThread = new Thread(() => TestMethodCover(new TestOption { Value = RandomInt, Name = "Test 3" }));

                backgroundThread.Start();

                waitHandle.WaitOne();
                waitHandle.Reset();
            });

            #endregion

            #region Test 4

            var taskList = new List<Task<bool>>();

            testCounter.ForEach(x =>
            {
                taskList.Add(TestMethodAsync(new TestOption { Value = RandomInt, Name = "Test 4" }));
            });

            var taskArray = taskList.ToArray();

            Task.WaitAll(taskArray);

            taskArray.ToList().ForEach(x =>
            {
                lock (consoleLocker)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Test 4 - Added: {TestMethod(new TestOption { Value = RandomInt, Name = "Test 4" })}");
                    Console.ForegroundColor = ConsoleColor.White;
                }
            });

            #endregion

            Console.ReadLine();
        }

        #region Methods

        static private bool TestMethod(TestOption option)
        {
            while (IsTestFileLocked)
            {
                Thread.Sleep(RandomInt);
            }

            lock (firstTestResults)
            {
                File.AppendAllText(
                    OptionContainer.FullTestFilePath,
                    $"{Environment.NewLine}{option.Name} - {DateTime.Now} {option.Value}"
                );

                firstTestResults.Add(option.Value > (maxValue - minValue) / 2);

                return option.Value > (maxValue - minValue) / 2;
            }
        }

        static private async Task<bool> TestMethodAsync(TestOption option)
        {
            while (IsTestFileLocked)
            {
                Thread.Sleep(RandomInt);
            }

            lock (firstTestResults)
            {
                File.AppendAllText(
                    OptionContainer.FullTestFilePath,
                    $"{Environment.NewLine}{option.Name} - {DateTime.Now} {option.Value}"
                );

                firstTestResults.Add(option.Value > (maxValue - minValue) / 2);

                return option.Value > (maxValue - minValue) / 2;
            }
        }

        static private void TestMethodCover(TestOption option)
        {
            lock (consoleLocker)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Test 3 - Added: {TestMethod(option)}");
                Console.ForegroundColor = ConsoleColor.White;
            }

            waitHandle.Set();
        }

        #endregion

        #region Properties

        private static string CurrentMethod => (new StackTrace()).GetFrame(1).GetMethod().Name;

        private static int RandomInt => random.Next(minValue, maxValue);

        private static bool IsTestFileLocked
        {
            get
            {
                FileStream stream = null;

                try
                {
                    stream = File.Open(OptionContainer.FullTestFilePath, FileMode.Open, FileAccess.Read, FileShare.None);
                }
                catch (IOException)
                {
                    return true;
                }
                finally
                {
                    if (stream != null)
                        stream.Close();
                }

                return false;
            }
        }

        #endregion
    }
}
