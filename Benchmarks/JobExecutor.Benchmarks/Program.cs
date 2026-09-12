using BenchmarkDotNet.Running;
using JobExecutor.Benchmarks.Benchmarks;

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
