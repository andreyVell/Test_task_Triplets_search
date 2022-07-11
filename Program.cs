using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Threading;

namespace Test_task_Triplets_search
{
    internal class Program
    {
        private static Dictionary<string, int> tripletsCountMap;

        #region UI methods

        private static void ShowInterface()
        {
            Console.Clear();
            Console.WriteLine("1) Enter the path to the file\n2) Exit");
        }

        private static string EnterFilePath()
        {
            Console.Clear();
            Console.WriteLine("Enter the path to the file:");
            return Console.ReadLine();
        }

        private static int EnterThreadNums()
        {
            Console.WriteLine("\nEnter number of threads:");
            try
            {
                return Convert.ToInt32(Console.ReadLine());
            }
            catch
            {
                Console.WriteLine("Error! Please press ENTER to return to the menu");
                Console.ReadLine();
                return 0;
            }
        }

        private static StreamReader OpenFile(string path)
        {
            try
            {
                return (new StreamReader(path));
            }
            catch
            {
                Console.WriteLine("Error opening the file! Please press ENTER to return to the menu");
                Console.ReadLine();
                return null;
            }
        }

        private static void ResultOutput(string result, Stopwatch time)
        {
            Console.WriteLine("\nThe most common triplets:" + result + "\n");
            Console.WriteLine($"Working time: {time.ElapsedMilliseconds}ms\n");
            Console.WriteLine("Please press ENTER to return to the menu");
            Console.ReadLine();
        }

        #endregion

        #region settings
        //all registers are taken into everywhere, but you can change, maybe it is necessary :)
        private const bool useAllRegisters = true;

        private static int threadNums = 0;

        #region splitSeparators
        private static char[] ignoredCharacters = new char[] {
            ' ',
            '0',
            '1',
            '2',
            '3',
            '4',
            '5',
            '6',
            '7',
            '8',
            '9',
            ',',
            '.',
            ':',
            ';',
            '[',
            ']',
            '!',
            '@',
            '#',
            '$',
            '%',
            '^',
            '&',
            '*',
            '(',
            ')',
            '/',
            '\n',
            '\r',
            '|',
            '"',
            '№',
            '?',
        };
        #endregion

        private static StreamReader fileInput = null;
        private static Stopwatch sw = new Stopwatch();
        private static bool isThereAnErrorInTheSettings;

        private static void PrepareAllSettings()
        {
            isThereAnErrorInTheSettings = false;
            string filePath = EnterFilePath();
            fileInput = OpenFile(filePath);
            if (fileInput is null) isThereAnErrorInTheSettings = true;
            threadNums = EnterThreadNums();
            if (threadNums == 0) isThereAnErrorInTheSettings = true;
        }

        #endregion

        #region computing methods

        /// <summary>
        /// Prepare for triplets computing. Сreating threads and distributing input text between them
        /// </summary>
        private static string FindTriplets(string inputText)
        {
            tripletsCountMap = new Dictionary<string, int>();
            var inputLines = inputText.Split('\r');
            int counter = 0;

            var calcThreads = new Thread[threadNums];
            //split inputLanes into (threadNums) arrays for different threads 
            var ArraysForCalcThreads = inputLines.GroupBy(_ => counter++ / (inputLines.Length / threadNums + 1)).Select(v => v.ToArray()).ToArray();

            //if we have fewer lines in the input file than the threads that the user specified,
            //then we cannot split the lines into smaller ones, bacause we may lose some triplets
            if (ArraysForCalcThreads.Length < threadNums)
                threadNums = ArraysForCalcThreads.Length;

            for (int i = 0; i < threadNums; i++)
            {
                Thread thread = new Thread(new ParameterizedThreadStart(PutTripletsIntoDictionary));
                thread.IsBackground = true;
                calcThreads[i] = thread;
                thread.Start(ArraysForCalcThreads[i]);
            }

            //waiting until all threads are completed
            for (int i = 0; i < threadNums; i++)
                calcThreads[i].Join();

            //former result top 10 triplets            
            return FormResult();
        }

        /// <summary>
        /// Filling the dictionary with triplets from an array of strings (string[])
        /// </summary>
        private static void PutTripletsIntoDictionary(object inputText)
        {
            Dictionary<string, int> currTripletsCountMap = new Dictionary<string, int>();
            var inputLines = inputText as string[];
            foreach (var line in inputLines)
            {
                //parse line into words
                var words = line.Split(ignoredCharacters, StringSplitOptions.RemoveEmptyEntries);
                //find all triplets in word
                foreach (var word in words)
                {
                    if (word.Length >= 3)
                    {
                        string triplet;
                        for (int index = 0; index < word.Length - 2; index++)
                        {
                            triplet = word.Substring(index, 3);
                            if (currTripletsCountMap.ContainsKey(triplet))
                                currTripletsCountMap[triplet]++;
                            else
                                currTripletsCountMap.Add(triplet, 1);
                        }
                    }
                }
            }
            //fill tripletsCountMap
            lock (tripletsCountMap)
            {
                foreach (var note in currTripletsCountMap)
                    if (tripletsCountMap.ContainsKey(note.Key))
                        tripletsCountMap[note.Key] += note.Value;
                    else
                        tripletsCountMap.Add(note.Key, note.Value);
            }
        }

        /// <summary>
        /// Form the output string based on the calculations performed
        /// </summary>
        private static string FormResult()
        {
            string result = "\n";
            int topCount = 10;
            for (int i = 0; i < topCount; i++)
            {
                int maxValue = tripletsCountMap.Values.Max();
                string maxKey = tripletsCountMap.First(x => x.Value == tripletsCountMap.Values.Max()).Key;
                result += maxKey + $"({maxValue})";
                if (i < topCount - 1)
                    result += ", ";
                tripletsCountMap[maxKey] = 0;
            }
            return result;
        }

        #endregion

        private static void Start()
        {
            PrepareAllSettings();
            if (isThereAnErrorInTheSettings)
                return;

            sw.Restart();
            string result;
            if (useAllRegisters)
                result = FindTriplets(fileInput.ReadToEnd().ToLower());
            else
                result = FindTriplets(fileInput.ReadToEnd());
            sw.Stop();
            fileInput.Close();

            ResultOutput(result, sw);
        }

        static void Main(string[] args)
        {
            while (true)
            {
                ShowInterface();
                string userInput = Console.ReadLine();
                switch (userInput)
                {
                    case "1":
                        Start();
                        break;
                    case "2":
                        return;
                }
            }
        }
    }
}