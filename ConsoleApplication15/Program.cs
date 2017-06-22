using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace ConsoleApplication15
{
    class Program
    {
        static void Main(string[] args)
        {
            var list = new List<Test>();
            list.Add(new Test { Name = "A1", Age = 11, Score=90});
            list.Add(new Test { Name = "A2", Age = 12, Score = 91 });
            list.Add(new Test { Name = "A3", Age = 11, Score = 92 });
           
            var pe = Expression.Parameter(typeof(Test), "x");
            
            var left = Expression.Property(pe,  typeof(Test).GetProperty("Name"));
            var condition = Expression.Constant("A2");
            var body = Expression.Equal(left, condition);
            var left2 = Expression.Property(pe, typeof(Test).GetProperty("Score"));
            var condition2 = Expression.Constant(90);
            var body2 = Expression.Equal(left2, condition2);

            var la = Expression.Lambda<Func<Test, bool>>(Expression.Or(body, body2), new [] { pe }).Compile();
            var res = list.Where(la);
          
        }
        static IList<TResult> GetWhatWant<TResult, TQuery, TDataSet>(TQuery query, IList<TDataSet> dataSet)
        {
            var res = new List<TResult>();
            var queryType = query.GetType();
            var properties = queryType.GetProperties();

            return res;
        }
    }

    public class Test
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public int Score { get; set; }
    }
    public class Query
    {
        [AB("Name=")]
        public string Name { get; set; }
    }
    public class AB : Attribute
    {
        private string _Name = string.Empty;
        private string _Op = string.Empty;

        public AB(string name)
        {
            _Name = name;
            _Op = "=";
        }

    }
}
