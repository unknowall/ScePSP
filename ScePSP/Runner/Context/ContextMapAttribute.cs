using System;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class ContextMapAttribute : Attribute
{
    public Type From { get; set; }
    public Type To { get; set; }

    public ContextMapAttribute(Type From, Type To)
    {
        this.From = From;
        this.To = To;
    }
}