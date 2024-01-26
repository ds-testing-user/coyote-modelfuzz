﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using Microsoft.Coyote;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Logging;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.SystematicTesting;

namespace TestDriver
{
    public static class Program
    {
        public static void Main()
        {
            string basePath = "output/";
            if (!Directory.Exists(basePath))
            {
                Directory.CreateDirectory(basePath);
            }

            int m = 7;
            int n = 10;
            string[] outputPaths = { "random", "state", "trace" };
            int numRepeat = 10;

            for (int i = 0; i < outputPaths.Length; i++)
            {
                List<int> bugs = new List<int>();
                List<int> bugIters = new List<int>();
                for (int j = 0; j < numRepeat; j++)
                {
                    Tuple<int, int> result = Main_(10000, 100, i, $"{basePath}/{outputPaths[i]}_{m}_{n}_cov_{j}", false, 13444 * j);
                    int numBugsFound = result.Item1;
                    int firstBugIteration = result.Item2;

                    bugs.Add(numBugsFound);
                    bugIters.Add(firstBugIteration);
                }

                ExportCsv(bugs, $"{basePath}/{outputPaths[i]}_{m}_{n}_bugs");
                ExportCsv(bugIters, $"{basePath}/{outputPaths[i]}_{m}_{n}_bug_iters");
            }
        }

        private static Tuple<int, int> Main_(uint iterations, uint steps, int runMode, string outputFile, bool logging, int rep)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            var configuration = Configuration.Create().WithTestingIterations(iterations)
                 .WithMaxSchedulingSteps(steps); // Extra scheduling steps are needed as later test iterations end early since they reach the bound.
            // configuration.WithSystematicFuzzingEnabled();
            configuration.WithSystematicFuzzingFallbackEnabled(false);
            // configuration.WithNumDelays(10);
            configuration.WithRandomGeneratorSeed((uint)((rep * 2562356) + 12441245));
            configuration.WithMutatorType("actor");
            // configuration.WithMutatorType("process");
            configuration.WithOutputFilePath(outputFile);
            configuration.WithRoundRobinFuzzingStrategy();
            // configuration.WithInterleavedFuzzingStrategy();
            configuration.WithRunMode(runMode);
            configuration.WithIndexOffset(1); // 1 for TPC, 3 for Raft
            configuration.WithVerbosityEnabled(VerbosityLevel.Info);
            configuration.WithTelemetryEnabled(false);
            configuration.WithConsoleLoggingEnabled(logging);
            configuration.WithTestIterationsRunToCompletion();

            Tuple<int, int> result = RunTest(MicroBenchmark.Program.Execute, configuration,
                "MicroBenchmark");
            // java -jar ./dist/tla2tools_server.jar -controlled ../tla-benchmarks/MicroBenchmark/MB_5_9.tla -config ../tla-benchmarks/MicroBenchmark/MB_5_9.cfg -mapperparams "name=mb"

            // RunTest(TwoPhaseCommit.Program.Execute, configuration,
            //     "TwoPhaseCommit");

            // RunTest(Microsoft.Coyote.Samples.CloudMessaging.Raft.Mocking.Program.Execute, configuration,
            //     "CloudMessaging.TestWithMocking");

            stopWatch.Stop();
            Console.WriteLine($"Done testing in {stopWatch.ElapsedMilliseconds}ms. All expected bugs found.");
            return result;
        }

        private static void RunTest(Action test, Configuration configuration, string testName,
            params string[] expectedBugs)
        {
            var engine = TestingEngine.Create(configuration, test);
            RunTest(engine, testName, expectedBugs);
        }

        private static void RunTest(Func<Task> test, Configuration configuration, string testName,
            params string[] expectedBugs)
        {
            var engine = TestingEngine.Create(configuration, test);
            RunTest(engine, testName, expectedBugs);
        }

        private static void RunTest(Func<ICoyoteRuntime, Task> test, Configuration configuration, string testName,
            params string[] expectedBugs)
        {
            var engine = TestingEngine.Create(configuration, test);
            RunTest(engine, testName, expectedBugs);
        }

        private static Tuple<int, int> RunTest(Action<IActorRuntime> test, Configuration configuration, string testName,
            params string[] expectedBugs)
        {
            var engine = TestingEngine.Create(configuration, test);
            return RunTest(engine, testName, expectedBugs);
        }

        private static Tuple<int, int> RunTest(TestingEngine engine, string testName, string[] expectedBugs)
        {
            Console.WriteLine($"Starting to test '{testName}'.");
            engine.Run();
            Console.WriteLine($"Done testing '{testName}'. Found {engine.TestReport.NumOfFoundBugs} bugs.");
            Console.WriteLine($"First bug at: {engine.TestReport.FirstBugIteration}");
            Tuple<int, int> result = new Tuple<int, int>(engine.TestReport.NumOfFoundBugs, engine.TestReport.FirstBugIteration);
            if (expectedBugs.Length > 0 && engine.TestReport.NumOfFoundBugs == 0)
            {
                foreach (var expectedBug in expectedBugs)
                {
                    Console.WriteLine($"Expected bug '{expectedBug}' not found.");
                }

                return result;
            }
            else if (expectedBugs.Length > 0 && engine.TestReport.NumOfFoundBugs > 0)
            {
                bool isFound = false;
                var actualBug = engine.TestReport.BugReports.First();
                foreach (var expectedBug in expectedBugs)
                {
                    if (actualBug.Contains(expectedBug))
                    {
                        isFound = true;
                        break;
                    }
                }

                if (!isFound)
                {
                    foreach (var expectedBug in expectedBugs)
                    {
                        Console.WriteLine($"Found '{actualBug}' bug instead of the expected bug '{expectedBug}'.");
                    }

                    return result;
                }

                Console.WriteLine($"Found expected '{actualBug}' bug.");
            }
            else if (engine.TestReport.NumOfFoundBugs > 0)
            {
                Console.WriteLine($"Unexpected '{engine.TestReport.BugReports.First()}' bug found.");
                return result;
            }

            return result;
        }

        public static void ExportCsv(List<int> result, string path)
        {
            using (var writer = new StreamWriter($"{path}.csv"))
            {
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    csv.WriteRecords(result);
                }
            }
        }
    }
}
