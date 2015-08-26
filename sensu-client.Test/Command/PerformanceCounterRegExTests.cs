using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Shouldly;
using sensu_client.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace sensu_client.Command.Tests
{
    [TestFixture]
    public class PerformanceCounterRegExTests
    {
        [Test]
        public void RecognizesEasyPatterns()
        {
            var pattern = @"\Memory\Available Bytes";
            var sut = new PerformanceCounterRegEx();

            var result = sut.split(pattern);

            result.Category.ShouldBe("Memory");
            result.Counter.ShouldBe("Available Bytes");
            result.Instance.ShouldBeNullOrEmpty();
        }

        [Test]
        public void RecognizesEasyPatternsAndIgnoreSpaces()
        {
            var pattern = @"  Objects  \  Semaphores  ";
            var sut = new PerformanceCounterRegEx();

            var result = sut.split(pattern);

            result.Category.ShouldBe("Objects");
            result.Counter.ShouldBe("Semaphores");
            result.Instance.ShouldBeNullOrEmpty();
        }

        [Test]
        public void RecognizesInstance()
        {
            var pattern = @"\Processor(_Total)\% Processor Time";
            var sut = new PerformanceCounterRegEx();

            var result = sut.split(pattern);

            result.Category.ShouldBe("Processor");
            result.Counter.ShouldBe("% Processor Time");
            result.Instance.ShouldBe("_Total");
        }

        [Test]
        public void RecognizesInstanceAndIgnoresSpaces()
        {
            var pattern = @"\ Process ( Idle  ) \ % Processor Time ";
            var sut = new PerformanceCounterRegEx();

            var result = sut.split(pattern);

            result.Category.ShouldBe("Process");
            result.Counter.ShouldBe("% Processor Time");
            result.Instance.ShouldBe("Idle");
        }
    }
}