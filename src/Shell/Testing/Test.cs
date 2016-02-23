using System;
using System.Collections.Generic;

namespace Shell.Testing
{
    public class Test
    {
        public static string Now()
        {
            return DateTime.Now.ToLongDateString();
        }

        static Random random = new Random();
        public static int Random()
        {
            return random.Next(100);
        }

        public static List<int> RandomNumbers(int count)
        {
            List<int> nums = new List<int>();
            for (var i = 0; i < count; i++)
                nums.Add(Random());
            return nums;
        }

        public static List<int> Numbers()
        {
            return new List<int> { 1, 2, 3, 4, 5 };
        }

        public static TestSimpleObject Person()
        {
            return new TestSimpleObject
            {
                Name = "Kerry",
                Age = "43",
                Numbers = Numbers(),
                Person = new TestSimpleObject { Name = "Long John Silver" }
            };
        }
    }

    public class TestSimpleObject
    {
        public string Name { get; set; }
        public string Age { get; set; }
        public List<int> Numbers { get; set; }
        public TestSimpleObject Person { get; set; }
    }
}