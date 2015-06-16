﻿using System;
using System.Collections.Generic;
using System.Linq;
using Serilog.Debugging;
using Serilog.Events;

namespace Serilog.Sinks.DynamoDB
{
  /// <summary>
  /// Simple property coverter for exteneded properties in Serilog
  /// </summary>
  public static class DynamoDBPropertyFormatter
  {
    static readonly HashSet<Type> ScalarTypes = new HashSet<Type>
    {
      typeof(bool), typeof(byte), typeof(short), typeof(ushort), typeof(int), typeof(uint),
      typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal), typeof(byte[])
    }; 

    /// <summary>
    /// Simplify the object so as to make handling the serialized
    /// representation easier.
    /// </summary>
    /// <param name="value">The value to simplify (possibly null).</param>
    /// <returns>A simplified representation.</returns>
    public static object Simplify(LogEventPropertyValue value)
    {
      var scalar = value as ScalarValue;
      if (scalar != null)
        return SimplifyScalar(scalar.Value);

      var dict = value as DictionaryValue;
      if (dict != null)
      {
        var result = new Dictionary<object, object>();
        foreach (var element in dict.Elements)
        {
          var key = SimplifyScalar(element.Key);
          if (result.ContainsKey(key))
          {
            SelfLog.WriteLine("The key {0} is not unique in the provided dictionary after simplification to {1}.", element.Key, key);
            return dict.Elements.Select(e => new Dictionary<string, object>
            {
              { "Key", SimplifyScalar(element.Key) },
              { "Value", Simplify(element.Value) }
            })
              .ToArray();
          }
                    
          result.Add(key, Simplify(element.Value));
        }
        return result;
      }

      var seq = value as SequenceValue;
      if (seq != null)
        return seq.Elements.Select(Simplify).ToArray();

      var str = value as StructureValue;
      if (str != null)
      {
        var props = str.Properties.ToDictionary(p => p.Name, p => Simplify(p.Value));
        if (str.TypeTag != null)
          props["$typeTag"] = str.TypeTag;
        return props;
      }

      return null;
    }
        
    static object SimplifyScalar(object value)
    {
      if (value == null) return null;

      var valueType = value.GetType();
      if (ScalarTypes.Contains(valueType)) return value;

      return value.ToString();
    }
  }
}