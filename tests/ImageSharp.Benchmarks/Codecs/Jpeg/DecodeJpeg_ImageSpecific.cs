﻿// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

using System.Drawing;
using System.IO;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;

using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Tests;
using CoreSize = SixLabors.Primitives.Size;
using SDImage = System.Drawing.Image;
// ReSharper disable InconsistentNaming

namespace SixLabors.ImageSharp.Benchmarks.Codecs.Jpeg
{
    /// <summary>
    /// Image-specific Jpeg benchmarks
    /// </summary>
    [Config(typeof(Config.ShortClr))]
    public class DecodeJpeg_ImageSpecific
    {
        public class Config : ManualConfig
        {
            public Config()
            {
                // Uncomment if you want to use any of the diagnoser
                this.Add(new BenchmarkDotNet.Diagnosers.MemoryDiagnoser());
            }

            public class ShortClr : Benchmarks.Config
            {
                public ShortClr()
                {
                    this.Add(
                        //Job.Clr.WithLaunchCount(1).WithWarmupCount(2).WithTargetCount(3),
                        Job.Core.WithLaunchCount(1).WithWarmupCount(2).WithTargetCount(3)
                    );
                }
            }
        }

        private byte[] jpegBytes;

        private string TestImageFullPath => Path.Combine(TestEnvironment.InputImagesDirectoryFullPath, this.TestImage);

        // NOTE:
        // The scaled result for very large image "TestImages.Jpeg.Issues.ExifGetString750Transform"
        // is almost the same as the result for Jpeg420Exif,
        // which proves that the execution time for the most common YCbCr 420 path scales linearly
        [Params(
            TestImages.Jpeg.Baseline.Lake,
            TestImages.Jpeg.Issues.BadRstProgressive518,

            TestImages.Jpeg.Baseline.Jpeg420Exif
            )]
        public string TestImage { get; set; }


        [GlobalSetup]
        public void ReadImages()
        {
            if (this.jpegBytes == null)
            {
                this.jpegBytes = File.ReadAllBytes(this.TestImageFullPath);
            }
        }

        [Benchmark(Baseline = true, Description = "Decode Jpeg - System.Drawing")]
        public Size JpegSystemDrawing()
        {
            using (var memoryStream = new MemoryStream(this.jpegBytes))
            {
                using (var image = SDImage.FromStream(memoryStream))
                {
                    return image.Size;
                }
            }
        }

        [Benchmark(Description = "Decode Jpeg - ImageSharp")]
        public CoreSize JpegImageSharp()
        {
            using (var memoryStream = new MemoryStream(this.jpegBytes))
            {
                using (var image = Image.Load<Rgba32>(memoryStream, new JpegDecoder(){ IgnoreMetadata = true}))
                {
                    return new CoreSize(image.Width, image.Height);
                }
            }
        }

        // RESULTS (2018 October 31):
        //
        // BenchmarkDotNet=v0.10.14, OS=Windows 10.0.17134
        // Intel Core i7-7700HQ CPU 2.80GHz (Kaby Lake), 1 CPU, 8 logical and 4 physical cores
        // Frequency=2742191 Hz, Resolution=364.6719 ns, Timer=TSC
        // .NET Core SDK=2.1.403
        //   [Host]     : .NET Core 2.1.5 (CoreCLR 4.6.26919.02, CoreFX 4.6.26919.02), 64bit RyuJIT
        //   Job-MCUBGX : .NET Framework 4.7.1 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.3190.0
        //   Job-TZIRPF : .NET Core 2.1.5 (CoreCLR 4.6.26919.02, CoreFX 4.6.26919.02), 64bit RyuJIT
        // 
        // 
        //                          Method | Runtime |                    TestImage |     Mean |     Error |    StdDev | Scaled | ScaledSD |    Gen 0 | Allocated |
        // ------------------------------- |-------- |----------------------------- |---------:|----------:|----------:|-------:|---------:|---------:|----------:|
        //  'Decode Jpeg - System.Drawing' |     Clr | Jpg/baseline/jpeg420exif.jpg | 17.10 ms |  4.720 ms | 0.2667 ms |   1.00 |     0.00 | 218.7500 | 757.88 KB |
        //      'Decode Jpeg - ImageSharp' |     Clr | Jpg/baseline/jpeg420exif.jpg | 57.77 ms |  4.626 ms | 0.2614 ms |   3.38 |     0.04 | 125.0000 | 566.01 KB |
        //                                 |         |                              |          |           |           |        |          |          |           |
        //  'Decode Jpeg - System.Drawing' |    Core | Jpg/baseline/jpeg420exif.jpg | 17.45 ms |  2.092 ms | 0.1182 ms |   1.00 |     0.00 | 218.7500 | 757.04 KB |
        //      'Decode Jpeg - ImageSharp' |    Core | Jpg/baseline/jpeg420exif.jpg | 48.39 ms | 14.562 ms | 0.8228 ms |   2.77 |     0.04 | 125.0000 | 529.96 KB |

        // RESULTS (2018 November 4):
        //                          Method |                                   TestImage |       Mean |      Error |    StdDev | Scaled | ScaledSD |     Gen 0 |    Gen 1 |    Gen 2 |   Allocated |
        // ------------------------------- |-------------------------------------------- |-----------:|-----------:|----------:|-------:|---------:|----------:|---------:|---------:|------------:|
        //  'Decode Jpeg - System.Drawing' |                       Jpg/baseline/Lake.jpg |   6.117 ms |  0.3923 ms | 0.0222 ms |   1.00 |     0.00 |   62.5000 |        - |        - |   205.83 KB |
        //      'Decode Jpeg - ImageSharp' |                       Jpg/baseline/Lake.jpg |  18.126 ms |  0.6023 ms | 0.0340 ms |   2.96 |     0.01 |         - |        - |        - |    19.97 KB |
        //                                 |                                             |            |            |           |        |          |           |          |          |             |
        //  'Decode Jpeg - System.Drawing' |                Jpg/baseline/jpeg420exif.jpg |  17.063 ms |  2.6096 ms | 0.1474 ms |   1.00 |     0.00 |  218.7500 |        - |        - |   757.04 KB |
        //      'Decode Jpeg - ImageSharp' |                Jpg/baseline/jpeg420exif.jpg |  41.366 ms |  1.0115 ms | 0.0572 ms |   2.42 |     0.02 |         - |        - |        - |    21.94 KB |
        //                                 |                                             |            |            |           |        |          |           |          |          |             |
        //  'Decode Jpeg - System.Drawing' | Jpg/issues/Issue518-Bad-RST-Progressive.jpg | 428.282 ms | 94.9163 ms | 5.3629 ms |   1.00 |     0.00 | 2375.0000 |        - |        - |  7403.76 KB |
        //      'Decode Jpeg - ImageSharp' | Jpg/issues/Issue518-Bad-RST-Progressive.jpg | 386.698 ms | 33.0065 ms | 1.8649 ms |   0.90 |     0.01 |  125.0000 | 125.0000 | 125.0000 | 35186.97 KB |
        //                                 |                                             |            |            |           |        |          |           |          |          |             |
        //  'Decode Jpeg - System.Drawing' |       Jpg/issues/issue750-exif-tranform.jpg |  95.192 ms |  3.1762 ms | 0.1795 ms |   1.00 |     0.00 | 1750.0000 |        - |        - |  5492.63 KB |
        //      'Decode Jpeg - ImageSharp' |       Jpg/issues/issue750-exif-tranform.jpg | 230.158 ms | 48.8128 ms | 2.7580 ms |   2.42 |     0.02 |  312.5000 | 312.5000 | 312.5000 | 58834.66 KB |
    }
}
