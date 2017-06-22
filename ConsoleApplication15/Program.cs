using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;

namespace ConsoleApplication15
{
    class Program
    {
        class CustomExpression
        {
            public string ParameterName { get; set; }
            public string ParameterValue { get; set; }
            public string Op { get; set; }
        }
        class CustomExpressionTree:Stack
        {
            
        }
        
        static void Convert(string str)
        {
            var split = new string[] { "&&", "||", "(", ")"};
            var split2 = new string[] { ">=", "<=", "=", "!=" };
            var arr = str.Split(split, StringSplitOptions.RemoveEmptyEntries).ToList().Select(x=> x.Replace(" ", "")).Where(x=>x!="");
           
            var opOrder = new List<Tuple<int, string>>();
            for(var i=0;i<split.Count();i++)
            {
                var index = 0;
                do
                {
                    index = str.IndexOf(split[i], index);
                    if (index < 0) break;
                    Tuple<int, string> tup = new Tuple<int, string>(index, split[i]);
                    index += split[i].Length;
                    opOrder.Add(tup);
                } while (index < str.Length);
            }
            opOrder = opOrder.OrderBy(x=>x.Item1).ToList();
            var result = new CustomExpressionTree();
            var index2 = 0;
            foreach(var express in arr)
            {
                var arr2 = express.Split(split2, StringSplitOptions.RemoveEmptyEntries).ToList().Select(x=>x.Replace(" ","")).ToArray();
                var sub = new CustomExpression { ParameterName= arr2[0], ParameterValue = arr2[1]};
                var op = express.Remove(express.Length - arr2[1].Length);
                op = op.Remove(0, arr2[0].Length);
                sub.Op = op;
               
                result.Push(sub);

                if (index2 < opOrder.Count)
                {
                    var oper = opOrder[index2];
                    result.Push(oper.Item2);
                }
                index2++;
            }
        }
        //"Age>=11 && Age<=12", "Name=A2", " Name=A1 || Name=A2"

        static void Main(string[] args)
        {
            Convert("Age>=11 && ( Age<=12 || Aage>=100) && (Name=A1 || Name=A2)");
            var list = new List<Test>();
            list.Add(new Test { Name = "A1", Age = 11, Score=89});
            list.Add(new Test { Name = "A4", Age = 11, Score = 96 });
            list.Add(new Test { Name = "A2", Age = 12, Score = 91 });
            list.Add(new Test { Name = "A3", Age = 11, Score = 92 });
            
            var pe = Expression.Parameter(typeof(Test), "x");
            //x.Name == A2 || (x.Score>=91 && x.Score<=100)
            var left = Expression.Property(pe,  typeof(Test).GetProperty("Name"));
            var condition = Expression.Constant("A2");
            var body = Expression.Equal(left, condition);

            var left2 = Expression.Property(pe, typeof(Test).GetProperty("Score"));
            var condition2 = Expression.Constant(95);
            var body2 = Expression.GreaterThanOrEqual(left2, condition2);
            var left3 = Expression.Property(pe, typeof(Test).GetProperty("Score"));
            var condition3 = Expression.Constant(100);
            var body3 = Expression.LessThanOrEqual(left3, condition3);
            var express = Expression.AndAlso(body2, body3);

            var la = Expression.Lambda<Func<Test, bool>>(Expression.Or(body, express), new [] { pe }).Compile();
            var res = list.Where(la);
            foreach (var item in res)
                Console.WriteLine($"{item.Name} {item.Age} {item.Score}");
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
