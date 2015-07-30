using System;
using System.Linq.Expressions;

namespace VMTest.Utilities
{
    public static class ExpressionDebugUtils
    {
        public static Expression ShowMeBlock(Expression runThisFirst, MemberExpression member, string text = null)
        {
            return Expression.Block(new[] { runThisFirst, ShowMe(member, text) });
        }

        public static Expression ShowMe(Expression x, string text = null)
        {
            if (text != null)
                return Expression.Call(typeof(ExpressionDebugUtils).GetMethod("TempoWithString"), new Expression[] { Expression.Convert(x, typeof(object)), Expression.Constant(text) });
            return Expression.Call(typeof(ExpressionDebugUtils).GetMethod("Tempo"), new[] { Expression.Convert(x, typeof(object)) });
        }

        public static void Tempo(object o)
        {
            if (o == null)
                Console.WriteLine("Showme: <<NULL>>");
            else
                Console.WriteLine("Showme: {0} {1}", o, o.GetType());
        }

        public static void TempoWithString(object o, string text)
        {
            if (o == null)
                Console.WriteLine("{0} Showme: <<NULL>>", text);
            else
                Console.WriteLine("{0} Showme: {1} {2}", text, o, o.GetType());
        }
    }

}
