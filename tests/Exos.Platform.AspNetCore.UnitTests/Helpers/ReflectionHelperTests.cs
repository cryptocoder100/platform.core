#pragma warning disable CA1812
using System;
using System.Diagnostics.CodeAnalysis;
using Exos.Platform.AspNetCore.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Exos.Platform.AspNetCore.UnitTests.Helpers
{
    [TestClass]
    public class ReflectionHelperTests
    {
        [TestMethod]
        public void Map_ShouldSucceed()
        {
            var n = ReflectionHelper.Map<Obj1, Obj2>(null);
            n.ShouldBeNull();

            var obj1 = new Obj1();
            var obj2 = ReflectionHelper.Map<Obj1, Obj2>(obj1, false);

            obj2.Property1.ShouldBe("Hello");
            obj2.Property2.ShouldBe(123);
            obj2.Property3.ShouldBe(obj1.Property3);
            obj2.Property4.ShouldBe("Pass");
            obj2.Property6.ShouldBe("Pass");
            obj2.Property7.ShouldBe("Pass");
        }

        [TestMethod]
        public void MapWithReflection_ShouldSucceed()
        {
            var obj1 = new Obj1();
            var obj2 = ReflectionHelper.MapWithReflection<Obj1, Obj2>(obj1, false);

            obj2.Property1.ShouldBe("Hello");
            obj2.Property2.ShouldBe(123);
            obj2.Property3.ShouldBe(obj1.Property3);
            obj2.Property4.ShouldBe("Pass");
            obj2.Property6.ShouldBe("Pass");
            obj2.Property7.ShouldBe("Pass");
        }

        [TestMethod]
        public void MapWithILEmit_ShouldSucceed()
        {
            var obj1 = new Obj1();
            var obj2 = ReflectionHelper.MapWithILEmit<Obj1, Obj2>(obj1, false);

            obj2.Property1.ShouldBe("Hello");
            obj2.Property2.ShouldBe(123);
            obj2.Property3.ShouldBe(obj1.Property3);
            obj2.Property4.ShouldBe("Pass");
            obj2.Property6.ShouldBe("Pass");
            obj2.Property7.ShouldBe("Pass");
        }

        private class Obj1
        {
            public string Property1 { get; set; } = "Hello";

            public int Property2 { get; set; } = 123;

            public Guid Property3 { get; set; } = Guid.NewGuid();

            public string PROPERTY4 { get; set; } = "Pass"; // Should map if case-insensitive (default)

            public string Property5 { get; set; } = "Fail"; // Should not map because not in target

            private string Property6 { get; set; } = "Fail"; // Should not map because source is private
        }

        private class Obj2
        {
            public string Property1 { get; set; }

            public int Property2 { get; set; }

            public Guid Property3 { get; set; }

            public string Property4 { get; set; }

            public string Property6 { get; set; } = "Pass"; // Should not map because source is private

            public string Property7 { get; set; } = "Pass"; // Should not map because not in source
        }
    }
}
#pragma warning restore CA1812