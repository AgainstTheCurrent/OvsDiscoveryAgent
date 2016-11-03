using System;
using System.Collections.Generic;
using System.Linq;

namespace OvsDiscoveryAgent
{
    public enum WqlLogicalOperator
    {
        AND,
        OR
    }
    public enum WqlValueOperator
    {
        Equal,
        NotEqual,
        ISA,
        Like
    }
    public abstract class WqlCondition
    {
        public abstract string ToStringWithOwner(string owner);
    }
    public class WqlLogicalCondition : WqlCondition
    {
        public WqlLogicalCondition(WqlCondition cond1, WqlLogicalOperator oper, WqlCondition cond2)
        {
            Condition1 = cond1;
            Condition2 = cond2;
            Operator = oper;
        }
        public WqlLogicalOperator Operator { get; set; }
        public WqlCondition Condition1 { get; set; }
        public WqlCondition Condition2 { get; set; }
        private string ConvertConditionToString(WqlCondition cond, string owner)
        {
            if (cond is WqlLogicalCondition)
            {
                return string.Format("({0})", cond.ToStringWithOwner(owner));
            }
            else
            {
                return cond.ToStringWithOwner(owner);
            }
        }
        public override string ToString()
        {
            return base.ToString();
        }
        public override string ToStringWithOwner(string owner)
        {
            return string.Format("{0} {1} {2}",
                                 ConvertConditionToString(Condition1, owner),
                                 Operator == WqlLogicalOperator.AND ? "AND" : "OR",
                                 ConvertConditionToString(Condition2, owner));
        }
    }
    public class WqlBasicCondition<T> : WqlCondition
    {
        public WqlBasicCondition() { }
        public WqlBasicCondition(string key, WqlValueOperator oper, T value)
        {
            Key = key;
            Value = value;
            Operator = oper;
        }
        public WqlValueOperator Operator { get; set; }
        public string Key { get; set; }
        public T Value { get; set; }
        public static string ToString(WqlValueOperator oper)
        {
            switch (oper)
            {
                case WqlValueOperator.Equal:
                    return "=";
                case WqlValueOperator.ISA:
                    return "ISA";
                case WqlValueOperator.Like:
                    return "LIKE";
                case WqlValueOperator.NotEqual:
                    return "<>";
                default:
                    throw new NotImplementedException("Unsupported operator: " + oper);
            }
        }

        public override string ToString()
        {
            return ToStringWithOwner(null);
        }
        public override string ToStringWithOwner(string owner)
        {
            var keys = new List<string>();
            if (!string.IsNullOrWhiteSpace(Key)) keys.Add(Key);
            if (!string.IsNullOrWhiteSpace(owner)) keys.Add(owner);
            string finalKey = string.Join(".", keys);
            if (Value is string || Value.ToString().Contains(' '))
            {
                return string.Format("{0} {1} '{2}'", finalKey, ToString(Operator), Value);
            }
            else
            {
                return string.Format("{0} {1} {2}", finalKey, ToString(Operator), Value);
            }
        }
    }
}
