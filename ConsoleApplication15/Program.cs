using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace ConsoleApplication15
{
    class CustomExpression
    {
        public string ParameterName { get; set; }
        public string ParameterValue { get; set; }
        public string Op { get; set; }
    }
    class CustomExpressionTree : Stack
    {

    }
    static class ConstBrace
    {
        public static string LeftBrace="(";
        public static string RightBrace = ")";
    }
    static class ConstJunction
    {
        public const string And = "&&";
        public const string Or = "||";

        public enum Flag
        {
            And = 12,
            Or = 11,
            NULL = -9999
        }
    }
    static class ConstOper
    {
        public const string GreaterEqual = ">=";
        public const string LessEqual = "<=";
        public const string Equal = "==";
        public const string NoEqual = "!=";
    }

    class Program
    {
        static string[] junctionSplit = new string[] { ConstJunction.And, ConstJunction.Or };
        static string[] OperSplit = new string[] { ConstOper.GreaterEqual, ConstOper.LessEqual, ConstOper.Equal, ConstOper.NoEqual};


        static Func<TObj, bool> Convert<TObj>(string whereStr) where TObj:class
        {
            var result = new CustomExpressionTree();

            result = ProcessWhereStr(whereStr);
            var result2 = SwapExp(result);

            var pe = Expression.Parameter(typeof(TObj), "x");
            var bianryExprestt = Lambda<TObj>(result2, pe).First();
            return Expression.Lambda<Func<TObj, bool>>(bianryExprestt, new[] { pe }).Compile();
        }
        //StartWith Contains ...
        static IList<BinaryExpression> Lambda<TObj>(CustomExpressionTree expressTree, ParameterExpression pe) where TObj:class
        {
            var flagArr = new List<int>();
            var expressionList = new List<BinaryExpression>();
            while (expressTree.Count > 0)
            {
                var stackContent = expressTree.Pop();
                if(stackContent is string)
                {
                    ProcessNonExp(flagArr, expressionList, stackContent.ToString());
                    continue;
                }
               
                if(stackContent is CustomExpression)
                {
                    var currExpress = (stackContent as CustomExpression);
                    ProcessExp<TObj>(pe, currExpress, flagArr, expressionList);
                }
            }
            while (flagArr.Count > 0)
            {
                var lastFlag = flagArr[flagArr.Count - 1];
                ConstructConjExp(lastFlag, expressionList);
                flagArr.RemoveAt(flagArr.Count - 1);
            }
            return expressionList;
        }

        static CustomExpressionTree ProcessWhereStr(string whereStr)
        {
            var result = new CustomExpressionTree();
            var opOrder = new List<Tuple<int, string>>();
            var index2 = 0;
            
            GetJunctionSort(whereStr, opOrder);
            var singleExps = whereStr.Split(junctionSplit, StringSplitOptions.RemoveEmptyEntries).ToList().Select(x => x.Replace(" ", "")).Where(x => x != "");

            foreach (var expression in singleExps)
            {
                var singleExp = expression.Split(OperSplit, StringSplitOptions.RemoveEmptyEntries).ToList().Select(x => x.Replace(" ", "")).ToArray();
                var subExp = new CustomExpression { ParameterName = singleExp[0], ParameterValue = singleExp[1] };
                var op = expression.Remove(expression.Length - singleExp[1].Length); //tail
                op = op.Remove(0, singleExp[0].Length); // header
                subExp.Op = op; //operation in middle

                ProcessSubExp(subExp, result);
                if (index2 < opOrder.Count)
                {
                    var oper = opOrder[index2];
                    var item2 = oper.Item2;
                    result.Push(item2);
                }
                index2++;
            }
            return result;
        }

        static void ProcessExp<TObj>(Expression pe, CustomExpression currExpress, IList<int> flagArr, IList<BinaryExpression> expressionList)
        {
            var lastFlag = (int)ConstJunction.Flag.NULL;
            if (flagArr.Count-1>=0)
                lastFlag = flagArr[flagArr.Count - 1];

            var subLeft = Expression.Property(pe, typeof(TObj).GetProperty(currExpress.ParameterName));
            var typ = typeof(TObj).GetProperty(currExpress.ParameterName).PropertyType;

            var condition = Expression.Constant(System.Convert.ChangeType(currExpress.ParameterValue, typ), typ);
            ConstructOperExp(currExpress, expressionList, subLeft, condition);
           
            if ( lastFlag!= (int)ConstJunction.Flag.NULL && lastFlag != (int)ConstJunction.Flag.Or && lastFlag != (int)ConstJunction.Flag.And)
                flagArr.RemoveAt(flagArr.Count - 1);
        }
        static void ProcessSubExp(CustomExpression subExp, CustomExpressionTree result)
        {
            var index = 0;
            do
            {
                index = subExp.ParameterName.IndexOf(ConstBrace.LeftBrace);
                if (index < 0) break;
                result.Push(ConstBrace.LeftBrace);
                subExp.ParameterName = subExp.ParameterName.Remove(index, 1);
            } while (index >= 0);

            var flagEnd = false;
            var cntRightBrace = 0;
            index = 0;
            do
            {
                index = subExp.ParameterValue.IndexOf(ConstBrace.RightBrace);
                if (index < 0) break;
                cntRightBrace++;
                subExp.ParameterValue = subExp.ParameterValue.Remove(index, 1);
                flagEnd = true;
            } while (index >= 0);

            result.Push(subExp);

            if (flagEnd)
                while (cntRightBrace-- > 0)
                    result.Push(ConstBrace.RightBrace);
        }
        static void ProcessNonExp(IList<int> flagArr, IList<BinaryExpression> expressionList, string stackContent)
        {
            if (ConstBrace.LeftBrace.Equals(stackContent))
                flagArr.Add(1);
            else if (ConstBrace.RightBrace.Equals(stackContent.ToString()))
            {
                var lastFlag = flagArr[flagArr.Count - 1];
                flagArr.RemoveAt(flagArr.Count - 1);
                ConstructConjExp(lastFlag, expressionList);
            }
            else if (junctionSplit.Contains(stackContent.ToString()))
            {
                switch (stackContent.ToString())
                {
                    case ConstJunction.Or:
                        flagArr.Add((int)ConstJunction.Flag.Or);
                        break;
                    case ConstJunction.And:
                        flagArr.Add((int)ConstJunction.Flag.And);
                        break;
                }
            }
        }

        static void ConstructOperExp(CustomExpression currExpress, IList<BinaryExpression> expressionList, MemberExpression subLeft, ConstantExpression condition)
        {
            switch (currExpress.Op)
            {
                case ConstOper.GreaterEqual:
                    expressionList.Add(Expression.GreaterThanOrEqual(subLeft, condition));
                    break;
                case ConstOper.LessEqual:
                    expressionList.Add(Expression.LessThanOrEqual(subLeft, condition));
                    break;
                case ConstOper.Equal:
                    expressionList.Add(Expression.Equal(subLeft, condition));
                    break;
                case ConstOper.NoEqual:
                    expressionList.Add(Expression.NotEqual(subLeft, condition));
                    break;
            }
        }
        static void ConstructConjExp(int lastFlag, IList<BinaryExpression> expressionList)
        {
            if (lastFlag == (int)ConstJunction.Flag.Or)
            {
                var left = expressionList[expressionList.Count - 1];
                expressionList.RemoveAt(expressionList.Count - 1);
                var right = expressionList[expressionList.Count - 1];
                expressionList.RemoveAt(expressionList.Count - 1);
                expressionList.Add(Expression.Or(left, right));
            }
            else if (lastFlag == (int)ConstJunction.Flag.And)
            {
                var left = expressionList[expressionList.Count - 1];
                expressionList.RemoveAt(expressionList.Count - 1);
                var right = expressionList[expressionList.Count - 1];
                expressionList.RemoveAt(expressionList.Count - 1);
                expressionList.Add(Expression.And(left, right));
            }
        }

        static void GetJunctionSort(string whereStr, List<Tuple<int, string>> opOrder)
        {
            for (var i = 0; i < junctionSplit.Count(); i++)
            {
                var index = 0;
                do
                {
                    index = whereStr.IndexOf(junctionSplit[i], index);
                    if (index < 0) break;
                    Tuple<int, string> tup = new Tuple<int, string>(index, junctionSplit[i]);
                    index += junctionSplit[i].Length;
                    opOrder.Add(tup);
                } while (index < whereStr.Length);
            }
            opOrder = opOrder.OrderBy(x => x.Item1).ToList();
        }
        static CustomExpressionTree SwapExp(CustomExpressionTree result)
        {
            var result2 = new CustomExpressionTree();
            while (result.Count > 0)
            {
                result2.Push(result.Pop());
            }
            return result2;
        }
        //end region
        static void Ex()
        {
            ParameterExpression param = Expression.Parameter(typeof(int));
            MethodCallExpression methodCall = Expression.Call(typeof(Console).GetMethod("WriteLine", new Type[] { typeof(int) }), param);

            Expression.Lambda<Action<int>>(methodCall, new ParameterExpression[] { param }).Compile()(100);

        }
        static void Main(string[] args)
        {
            Ex();
            Console.WriteLine("===");

            var la = Convert<Test>("( Age<=11 || Age>=12) && (Name==A1 || Name==A2)");
            Console.WriteLine("===");
            Console.Read();
            var list = new List<Test>();
            list.Add(new Test { Name = "A1", Age = 11, Score=89});
            list.Add(new Test { Name = "A4", Age = 11, Score = 96 });
            list.Add(new Test { Name = "A2", Age = 12, Score = 91 });
            list.Add(new Test { Name = "A3", Age = 11, Score = 92 });
            //foreach (var item in list.Where(x => x.Name == "A3" && ((x.Age <= 11 || x.Age >= 100) || (x.Name == "A1" || x.Name == "A2"))))
            //    Console.WriteLine(item.Name);
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

            //la = Expression.Lambda<Func<Test, bool>>(Expression.Or(body, express), new [] { pe }).Compile();
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
